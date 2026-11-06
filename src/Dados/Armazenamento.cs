using System.Text.Json;

namespace dashboardforms;

static class Armazenamento
{
    public static readonly string PastaRaiz = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sallona");

    static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    public static string PastaDaConta(int idConta) =>
        Path.Combine(PastaRaiz, "contas", idConta.ToString());

    public static List<T> Carregar<T>(string arquivo)
    {
        try
        {
            if (File.Exists(arquivo))
                return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(arquivo)) ?? new();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            try { File.Copy(arquivo, arquivo + ".bak", true); } catch { }
        }
        return new();
    }

    public static T? CarregarObjeto<T>(string arquivo) where T : class
    {
        try
        {
            if (File.Exists(arquivo))
                return JsonSerializer.Deserialize<T>(File.ReadAllText(arquivo));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            try { File.Copy(arquivo, arquivo + ".bak", true); } catch { }
        }
        return null;
    }

    public static void Salvar<T>(string arquivo, T dados)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
        string temp = arquivo + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(dados, Opcoes));
        File.Move(temp, arquivo, overwrite: true);
    }

    public static void SalvarTexto(string arquivo, string conteudo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
        string temp = arquivo + ".tmp";
        File.WriteAllText(temp, conteudo);
        File.Move(temp, arquivo, overwrite: true);
    }

    public static bool Tentar(IWin32Window? dono, Action acao)
    {
        try
        {
            acao();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(dono, "Não foi possível salvar.\n\n" + ex.Message,
                "Sallona", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}

static class Repositorios
{
    public static ServicoRepositorio Servicos { get; private set; } = null!;
    public static ClienteRepositorio Clientes { get; private set; } = null!;
    public static AtendimentoRepositorio Atendimentos { get; private set; } = null!;
    public static ConfiguracaoRepositorio Configuracao { get; private set; } = null!;
    public static ProfissionalRepositorio Profissionais { get; private set; } = null!;
    public static ProdutoRepositorio Produtos { get; private set; } = null!;
    public static EstoqueRepositorio Estoque { get; private set; } = null!;

    public static DespesaRepositorio Despesas { get; private set; } = null!;
    public static AgendamentoRepositorio Agendamentos { get; private set; } = null!;

    public static void Abrir(Usuario usuario)
    {
        string pasta = Armazenamento.PastaDaConta(usuario.Id);
        Servicos = new ServicoRepositorio(pasta);
        Clientes = new ClienteRepositorio(pasta);
        Atendimentos = new AtendimentoRepositorio(pasta);
        Configuracao = new ConfiguracaoRepositorio(pasta);
        Profissionais = new ProfissionalRepositorio(pasta);
        Produtos = new ProdutoRepositorio(pasta);
        Estoque = new EstoqueRepositorio(pasta);
        Despesas = new DespesaRepositorio(pasta);
        Agendamentos = new AgendamentoRepositorio(pasta);
    }
}

class Configuracao
{
    public decimal MetaMensal { get; set; }
    public string PeriodoPadrao { get; set; } = "Mês";
    public decimal ComissaoPadrao { get; set; }
    public int EstoqueMinimoPadrao { get; set; } = 2;
    public int AberturaMin { get; set; } = 8 * 60;
    public int FechamentoMin { get; set; } = 18 * 60;
    public int AlmocoInicioMin { get; set; } = 12 * 60;
    public int AlmocoFimMin { get; set; } = 13 * 60;
}

class ConfiguracaoRepositorio
{
    readonly string arquivo;
    readonly Configuracao dados;

    public ConfiguracaoRepositorio(string pasta)
    {
        arquivo = Path.Combine(pasta, "configuracao.json");
        dados = Armazenamento.CarregarObjeto<Configuracao>(arquivo) ?? new();
    }

    public decimal MetaMensal => dados.MetaMensal;
    public Configuracao Dados => dados;

    public void SalvarExpediente(int abertura, int fechamento, int almocoInicio, int almocoFim)
    {
        var antes = (dados.AberturaMin, dados.FechamentoMin, dados.AlmocoInicioMin, dados.AlmocoFimMin);
        (dados.AberturaMin, dados.FechamentoMin, dados.AlmocoInicioMin, dados.AlmocoFimMin) = (abertura, fechamento, almocoInicio, almocoFim);
        try { Armazenamento.Salvar(arquivo, dados); }
        catch { (dados.AberturaMin, dados.FechamentoMin, dados.AlmocoInicioMin, dados.AlmocoFimMin) = antes; throw; }
    }
    public string PeriodoPadrao => dados.PeriodoPadrao;
    public decimal ComissaoPadrao => dados.ComissaoPadrao;
    public int EstoqueMinimoPadrao => dados.EstoqueMinimoPadrao;

    public void SalvarPreferencias(decimal meta, string periodo, decimal comissao, int estoqueMinimo)
    {
        var antes = (dados.MetaMensal, dados.PeriodoPadrao, dados.ComissaoPadrao, dados.EstoqueMinimoPadrao);
        (dados.MetaMensal, dados.PeriodoPadrao, dados.ComissaoPadrao, dados.EstoqueMinimoPadrao) = (meta, periodo, comissao, estoqueMinimo);
        try { Armazenamento.Salvar(arquivo, dados); }
        catch { (dados.MetaMensal, dados.PeriodoPadrao, dados.ComissaoPadrao, dados.EstoqueMinimoPadrao) = antes; throw; }
    }

    public void DefinirMeta(decimal valor)
    {
        decimal anterior = dados.MetaMensal;
        dados.MetaMensal = valor;
        try { Armazenamento.Salvar(arquivo, dados); }
        catch { dados.MetaMensal = anterior; throw; }
    }
}
