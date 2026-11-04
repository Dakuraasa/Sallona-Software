namespace dashboardforms;

// ===== Modelo: um atendimento (um cliente, um ou mais serviços) =====
public class Atendimento
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNome { get; set; } = "";     // guardado para o histórico não mudar se o cadastro mudar
    public int ProfissionalId { get; set; }            // 0 = atendimento antigo (só com o nome)
    public string Profissional { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.Now;
    public List<ItemAtendimento> Itens { get; set; } = new();

    public string? FormaPagamento { get; set; }        // null = ainda não pago
    public DateTime? PagoEm { get; set; }

    public bool Pago => FormaPagamento != null;
    public decimal Total => Itens.Sum(i => i.Valor);
    public string Descricao => string.Join(" + ", Itens.Select(i => i.Servico));

    public Atendimento Copia()
    {
        var c = (Atendimento)MemberwiseClone();
        c.Itens = Itens.Select(i => i with { }).ToList();
        return c;
    }
}

// Serviço realizado e o preço cobrado NAQUELE dia
// (se o preço do catálogo mudar depois, o histórico continua certo).
public record ItemAtendimento(string Servico, decimal Valor);

// ===== Repositório de atendimentos =====
// Ficam em atendimentos.json, na pasta da conta que fez login — começa vazio.
// TODO: quando a Agenda e o MySQL estiverem prontos, os atendimentos virão de lá.
class AtendimentoRepositorio
{
    readonly string arquivo;
    readonly List<Atendimento> atendimentos;

    public AtendimentoRepositorio(string pasta)
    {
        arquivo = Path.Combine(pasta, "atendimentos.json");
        atendimentos = Armazenamento.Carregar<Atendimento>(arquivo);
    }

    public List<Atendimento> Todos() => atendimentos.Select(a => a.Copia()).ToList();

    public List<Atendimento> DoCliente(int clienteId) =>
        atendimentos.Where(a => a.ClienteId == clienteId).OrderByDescending(a => a.Data).Select(a => a.Copia()).ToList();

    public List<Atendimento> DoProfissional(Profissional p) =>
        atendimentos.Where(a => a.ProfissionalId == p.Id ||
                                (a.ProfissionalId == 0 && string.Equals(a.Profissional, p.Nome, StringComparison.CurrentCultureIgnoreCase)))
                    .Select(a => a.Copia()).ToList();

    public bool UsaServico(string nome) =>
        atendimentos.Any(a => a.Itens.Any(i => string.Equals(i.Servico, nome, StringComparison.CurrentCultureIgnoreCase)));

    // Data da última visita (atendimento pago) de cada cliente
    public Dictionary<int, DateTime> UltimasVisitas() =>
        atendimentos.Where(a => a.Pago)
                    .GroupBy(a => a.ClienteId)
                    .ToDictionary(g => g.Key, g => g.Max(a => a.Data));

    public void Adicionar(Atendimento a)
    {
        a.Id = atendimentos.Count == 0 ? 1 : atendimentos.Max(x => x.Id) + 1;
        atendimentos.Add(a.Copia());
        try { Salvar(); }
        catch { atendimentos.RemoveAt(atendimentos.Count - 1); throw; }
    }

    public void RegistrarPagamento(int id, string forma)
    {
        var a = atendimentos.FirstOrDefault(x => x.Id == id);
        if (a == null || a.Pago) return;
        a.FormaPagamento = forma;
        a.PagoEm = DateTime.Now;
        try { Salvar(); }
        catch { a.FormaPagamento = null; a.PagoEm = null; throw; }
    }

    public void Excluir(int id)
    {
        int i = atendimentos.FindIndex(x => x.Id == id);
        if (i < 0) return;
        var removido = atendimentos[i];
        atendimentos.RemoveAt(i);
        try { Salvar(); }
        catch { atendimentos.Insert(i, removido); throw; }
    }

    void Salvar() => Armazenamento.Salvar(arquivo, atendimentos);
}
