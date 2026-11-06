namespace dashboardforms;

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string? Email { get; set; }
    public DateTime Cadastro { get; set; } = DateTime.Now;

    public Cliente Copia() => (Cliente)MemberwiseClone();
}

class ClienteRepositorio
{
    readonly string arquivo;
    readonly List<Cliente> clientes;

    public ClienteRepositorio(string pasta)
    {
        arquivo = Path.Combine(pasta, "clientes.json");
        clientes = Armazenamento.Carregar<Cliente>(arquivo);
    }

    public List<Cliente> Todos() => clientes.Select(c => c.Copia()).ToList();

    public Cliente? PorId(int id) => clientes.FirstOrDefault(c => c.Id == id)?.Copia();

    public bool NomeEmUso(string nome, int ignorarId) =>
        clientes.Any(c => c.Id != ignorarId &&
                          string.Equals(c.Nome.Trim(), nome.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public void Adicionar(Cliente c)
    {
        c.Id = clientes.Count == 0 ? 1 : clientes.Max(x => x.Id) + 1;
        clientes.Add(c.Copia());
        try { Salvar(); }
        catch { clientes.RemoveAt(clientes.Count - 1); throw; }
    }

    public void Atualizar(Cliente c)
    {
        int i = clientes.FindIndex(x => x.Id == c.Id);
        if (i < 0) return;
        var antigo = clientes[i];
        clientes[i] = c.Copia();
        try { Salvar(); }
        catch { clientes[i] = antigo; throw; }
    }

    public void Excluir(int id)
    {
        if (Repositorios.Atendimentos.DoCliente(id).Count > 0)
            throw new InvalidOperationException("Este cliente já tem atendimentos registrados e não pode ser excluído.");
        if (Repositorios.Agendamentos.UsaCliente(id))
            throw new InvalidOperationException("Este cliente tem agendamentos marcados. Cancele-os antes de excluir.");
        int i = clientes.FindIndex(x => x.Id == id);
        if (i < 0) return;
        var removido = clientes[i];
        clientes.RemoveAt(i);
        try { Salvar(); }
        catch { clientes.Insert(i, removido); throw; }
    }

    void Salvar() => Armazenamento.Salvar(arquivo, clientes);
}
