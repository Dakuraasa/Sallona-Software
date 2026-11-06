namespace dashboardforms;

public class Produto : IEntidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Categoria { get; set; } = "Cabelo";
    public string Marca { get; set; } = "";
    public string Unidade { get; set; } = "un";
    public decimal PrecoVenda { get; set; }
    public decimal Custo { get; set; }
    public int Quantidade { get; set; }
    public int EstoqueMinimo { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime Cadastro { get; set; } = DateTime.Now;

    public bool EstoqueBaixo => Ativo && Quantidade <= EstoqueMinimo;
    public string Situacao => !Ativo ? "Inativo" : Quantidade <= 0 ? "Sem estoque" : EstoqueBaixo ? "Baixo" : "OK";

    public decimal? MargemPct => PrecoVenda > 0 ? Math.Round((PrecoVenda - Custo) / PrecoVenda * 100, 1) : null;
}

class ProdutoRepositorio : RepositorioJson<Produto>
{
    public static readonly string[] Categorias = { "Cabelo", "Unhas", "Estética", "Maquiagem", "Barba", "Higiene e limpeza", "Outros" };
    public static readonly string[] Unidades = { "un", "frasco", "caixa", "pacote", "tubo", "ml", "g", "L", "kg" };

    public ProdutoRepositorio(string pasta) : base(pasta, "produtos.json") { }

    public bool NomeEmUso(string nome, int ignorarId) =>
        itens.Any(p => p.Id != ignorarId && string.Equals(p.Nome.Trim(), nome.Trim(), StringComparison.CurrentCultureIgnoreCase));

    internal void DefinirQuantidade(int id, int nova)
    {
        var p = itens.FirstOrDefault(x => x.Id == id) ?? throw new InvalidOperationException("Produto não encontrado.");
        int antiga = p.Quantidade;
        p.Quantidade = nova;
        try { Salvar(); }
        catch { p.Quantidade = antiga; throw; }
    }

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
