using System.Net;
namespace Desafio.Api.Tests;

[Collection(ColecaoDaApi.Nome)]
public class BeneficiariosTests(ApiFixture fixture) : IAsyncLifetime
{
    private HttpClient Client => fixture.Client;

    public Task InitializeAsync() => fixture.LimparAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static object CorpoDeCriacao(string cpf, Guid? planoId = null) => new
    {
        NomeCompleto = "Maria Aparecida da Silva",
        Cpf = cpf,
        DataNascimento = "1990-05-12",
        PlanoId = planoId ?? Planos.Bronze
    };

    // ------------------------------------------------------------------ criação

    [Fact]
    public async Task Criar_deve_devolver_201_com_header_location()
    {
        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("52998224725")));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);

        var corpo = await resposta.CorpoAsync();
        Assert.NotEqual(Guid.Empty, corpo.GetProperty("id").GetGuid());
        Assert.Equal("52998224725", corpo.GetProperty("cpf").GetString());
        Assert.Equal("ATIVO", corpo.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Criar_com_cpf_ja_cadastrado_deve_devolver_409()
    {
        await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("71428793860")));

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("71428793860")));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_com_plano_inexistente_deve_devolver_422()
    {
        var resposta = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(CorpoDeCriacao("39053344705", Planos.Inexistente)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_deve_validar_corpo_antes_de_consultar_plano()
    {
        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(new
        {
            NomeCompleto = "",
            Cpf = "39053344705",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Inexistente
        }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains((await resposta.CorpoAsync()).GetProperty("detalhes").EnumerateArray(), detalhe =>
            detalhe.GetProperty("campo").GetString() == "nome_completo");
    }

    // ------------------------------------------------------------------ consulta por id

    [Fact]
    public async Task Obter_deve_devolver_o_beneficiario()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.GetAsync($"/beneficiarios/{beneficiario.Id}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal(beneficiario.Id, corpo.GetProperty("id").GetGuid());
        Assert.Equal(beneficiario.Cpf, corpo.GetProperty("cpf").GetString());
        Assert.Equal(Planos.Bronze, corpo.GetProperty("plano_id").GetGuid());
    }

    [Fact]
    public async Task Obter_inexistente_deve_devolver_404()
    {
        var resposta = await Client.GetAsync($"/beneficiarios/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ atualização

    [Fact]
    public async Task Atualizar_deve_alterar_os_dados_do_beneficiario()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Joana Ribeiro Nunes",
            DataNascimento = "1985-03-20",
            PlanoId = Planos.Ouro,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal("Joana Ribeiro Nunes", corpo.GetProperty("nome_completo").GetString());
        Assert.Equal(Planos.Ouro, corpo.GetProperty("plano_id").GetGuid());
    }

    [Fact]
    public async Task Atualizar_inexistente_deve_devolver_404()
    {
        var resposta = await Client.PutAsync($"/beneficiarios/{Guid.NewGuid()}", Http.Json(new
        {
            NomeCompleto = "Nao Existe",
            DataNascimento = "1985-03-20",
            PlanoId = Planos.Ouro,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_apontando_para_plano_inexistente_deve_devolver_422()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Maria Aparecida da Silva",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Inexistente,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_deve_validar_corpo_antes_de_consultar_plano()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Inexistente,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains((await resposta.CorpoAsync()).GetProperty("detalhes").EnumerateArray(), detalhe =>
            detalhe.GetProperty("campo").GetString() == "nome_completo");
    }

    [Fact]
    public async Task Atualizar_sem_status_deve_devolver_400_com_regra_obrigatorio()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Maria Aparecida da Silva",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Bronze
        }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var detalhe = Assert.Single((await resposta.CorpoAsync()).GetProperty("detalhes").EnumerateArray());
        Assert.Equal("status", detalhe.GetProperty("campo").GetString());
        Assert.Equal("obrigatorio", detalhe.GetProperty("regra").GetString());
    }

    [Fact]
    public async Task Plano_excluido_deve_ser_recusado_na_criacao_e_na_atualizacao()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1, Planos.Prata)).Single();
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/planos/{Planos.Bronze}")).StatusCode);

        var criacao = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(CorpoDeCriacao("39053344705", Planos.Bronze)));
        var atualizacao = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            beneficiario.NomeCompleto,
            DataNascimento = beneficiario.DataNascimento.ToString("yyyy-MM-dd"),
            PlanoId = Planos.Bronze,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, criacao.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, atualizacao.StatusCode);
    }

    // ------------------------------------------------------------------ exclusão

    [Fact]
    public async Task Excluir_deve_ser_logico_e_tirar_o_beneficiario_das_consultas()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var exclusao = await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);

        var consulta = await Client.GetAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);

        var listagem = await (await Client.GetAsync("/beneficiarios?pagina=1&tamanho=50")).CorpoAsync();
        Assert.Equal(0, listagem.GetProperty("total").GetInt32());

        var novaExclusao = await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NotFound, novaExclusao.StatusCode);
    }

    [Fact]
    public async Task Cpf_de_beneficiario_excluido_deve_continuar_ocupado()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao(beneficiario.Cpf)));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ listagem

    [Fact]
    public async Task Listar_deve_devolver_envelope_paginado()
    {
        await fixture.SemearBeneficiariosAsync(3);

        var resposta = await Client.GetAsync("/beneficiarios?pagina=1&tamanho=10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal(3, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(1, corpo.GetProperty("pagina").GetInt32());
        Assert.Equal(10, corpo.GetProperty("tamanho").GetInt32());
        Assert.Equal(3, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listar_deve_respeitar_pagina_e_tamanho()
    {
        await fixture.SemearBeneficiariosAsync(25);

        var corpo = await (await Client.GetAsync("/beneficiarios?pagina=3&tamanho=10")).CorpoAsync();

        Assert.Equal(5, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(3, corpo.GetProperty("pagina").GetInt32());
        Assert.Equal(25, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listar_pagina_alem_do_total_deve_devolver_lista_vazia()
    {
        await fixture.SemearBeneficiariosAsync(3);

        var resposta = await Client.GetAsync("/beneficiarios?pagina=2&tamanho=10");
        var corpo = await resposta.CorpoAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(0, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(3, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listar_deve_combinar_os_filtros_de_status_e_plano()
    {
        await fixture.SemearBeneficiariosAsync(4, Planos.Bronze, "ATIVO", 100);
        await fixture.SemearBeneficiariosAsync(6, Planos.Bronze, "INATIVO", 200);
        await fixture.SemearBeneficiariosAsync(3, Planos.Prata, "ATIVO", 300);

        var corpo = await (await Client.GetAsync(
            $"/beneficiarios?tamanho=50&status=ATIVO&plano_id={Planos.Bronze}")).CorpoAsync();

        Assert.Equal(4, corpo.GetProperty("total").GetInt32());
        Assert.All(
            corpo.GetProperty("dados").EnumerateArray(),
            beneficiario =>
            {
                Assert.Equal("ATIVO", beneficiario.GetProperty("status").GetString());
                Assert.Equal(Planos.Bronze, beneficiario.GetProperty("plano_id").GetGuid());
            });
    }

    [Fact]
    public async Task Listar_deve_aplicar_filtros_de_status_e_plano_isoladamente()
    {
        await fixture.SemearBeneficiariosAsync(2, Planos.Bronze, "ATIVO", 400);
        await fixture.SemearBeneficiariosAsync(3, Planos.Prata, "INATIVO", 500);

        var porStatus = await (await Client.GetAsync("/beneficiarios?tamanho=50&status=INATIVO")).CorpoAsync();
        var porPlano = await (await Client.GetAsync($"/beneficiarios?tamanho=50&plano_id={Planos.Bronze}")).CorpoAsync();

        Assert.Equal(3, porStatus.GetProperty("total").GetInt32());
        Assert.All(porStatus.GetProperty("dados").EnumerateArray(), item =>
            Assert.Equal("INATIVO", item.GetProperty("status").GetString()));
        Assert.Equal(2, porPlano.GetProperty("total").GetInt32());
        Assert.All(porPlano.GetProperty("dados").EnumerateArray(), item =>
            Assert.Equal(Planos.Bronze, item.GetProperty("plano_id").GetGuid()));
    }

    [Fact]
    public async Task Listar_sem_informar_tamanho_deve_devolver_10_itens_por_pagina()
    {
        await fixture.SemearBeneficiariosAsync(25);

        var corpo = await (await Client.GetAsync("/beneficiarios")).CorpoAsync();

        Assert.Equal(10, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(10, corpo.GetProperty("tamanho").GetInt32());
        Assert.Equal(25, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Atualizar_dados_de_beneficiario_inativo_deve_devolver_409()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(
            1, Planos.Bronze, "INATIVO", 500)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Nome Corrigido do Inativo",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Bronze,
            Status = "INATIVO"
        }));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData("", "obrigatorio")]
    [InlineData("5299822472", "deve_conter_11_digitos")]
    [InlineData("529.982.247", "somente_digitos")]
    [InlineData("00000000000", "digitos_repetidos")]
    [InlineData("12345678901", "digitos_verificadores_invalidos")]
    public async Task Criar_com_cpf_invalido_deve_detalhar_a_regra(string cpf, string regraEsperada)
    {
        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao(cpf)));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.CorpoAsync();
        var detalhe = Assert.Single(corpo.GetProperty("detalhes").EnumerateArray());
        Assert.Equal("cpf", detalhe.GetProperty("campo").GetString());
        Assert.Equal(regraEsperada, detalhe.GetProperty("regra").GetString());
    }

    [Fact]
    public async Task Criar_deve_ignorar_campos_controlados_pelo_servidor()
    {
        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(new
        {
            NomeCompleto = "Maria Aparecida da Silva",
            Cpf = "52998224725",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Bronze,
            Id = Guid.NewGuid(),
            Status = "INATIVO",
            DataCadastro = "2000-01-01T00:00:00Z"
        }));
        var corpo = await resposta.CorpoAsync();
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("ATIVO", corpo.GetProperty("status").GetString());
        Assert.True(corpo.GetProperty("data_cadastro").GetDateTime() > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Listar_com_paginacao_invalida_deve_devolver_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/beneficiarios?pagina=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/beneficiarios?tamanho=101")).StatusCode);
    }

    [Theory]
    [InlineData("/beneficiarios?pagina=abc")]
    [InlineData("/beneficiarios?tamanho=abc")]
    public async Task Listar_com_paginacao_nao_numerica_deve_devolver_400(string url)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Atualizar_status_de_beneficiario_inativo_deve_reativar()
    {
        var b = (await fixture.SemearBeneficiariosAsync(1, Planos.Bronze, "INATIVO", 700)).Single();
        var resposta = await Client.PutAsync($"/beneficiarios/{b.Id}", Http.Json(new
        {
            b.NomeCompleto,
            DataNascimento = b.DataNascimento.ToString("yyyy-MM-dd"),
            b.PlanoId,
            Status = "ATIVO"
        }));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("ATIVO", (await resposta.CorpoAsync()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Reativar_beneficiario_deve_manter_vinculo_com_plano_excluido()
    {
        var b = (await fixture.SemearBeneficiariosAsync(1, Planos.Bronze, "INATIVO", 750)).Single();
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/planos/{Planos.Bronze}")).StatusCode);

        var resposta = await Client.PutAsync($"/beneficiarios/{b.Id}", Http.Json(new
        {
            b.NomeCompleto,
            DataNascimento = b.DataNascimento.ToString("yyyy-MM-dd"),
            b.PlanoId,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("ATIVO", (await resposta.CorpoAsync()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Criacoes_concorrentes_com_mesmo_cpf_devem_criar_apenas_um_registro()
    {
        var requisicoes = Enumerable.Range(0, 2)
            .Select(_ => Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("39053344705"))))
            .ToArray();
        var respostas = await Task.WhenAll(requisicoes);
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Conflict);
    }
}
