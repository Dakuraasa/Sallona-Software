using System.Globalization;
using System.Text.Json;

namespace dashboardforms;

// ===== Modelo: um serviço do catálogo do salão =====
public class Servico
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Categoria { get; set; } = "Cabelo";
    public decimal Preco { get; set; }
    public int DuracaoMin { get; set; } = 30;
    public bool Ativo { get; set; } = true;

    public Servico Copia() => (Servico)MemberwiseClone();
}

// ===== Formatação no padrão brasileiro =====
static class Formato
{
    public static readonly CultureInfo Br = new("pt-BR");

    public static string Dinheiro(decimal v) => v.ToString("C", Br);

    public static string Duracao(int min) =>
        min < 60 ? $"{min} min"
        : min % 60 == 0 ? $"{min / 60}h"
        : $"{min / 60}h{min % 60:00}";
}

// ===== Acesso aos dados compartilhado por todas as páginas =====
static class Repositorios
{
    // A Agenda, o Financeiro etc. também vão usar esta mesma lista de serviços.
    public static readonly ServicoRepositorio Servicos = new();
}

// ===== Repositório de serviços =====
// Por enquanto os dados ficam salvos em um arquivo JSON em %AppData%\Sallona\servicos.json,
// para não sumirem quando o programa fecha.
// TODO: quando o banco MySQL estiver pronto, trocar Carregar/Salvar por consultas SQL,
// mantendo os mesmos métodos públicos — assim a tela não precisa mudar.
class ServicoRepositorio
{
    static readonly string Pasta = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sallona");
    static readonly string Arquivo = Path.Combine(Pasta, "servicos.json");
    static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    readonly List<Servico> servicos;

    public ServicoRepositorio() => servicos = Carregar();

    public List<Servico> Todos() =>
        servicos.OrderBy(s => s.Nome, StringComparer.Create(Formato.Br, true))
                .Select(s => s.Copia())
                .ToList();

    public Servico? PorId(int id) => servicos.FirstOrDefault(s => s.Id == id)?.Copia();

    public bool NomeEmUso(string nome, int ignorarId) =>
        servicos.Any(s => s.Id != ignorarId &&
                          string.Equals(s.Nome.Trim(), nome.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public void Adicionar(Servico s)
    {
        s.Id = servicos.Count == 0 ? 1 : servicos.Max(x => x.Id) + 1;
        servicos.Add(s.Copia());
        Salvar();
    }

    public void Atualizar(Servico s)
    {
        int i = servicos.FindIndex(x => x.Id == s.Id);
        if (i < 0) return;
        servicos[i] = s.Copia();
        Salvar();
    }

    // Desativar em vez de apagar: o serviço some da hora de agendar,
    // mas continua no histórico de atendimentos e nos relatórios.
    public void DefinirAtivo(int id, bool ativo)
    {
        var s = servicos.FirstOrDefault(x => x.Id == id);
        if (s == null) return;
        s.Ativo = ativo;
        Salvar();
    }

    // TODO: quando existir a tabela de atendimentos, verificar se algum atendimento usa este serviço.
    public bool PossuiAtendimentos(int id) => false;

    // Excluir de verdade só é permitido para serviços que nunca foram usados.
    public void Excluir(int id)
    {
        if (PossuiAtendimentos(id))
            throw new InvalidOperationException("Este serviço já foi usado em atendimentos. Desative-o em vez de excluir.");
        servicos.RemoveAll(x => x.Id == id);
        Salvar();
    }

    List<Servico> Carregar()
    {
        try
        {
            if (File.Exists(Arquivo))
                return JsonSerializer.Deserialize<List<Servico>>(File.ReadAllText(Arquivo)) ?? new();
        }
        catch (Exception)
        {
            // Arquivo corrompido: guarda uma cópia para não perder nada e começa uma lista nova.
            try { File.Copy(Arquivo, Arquivo + ".bak", true); } catch { }
            return new();
        }

        // Primeira vez que o programa abre: começa com alguns serviços de exemplo,
        // que podem ser editados ou excluídos pela tela.
        var exemplos = Exemplos();
        try { Directory.CreateDirectory(Pasta); File.WriteAllText(Arquivo, JsonSerializer.Serialize(exemplos, Opcoes)); } catch { }
        return exemplos;
    }

    void Salvar()
    {
        Directory.CreateDirectory(Pasta);
        File.WriteAllText(Arquivo, JsonSerializer.Serialize(servicos, Opcoes));
    }

    static List<Servico> Exemplos()
    {
        var lista = new List<Servico>
        {
            new() { Nome = "Corte feminino",        Categoria = "Cabelo",   Preco = 120, DuracaoMin = 60 },
            new() { Nome = "Corte masculino",       Categoria = "Cabelo",   Preco = 55,  DuracaoMin = 30 },
            new() { Nome = "Escova",                Categoria = "Cabelo",   Preco = 70,  DuracaoMin = 45 },
            new() { Nome = "Hidratação",            Categoria = "Cabelo",   Preco = 90,  DuracaoMin = 60 },
            new() { Nome = "Coloração",             Categoria = "Cabelo",   Preco = 320, DuracaoMin = 120 },
            new() { Nome = "Progressiva",           Categoria = "Cabelo",   Preco = 450, DuracaoMin = 180 },
            new() { Nome = "Manicure",              Categoria = "Unhas",    Preco = 40,  DuracaoMin = 40 },
            new() { Nome = "Pedicure",              Categoria = "Unhas",    Preco = 45,  DuracaoMin = 45 },
            new() { Nome = "Design de sobrancelha", Categoria = "Estética", Preco = 35,  DuracaoMin = 30 },
            new() { Nome = "Barba",                 Categoria = "Barba",    Preco = 40,  DuracaoMin = 30 },
        };
        for (int i = 0; i < lista.Count; i++) lista[i].Id = i + 1;
        return lista;
    }
}
