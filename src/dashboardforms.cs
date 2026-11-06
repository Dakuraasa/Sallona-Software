using System.Data;
using System.Text;
using dashboardforms.Pagamentos;

namespace dashboardforms;

// ===== Janela principal: menu lateral + área onde as páginas aparecem =====
// Abre depois do login. Todos os números do Dashboard são CALCULADOS a partir do que
// a conta logada cadastrou (clientes, serviços, atendimentos) — nada é inventado.
public class DashboardForm : Form
{
    readonly Usuario usuario;

    /// <summary>true quando a pessoa clicou em "Sair" (o Program volta para a tela de login).</summary>
    public bool SaiuDaConta { get; private set; }

    static readonly string[] Periodos = { "Dia", "Semana", "Mês", "Ano" };
    string periodoAtual = "Mês";

    readonly Label[] kpiValor = new Label[4];
    readonly Label[] kpiVar = new Label[4];
    readonly GraficoBarras grafico = new() { Dock = DockStyle.Fill, Vazio = "Nenhum atendimento pago neste período" };
    readonly ListaProgresso maisRealizados = new() { Dock = DockStyle.Fill, Vazio = "Nenhum atendimento neste mês ainda" };
    readonly ListaLinhas destaques = new() { Dock = DockStyle.Fill };
    readonly Label tituloGrafico = new();
    readonly Dictionary<string, Button> botoesPeriodo = new();
    readonly Dictionary<string, Button> botoesMenu = new();
    readonly DataTable tabela = new();
    readonly DataGridView grid = new();
    readonly Label gridVazio = new();
    readonly Font fonteStatus = new("Segoe UI", 9f, FontStyle.Bold);

    // cartão "Meta do mês" (sidebar)
    readonly Label metaPct = new(), metaTexto = new();
    readonly Panel metaPreenchida = new() { Dock = DockStyle.Left, BackColor = Cores.Primaria };
    readonly Panel metaBarra = new() { Dock = DockStyle.Top, Height = 10, BackColor = Color.FromArgb(70, 30, 38) };
    int metaPorcento;

    // Área onde as páginas são trocadas quando se clica no menu
    readonly Panel areaPaginas = new() { Dock = DockStyle.Fill, BackColor = Cores.Fundo };
    readonly Dictionary<string, Control> paginas = new();

    public DashboardForm(Usuario usuario)
    {
        this.usuario = usuario;
        periodoAtual = Periodos.Contains(Repositorios.Configuracao.PeriodoPadrao) ? Repositorios.Configuracao.PeriodoPadrao : "Mês";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Cores.Fundo;
        MinimumSize = new Size(1100, 680);
        Size = new Size(1320, 780);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        paginas["Dashboard"] = ConstruirConteudo();
        Controls.Add(areaPaginas);
        Controls.Add(ConstruirSidebar());

        MostrarPagina("Dashboard");
    }

    // =====================================================================
    // Sidebar
    // =====================================================================
    Control ConstruirSidebar()
    {
        var sb = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = Color.White, Padding = new Padding(14, 10, 14, 14) };

        var logoPath = Path.Combine(AppContext.BaseDirectory, "logo.jpg");
        var logo = new PictureBox { Dock = DockStyle.Top, Height = 96, SizeMode = PictureBoxSizeMode.Zoom };
        if (File.Exists(logoPath)) logo.Image = Image.FromFile(logoPath);

        // alturas compactas para caber tudo (inclusive "Sair") em telas de notebook de 768px
        var menu = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 8 * 40 + 8, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        foreach (var (icone, nome) in new[] { ("📊", "Dashboard"), ("👥", "Clientes"), ("✂", "Serviços"), ("💇", "Profissionais"),
                                              ("💰", "Financeiro"), ("📦", "Estoque"), ("🛍", "Produtos"), ("📖", "Agenda") })
            menu.Controls.Add(NovoBotaoMenu(icone, nome));

