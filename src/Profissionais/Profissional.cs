namespace dashboardforms;

public class Profissional : IEntidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Funcao { get; set; } = "";
    public string Telefone { get; set; } = "";
    public decimal ComissaoPct { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime Cadastro { get; set; } = DateTime.Now;
}

class ProfissionalRepositorio : RepositorioJson<Profissional>
{
    public ProfissionalRepositorio(string pasta) : base(pasta, "profissionais.json") { }

    public List<Profissional> Ativos() =>
        Todos().Where(p => p.Ativo).OrderBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

    public bool NomeEmUso(string nome, int ignorarId) =>
        itens.Any(p => p.Id != ignorarId && string.Equals(p.Nome.Trim(), nome.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public override void Excluir(int id)
    {
        var p = PorId(id);
        if (p != null && Repositorios.Atendimentos.DoProfissional(p).Count > 0)
            throw new InvalidOperationException("Este profissional já tem atendimentos registrados. Desative-o em vez de excluir.");
        if (Repositorios.Agendamentos.UsaProfissional(id))
            throw new InvalidOperationException("Este profissional tem agendamentos na Agenda. Desative-o em vez de excluir.");
        base.Excluir(id);
    }

    public void DefinirAtivo(int id, bool ativo)
    {
        var p = PorId(id);
        if (p == null) return;
        p.Ativo = ativo;
        Atualizar(p);
    }
}
