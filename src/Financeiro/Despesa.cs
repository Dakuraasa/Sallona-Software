namespace dashboardforms;

// ===== Modelo: uma despesa do salão (aluguel, luz, produtos...) =====
public class Despesa : IEntidade
{
    public int Id { get; set; }
    public DateTime Data { get; set; } = DateTime.Today;
    public string Descricao { get; set; } = "";
    public string Categoria { get; set; } = "Outros";
    public decimal Valor { get; set; }
}

// ===== Repositório de despesas (despesas.json na pasta da conta) =====
class DespesaRepositorio : RepositorioJson<Despesa>
{
    public static readonly string[] Categorias =
        { "Aluguel", "Água / Luz / Internet", "Produtos", "Salários", "Impostos", "Manutenção", "Outros" };

    public DespesaRepositorio(string pasta) : base(pasta, "despesas.json") { }
}