        // Meta do mês: quem define é o usuário (clicando no cartão)
        var meta = new Card { Dock = DockStyle.Bottom, Height = 120, Preenchimento = Cores.Escuro, Margin = new Padding(0), Cursor = Cursors.Hand };
        var lm = new Label { Text = "Meta do mês  ✎", ForeColor = Cores.Rosa, Dock = DockStyle.Top, Height = 22, BackColor = Color.Transparent };
        metaPct.ForeColor = Color.White; metaPct.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
        metaPct.Dock = DockStyle.Top; metaPct.Height = 40; metaPct.BackColor = Color.Transparent;
        metaBarra.Controls.Add(metaPreenchida);
        metaBarra.Resize += (_, _) => metaPreenchida.Width = metaBarra.Width * Math.Min(100, metaPorcento) / 100;
        metaTexto.ForeColor = Color.White; metaTexto.Dock = DockStyle.Top; metaTexto.Height = 24;
        metaTexto.BackColor = Color.Transparent; metaTexto.Padding = new Padding(0, 6, 0, 0);
        meta.Controls.Add(metaTexto); meta.Controls.Add(metaBarra); meta.Controls.Add(metaPct); meta.Controls.Add(lm);
        foreach (Control c in new Control[] { meta, lm, metaPct, metaBarra, metaPreenchida, metaTexto })
        {
            c.Cursor = Cursors.Hand;
            c.Click += (_, _) => DefinirMeta();
        }

        var rodape = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 84, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        rodape.Controls.Add(NovoBotaoMenu("⚙", "Configurações"));
        rodape.Controls.Add(NovoBotaoMenu("⏻", "Sair"));

