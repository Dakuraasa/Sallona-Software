using System.Data;

namespace dashboardforms;

// ===== Página de Produtos: cadastro, preço, custo e margem =====
// (a quantidade em estoque é controlada na página Estoque)
class ProdutosPage : UserControl, IPagina
{
    static ProdutoRepositorio repo => Repositorios.Produtos;

    readonly DataTable tabela = new();
    readonly DataGridView grid;
    readonly Label[] kpiValor, kpiSub;
    readonly Label lblNome = new(), lblCategoria = new();
    readonly ListaLinhas linhas = new() { Dock = DockStyle.Top, Height = 172 };
    readonly FlowLayoutPanel acoes = new() { Dock = DockStyle.Top, Height = 46, BackColor = Color.White, Padding = new Padding(0, 6, 0, 0), WrapContents = false };
    readonly BotaoAnimado btnAtivar;
    readonly Label lblDica = Ui.Dica("", DockStyle.Fill);
    readonly ComboBox cbCategoria = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Margin = new Padding(0, 6, 8, 0), Font = new Font("Segoe UI", 9.5f) };
    string busca = "";
    bool atualizando;

    public ProdutosPage()
    {
        BackColor = Cores.Fundo;
        DoubleBuffered = true;

        foreach (var (nome, tipo) in new[] { ("Id", typeof(int)), ("Produto", typeof(string)), ("Categoria", typeof(string)), ("Marca", typeof(string)),
                                               ("Custo", typeof(decimal)), ("Preço de venda", typeof(decimal)), ("Margem", typeof(string)),
                                               ("Estoque", typeof(string)), ("Situação", typeof(string)) })
            tabela.Columns.Add(nome, tipo);
        grid = Ui.Tabela(tabela);
        Ui.ColorirSituacao(grid, "Situação", CorSituacao);
        grid.SelectionChanged += (_, _) => { if (!atualizando) MostrarDetalhes(); };
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };

        cbCategoria.Items.Add("Todas as categorias");
        cbCategoria.Items.AddRange(ProdutoRepositorio.Categorias);
        cbCategoria.SelectedIndex = 0;
        cbCategoria.SelectedIndexChanged += (_, _) => Recarregar();
        var novo = Ui.BotaoPrincipal("+  Novo produto", 150);
        novo.Click += (_, _) => Novo();

        var raiz = Ui.Raiz(64, 124, 0);
        var cab = Ui.Cabecalho("Produtos", Ui.Busca("Buscar produto ou marca...", t => { busca = t; Recarregar(); }), cbCategoria, novo);
        raiz.Controls.Add(cab, 0, 0); raiz.SetColumnSpan(cab, 2);

        var kpis = Ui.Kpis(new[] { "Produtos ativos", "Valor em estoque", "Estoque baixo", "Margem média" }, out kpiValor, out kpiSub);
        raiz.Controls.Add(kpis, 0, 1); raiz.SetColumnSpan(kpis, 2);

        var cTab = Ui.Cartao();
        cTab.Controls.Add(grid);
        cTab.Controls.Add(Ui.Titulo("Catálogo de produtos"));
        cTab.Controls.Add(Ui.Dica("Preço de venda zerado = produto só para uso no salão. Entradas e saídas ficam na página Estoque."));
        raiz.Controls.Add(cTab, 0, 2);

        raiz.Controls.Add(ConstruirDetalhes(out btnAtivar), 1, 2);
        Controls.Add(raiz);
        Recarregar();
    }

    public static Color? CorSituacao(string s) => s switch
    {
        "OK" => Cores.Verde,
        "Baixo" => Cores.Coral,
        "Sem estoque" => Cores.Vinho,
        _ => Cores.Cinza
    };

    Control ConstruirDetalhes(out BotaoAnimado ativar)
    {
        var c = Ui.Cartao();
        lblNome.Dock = DockStyle.Top; lblNome.Height = 34; lblNome.ForeColor = Cores.Escuro; lblNome.BackColor = Color.Transparent;
        lblNome.Font = new Font("Segoe UI", 14f, FontStyle.Bold); lblNome.AutoEllipsis = true;
        lblCategoria.Dock = DockStyle.Top; lblCategoria.Height = 24; lblCategoria.ForeColor = Cores.Primaria; lblCategoria.BackColor = Color.Transparent;
        lblCategoria.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

        var editar = Ui.Botao("Editar", Cores.Primaria, Color.White, 84); editar.Margin = new Padding(0, 0, 6, 0);
        ativar = Ui.Botao("Desativar", Cores.Fundo, Cores.Vinho, 90); ativar.Margin = new Padding(0, 0, 6, 0);
        var excluir = Ui.Botao("Excluir", Color.White, Cores.Coral, 80); excluir.Margin = new Padding(0);
        editar.Click += (_, _) => Editar();
        ativar.Click += (_, _) => AlternarAtivo();
        excluir.Click += (_, _) => Excluir();
        acoes.Controls.AddRange(new Control[] { editar, ativar, excluir });

        c.Controls.Add(lblDica);
        c.Controls.Add(acoes);
        c.Controls.Add(linhas);
        c.Controls.Add(lblCategoria);
        c.Controls.Add(lblNome);
        c.Controls.Add(Ui.Titulo("Detalhes"));
        return c;
    }

    public void Recarregar()
    {
        int? manter = Ui.IdSelecionado(grid);
        string cat = cbCategoria.SelectedIndex <= 0 ? "" : (string)cbCategoria.SelectedItem!;
        var todos = repo.Todos().OrderBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

        atualizando = true;
        tabela.Rows.Clear();
        foreach (var p in todos.Where(p => (cat.Length == 0 || p.Categoria == cat) &&
                                           (busca.Length == 0 || p.Nome.Contains(busca, StringComparison.CurrentCultureIgnoreCase) ||
                                            p.Marca.Contains(busca, StringComparison.CurrentCultureIgnoreCase))))
            tabela.Rows.Add(p.Id, p.Nome, p.Categoria, p.Marca, p.Custo, p.PrecoVenda,
                            p.MargemPct is decimal m ? $"{m:0.#}%" : "—", $"{p.Quantidade} {p.Unidade}", p.Situacao);
        Ui.Selecionar(grid, manter);
        atualizando = false;

        var ativos = todos.Where(p => p.Ativo).ToList();
        var vendidos = ativos.Where(p => p.MargemPct.HasValue).ToList();
        kpiValor[0].Text = ativos.Count.ToString(); kpiSub[0].Text = $"de {todos.Count} cadastrados";
        kpiValor[1].Text = Formato.Dinheiro(ativos.Sum(p => p.Custo * Math.Max(0, p.Quantidade))); kpiSub[1].Text = "pelo preço de custo";
        int baixos = ativos.Count(p => p.EstoqueBaixo);
        kpiValor[2].Text = baixos.ToString(); kpiSub[2].Text = baixos == 0 ? "tudo em ordem" : "produto(s) para repor";
        kpiSub[2].ForeColor = baixos == 0 ? Cores.Verde : Cores.Coral;
        kpiValor[3].Text = vendidos.Count == 0 ? "—" : $"{vendidos.Average(p => p.MargemPct!.Value):0.#}%";
        kpiSub[3].Text = "dos produtos à venda";
        MostrarDetalhes();
    }

    Produto? Selecionado() => Ui.IdSelecionado(grid) is int id ? repo.PorId(id) : null;

    void MostrarDetalhes()
    {
        var p = Selecionado();
        linhas.Itens.Clear();
        acoes.Visible = p != null;
        if (p == null)
        {
            lblNome.Text = repo.Todos().Count == 0 ? "Nenhum produto ainda" : "Nenhum selecionado";
            lblCategoria.Text = "";
            lblDica.Text = "Cadastre os produtos que o salão usa ou vende em \"+ Novo produto\".";
        }
        else
        {
            lblNome.Text = p.Nome;
            lblCategoria.Text = (p.Marca.Length > 0 ? $"{p.Categoria} · {p.Marca}" : p.Categoria).ToUpper();
            linhas.Itens.Add(("Custo", Formato.Dinheiro(p.Custo), Cores.Escuro));
            linhas.Itens.Add(("Preço de venda", p.PrecoVenda > 0 ? Formato.Dinheiro(p.PrecoVenda) : "uso interno", Cores.Escuro));
            linhas.Itens.Add(("Margem", p.MargemPct is decimal m ? $"{m:0.#}%" : "—", Cores.Escuro));
            linhas.Itens.Add(("Em estoque", $"{p.Quantidade} {p.Unidade}", CorSituacao(p.Situacao) ?? Cores.Escuro));
            linhas.Itens.Add(("Estoque mínimo", $"{p.EstoqueMinimo} {p.Unidade}", Cores.Escuro));
            linhas.Itens.Add(("Situação", p.Situacao, CorSituacao(p.Situacao) ?? Cores.Cinza));
            btnAtivar.Text = p.Ativo ? "Desativar" : "Reativar";
            btnAtivar.ForeColor = p.Ativo ? Cores.Vinho : Cores.Verde;
            lblDica.Text = "Para dar entrada, baixa ou corrigir a quantidade, use a página Estoque.";
        }
        linhas.Invalidate();
    }

    void Novo()
    {
        using var dlg = new ProdutoDialog(null);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        bool ok = Armazenamento.Tentar(FindForm(), () =>
        {
            int inicial = dlg.Resultado.Quantidade;
            dlg.Resultado.Quantidade = 0;
            repo.Adicionar(dlg.Resultado);
            // a quantidade inicial entra como movimentação, para aparecer no histórico
            if (inicial > 0)
                Repositorios.Estoque.Movimentar(dlg.Resultado.Id, TipoMovimento.Entrada, inicial, EstoqueRepositorio.MotivoInicial);
        });
        Recarregar();
        if (ok) Ui.Selecionar(grid, dlg.Resultado.Id);
    }

    void Editar()
    {
        var p = Selecionado();
        if (p == null) return;
        using var dlg = new ProdutoDialog(p);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (Armazenamento.Tentar(FindForm(), () => repo.Atualizar(dlg.Resultado))) Recarregar();
    }

    void AlternarAtivo()
    {
        var p = Selecionado();
        if (p == null) return;
        if (Armazenamento.Tentar(FindForm(), () => repo.DefinirAtivo(p.Id, !p.Ativo))) Recarregar();
    }

    void Excluir()
    {
        var p = Selecionado();
        if (p == null || !Ui.Confirmar(FindForm(), $"Excluir \"{p.Nome}\"?\nEssa ação não pode ser desfeita.", "Excluir produto")) return;
        if (Armazenamento.Tentar(FindForm(), () => repo.Excluir(p.Id))) Recarregar();
    }
}

