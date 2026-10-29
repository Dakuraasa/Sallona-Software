using System.Data;

namespace dashboardforms;

// ===== Página de Serviços: catálogo com preço, duração e categoria =====
// TODO (melhoria futura): quando houver login, só o gerente poderá editar preço,
// desativar ou excluir; funcionários apenas visualizam.
class ServicosPage : UserControl
{
    readonly ServicoRepositorio repo = Repositorios.Servicos;
    readonly DataTable tabela = new();
    readonly DataGridView grid = new();
    readonly Label[] kpiValor = new Label[4];
    readonly Label[] kpiSub = new Label[4];
    readonly Dictionary<string, Button> botoesFiltro = new();
    readonly TextBox busca = new() { BorderStyle = BorderStyle.None, PlaceholderText = "Buscar serviço...", Font = new Font("Segoe UI", 10f), Location = new Point(14, 9), Width = 190 };
    readonly Font fonteNegrito = new("Segoe UI", 9f, FontStyle.Bold);

    // painel de detalhes
    readonly Label lblNome = new() { Dock = DockStyle.Top, Height = 34, ForeColor = Cores.Escuro, BackColor = Color.Transparent, Font = new Font("Segoe UI", 14f, FontStyle.Bold), AutoEllipsis = true };
    readonly Label lblCategoria = new() { Dock = DockStyle.Top, Height = 24, ForeColor = Cores.Primaria, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
    readonly ListaLinhas linhas = new() { Dock = DockStyle.Top, Height = 74 };
    readonly FlowLayoutPanel botoesAcao = new() { Dock = DockStyle.Top, Height = 46, BackColor = Color.White, Padding = new Padding(0, 6, 0, 0), WrapContents = false };
    readonly BotaoAnimado btnEditar = NovoBotao("Editar", Cores.Primaria, Color.White);
    readonly BotaoAnimado btnAtivar = NovoBotao("Desativar", Cores.Fundo, Cores.Vinho);
    readonly BotaoAnimado btnExcluir = NovoBotao("Excluir", Color.White, Cores.Coral);
    readonly Label lblDica = new() { Dock = DockStyle.Fill, ForeColor = Cores.Cinza, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8.5f), Padding = new Padding(0, 4, 0, 0) };
    readonly ListaProgresso porCategoria = new() { Dock = DockStyle.Fill };

    string filtro = "Todos";
    bool atualizando;