        sb.Controls.Add(menu);
        sb.Controls.Add(logo);
        sb.Controls.Add(rodape);
        sb.Controls.Add(meta);
        return sb;
    }

    Button NovoBotaoMenu(string icone, string nome)
    {
        var b = new Button
        {
            Text = $"{icone}   {nome}",
            Size = new Size(210, 36),
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Color.White,
            ForeColor = Cores.Texto,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 4)
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Cores.Fundo;
        b.Click += (_, _) =>
        {
            if (nome == "Sair") { Sair(); return; }
            MostrarPagina(nome);
        };
        botoesMenu[nome] = b;
        return b;
    }

    void Sair()
    {
        var r = MessageBox.Show(this, "Deseja sair da sua conta?", "Sair", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (r != DialogResult.Yes) return;
        SaiuDaConta = true;   // o Program mostra a tela de login de novo
        Close();
    }

    void MarcarMenu(string nome)
    {
        foreach (var (n, b) in botoesMenu)
        {
            bool ativo = n == nome;
            b.BackColor = ativo ? Cores.Escuro : Color.White;
            b.ForeColor = ativo ? Color.White : Cores.Texto;
            b.FlatAppearance.MouseOverBackColor = ativo ? Cores.Escuro : Cores.Fundo;
        }
    }

    void DefinirMeta()
    {
        using var dlg = new MetaDialog(Repositorios.Configuracao.MetaMensal);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (Armazenamento.Tentar(this, () => Repositorios.Configuracao.DefinirMeta(dlg.Valor)))
            AtualizarMeta(Repositorios.Atendimentos.Todos());
    }

    // =====================================================================
    // Navegação entre páginas
    // =====================================================================
    void MostrarPagina(string nome)
    {
        if (!paginas.TryGetValue(nome, out var pagina))
        {
            // As páginas são criadas só na primeira vez que são abertas
            pagina = nome switch
            {
                "Clientes" => new ClientesPage { Dock = DockStyle.Fill },
                "Serviços" => new ServicosPage { Dock = DockStyle.Fill },
                "Profissionais" => new ProfissionaisPage { Dock = DockStyle.Fill },
                "Produtos" => new ProdutosPage { Dock = DockStyle.Fill },
                "Estoque" => new EstoquePage { Dock = DockStyle.Fill },
                "Financeiro" => new FinanceiroPage { Dock = DockStyle.Fill },
                "Configurações" => NovaPaginaConfiguracoes(),
                _ => PaginaEmBreve(nome)   // páginas que outros membros do grupo ainda vão fazer
            };
            paginas[nome] = pagina;
        }
        else if (pagina is IPagina p)
        {
            p.Recarregar();   // os dados podem ter mudado em outra página
        }

        if (nome == "Dashboard") AtualizarDashboard();
        Exibir(pagina, nome, nome);
    }

    Control NovaPaginaConfiguracoes()
    {
        var p = new ConfiguracoesPage(usuario) { Dock = DockStyle.Fill };
        p.Desconectar += Sair;                                       // botão "Desconectar" (pergunta antes)
        p.SessaoEncerrada += () => { SaiuDaConta = true; Close(); }; // backup restaurado / conta excluída
        p.ContaAlterada += () => Text = $"Sallona - Configurações  ·  {NomeExibido()}";
        return p;
    }

    string NomeExibido() => string.IsNullOrWhiteSpace(usuario.NomeSalao) ? usuario.Nome : usuario.NomeSalao;

    // Coloca um controle na área de páginas. "menu" é o item da sidebar que fica destacado.
    void Exibir(Control pagina, string titulo, string menu)
    {
        var anteriores = areaPaginas.Controls.Cast<Control>().Where(c => c != pagina).ToList();

        areaPaginas.SuspendLayout();
        areaPaginas.Controls.Clear();   // só remove da tela; as páginas guardadas em "paginas" continuam vivas
        areaPaginas.Controls.Add(pagina);
        areaPaginas.ResumeLayout();

        // Telas temporárias (ex.: Pagamento) não ficam guardadas: libera depois que o clique atual terminar
        foreach (var c in anteriores)
            if (!paginas.ContainsValue(c)) BeginInvoke(new Action(c.Dispose));

        Text = $"Sallona - {titulo}  ·  {NomeExibido()}";
        MarcarMenu(menu);
    }

    static Control PaginaEmBreve(string nome)
    {
        var fundo = new Panel { Dock = DockStyle.Fill, BackColor = Cores.Fundo, Padding = new Padding(20, 16, 20, 16) };
        var titulo = new Label { Text = nome, Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = Cores.Escuro, AutoSize = true, Location = new Point(20, 24) };

        var card = new Card { Size = new Size(420, 180), Padding = new Padding(24) };
        var msg = new Label
        {
            Text = "Esta página ainda está em desenvolvimento.",
            Dock = DockStyle.Fill,
            ForeColor = Cores.Cinza,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.TopCenter,
            Font = new Font("Segoe UI", 10f)
        };
        var cabecalho = new Label
        {
            Text = "Em breve",
            Dock = DockStyle.Top,
            Height = 64,
            ForeColor = Cores.Primaria,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold)
        };
        card.Controls.Add(msg);
        card.Controls.Add(cabecalho);

        fundo.Controls.Add(titulo);
        fundo.Controls.Add(card);
        fundo.Resize += (_, _) => card.Location = new Point((fundo.Width - card.Width) / 2, (fundo.Height - card.Height) / 2);
        return fundo;
    }

    // =====================================================================
    // Conteúdo do Dashboard
    // =====================================================================
    Control ConstruirConteudo()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 16),
            BackColor = Cores.Fundo,
            ColumnCount = 2,
            RowCount = 4
        };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var cab = ConstruirCabecalho();
        raiz.Controls.Add(cab, 0, 0); raiz.SetColumnSpan(cab, 2);

        var kpis = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Cores.Fundo, Margin = new Padding(0) };
        for (int i = 0; i < 4; i++) kpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        string[] titulos = { "Faturamento", "Atendimentos", "Novos clientes", "Ticket médio" };
        for (int i = 0; i < 4; i++) kpis.Controls.Add(NovoKpi(i, titulos[i]), i, 0);
        raiz.Controls.Add(kpis, 0, 1); raiz.SetColumnSpan(kpis, 2);

        var cGraf = NovoCard();
        cGraf.Controls.Add(grafico);
        EstiloTitulo(tituloGrafico, "Faturamento");
        cGraf.Controls.Add(tituloGrafico);
        raiz.Controls.Add(cGraf, 0, 2);

        var cServ = NovoCard();
        cServ.Controls.Add(maisRealizados);
        cServ.Controls.Add(Titulo("Serviços mais realizados (% do mês)"));
        raiz.Controls.Add(cServ, 1, 2);

        var cTab = NovoCard();
        cTab.Controls.Add(ConstruirGrid());
        cTab.Controls.Add(gridVazio);
        cTab.Controls.Add(Titulo("Últimos atendimentos"));
        cTab.Controls.Add(new Label
        {
            Text = "Clique duas vezes em um atendimento pendente para registrar o pagamento (botão direito: mais opções).",
            Dock = DockStyle.Bottom,
            Height = 22,
            ForeColor = Cores.Cinza,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.BottomLeft
        });
        raiz.Controls.Add(cTab, 0, 3);

        var cAl = NovoCard();
        cAl.Controls.Add(destaques);
        cAl.Controls.Add(Titulo("Alertas e destaques"));
        raiz.Controls.Add(cAl, 1, 3);

        return raiz;
    }

    Control ConstruirCabecalho()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Cores.Fundo };

        var titulo = new Label
        {
            Text = "Dashboard",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = Cores.Escuro,
            AutoSize = true,
            Location = new Point(0, 8)
        };

        var direita = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 500, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 14, 0, 0) };
        foreach (var nome in Periodos)
        {
            var b = new Button { Text = nome, Size = new Size(76, 34), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 6, 0) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => { periodoAtual = nome; AtualizarDashboard(); };
            botoesPeriodo[nome] = b;
            direita.Controls.Add(b);
        }
        var novo = new BotaoAnimado
        {
            Text = "+  Novo atendimento",
            Size = new Size(160, 34),
            BackColor = Cores.Escuro,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };
        novo.Click += (_, _) => NovoAtendimento();
        direita.Controls.Add(novo);

        var busca = new TextBox { BorderStyle = BorderStyle.None, PlaceholderText = "Buscar cliente...", Font = new Font("Segoe UI", 10f), Location = new Point(14, 9), Width = 190 };
        var caixa = new Card { Raio = 17, Size = new Size(222, 34), Location = new Point(250, 14), Padding = new Padding(0) };
        caixa.Controls.Add(busca);
        busca.TextChanged += (_, _) =>
        {
            var t = busca.Text.Trim();
            tabela.DefaultView.RowFilter = t.Length == 0 ? "" : $"Cliente LIKE '%{EscaparLike(t)}%'";
            AtualizarAvisoGrid();
        };

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
        Color txt = escuro ? Color.White : Cores.Escuro;
        kpiVar[i] = new Label { Dock = DockStyle.Top, Height = 22, ForeColor = Cores.Verde, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        kpiValor[i] = new Label { Dock = DockStyle.Top, Height = 40, ForeColor = txt, BackColor = Color.Transparent, Font = new Font("Segoe UI", 20f, FontStyle.Bold) };
        var lt = new Label { Text = titulo, Dock = DockStyle.Top, Height = 24, ForeColor = escuro ? Cores.Rosa : Cores.Cinza, BackColor = Color.Transparent };
        c.Controls.Add(kpiVar[i]); c.Controls.Add(kpiValor[i]); c.Controls.Add(lt);
        return c;
    }

    Card NovoCard() => new() { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 12) };

    static Label Titulo(string txt) => EstiloTitulo(new Label(), txt);

    static Label EstiloTitulo(Label l, string txt)
    {
        l.Text = txt;
        l.Dock = DockStyle.Top;
        l.Height = 32;
        l.BackColor = Color.Transparent;
        l.ForeColor = Cores.Escuro;
        l.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        return l;
    }

    DataGridView ConstruirGrid()
    {
        tabela.Columns.Add("Data", typeof(DateTime));
        tabela.Columns.Add("Cliente"); tabela.Columns.Add("Serviço"); tabela.Columns.Add("Profissional");
        tabela.Columns.Add("Valor", typeof(decimal)); tabela.Columns.Add("Pagamento"); tabela.Columns.Add("Status");
        tabela.Columns.Add("Id", typeof(int));   // id do atendimento (coluna escondida)

        grid.Dock = DockStyle.Fill;
        grid.DataSource = tabela.DefaultView;
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
        grid.RowTemplate.Height = 34;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Cores.Cinza;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = fonteStatus;
        grid.DefaultCellStyle.ForeColor = Cores.Texto;
        grid.DefaultCellStyle.SelectionBackColor = Cores.Fundo;
        grid.DefaultCellStyle.SelectionForeColor = Cores.Escuro;

        grid.DataBindingComplete += (_, _) =>
        {
            if (grid.Columns["Id"] is { } id) id.Visible = false;
            if (grid.Columns["Data"] is { } d) { d.DefaultCellStyle.Format = "dd/MM HH:mm"; d.FillWeight = 70; }
            if (grid.Columns["Valor"] is { } v) { v.DefaultCellStyle.Format = "C2"; v.DefaultCellStyle.FormatProvider = Formato.Br; v.FillWeight = 70; }
        };
        grid.CellFormatting += (_, e) =>
        {
            if (e.CellStyle != null && grid.Columns[e.ColumnIndex].Name == "Status" && e.Value is string s)
            {
                e.CellStyle.ForeColor = s == "Pago" ? Cores.Verde : Cores.Vinho;
                e.CellStyle.Font = fonteStatus;   // fonte criada uma vez só (criar aqui vazaria memória)
            }
        };
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && AtendimentoDaLinha(e.RowIndex) is { } a) AbrirPagamento(a);
        };

        // botão direito: registrar pagamento / excluir
        var menu = new ContextMenuStrip();
        var itemPagar = menu.Items.Add("Registrar pagamento", null, (_, _) => { if (Selecionado() is { } a) AbrirPagamento(a); });
        menu.Items.Add("Excluir atendimento", null, (_, _) => ExcluirSelecionado());
        menu.Opening += (_, e) =>
        {
            var a = Selecionado();
            if (a == null) { e.Cancel = true; return; }
            itemPagar.Enabled = !a.Pago;
        };
        grid.ContextMenuStrip = menu;
        grid.CellMouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                grid.CurrentCell = grid.Rows[e.RowIndex].Cells[1];
        };

        gridVazio.Dock = DockStyle.Fill;
        gridVazio.TextAlign = ContentAlignment.MiddleCenter;
        gridVazio.ForeColor = Cores.Cinza;
        gridVazio.BackColor = Color.Transparent;
        gridVazio.Visible = false;
        return grid;
    }

    Atendimento? AtendimentoDaLinha(int linha) =>
        grid.Rows[linha].DataBoundItem is DataRowView v
            ? Repositorios.Atendimentos.Todos().FirstOrDefault(a => a.Id == (int)v["Id"])
            : null;

    Atendimento? Selecionado() => grid.CurrentRow == null ? null : AtendimentoDaLinha(grid.CurrentRow.Index);

    // =====================================================================
    // Ações
    // =====================================================================
    void NovoAtendimento()
    {
        var clientes = Repositorios.Clientes.Todos();
        var servicos = Repositorios.Servicos.Todos().Where(s => s.Ativo).ToList();
        var profissionais = Repositorios.Profissionais.Ativos();

        // o que falta cadastrar antes de registrar um atendimento
        var (falta, oque) = clientes.Count == 0 ? ("Clientes", "um cliente")
                          : servicos.Count == 0 ? ("Serviços", "um serviço ativo")
                          : profissionais.Count == 0 ? ("Profissionais", "um profissional ativo")
                          : ("", "");
        if (falta.Length > 0)
        {
            if (MessageBox.Show(this, $"Antes de registrar um atendimento, cadastre pelo menos {oque}.\n\nAbrir a página de {falta} agora?",
                    "Novo atendimento", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                MostrarPagina(falta);
            return;
        }

        using var dlg = new AtendimentoDialog(clientes, servicos, profissionais);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!Armazenamento.Tentar(this, () => Repositorios.Atendimentos.Adicionar(dlg.Resultado))) return;
        AtualizarDashboard();

        if (MessageBox.Show(this, "Atendimento registrado como pendente.\n\nRegistrar o pagamento agora?", "Novo atendimento",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            AbrirPagamento(dlg.Resultado);
    }

    void ExcluirSelecionado()
    {
        var a = Selecionado();
        if (a == null) return;
        string aviso = a.Pago ? "\n\nAtenção: ele já foi pago, e o valor sairá do faturamento." : "";
        var r = MessageBox.Show(this, $"Excluir o atendimento de {a.ClienteNome} ({a.Descricao})?{aviso}",
            "Excluir atendimento", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (r != DialogResult.Yes) return;
        if (Armazenamento.Tentar(this, () => Repositorios.Atendimentos.Excluir(a.Id)))
            AtualizarDashboard();
    }

    // ---------- Pagamento de um atendimento pendente ----------
    void AbrirPagamento(Atendimento a)
    {
        if (a.Pago) return;

        // descrição de cada serviço vem do catálogo (categoria · duração)
        var catalogo = Repositorios.Servicos.Todos();
        var itens = a.Itens.Select(i =>
        {
            var c = catalogo.FirstOrDefault(x => string.Equals(x.Nome, i.Servico, StringComparison.CurrentCultureIgnoreCase));
            string detalhe = c == null ? "" : $"{c.Categoria} · {Formato.Duracao(c.DuracaoMin)}";
            return new PagamentoPage.ServicoRealizado(i.Servico, detalhe, i.Valor);
        });

        var pagina = new PagamentoPage { Dock = DockStyle.Fill };
        pagina.Carregar(a.ClienteNome, a.Data, a.Profissional, itens);
        pagina.VoltarSolicitado += () => MostrarPagina("Dashboard");
        pagina.AoConfirmar = (forma, _) => Armazenamento.Tentar(this, () => Repositorios.Atendimentos.RegistrarPagamento(a.Id, forma));
        Exibir(pagina, "Pagamento", "Dashboard");
    }

    // =====================================================================
    // Cálculo dos números (tudo a partir dos dados da conta)
    // =====================================================================
    record Periodo(DateTime Inicio, DateTime Fim, DateTime InicioAnterior, string[] Rotulos, Func<DateTime, int> Fatia, int FatiaAtual, string Nome);

    static Periodo CalcularPeriodo(string nome, DateTime agora)
    {
        var hoje = agora.Date;
        switch (nome)
        {
            case "Dia":
            {
                // fatias de 2 em 2 horas, das 8h às 20h (antes das 8h conta na primeira, depois das 20h na última)
                int Fatia(DateTime d) => Math.Clamp((d.Hour - 8) / 2, 0, 6);
                return new(hoje, hoje.AddDays(1), hoje.AddDays(-1),
                    new[] { "8h", "10h", "12h", "14h", "16h", "18h", "20h" }, Fatia, Fatia(agora), "hoje");
            }
            case "Semana":
            {
                int desdeSegunda = ((int)hoje.DayOfWeek + 6) % 7;
                var seg = hoje.AddDays(-desdeSegunda);
                return new(seg, seg.AddDays(7), seg.AddDays(-7),
                    new[] { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom" }, d => (d.Date - seg).Days, desdeSegunda, "nesta semana");
            }
            case "Ano":
            {
                var ini = new DateTime(hoje.Year, 1, 1);
                return new(ini, ini.AddYears(1), ini.AddYears(-1),
                    new[] { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" },
                    d => d.Month - 1, hoje.Month - 1, "neste ano");
            }
            default:   // Mês
            {
                var ini = new DateTime(hoje.Year, hoje.Month, 1);
                int semanas = (DateTime.DaysInMonth(hoje.Year, hoje.Month) + 6) / 7;
                var rotulos = Enumerable.Range(1, semanas).Select(i => $"Sem {i}").ToArray();
                return new(ini, ini.AddMonths(1), ini.AddMonths(-1), rotulos, d => (d.Day - 1) / 7, (hoje.Day - 1) / 7, "neste mês");
            }
        }
    }

    void AtualizarDashboard()
    {
        var atendimentos = Repositorios.Atendimentos.Todos();
        var clientes = Repositorios.Clientes.Todos();
        var per = CalcularPeriodo(periodoAtual, DateTime.Now);

        bool Dentro(DateTime d, DateTime ini, DateTime fim) => d >= ini && d < fim;
        var atuais = atendimentos.Where(a => Dentro(a.Data, per.Inicio, per.Fim)).ToList();
        var anteriores = atendimentos.Where(a => Dentro(a.Data, per.InicioAnterior, per.Inicio)).ToList();

        // ---- KPIs ----
        decimal fat = atuais.Where(a => a.Pago).Sum(a => a.Total);
        decimal fatAnt = anteriores.Where(a => a.Pago).Sum(a => a.Total);
        int qtd = atuais.Count, qtdAnt = anteriores.Count;
        int novos = clientes.Count(c => Dentro(c.Cadastro, per.Inicio, per.Fim));
        int novosAnt = clientes.Count(c => Dentro(c.Cadastro, per.InicioAnterior, per.Inicio));
        int pagos = atuais.Count(a => a.Pago), pagosAnt = anteriores.Count(a => a.Pago);
        decimal ticket = pagos == 0 ? 0 : fat / pagos;
        decimal ticketAnt = pagosAnt == 0 ? 0 : fatAnt / pagosAnt;

        MostrarKpi(0, Formato.Dinheiro(fat), VariacaoPercentual(fat, fatAnt));
        MostrarKpi(1, qtd.ToString("N0", Formato.Br), VariacaoAbsoluta(qtd, qtdAnt));
        MostrarKpi(2, novos.ToString("N0", Formato.Br), VariacaoAbsoluta(novos, novosAnt));
        MostrarKpi(3, Formato.Dinheiro(Math.Round(ticket, 2)), VariacaoPercentual(ticket, ticketAnt));

        // ---- Gráfico de faturamento por fatia do período ----
        var valores = new float[per.Rotulos.Length];
        foreach (var a in atuais.Where(a => a.Pago))
        {
            int i = per.Fatia(a.Data);
            if (i >= 0 && i < valores.Length) valores[i] += (float)a.Total;
        }
        grafico.Rotulos = per.Rotulos;
        grafico.Valores = valores;
        grafico.Destaque = per.FatiaAtual;
        grafico.Animar();   // as barras "crescem" ao trocar de período
        tituloGrafico.Text = $"Faturamento {per.Nome}";

        // ---- Serviços mais realizados no mês ----
        var mes = CalcularPeriodo("Mês", DateTime.Now);
        var itensMes = atendimentos.Where(a => Dentro(a.Data, mes.Inicio, mes.Fim)).SelectMany(a => a.Itens).ToList();
        Color[] paleta = { Cores.Primaria, Cores.Coral, Cores.Vinho, Cores.Rosa };
        maisRealizados.Itens.Clear();
        foreach (var (g, i) in itensMes.GroupBy(x => x.Servico, StringComparer.CurrentCultureIgnoreCase)
                                       .OrderByDescending(g => g.Count()).Take(5).Select((g, i) => (g, i)))
            maisRealizados.Itens.Add(($"{g.Key} ({g.Count()})", (int)Math.Round(g.Count() * 100.0 / itensMes.Count), paleta[i % paleta.Length]));
        maisRealizados.Invalidate();

        // ---- Alertas e destaques ----
        destaques.Itens.Clear();
        var pendentes = atendimentos.Where(a => !a.Pago).OrderBy(a => a.Data).ToList();
        destaques.Itens.Add(($"Pagamentos pendentes ({pendentes.Count})", null, Color.Empty));
        if (pendentes.Count == 0) destaques.Itens.Add(("Nenhum pagamento pendente", "✓", Cores.Verde));
        foreach (var a in pendentes.Take(3)) destaques.Itens.Add((a.ClienteNome, Formato.Dinheiro(a.Total), Cores.Vinho));

        var baixos = Repositorios.Produtos.Todos().Where(p => p.EstoqueBaixo).OrderBy(p => p.Quantidade).ToList();
        if (baixos.Count > 0)
        {
            destaques.Itens.Add(($"Estoque baixo ({baixos.Count})", null, Color.Empty));
            foreach (var p in baixos.Take(3)) destaques.Itens.Add((p.Nome, $"{p.Quantidade} {p.Unidade}", Cores.Coral));
        }

        destaques.Itens.Add(("Profissional do mês", null, Color.Empty));
        var top = atendimentos.Where(a => a.Pago && Dentro(a.Data, mes.Inicio, mes.Fim))
                              .GroupBy(a => a.Profissional, StringComparer.CurrentCultureIgnoreCase)
                              .Select(g => (Nome: g.Key, Total: g.Sum(a => a.Total)))
                              .OrderByDescending(x => x.Total).FirstOrDefault();
        destaques.Itens.Add(top.Nome == null ? ("Sem atendimentos pagos no mês", "—", Cores.Cinza) : (top.Nome, Formato.Dinheiro(top.Total), Cores.Verde));

        destaques.Itens.Add(("Cadastros", null, Color.Empty));
        destaques.Itens.Add(("Clientes", clientes.Count.ToString(), Cores.Escuro));
        destaques.Itens.Add(("Serviços ativos", Repositorios.Servicos.Todos().Count(s => s.Ativo).ToString(), Cores.Escuro));
        destaques.Invalidate();

        PreencherTabela(atendimentos);
        AtualizarMeta(atendimentos);

        foreach (var (nome, b) in botoesPeriodo)
        {
            bool ativo = nome == periodoAtual;
            b.BackColor = ativo ? Cores.Primaria : Color.White;
            b.ForeColor = ativo ? Color.White : Cores.Texto;
        }
    }

    void MostrarKpi(int i, string valor, (string Texto, bool Caiu) variacao)
    {
        kpiValor[i].Text = valor;
        kpiVar[i].Text = variacao.Texto;
        bool semComparacao = variacao.Texto.StartsWith("sem");
        kpiVar[i].ForeColor = semComparacao ? (i == 0 ? Cores.Rosa : Cores.Cinza)
                            : variacao.Caiu ? Cores.Coral
                            : (i == 0 ? Color.FromArgb(120, 220, 160) : Cores.Verde);
    }

    // dinheiro: variação em %
    static (string, bool) VariacaoPercentual(decimal atual, decimal anterior)
    {
        if (anterior == 0) return ("sem dados do período anterior", false);
        decimal pct = (atual - anterior) / anterior * 100;
        return ($"{(pct < 0 ? "▼" : "▲")} {Math.Abs(pct).ToString("0.#", Formato.Br)}% vs. anterior", pct < 0);
    }

    // contagens: variação em números
    static (string, bool) VariacaoAbsoluta(int atual, int anterior)
    {
        if (atual == 0 && anterior == 0) return ("sem dados do período anterior", false);
        int dif = atual - anterior;
        return (dif == 0 ? "= igual ao anterior" : $"{(dif < 0 ? "▼" : "▲")} {Math.Abs(dif)} vs. anterior", dif < 0);
    }

    void AtualizarMeta(List<Atendimento> atendimentos)
    {
        var mes = CalcularPeriodo("Mês", DateTime.Now);
        decimal fatMes = atendimentos.Where(a => a.Pago && a.Data >= mes.Inicio && a.Data < mes.Fim).Sum(a => a.Total);
        decimal meta = Repositorios.Configuracao.MetaMensal;

        if (meta <= 0)
        {
            metaPorcento = 0;
            metaPct.Text = "—";
            metaTexto.Text = "Clique para definir sua meta";
        }
        else
        {
            metaPorcento = (int)Math.Round(fatMes * 100 / meta);
            metaPct.Text = $"{metaPorcento}%";
            metaTexto.Text = $"{Formato.Dinheiro(fatMes)} de {Formato.Dinheiro(meta)}";
        }
        metaPreenchida.Width = metaBarra.Width * Math.Min(100, metaPorcento) / 100;
    }

    void PreencherTabela(List<Atendimento> atendimentos)
    {
        tabela.Rows.Clear();
        foreach (var a in atendimentos.OrderByDescending(a => a.Data))
            tabela.Rows.Add(a.Data, a.ClienteNome, a.Descricao, a.Profissional, a.Total,
                            a.FormaPagamento ?? "—", a.Pago ? "Pago" : "Pendente", a.Id);
        AtualizarAvisoGrid();
    }

    void AtualizarAvisoGrid()
    {
        bool vazio = tabela.DefaultView.Count == 0;
        gridVazio.Text = tabela.Rows.Count == 0
            ? "Nenhum atendimento registrado ainda.\nClique em \"+ Novo atendimento\" para começar."
            : "Nenhum atendimento encontrado para essa busca.";
        gridVazio.Visible = vazio;
        grid.Visible = !vazio;
    }

    // Escapa os caracteres especiais do LIKE do DataView ( ' [ ] * % )
    static string EscaparLike(string texto)
    {
        var sb = new StringBuilder();
        foreach (char c in texto)
        {
            if (c == '\'') sb.Append("''");
            else if (c is '[' or ']' or '*' or '%') sb.Append('[').Append(c).Append(']');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fonteStatus.Dispose();
            // páginas que não estão na tela no momento também precisam ser liberadas
            foreach (var p in paginas.Values) if (p.Parent == null) p.Dispose();
        }
        base.Dispose(disposing);
    }
}
