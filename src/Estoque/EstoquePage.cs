using System.Data;

namespace dashboardforms;

// ===== Página de Estoque: quanto tem de cada produto, entradas, saídas e histórico =====
class EstoquePage : UserControl, IPagina
{
    readonly DataTable tabProdutos = new(), tabMovs = new();
    readonly DataGridView gridProdutos, gridMovs;
    readonly Label[] kpiValor, kpiSub;
    readonly Label tituloHistorico = Ui.Titulo("Histórico de movimentações");
    readonly CheckBox chkSoBaixo = new() { Text = "Só estoque baixo", AutoSize = true, ForeColor = Cores.Texto, Margin = new Padding(0, 8, 8, 0) };
    readonly CheckBox chkSoSelecionado = new() { Text = "Só do produto selecionado", AutoSize = true, ForeColor = Cores.Texto, Dock = DockStyle.Top, Height = 26, BackColor = Color.Transparent };
    readonly Label avisoVazio = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Cores.Cinza, BackColor = Color.Transparent, Visible = false };
    string busca = "";
    bool atualizando;

    public EstoquePage()
    {
        BackColor = Cores.Fundo;
        DoubleBuffered = true;

        foreach (var (n, t) in new[] { ("Id", typeof(int)), ("Produto", typeof(string)), ("Categoria", typeof(string)),
                                         ("Em estoque", typeof(int)), ("Mínimo", typeof(int)), ("Un.", typeof(string)),
                                         ("Valor (custo)", typeof(decimal)), ("Situação", typeof(string)) })
            tabProdutos.Columns.Add(n, t);
        foreach (var (n, t) in new[] { ("Id", typeof(int)), ("Data", typeof(DateTime)), ("Produto", typeof(string)), ("Tipo", typeof(string)),
                                         ("Qtd.", typeof(string)), ("Saldo", typeof(int)), ("Motivo", typeof(string)) })
            tabMovs.Columns.Add(n, t);

        gridProdutos = Ui.Tabela(tabProdutos);
        Ui.ColorirSituacao(gridProdutos, "Situação", ProdutosPage.CorSituacao);
        gridProdutos.SelectionChanged += (_, _) => { if (!atualizando && chkSoSelecionado.Checked) PreencherHistorico(); };
        gridProdutos.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Movimentar(TipoMovimento.Entrada); };

        gridMovs = Ui.Tabela(tabMovs);
        Ui.ColorirSituacao(gridMovs, "Tipo", s => s == TipoMovimento.Entrada ? Cores.Verde : s == TipoMovimento.Saida ? Cores.Vinho : Cores.Primaria);

        chkSoBaixo.CheckedChanged += (_, _) => Recarregar();
        chkSoSelecionado.CheckedChanged += (_, _) => PreencherHistorico();

        var entrada = Ui.Botao("+ Entrada", Cores.Verde, Color.White, 96);
        var saida = Ui.Botao("− Saída", Cores.Primaria, Color.White, 86);
        var ajuste = Ui.Botao("Ajustar", Cores.Escuro, Color.White, 86);
        entrada.Click += (_, _) => Movimentar(TipoMovimento.Entrada);
        saida.Click += (_, _) => Movimentar(TipoMovimento.Saida);
        ajuste.Click += (_, _) => Movimentar(TipoMovimento.Ajuste);

        var raiz = Ui.Raiz(64, 124, 0);
        raiz.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, 55);
        raiz.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 45);
        var cab = Ui.Cabecalho("Estoque", Ui.Busca("Buscar produto...", t => { busca = t; Recarregar(); }, 170), chkSoBaixo, entrada, saida, ajuste);
        raiz.Controls.Add(cab, 0, 0); raiz.SetColumnSpan(cab, 2);

        var kpis = Ui.Kpis(new[] { "Estoque baixo", "Valor em estoque", "Entradas no mês", "Saídas no mês" }, out kpiValor, out kpiSub);
        raiz.Controls.Add(kpis, 0, 1); raiz.SetColumnSpan(kpis, 2);

        var cProd = Ui.Cartao();
        cProd.Controls.Add(gridProdutos);
        cProd.Controls.Add(avisoVazio);
        cProd.Controls.Add(Ui.Titulo("Produtos em estoque"));
        cProd.Controls.Add(Ui.Dica("Selecione um produto e use Entrada, Saída ou Ajustar (corrigir a contagem). Duplo clique = entrada."));
        raiz.Controls.Add(cProd, 0, 2);

        var cHist = Ui.Cartao();
        cHist.Controls.Add(gridMovs);
        cHist.Controls.Add(chkSoSelecionado);
        cHist.Controls.Add(tituloHistorico);
        raiz.Controls.Add(cHist, 1, 2);

        Controls.Add(raiz);
        Recarregar();
    }

    public void Recarregar()
    {
        int? manter = Ui.IdSelecionado(gridProdutos);
        var ativos = Repositorios.Produtos.Todos().Where(p => p.Ativo).OrderBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

        atualizando = true;
        tabProdutos.Rows.Clear();
        foreach (var p in ativos.Where(p => (!chkSoBaixo.Checked || p.EstoqueBaixo) &&
                                            (busca.Length == 0 || p.Nome.Contains(busca, StringComparison.CurrentCultureIgnoreCase))))
            tabProdutos.Rows.Add(p.Id, p.Nome, p.Categoria, p.Quantidade, p.EstoqueMinimo, p.Unidade, p.Custo * Math.Max(0, p.Quantidade), p.Situacao);
        Ui.Selecionar(gridProdutos, manter);
        atualizando = false;

        avisoVazio.Text = ativos.Count == 0
            ? "Nenhum produto ativo.\nCadastre os produtos na página Produtos."
            : "Nenhum produto encontrado com esse filtro.";
        avisoVazio.Visible = tabProdutos.Rows.Count == 0;
        gridProdutos.Visible = !avisoVazio.Visible;

        // KPIs
        var mes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var movsMes = Repositorios.Estoque.Todos().Where(m => m.Data >= mes).ToList();
        int baixos = ativos.Count(p => p.EstoqueBaixo);
        kpiValor[0].Text = baixos.ToString(); kpiSub[0].Text = baixos == 0 ? "nada para repor" : "produto(s) para repor";
        kpiValor[1].Text = Formato.Dinheiro(ativos.Sum(p => p.Custo * Math.Max(0, p.Quantidade))); kpiSub[1].Text = "pelo preço de custo";
        kpiValor[2].Text = movsMes.Where(m => m.Tipo == TipoMovimento.Entrada).Sum(m => m.Quantidade).ToString(); kpiSub[2].Text = "unidades que entraram";
        kpiValor[3].Text = (-movsMes.Where(m => m.Tipo == TipoMovimento.Saida).Sum(m => m.Quantidade)).ToString(); kpiSub[3].Text = "unidades que saíram";

        PreencherHistorico();
    }

    void PreencherHistorico()
    {
        int? prod = chkSoSelecionado.Checked ? Ui.IdSelecionado(gridProdutos) : null;
        var movs = Repositorios.Estoque.Todos()
            .Where(m => prod == null || m.ProdutoId == prod)
            .OrderByDescending(m => m.Data).Take(300);
        tabMovs.Rows.Clear();
        foreach (var m in movs)
            tabMovs.Rows.Add(m.Id, m.Data, m.ProdutoNome, m.Tipo, m.Quantidade > 0 ? $"+{m.Quantidade}" : m.Quantidade.ToString(),
                             m.SaldoApos, m.Observacao.Length > 0 ? $"{m.Motivo} — {m.Observacao}" : m.Motivo);
        tituloHistorico.Text = tabMovs.Rows.Count == 0 ? "Histórico de movimentações (vazio)" : "Histórico de movimentações";
    }

    void Movimentar(string tipo)
    {
        var p = Ui.IdSelecionado(gridProdutos) is int id ? Repositorios.Produtos.PorId(id) : null;
        if (p == null)
        {
            MessageBox.Show(FindForm(), "Selecione um produto na tabela primeiro.", "Estoque", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new MovimentoDialog(p, tipo);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK) Recarregar();
    }
}

