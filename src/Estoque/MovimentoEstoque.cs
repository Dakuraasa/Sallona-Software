namespace dashboardforms;

// ===== Uma movimentação de estoque (o histórico do que entrou e saiu) =====
public class MovimentoEstoque : IEntidade
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.Now;
    public string Tipo { get; set; } = TipoMovimento.Entrada;
    public string Motivo { get; set; } = "";
    public int Quantidade { get; set; }        // + entrou, − saiu
    public int SaldoApos { get; set; }
    public string Observacao { get; set; } = "";
}

static class TipoMovimento
{
    public const string Entrada = "Entrada";
    public const string Saida = "Saída";
    public const string Ajuste = "Ajuste";

    public static readonly string[] MotivosEntrada = { "Compra", "Devolução", "Brinde / amostra", "Outro" };
    public static readonly string[] MotivosSaida = { "Uso no salão", "Venda", "Vencido / estragado", "Perda", "Outro" };
}

// ===== Repositório de movimentações (movimentos_estoque.json na pasta da conta) =====
// Toda mudança de quantidade passa por aqui: atualiza o produto E guarda o histórico.
class EstoqueRepositorio : RepositorioJson<MovimentoEstoque>
{
    public const string MotivoInicial = "Estoque inicial";

    public EstoqueRepositorio(string pasta) : base(pasta, "movimentos_estoque.json") { }

    public List<MovimentoEstoque> DoProduto(int produtoId) =>
        itens.Where(m => m.ProdutoId == produtoId).OrderByDescending(m => m.Data).Select(Copiar).ToList();

    /// <summary>
    /// Entrada/Saída: "quantidade" é quanto entra ou sai (positivo).
    /// Ajuste: "quantidade" é o NOVO total contado na prateleira.
    /// </summary>
    public MovimentoEstoque Movimentar(int produtoId, string tipo, int quantidade, string motivo, string observacao = "")
    {
        var p = Repositorios.Produtos.PorId(produtoId) ?? throw new InvalidOperationException("Produto não encontrado.");
        if (quantidade < 0) throw new InvalidOperationException("A quantidade não pode ser negativa.");

        int delta = tipo switch
        {
            TipoMovimento.Entrada => quantidade,
            TipoMovimento.Saida => -quantidade,
            TipoMovimento.Ajuste => quantidade - p.Quantidade,
            _ => throw new InvalidOperationException("Tipo de movimentação inválido.")
        };
        if (tipo != TipoMovimento.Ajuste && quantidade == 0) throw new InvalidOperationException("Informe uma quantidade maior que zero.");
        if (tipo == TipoMovimento.Ajuste && delta == 0) throw new InvalidOperationException("A quantidade contada é igual à do sistema; nada para ajustar.");
        int saldo = p.Quantidade + delta;
        if (saldo < 0) throw new InvalidOperationException($"Só há {p.Quantidade} {p.Unidade} em estoque.");

        var mov = new MovimentoEstoque
        {
            ProdutoId = p.Id, ProdutoNome = p.Nome, Tipo = tipo, Motivo = motivo,
            Quantidade = delta, SaldoApos = saldo, Observacao = observacao.Trim()
        };

        // primeiro o produto, depois o histórico; se o histórico falhar, desfaz o produto
        Repositorios.Produtos.DefinirQuantidade(p.Id, saldo);
        try { Adicionar(mov); }
        catch { Repositorios.Produtos.DefinirQuantidade(p.Id, p.Quantidade); throw; }
        return mov;
    }

    internal void RemoverDoProduto(int produtoId)
    {
        var antes = itens.ToList();
        itens.RemoveAll(m => m.ProdutoId == produtoId);
        try { Salvar(); }
        catch { itens.Clear(); itens.AddRange(antes); throw; }
    }
}
