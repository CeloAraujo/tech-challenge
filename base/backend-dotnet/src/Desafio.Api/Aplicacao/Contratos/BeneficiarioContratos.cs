using Desafio.Api.Dominio;

namespace Desafio.Api.Aplicacao.Contratos;

public sealed record BeneficiarioCriacaoRequest(
    string? NomeCompleto,
    string? Cpf,
    DateOnly DataNascimento,
    Guid PlanoId);

public sealed record BeneficiarioAtualizacaoRequest(
    string? NomeCompleto,
    DateOnly DataNascimento,
    Guid PlanoId,
    StatusBeneficiario Status);
