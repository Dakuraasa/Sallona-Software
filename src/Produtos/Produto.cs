namespace dashboardforms;

// ===== Modelo: um produto (para uso no salão ou para revenda) =====
public class Produto : IEntidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Categoria { get; set; } = "Cabelo";
    public string Marca { get; set; } = "";
    public string Unidade { get; set; } = "un";        // un, frasco, caixa, ml, g...
    public decimal PrecoVenda { get; set; }            // 0 = não é vendido (só uso interno)
    public decimal Custo { get; set; }                 // quanto o salão paga por unidade
    public int Quantidade { get; set; }                // estoque atual (muda pelas movimentações)
    public int EstoqueMinimo { get; set; }             // abaixo disso aparece o alerta de estoque baixo
    public bool Ativo { get; set; } = true;
    public DateTime Cadastro { get; set; } = DateTime.Now;

    public bool EstoqueBaixo => Ativo && Quantidade <= EstoqueMinimo;
    public string Situacao => !Ativo ? "Inativo" : Quantidade <= 0 ? "Sem estoque" : EstoqueBaixo ? "Baixo" : "OK";
    // margem de lucro sobre o preço de venda (só faz sentido se o produto é vendido)
    public decimal? MargemPct => PrecoVenda > 0 ? Math.Round((PrecoVenda - Custo) / PrecoVenda * 100, 1) : null;
}

// ===== Repositório de produtos (produtos.json na pasta da conta) =====
// A quantidade só muda pelo EstoqueRepositorio (entrada, saída, ajuste), que guarda o histórico.
class ProdutoRepositorio : RepositorioJson<Produto>
{
    public static readonly string[] Categorias = { "Cabelo", "Unhas", "Estética", "Maquiagem", "Barba", "Higiene e limpeza", "Outros" };
    public static readonly string[] Unidades = { "un", "frasco", "caixa", "pacote", "tubo", "ml", "g", "L", "kg" };

    public ProdutoRepositorio(string pasta) : base(pasta, "produtos.json") { }

    public bool NomeEmUso(string nome, int ignorarId) =>
        itens.Any(p => p.Id != ignorarId && string.Equals(p.Nome.Trim(), nome.Trim(), StringComparison.CurrentCultureIgnoreCase));

    // Usado só pelo EstoqueRepositorio
    internal void DefinirQuantidade(int id, int nova)
    {
        var p = itens.FirstOrDefault(x => x.Id == id) ?? throw new InvalidOperationException("Produto não encontrado.");
        int antiga = p.Quantidade;
        p.Quantidade = nova;
        try { Salvar(); }
        catch { p.Quantidade = antiga; throw; }
    }

    // Excluir só se o produto nunca foi movimentado além do estoque inicial; senão, desative.
    public override void Excluir(int id)
    {
        if (Repositorios.Estoque.DoProduto(id).Any(m => m.Motivo != EstoqueRepositorio.MotivoInicial))
            throw new InvalidOperationException("Este produto já tem movimentações de estoque. Desative-o em vez de excluir.");
        base.Excluir(id);
        Repositorios.Estoque.RemoverDoProduto(id);
    }

    public void DefinirAtivo(int id, bool ativo)
    {
        var p = PorId(id);
        if (p == null) return;
        p.Ativo = ativo;
        Atualizar(p);
    }
}
