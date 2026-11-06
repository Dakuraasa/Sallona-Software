using System.Text.Json;

namespace dashboardforms;

// ===== Onde os dados ficam salvos =====
// Ainda não usamos banco de dados: cada conta tem a sua própria pasta em
//   %AppData%\Sallona\contas\<id da conta>\
// com um arquivo JSON para cada assunto (clientes.json, servicos.json...).
// TODO: quando o MySQL estiver pronto, os repositórios passam a usar SQL
// e esta classe deixa de ser necessária — as telas não mudam.
static class Armazenamento
{
    public static readonly string PastaRaiz = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sallona");

    static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    public static string PastaDaConta(int idConta) =>
        Path.Combine(PastaRaiz, "contas", idConta.ToString());

    // Lê uma lista de um arquivo JSON. Se o arquivo não existir, devolve lista vazia.
    // Se estiver corrompido, guarda uma cópia .bak e começa do zero (não perde o original).
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

    // Grava primeiro num arquivo temporário e só depois troca pelo oficial:
    // se o programa cair no meio da gravação, o arquivo antigo continua inteiro.
    // Pode lançar IOException/UnauthorizedAccessException — quem chama trata e avisa o usuário.
    public static void Salvar<T>(string arquivo, T dados)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
        string temp = arquivo + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(dados, Opcoes));
        File.Move(temp, arquivo, overwrite: true);
    }

    // Grava um texto pronto (usado pela restauração de backup), também de forma segura
    public static void SalvarTexto(string arquivo, string conteudo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
        string temp = arquivo + ".tmp";
        File.WriteAllText(temp, conteudo);
        File.Move(temp, arquivo, overwrite: true);
    }

    // Executa uma gravação; se der erro, avisa em vez de fechar o programa.
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

// ===== Repositórios da conta que fez login =====
// As telas sempre usam Repositorios.X; o Program chama Abrir(...) logo depois do login.
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
    }
}

// ===== Preferências da conta (editadas em Configurações) =====
class Configuracao
{
    public decimal MetaMensal { get; set; }
    public string PeriodoPadrao { get; set; } = "Mês";      // período que o Dashboard abre
    public decimal ComissaoPadrao { get; set; }             // % sugerida ao cadastrar profissional
    public int EstoqueMinimoPadrao { get; set; } = 2;       // sugerido ao cadastrar produto
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
