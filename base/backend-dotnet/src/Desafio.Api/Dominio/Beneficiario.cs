namespace Desafio.Api.Dominio;

public enum StatusBeneficiario { ATIVO, INATIVO }

public class Beneficiario
{
    private Beneficiario() { }

    public Beneficiario(string? nomeCompleto, string? cpf, DateOnly dataNascimento, Guid planoId)
    {
        Id = Guid.NewGuid();
        Cpf = ValidarCpf(cpf);
        DefinirDados(nomeCompleto, dataNascimento, planoId);
        Status = StatusBeneficiario.ATIVO;
        DataCadastro = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string NomeCompleto { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public DateOnly DataNascimento { get; private set; }
    public StatusBeneficiario Status { get; private set; }
    public Guid PlanoId { get; private set; }
    public Plano? Plano { get; private set; }
    public DateTime DataCadastro { get; private set; }
    public DateTime? ExcluidoEm { get; private set; }

    public void Atualizar(string? nomeCompleto, DateOnly dataNascimento, Guid planoId, StatusBeneficiario status)
    {
        var nome = nomeCompleto?.Trim() ?? string.Empty;
        if (Status == StatusBeneficiario.INATIVO &&
            (nome != NomeCompleto || dataNascimento != DataNascimento || planoId != PlanoId))
            throw new ConflitoException("Beneficiário inativo permite somente alteração de status");

        DefinirDados(nome, dataNascimento, planoId);
        Status = status;
    }

    public static void ValidarDadosCadastrais(string? nomeCompleto, DateOnly dataNascimento, Guid planoId) =>
        ValidarDados(nomeCompleto, dataNascimento, planoId);

    public void Excluir() => ExcluidoEm = DateTime.UtcNow;

    private void DefinirDados(string? nomeCompleto, DateOnly dataNascimento, Guid planoId)
    {
        ValidarDados(nomeCompleto, dataNascimento, planoId);
        NomeCompleto = nomeCompleto!.Trim();
        DataNascimento = dataNascimento;
        PlanoId = planoId;
    }

    private static void ValidarDados(string? nomeCompleto, DateOnly dataNascimento, Guid planoId)
    {
        nomeCompleto = nomeCompleto?.Trim() ?? string.Empty;
        var detalhes = new List<DetalheErro>();
        if (nomeCompleto.Length == 0) detalhes.Add(new("nome_completo", "obrigatorio"));
        else if (nomeCompleto.Length < 3) detalhes.Add(new("nome_completo", "tamanho_minimo"));
        else if (nomeCompleto.Length > 120) detalhes.Add(new("nome_completo", "tamanho_maximo"));
        if (dataNascimento == default) detalhes.Add(new("data_nascimento", "obrigatorio"));
        else if (dataNascimento >= DateOnly.FromDateTime(DateTime.UtcNow)) detalhes.Add(new("data_nascimento", "deve_ser_data_passada"));
        if (planoId == Guid.Empty) detalhes.Add(new("plano_id", "obrigatorio"));
        if (detalhes.Count > 0) throw new ValidacaoException("Dados do beneficiário inválidos", detalhes);
    }

    private static string ValidarCpf(string? cpf)
    {
        if (string.IsNullOrEmpty(cpf))
            throw ErroCpf("CPF é obrigatório", "obrigatorio");

        if (cpf.Length != 11)
            throw ErroCpf("CPF deve conter exatamente 11 dígitos", "deve_conter_11_digitos");

        if (cpf.Any(caractere => !char.IsAsciiDigit(caractere)))
            throw ErroCpf("CPF deve conter somente dígitos, sem máscara ou espaços", "somente_digitos");

        if (cpf.Distinct().Count() == 1)
            throw ErroCpf("CPF não pode ser uma sequência de dígitos repetidos", "digitos_repetidos");

        if (CalcularDigito(cpf[..9], 10) != cpf[9] - '0' ||
            CalcularDigito(cpf[..10], 11) != cpf[10] - '0')
            throw ErroCpf("Dígitos verificadores do CPF são inválidos", "digitos_verificadores_invalidos");

        return cpf;
    }

    private static ValidacaoException ErroCpf(string mensagem, string regra) =>
        new(mensagem, [new("cpf", regra)]);

    private static int CalcularDigito(string digitos, int pesoInicial)
    {
        var resto = digitos.Select((d, i) => (d - '0') * (pesoInicial - i)).Sum() * 10 % 11;
        return resto == 10 ? 0 : resto;
    }
}