    public ServicosPage()
    {
        BackColor = Cores.Fundo;
        DoubleBuffered = true;

        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 16),
            BackColor = Cores.Fundo,
            ColumnCount = 2,
            RowCount = 3
        };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cab = ConstruirCabecalho();
        raiz.Controls.Add(cab, 0, 0); raiz.SetColumnSpan(cab, 2);

        var kpis = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Cores.Fundo, Margin = new Padding(0) };
        for (int i = 0; i < 4; i++) kpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        string[] titulos = { "Serviços ativos", "Preço médio", "Duração média", "Categorias" };
        for (int i = 0; i < 4; i++) kpis.Controls.Add(NovoKpi(i, titulos[i]), i, 0);
        raiz.Controls.Add(kpis, 0, 1); raiz.SetColumnSpan(kpis, 2);

        var cTab = NovoCard();
        cTab.Controls.Add(ConstruirGrid());
        cTab.Controls.Add(Titulo("Catálogo de serviços"));
        raiz.Controls.Add(cTab, 0, 2);

        var direita = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Cores.Fundo, Margin = new Padding(0) };
        direita.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        direita.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        direita.Controls.Add(ConstruirDetalhes(), 0, 0);
        var cCat = NovoCard();
        cCat.Controls.Add(porCategoria);
        cCat.Controls.Add(Titulo("Serviços por categoria"));
        direita.Controls.Add(cCat, 0, 1);
        raiz.Controls.Add(direita, 1, 2);

        Controls.Add(raiz);
        AplicarFiltro("Todos");
    }

    // ---------- Cabeçalho: título, busca, filtros e botão de novo serviço ----------
    Control ConstruirCabecalho()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Cores.Fundo };
        var titulo = new Label { Text = "Serviços", Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = Cores.Escuro, AutoSize = true, Location = new Point(0, 8) };

        var direita = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 430, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 14, 0, 0) };
        foreach (var nome in new[] { "Todos", "Ativos", "Inativos" })
        {
            var b = new Button { Text = nome, Size = new Size(80, 34), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 6, 0) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => AplicarFiltro(nome);
            botoesFiltro[nome] = b;
            direita.Controls.Add(b);
        }
        var novo = new BotaoAnimado
        {
            Text = "+  Novo serviço",
            Size = new Size(150, 34),
            BackColor = Cores.Escuro,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };
        novo.Click += (_, _) => NovoServico();
        direita.Controls.Add(novo);

        var caixa = new Card { Raio = 17, Size = new Size(222, 34), Location = new Point(250, 14), Padding = new Padding(0) };
        caixa.Controls.Add(busca);
        busca.TextChanged += (_, _) => Atualizar();

        p.Controls.Add(titulo);
        p.Controls.Add(caixa);
        p.Controls.Add(direita);
        p.Resize += (_, _) => caixa.Left = Math.Max(titulo.Right + 20, p.Width - direita.Width - caixa.Width - 12);
        return p;
    }

    Card NovoKpi(int i, string titulo)
    {
        bool escuro = i == 0;
        var c = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, i == 3 ? 0 : 12, 12), Preenchimento = escuro ? Cores.Escuro : Color.White };
        kpiSub[i] = new Label { Dock = DockStyle.Top, Height = 22, ForeColor = escuro ? Cores.Rosa : Cores.Cinza, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        kpiValor[i] = new Label { Dock = DockStyle.Top, Height = 40, ForeColor = escuro ? Color.White : Cores.Escuro, BackColor = Color.Transparent, Font = new Font("Segoe UI", 20f, FontStyle.Bold) };
        var lt = new Label { Text = titulo, Dock = DockStyle.Top, Height = 24, ForeColor = escuro ? Cores.Rosa : Cores.Cinza, BackColor = Color.Transparent };
        c.Controls.Add(kpiSub[i]); c.Controls.Add(kpiValor[i]); c.Controls.Add(lt);
        return c;
    }

    static Card NovoCard() => new() { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 12) };

    static Label Titulo(string txt) => new()
    {
        Text = txt,
        Dock = DockStyle.Top,
        Height = 32,
        BackColor = Color.Transparent,
        ForeColor = Cores.Escuro,
        Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
    };

    static BotaoAnimado NovoBotao(string texto, Color fundo, Color letra) => new()
    {
        Text = texto,
        Size = new Size(84, 34),
        BackColor = fundo,
        ForeColor = letra,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 6, 0)
    };

    // ---------- Tabela ----------
    DataGridView ConstruirGrid()
    {
        tabela.Columns.Add("Id", typeof(int));
        tabela.Columns.Add("Servico", typeof(string));
        tabela.Columns.Add("Categoria", typeof(string));
        tabela.Columns.Add("Preco", typeof(decimal));
        tabela.Columns.Add("Duracao", typeof(int));
        tabela.Columns.Add("Status", typeof(string));

        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Color.White;
        grid.GridColor = Color.FromArgb(240, 230, 228);
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 34;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.RowTemplate.Height = 36;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Cores.Cinza;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = fonteNegrito;
        grid.DefaultCellStyle.ForeColor = Cores.Texto;
        grid.DefaultCellStyle.SelectionBackColor = Cores.Fundo;
        grid.DefaultCellStyle.SelectionForeColor = Cores.Escuro;

        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Servico", HeaderText = "Serviço", FillWeight = 34 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Categoria", HeaderText = "Categoria", FillWeight = 20 });
        var colPreco = new DataGridViewTextBoxColumn { DataPropertyName = "Preco", HeaderText = "Preço", FillWeight = 16 };
        colPreco.DefaultCellStyle.Format = "C2";
        colPreco.DefaultCellStyle.FormatProvider = Formato.Br;
        grid.Columns.Add(colPreco);
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Duracao", HeaderText = "Duração", FillWeight = 14 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 14 });

        grid.DataSource = tabela;

        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid.Rows[e.RowIndex].DataBoundItem is not DataRowView v) return;
            bool inativo = (string)v["Status"] == "Inativo";
            if (inativo) e.CellStyle.ForeColor = Cores.Cinza;

            string col = grid.Columns[e.ColumnIndex].DataPropertyName;
            if (col == "Duracao" && e.Value is int min)
            {
                e.Value = Formato.Duracao(min);
                e.FormattingApplied = true;
            }
            else if (col == "Status")
            {
                e.CellStyle.ForeColor = inativo ? Cores.Cinza : Cores.Verde;
                e.CellStyle.Font = fonteNegrito;
            }
        };
        grid.SelectionChanged += (_, _) => { if (!atualizando) MostrarDetalhes(); };
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditarSelecionado(); };
        return grid;
    }

    // ---------- Detalhes do serviço selecionado ----------
    Control ConstruirDetalhes()
    {
        var c = NovoCard();
        btnEditar.Click += (_, _) => EditarSelecionado();
        btnAtivar.Click += (_, _) => AlternarAtivo();
        btnExcluir.Click += (_, _) => ExcluirSelecionado();
        botoesAcao.Controls.Add(btnEditar);
        botoesAcao.Controls.Add(btnAtivar);
        botoesAcao.Controls.Add(btnExcluir);

        // Dock.Top empilha de baixo para cima: o último adicionado fica no topo
        c.Controls.Add(lblDica);
        c.Controls.Add(botoesAcao);
        c.Controls.Add(linhas);
        c.Controls.Add(lblCategoria);
        c.Controls.Add(lblNome);
        c.Controls.Add(Titulo("Detalhes"));
        return c;
    }

    Servico? Selecionado()
    {
        if (grid.CurrentRow?.DataBoundItem is not DataRowView v) return null;
        return repo.PorId((int)v["Id"]);
    }

    void MostrarDetalhes()
    {
        var s = Selecionado();
        linhas.Itens.Clear();
        if (s == null)
        {
            lblNome.Text = "Nenhum serviço selecionado";
            lblCategoria.Text = "";
            botoesAcao.Visible = false;
            lblDica.Text = "Clique em um serviço da tabela para ver os detalhes, ou em \"+ Novo serviço\" para cadastrar.";
        }
        else
        {
            lblNome.Text = s.Nome;
            lblCategoria.Text = s.Categoria.ToUpper();
            linhas.Itens.Add(("Preço", Formato.Dinheiro(s.Preco), Cores.Escuro));
            linhas.Itens.Add(("Duração", Formato.Duracao(s.DuracaoMin), Cores.Escuro));
            linhas.Itens.Add(("Situação", s.Ativo ? "Ativo" : "Inativo", s.Ativo ? Cores.Verde : Cores.Cinza));
            botoesAcao.Visible = true;
            btnAtivar.Text = s.Ativo ? "Desativar" : "Reativar";
            btnAtivar.ForeColor = s.Ativo ? Cores.Vinho : Cores.Verde;
            lblDica.Text = s.Ativo
                ? "Serviços desativados não aparecem na hora de agendar, mas continuam no histórico e nos relatórios."
                : "Este serviço está desativado: não aparece na agenda, mas continua no histórico.";
        }
        linhas.Invalidate();
    }

    // ---------- Ações ----------
    void NovoServico()
    {
        using var dlg = new ServicoDialog(null, nome => repo.NomeEmUso(nome, 0));
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        repo.Adicionar(dlg.Resultado);
        busca.Text = "";
        // se o filtro atual esconderia o serviço novo, mostra todos
        if ((filtro == "Ativos") != dlg.Resultado.Ativo && filtro != "Todos") filtro = "Todos";
        AplicarFiltro(filtro, dlg.Resultado.Id);
    }

    void EditarSelecionado()
    {
        var s = Selecionado();
        if (s == null) return;
        using var dlg = new ServicoDialog(s, nome => repo.NomeEmUso(nome, s.Id));
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        repo.Atualizar(dlg.Resultado);
        Atualizar(s.Id);
    }

    void AlternarAtivo()
    {
        var s = Selecionado();
        if (s == null) return;
        repo.DefinirAtivo(s.Id, !s.Ativo);
        Atualizar(s.Id);
    }

    void ExcluirSelecionado()
    {
        var s = Selecionado();
        if (s == null) return;
        if (repo.PossuiAtendimentos(s.Id))
        {
            MessageBox.Show(FindForm(), "Este serviço já foi usado em atendimentos e não pode ser excluído.\nUse \"Desativar\" para tirá-lo da agenda sem perder o histórico.",
                "Sallona", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var r = MessageBox.Show(FindForm(), $"Excluir o serviço \"{s.Nome}\"?\nEssa ação não pode ser desfeita.",
            "Excluir serviço", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (r != DialogResult.Yes) return;
        repo.Excluir(s.Id);
        Atualizar();
    }

    // ---------- Atualização da tela ----------
    void AplicarFiltro(string nome, int? selecionar = null)
    {
        filtro = nome;
        foreach (var (n, b) in botoesFiltro)
        {
            bool ativo = n == nome;
            b.BackColor = ativo ? Cores.Primaria : Color.White;
            b.ForeColor = ativo ? Color.White : Cores.Texto;
        }
        Atualizar(selecionar);
    }

    void Atualizar(int? selecionar = null)
    {
        int? manter = selecionar ?? (Selecionado()?.Id);
        var todos = repo.Todos();
        string termo = busca.Text.Trim();

        var visiveis = todos.Where(s =>
                (filtro == "Todos" || (filtro == "Ativos") == s.Ativo) &&
                (termo.Length == 0 ||
                 s.Nome.Contains(termo, StringComparison.CurrentCultureIgnoreCase) ||
                 s.Categoria.Contains(termo, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();

        atualizando = true;
        tabela.Rows.Clear();
        foreach (var s in visiveis)
            tabela.Rows.Add(s.Id, s.Nome, s.Categoria, s.Preco, s.DuracaoMin, s.Ativo ? "Ativo" : "Inativo");
        if (grid.Rows.Count > 0)
        {
            // volta a selecionar o serviço que estava aberto (ou o primeiro da lista)
            var alvo = grid.Rows.Cast<DataGridViewRow>()
                           .FirstOrDefault(r => r.DataBoundItem is DataRowView v && (int)v["Id"] == manter)
                       ?? grid.Rows[0];
            grid.ClearSelection();
            grid.CurrentCell = alvo.Cells[1];
            alvo.Selected = true;
        }
        atualizando = false;

        MostrarDetalhes();
        AtualizarResumo(todos);
    }

    // Números calculados a partir dos serviços cadastrados (não são fictícios)
    void AtualizarResumo(List<Servico> todos)
    {
        var ativos = todos.Where(s => s.Ativo).ToList();

        kpiValor[0].Text = ativos.Count.ToString();
        kpiSub[0].Text = $"de {todos.Count} cadastrados";

        kpiValor[1].Text = ativos.Count == 0 ? "—" : Formato.Dinheiro(Math.Round(ativos.Average(s => s.Preco), 2));
        kpiSub[1].Text = "entre os serviços ativos";

        kpiValor[2].Text = ativos.Count == 0 ? "—" : Formato.Duracao((int)Math.Round(ativos.Average(s => s.DuracaoMin)));
        kpiSub[2].Text = "tempo médio na agenda";

        var grupos = ativos.GroupBy(s => s.Categoria)
                           .Select(g => (Nome: g.Key, Qtd: g.Count()))
                           .OrderByDescending(g => g.Qtd)
                           .ToList();
        kpiValor[3].Text = grupos.Count.ToString();
        kpiSub[3].Text = grupos.Count == 0 ? "nenhuma ativa" : $"mais comum: {grupos[0].Nome}";

        Color[] paleta = { Cores.Primaria, Cores.Coral, Cores.Vinho, Cores.Rosa };
        porCategoria.Itens.Clear();
        for (int i = 0; i < grupos.Count && i < 5; i++)
        {
            int pct = (int)Math.Round(grupos[i].Qtd * 100.0 / ativos.Count);
            porCategoria.Itens.Add(($"{grupos[i].Nome} ({grupos[i].Qtd})", pct, paleta[i % paleta.Length]));
        }
        porCategoria.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) fonteNegrito.Dispose();
        base.Dispose(disposing);
    }
}