// ===== Janela de entrada / saída / ajuste de um produto =====
class MovimentoDialog : Formulario
{
    public MovimentoDialog(Produto p, string tipo)
        : base(tipo == TipoMovimento.Ajuste ? "Ajustar contagem" : tipo == TipoMovimento.Entrada ? "Entrada no estoque" : "Saída do estoque",
               "Confirmar")
    {
        AdicionarControle(new Label
        {
            Text = $"{p.Nome}\nEm estoque agora: {p.Quantidade} {p.Unidade}",
            AutoSize = true, ForeColor = Cores.Primaria, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4)
        });

        string rotulo = tipo == TipoMovimento.Ajuste ? $"Quantidade contada na prateleira ({p.Unidade})"
                      : tipo == TipoMovimento.Entrada ? $"Quantidade que entrou ({p.Unidade})" : $"Quantidade que saiu ({p.Unidade})";
        var num = AdicionarCampo(rotulo, new NumericUpDown { Maximum = 100000, Minimum = 0 });
        num.Value = tipo == TipoMovimento.Ajuste ? Math.Max(0, p.Quantidade) : 1;

        ComboBox? cbMotivo = null;
        if (tipo != TipoMovimento.Ajuste)
        {
            cbMotivo = AdicionarCampo("Motivo", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            cbMotivo.Items.AddRange(tipo == TipoMovimento.Entrada ? TipoMovimento.MotivosEntrada : TipoMovimento.MotivosSaida);
            cbMotivo.SelectedIndex = 0;
        }
        var txtObs = AdicionarCampo("Observação (opcional)", NovaCaixaTexto(120));
        if (tipo == TipoMovimento.Ajuste) txtObs.PlaceholderText = "ex.: contagem de fim de mês";

        AoSalvar = () =>
        {
            string motivo = cbMotivo?.SelectedItem as string ?? "Contagem / correção";
            // Movimentar valida (quantidade, estoque suficiente) e lança InvalidOperationException com a mensagem
            Repositorios.Estoque.Movimentar(p.Id, tipo, (int)num.Value, motivo, txtObs.Text);
        };
    }
}
