using System.Text.Json;
using Desafio.Api.Api.Middlewares;
using Desafio.Api.Dominio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace Desafio.Api.Tests;

public class TratamentoDeErroMiddlewareTests
{
    [Fact]
    public async Task Excecao_de_dominio_deve_preservar_status_e_detalhes_no_contrato_padrao()
    {
        var excecao = new NaoProcessavelException(
            "Plano informado não pode ser utilizado",
            [new DetalheErro("plano_id", "inexistente")]);
        var contexto = NovoContexto();
        var middleware = NovoMiddleware(_ => throw excecao);

        await middleware.InvokeAsync(contexto);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, contexto.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", contexto.Response.ContentType);

        var corpo = await LerCorpoAsync(contexto);
        Assert.Equal("NaoProcessavel", corpo.GetProperty("erro").GetString());
        Assert.Equal(excecao.Message, corpo.GetProperty("mensagem").GetString());

        var detalhe = Assert.Single(corpo.GetProperty("detalhes").EnumerateArray());
        Assert.Equal("plano_id", detalhe.GetProperty("campo").GetString());
        Assert.Equal("inexistente", detalhe.GetProperty("regra").GetString());
    }

    [Fact]
    public async Task Excecao_inesperada_deve_retornar_500_sem_expor_informacao_interna()
    {
        const string segredoInterno = "senha-do-banco=nao-expor";
        var contexto = NovoContexto();
        var middleware = NovoMiddleware(_ => throw new InvalidOperationException(segredoInterno));

        await middleware.InvokeAsync(contexto);

        Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", contexto.Response.ContentType);

        var json = await LerTextoAsync(contexto);
        Assert.DoesNotContain(segredoInterno, json);

        using var corpo = JsonDocument.Parse(json);
        Assert.Equal("ErroInterno", corpo.RootElement.GetProperty("erro").GetString());
        Assert.Equal("Erro interno ao processar a requisição", corpo.RootElement.GetProperty("mensagem").GetString());
        Assert.Empty(corpo.RootElement.GetProperty("detalhes").EnumerateArray());
    }

    [Fact]
    public async Task Requisicao_sem_erro_deve_preservar_resposta_do_proximo_componente()
    {
        var contexto = NovoContexto();
        var middleware = NovoMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status202Accepted;
            await ctx.Response.WriteAsync("processado");
        });

        await middleware.InvokeAsync(contexto);

        Assert.Equal(StatusCodes.Status202Accepted, contexto.Response.StatusCode);
        Assert.Equal("processado", await LerTextoAsync(contexto));
    }

    [Fact]
    public async Task Excecao_apos_inicio_da_resposta_nao_deve_tentar_substituir_o_conteudo()
    {
        var recurso = new RespostaJaIniciada
        {
            StatusCode = StatusCodes.Status200OK,
            Body = new MemoryStream()
        };
        await recurso.Body.WriteAsync("parcial"u8.ToArray());
        var contexto = new DefaultHttpContext();
        contexto.Features.Set<IHttpResponseFeature>(recurso);
        var middleware = NovoMiddleware(_ => throw new InvalidOperationException("falha tardia"));

        await middleware.InvokeAsync(contexto);

        Assert.Equal(StatusCodes.Status200OK, contexto.Response.StatusCode);
        recurso.Body.Position = 0;
        using var leitor = new StreamReader(recurso.Body);
        Assert.Equal("parcial", await leitor.ReadToEndAsync());
    }

    private static TratamentoDeErroMiddleware NovoMiddleware(RequestDelegate proximo) =>
        new(proximo, NullLogger<TratamentoDeErroMiddleware>.Instance);

    private static DefaultHttpContext NovoContexto()
    {
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();
        return contexto;
    }

    private static async Task<JsonElement> LerCorpoAsync(DefaultHttpContext contexto)
    {
        using var documento = JsonDocument.Parse(await LerTextoAsync(contexto));
        return documento.RootElement.Clone();
    }

    private static async Task<string> LerTextoAsync(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        using var leitor = new StreamReader(contexto.Response.Body, leaveOpen: true);
        return await leitor.ReadToEndAsync();
    }

    private sealed class RespostaJaIniciada : IHttpResponseFeature
    {
        public int StatusCode { get; set; }
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