// ===== Janela para cadastrar ou editar um produto =====
class ProdutoDialog : Formulario
{
    public Produto Resultado { get; }

    public ProdutoDialog(Produto? existente) : base(existente == null ? "Novo produto" : "Editar produto")
    {
        bool novo = existente == null;
        Resultado = existente ?? new Produto { EstoqueMinimo = Repositorios.Configuracao.EstoqueMinimoPadrao };

        var txtNome = AdicionarCampo("Nome do produto", NovaCaixaTexto(80));
        var cbCat = AdicionarCampo("Categoria", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        cbCat.Items.AddRange(ProdutoRepositorio.Categorias);
        var txtMarca = AdicionarCampo("Marca (opcional)", NovaCaixaTexto(60));
        var cbUn = AdicionarCampo("Unidade", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        cbUn.Items.AddRange(ProdutoRepositorio.Unidades);
        var numCusto = AdicionarCampo("Custo por unidade (R$)", new NumericUpDown { DecimalPlaces = 2, Maximum = 100000, ThousandsSeparator = true });
        var numVenda = AdicionarCampo("Preço de venda (R$) — 0 se for só para uso no salão", new NumericUpDown { DecimalPlaces = 2, Maximum = 100000, ThousandsSeparator = true });
        var numMin = AdicionarCampo("Estoque mínimo (avisa quando chegar nele)", new NumericUpDown { Maximum = 100000 });
        NumericUpDown? numInicial = novo ? AdicionarCampo("Quantidade em estoque agora", new NumericUpDown { Maximum = 100000 }) : null;
        var chkAtivo = new CheckBox { Text = "Produto ativo", AutoSize = true, ForeColor = Cores.Texto, Margin = new Padding(0, 14, 0, 0) };
        AdicionarControle(chkAtivo);

        txtNome.Text = Resultado.Nome;
        if (!cbCat.Items.Contains(Resultado.Categoria)) cbCat.Items.Add(Resultado.Categoria);
        cbCat.SelectedItem = Resultado.Categoria;
        if (!cbUn.Items.Contains(Resultado.Unidade)) cbUn.Items.Add(Resultado.Unidade);
        cbUn.SelectedItem = Resultado.Unidade;
        txtMarca.Text = Resultado.Marca;
        numCusto.Value = Math.Clamp(Resultado.Custo, 0, numCusto.Maximum);
        numVenda.Value = Math.Clamp(Resultado.PrecoVenda, 0, numVenda.Maximum);
        numMin.Value = Math.Clamp(Resultado.EstoqueMinimo, 0, (int)numMin.Maximum);
        chkAtivo.Checked = Resultado.Ativo;

        Validar = () =>
        {
            string nome = txtNome.Text.Trim();
            decimal custo = Math.Round(numCusto.Value, 2), venda = Math.Round(numVenda.Value, 2);
            if (nome.Length == 0) return "Informe o nome do produto.";
            if (Repositorios.Produtos.NomeEmUso(nome, Resultado.Id)) return "Já existe um produto com esse nome.";
            if (venda > 0 && venda < custo) return "O preço de venda está abaixo do custo. Confira os valores.";
            return null;
        };
        AoSalvar = () =>
        {
            Resultado.Nome = txtNome.Text.Trim();
            Resultado.Categoria = (string)cbCat.SelectedItem!;
            Resultado.Marca = txtMarca.Text.Trim();
            Resultado.Unidade = (string)cbUn.SelectedItem!;
            Resultado.Custo = Math.Round(numCusto.Value, 2);
            Resultado.PrecoVenda = Math.Round(numVenda.Value, 2);
            Resultado.EstoqueMinimo = (int)numMin.Value;
            Resultado.Ativo = chkAtivo.Checked;
            if (numInicial != null) Resultado.Quantidade = (int)numInicial.Value;
        };
    }
}
