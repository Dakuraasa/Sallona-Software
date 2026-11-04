using System.Globalization;

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

// ===== Repositório de serviços =====
// Os serviços ficam salvos em servicos.json, na pasta da conta que fez login
// (veja Armazenamento.cs). A lista começa vazia: quem cadastra os serviços é o usuário.
// TODO: quando o banco MySQL estiver pronto, trocar Carregar/Salvar por consultas SQL,
// mantendo os mesmos métodos públicos — assim a tela não precisa mudar.
class ServicoRepositorio
{
    readonly string arquivo;
    readonly List<Servico> servicos;

    public ServicoRepositorio(string pasta)
    {
        arquivo = Path.Combine(pasta, "servicos.json");
        servicos = Armazenamento.Carregar<Servico>(arquivo);
    }

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
        try { Salvar(); }
        catch { servicos.RemoveAt(servicos.Count - 1); throw; }   // desfaz se não conseguiu gravar
    }

    public void Atualizar(Servico s)
    {
        int i = servicos.FindIndex(x => x.Id == s.Id);
        if (i < 0) return;
        var antigo = servicos[i];
        servicos[i] = s.Copia();
        try { Salvar(); }
        catch { servicos[i] = antigo; throw; }
    }

    // Desativar em vez de apagar: o serviço some da hora de agendar,
    // mas continua no histórico de atendimentos e nos relatórios.
    public void DefinirAtivo(int id, bool ativo)
    {
        var s = servicos.FirstOrDefault(x => x.Id == id);
        if (s == null) return;
        bool anterior = s.Ativo;
        s.Ativo = ativo;
        try { Salvar(); }
        catch { s.Ativo = anterior; throw; }
    }

    // Um serviço já usado em algum atendimento não pode ser excluído (só desativado).
    public bool PossuiAtendimentos(int id)
    {
        var s = servicos.FirstOrDefault(x => x.Id == id);
        return s != null && Repositorios.Atendimentos.UsaServico(s.Nome);
    }

    // Excluir de verdade só é permitido para serviços que nunca foram usados.
    public void Excluir(int id)
    {
        if (PossuiAtendimentos(id))
            throw new InvalidOperationException("Este serviço já foi usado em atendimentos. Desative-o em vez de excluir.");
        int i = servicos.FindIndex(x => x.Id == id);
        if (i < 0) return;
        var removido = servicos[i];
        servicos.RemoveAt(i);
        try { Salvar(); }
        catch { servicos.Insert(i, removido); throw; }
    }

    void Salvar() => Armazenamento.Salvar(arquivo, servicos);
}
