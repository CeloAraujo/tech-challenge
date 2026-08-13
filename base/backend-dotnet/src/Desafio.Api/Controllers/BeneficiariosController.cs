using Desafio.Api.Api.Contratos;
using Desafio.Api.Aplicacao;
using Desafio.Api.Aplicacao.Contratos;
using Desafio.Api.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.Api.Controllers;

[ApiController]
[Route("beneficiarios")]
[Produces("application/json")]
public class BeneficiariosController(BeneficiarioServico servico) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaResponse<BeneficiarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanho = 10,
        [FromQuery] StatusBeneficiario? status = null, [FromQuery(Name = "plano_id")] Guid? planoId = null,
        CancellationToken cancellationToken = default)
    {
        var (dados, total) = await servico.ListarAsync(pagina, tamanho, status, planoId, cancellationToken);
        return Ok(new PaginaResponse<BeneficiarioResponse>(dados.Select(BeneficiarioResponse.De).ToList(), pagina, tamanho, total));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(BeneficiarioResponse.De(await servico.ObterAsync(id, ct)));

    [HttpPost]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] BeneficiarioCriacaoRequest r, CancellationToken ct)
    {
        var b = await servico.CriarAsync(r, ct);
        return CreatedAtAction(nameof(Obter), new { id = b.Id }, BeneficiarioResponse.De(b));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] BeneficiarioAtualizacaoRequest r, CancellationToken ct)
    {
        var b = await servico.AtualizarAsync(id, r, ct);
        return Ok(BeneficiarioResponse.De(b));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await servico.ExcluirAsync(id, ct);
        return NoContent();
    }
}
