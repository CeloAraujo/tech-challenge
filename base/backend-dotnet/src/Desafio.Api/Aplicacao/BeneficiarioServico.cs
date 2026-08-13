using Desafio.Api.Aplicacao.Contratos;
using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Desafio.Api.Aplicacao;

public class BeneficiarioServico(AppDbContext db)
{
    private const string CodigoViolacaoDeUnicidade = "23505";

    public async Task<(IReadOnlyList<Beneficiario> Dados, int Total)> ListarAsync(
        int pagina, int tamanho, StatusBeneficiario? status, Guid? planoId, CancellationToken cancellationToken)
    {
        if (pagina < 1) throw new ValidacaoException("Paginação inválida", [new("pagina", "minimo_1")]);
        if (tamanho is < 1 or > 100) throw new ValidacaoException("Paginação inválida", [new("tamanho", "entre_1_e_100")]);
        if (status.HasValue && !Enum.IsDefined(status.Value))
            throw new ValidacaoException("Filtro inválido", [new("status", "invalido")]);

        var consulta = db.Beneficiarios.AsNoTracking();
        if (status.HasValue) consulta = consulta.Where(b => b.Status == status.Value);
        if (planoId.HasValue) consulta = consulta.Where(b => b.PlanoId == planoId.Value);

        var total = await consulta.CountAsync(cancellationToken);
        var dados = await consulta.OrderBy(b => b.DataCadastro).ThenBy(b => b.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync(cancellationToken);
        return (dados, total);
    }

    public async Task<Beneficiario> ObterAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Beneficiarios.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
        ?? throw new NaoEncontradoException("Beneficiário não encontrado");

    public async Task<Beneficiario> CriarAsync(BeneficiarioCriacaoRequest request, CancellationToken ct)
    {
        await GarantirPlanoAsync(request.PlanoId, ct);
        var beneficiario = new Beneficiario(
            request.NomeCompleto,
            request.Cpf,
            request.DataNascimento,
            request.PlanoId);
        if (await db.Beneficiarios.IgnoreQueryFilters().AnyAsync(b => b.Cpf == beneficiario.Cpf, ct)) throw CpfDuplicado();
        db.Beneficiarios.Add(beneficiario);
        await SalvarAsync(ct);
        return beneficiario;
    }

    public async Task<Beneficiario> AtualizarAsync(Guid id, BeneficiarioAtualizacaoRequest request, CancellationToken ct)
    {
        var beneficiario = await ObterAsync(id, ct);
        if (!Enum.IsDefined(request.Status))
            throw new ValidacaoException("Dados do beneficiário inválidos", [new("status", "invalido")]);

        var mantemDadosDeInativo = beneficiario.Status == StatusBeneficiario.INATIVO
            && string.Equals(request.NomeCompleto?.Trim(), beneficiario.NomeCompleto, StringComparison.Ordinal)
            && request.DataNascimento == beneficiario.DataNascimento
            && request.PlanoId == beneficiario.PlanoId;

        // Reativação não cria vínculo novo: o plano histórico pode ter sido excluído depois do cadastro.
        if (!mantemDadosDeInativo)
            await GarantirPlanoAsync(request.PlanoId, ct);

        beneficiario.Atualizar(
            request.NomeCompleto,
            request.DataNascimento,
            request.PlanoId,
            request.Status);
        await db.SaveChangesAsync(ct);
        return beneficiario;
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        var beneficiario = await ObterAsync(id, ct);
        beneficiario.Excluir();
        await db.SaveChangesAsync(ct);
    }

    private async Task GarantirPlanoAsync(Guid planoId, CancellationToken ct)
    {
        if (planoId == Guid.Empty)
            throw new ValidacaoException("Dados do beneficiário inválidos", [new("plano_id", "obrigatorio")]);
        if (!await db.Planos.AsNoTracking().AnyAsync(p => p.Id == planoId, ct))
            throw new NaoProcessavelException("Plano não encontrado", [new("plano_id", "inexistente")]);
    }

    private async Task SalvarAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: CodigoViolacaoDeUnicidade })
        { throw CpfDuplicado(); }
    }

    private static ConflitoException CpfDuplicado() => new("Já existe beneficiário com esse CPF", [new("cpf", "duplicado")]);
}
