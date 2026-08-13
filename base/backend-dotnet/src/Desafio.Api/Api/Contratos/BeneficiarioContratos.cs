using Desafio.Api.Dominio;

namespace Desafio.Api.Api.Contratos;

public sealed record BeneficiarioResponse(Guid Id, string NomeCompleto, string Cpf, DateOnly DataNascimento,
    StatusBeneficiario Status, Guid PlanoId, DateTime DataCadastro)
{
    public static BeneficiarioResponse De(Beneficiario b) =>
        new(b.Id, b.NomeCompleto, b.Cpf, b.DataNascimento, b.Status, b.PlanoId, b.DataCadastro);
}

public sealed record PaginaResponse<T>(IReadOnlyList<T> Dados, int Pagina, int Tamanho, int Total);
