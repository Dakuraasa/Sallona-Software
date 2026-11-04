using System.Data;
using System.Text;
using dashboardforms.Pagamentos;

namespace dashboardforms;

public class DashboardForm : Form
{

    public event Action<string>? Navegar;

    record Dados(string Fat, string FatVar, string Atend, string AtendVar, string Novos, string NovosVar,
                 string Ticket, string TicketVar, string[] Rotulos, float[] Valores, int Destaque);

    readonly Dictionary<string, Dados> periodos = new()
    {
        ["Dia"] = new("R$ 1.840", "▲ 6,2%", "14", "▲ 2", "3", "▲ 1", "R$ 131", "▲ 3,1%",
            new[] { "9h", "11h", "13h", "15h", "17h", "19h" }, new float[] { 180, 420, 260, 510, 330, 140 }, 3),
        ["Semana"] = new("R$ 9.620", "▲ 4,8%", "72", "▲ 5", "11", "▼ 2", "R$ 134", "▲ 1,9%",
            new[] { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" }, new float[] { 980, 1240, 1410, 1620, 2380, 1990 }, 4),
        ["Mês"] = new("R$ 38.450", "▲ 4,2%", "286", "▲ 12", "41", "▼ 1", "R$ 134", "▲ 2,3%",
            new[] { "Sem 1", "Sem 2", "Sem 3", "Sem 4" }, new float[] { 8900, 9620, 10150, 9780 }, 2),
        // Ano: soma de jan a out (nov/dez ainda não aconteceram)
        ["Ano"] = new("R$ 338.850", "▲ 9,6%", "2.587", "▲ 194", "468", "▲ 24", "R$ 131", "▲ 1,2%",
            new[] { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" },
            new float[] { 28000, 26500, 31000, 33200, 35800, 34100, 36500, 37200, 38100, 38450, 0, 0 }, 9),
    };

    readonly Label[] kpiValor = new Label[4];
    readonly Label[] kpiVar = new Label[4];
    readonly GraficoBarras grafico = new() { Dock = DockStyle.Fill };
    readonly Dictionary<string, Button> botoesPeriodo = new();
    readonly Dictionary<string, Button> botoesMenu = new();
    readonly DataTable tabela = new();
    readonly Font fonteStatus = new("Segoe UI", 9f, FontStyle.Bold);

    // Meta do mês (depois virá do banco)
    const decimal FaturamentoMes = 38450m, MetaMes = 49000m;

    // ---------- atendimentos de exemplo (os preços batem com o catálogo de Serviços) ----------
    class Atendimento
    {
        public required string Cliente { get; init; }
        public required string Profissional { get; init; }
        public required (string Nome, decimal Valor)[] Servicos { get; init; }
        public DateTime Data { get; init; } = DateTime.Today;
        public string? Forma { get; set; }          // null = ainda não pago
        public bool Pago => Forma != null;
        public decimal Total => Servicos.Sum(s => s.Valor);
    }

    readonly List<Atendimento> atendimentos = new()
    {
        new() { Cliente = "Camila Souza",   Profissional = "Juliana",  Servicos = new[] { ("Escova", 70m), ("Hidratação", 90m) }, Forma = "Pix" },
        new() { Cliente = "Fernanda Lima",  Profissional = "Patrícia", Servicos = new[] { ("Coloração", 320m) }, Forma = "Cartão" },
        new() { Cliente = "Marcos Pereira", Profissional = "Rafael",   Servicos = new[] { ("Corte masculino", 55m) }, Forma = "Dinheiro" },
        new() { Cliente = "Aline Costa",    Profissional = "Bianca",   Servicos = new[] { ("Manicure", 40m), ("Pedicure", 45m) } },
        new() { Cliente = "Renata Alves",   Profissional = "Juliana",  Servicos = new[] { ("Corte feminino", 120m) }, Forma = "Cartão" },
        new() { Cliente = "Beatriz Nunes",  Profissional = "Patrícia", Servicos = new[] { ("Progressiva", 450m) } },
        new() { Cliente = "Lucas Martins",  Profissional = "Rafael",   Servicos = new[] { ("Barba", 40m) }, Forma = "Pix" },
    };

    // Área onde as páginas são trocadas quando se clica no menu
    readonly Panel areaPaginas = new() { Dock = DockStyle.Fill, BackColor = Cores.Fundo };
    readonly Dictionary<string, Control> paginas = new();

    public DashboardForm()
    {
        Text = "Sallona - Dashboard";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Cores.Fundo;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1320, 780);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        paginas["Dashboard"] = ConstruirConteudo();
        Controls.Add(areaPaginas);
        Controls.Add(ConstruirSidebar());

        AplicarPeriodo("Mês");
        MostrarPagina("Dashboard");
    }

    Control ConstruirSidebar()
    {
        var sb = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = Color.White, Padding = new Padding(14, 10, 14, 14) };

        var logoPath = Path.Combine(AppContext.BaseDirectory, "logo.jpg");
        var logo = new PictureBox { Dock = DockStyle.Top, Height = 140, SizeMode = PictureBoxSizeMode.Zoom };
        if (File.Exists(logoPath)) logo.Image = Image.FromFile(logoPath);

        var menu = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 8 * 44 + 8, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        foreach (var (icone, nome) in new[] { ("📊", "Dashboard"), ("👥", "Clientes"), ("✂", "Serviços"), ("💇", "Profissionais"),
                                              ("💰", "Financeiro"), ("📦", "Estoque"), ("🛍", "Produtos"), ("📖", "Agenda") })
            menu.Controls.Add(NovoBotaoMenu(icone, nome));

        var meta = new Card { Dock = DockStyle.Bottom, Height = 120, Preenchimento = Cores.Escuro, Margin = new Padding(0) };
        var lm = new Label { Text = "Meta do mês", ForeColor = Cores.Rosa, Dock = DockStyle.Top, Height = 22, BackColor = Color.Transparent };
        int pctMeta = (int)Math.Round(FaturamentoMes * 100 / MetaMes);
        var lv = new Label { Text = $"{pctMeta}%", ForeColor = Color.White, Font = new Font("Segoe UI", 20f, FontStyle.Bold), Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
        var barra = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.FromArgb(70, 30, 38) };
        var preenchida = new Panel { Dock = DockStyle.Left, BackColor = Cores.Primaria };
        barra.Controls.Add(preenchida);
        barra.Resize += (_, _) => preenchida.Width = barra.Width * Math.Min(100, pctMeta) / 100;
        var lt = new Label { Text = $"R$ {(FaturamentoMes / 1000).ToString("0.#", Formato.Br)}k de R$ {(MetaMes / 1000).ToString("0.#", Formato.Br)}k", ForeColor = Color.White, Dock = DockStyle.Top, Height = 24, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        meta.Controls.Add(lt); meta.Controls.Add(barra); meta.Controls.Add(lv); meta.Controls.Add(lm);

        var rodape = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 92, FlowDirection = FlowDirection.TopDown, WrapContents = false };
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
            Size = new Size(210, 40),
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
            if (nome == "Sair") { Close(); return; }
            MostrarPagina(nome);
            Navegar?.Invoke(nome);
        };
        botoesMenu[nome] = b;
        return b;
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

    // ---------- Navegação entre páginas ----------
    void MostrarPagina(string nome)
    {
        if (!paginas.TryGetValue(nome, out var pagina))
        {
            // As páginas são criadas só na primeira vez que são abertas
            pagina = nome switch
            {
                "Clientes" => new ClientesPage { Dock = DockStyle.Fill },
                "Serviços" => new ServicosPage { Dock = DockStyle.Fill },
                _ => PaginaEmBreve(nome)   // páginas que outros membros do grupo ainda vão fazer
            };
            paginas[nome] = pagina;
        }

        Exibir(pagina, nome, nome);
    }

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

        Text = $"Sallona - {titulo}";
        MarcarMenu(menu);
    }

    // ---------- Pagamento de um atendimento pendente ----------
    void AbrirPagamento(Atendimento a)
    {
        if (a.Pago) return;

        // descrição de cada serviço vem do catálogo (categoria · duração)
        var catalogo = Repositorios.Servicos.Todos();
        var itens = a.Servicos.Select(s =>
        {
            var c = catalogo.FirstOrDefault(x => string.Equals(x.Nome, s.Nome, StringComparison.CurrentCultureIgnoreCase));
            string detalhe = c == null ? "" : $"{c.Categoria} · {Formato.Duracao(c.DuracaoMin)}";
            return new PagamentoPage.ServicoRealizado(s.Nome, detalhe, s.Valor);
        });

        var pagina = new PagamentoPage { Dock = DockStyle.Fill };
        pagina.Carregar(a.Cliente, a.Data, a.Profissional, itens);
        pagina.VoltarSolicitado += () => MostrarPagina("Dashboard");
        pagina.PagamentoConfirmado += (forma, _) =>
        {
            a.Forma = forma;    // TODO: gravar no MySQL
            PreencherTabela();
        };
        Exibir(pagina, "Pagamento", "Dashboard");
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

    // ---------- Conteúdo ----------
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
        cGraf.Controls.Add(Titulo("Faturamento"));
        raiz.Controls.Add(cGraf, 0, 2);

        var cServ = NovoCard();
        var lp = new ListaProgresso { Dock = DockStyle.Fill };
        lp.Itens.AddRange(new[]
        {
            ("Corte feminino", 30, Cores.Primaria), ("Escova", 23, Cores.Coral), ("Coloração", 18, Cores.Vinho),
            ("Manicure", 17, Cores.Rosa), ("Hidratação", 12, Cores.Primaria),
        });
        cServ.Controls.Add(lp);
        cServ.Controls.Add(Titulo("Serviços mais realizados (% do mês)"));
        raiz.Controls.Add(cServ, 1, 2);


        var cTab = NovoCard();
        cTab.Controls.Add(ConstruirGrid());
        cTab.Controls.Add(Titulo("Últimos atendimentos"));
        cTab.Controls.Add(new Label
        {
            Text = "Clique duas vezes em um atendimento pendente para registrar o pagamento.",
            Dock = DockStyle.Bottom,
            Height = 22,
            ForeColor = Cores.Cinza,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.BottomLeft
        });
        raiz.Controls.Add(cTab, 0, 3);

        var cAl = NovoCard();
        var ll = new ListaLinhas { Dock = DockStyle.Fill };
        ll.Itens.AddRange(new (string, string?, Color)[]
        {
            ("Estoque baixo", null, Color.Empty),
            ("Shampoo hidratante 1L", "2 un.", Cores.Vinho),
            ("Esmalte nude", "4 un.", Cores.Vinho),
            ("Tintura 7.1", "3 un.", Cores.Vinho),
            ("Aniversariantes do dia", null, Color.Empty),
            ("Camila Souza", "🎂 hoje", Cores.Primaria),
            ("Fernanda Lima", "🎂 hoje", Cores.Primaria),
            ("Profissional do mês", null, Color.Empty),
            ("Juliana Rocha", "R$ 9.870", Cores.Verde),
        });
        cAl.Controls.Add(ll);
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

        var filtros = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 330, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 14, 0, 0) };
        foreach (var nome in new[] { "Dia", "Semana", "Mês", "Ano" })
        {
            var b = new Button { Text = nome, Size = new Size(76, 34), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 6, 0) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => AplicarPeriodo(nome);
            botoesPeriodo[nome] = b;
            filtros.Controls.Add(b);
        }

        var busca = new TextBox { BorderStyle = BorderStyle.None, PlaceholderText = "Buscar cliente...", Font = new Font("Segoe UI", 10f), Location = new Point(14, 9), Width = 190 };
        var caixa = new Card { Raio = 17, Size = new Size(222, 34), Location = new Point(250, 14), Padding = new Padding(0) };
        caixa.Controls.Add(busca);
        busca.TextChanged += (_, _) =>
        {
            var t = busca.Text.Trim();
            tabela.DefaultView.RowFilter = t.Length == 0 ? "" : $"Cliente LIKE '%{EscaparLike(t)}%'";
        };

        p.Controls.Add(titulo);
        p.Controls.Add(caixa);
        p.Controls.Add(filtros);
        p.Resize += (_, _) => caixa.Left = Math.Max(titulo.Right + 20, p.Width - filtros.Width - caixa.Width - 12);
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

    static Label Titulo(string txt) => new()
    {
        Text = txt,
        Dock = DockStyle.Top,
        Height = 32,
        BackColor = Color.Transparent,
        ForeColor = Cores.Escuro,
        Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
    };

    DataGridView ConstruirGrid()
    {
        tabela.Columns.Add("Cliente"); tabela.Columns.Add("Serviço"); tabela.Columns.Add("Profissional");
        tabela.Columns.Add("Valor"); tabela.Columns.Add("Pagamento"); tabela.Columns.Add("Status");
        tabela.Columns.Add("Indice", typeof(int));   // posição na lista de atendimentos (coluna escondida)
        PreencherTabela();

        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = tabela.DefaultView,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(240, 230, 228),
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 34,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };
        g.RowTemplate.Height = 34;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Cores.Cinza;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        g.DefaultCellStyle.ForeColor = Cores.Texto;
        g.DefaultCellStyle.SelectionBackColor = Cores.Fundo;
        g.DefaultCellStyle.SelectionForeColor = Cores.Escuro;
        g.DataBindingComplete += (_, _) => { if (g.Columns["Indice"] is { } c) c.Visible = false; };
        g.CellFormatting += (_, e) =>
        {
            if (g.Columns[e.ColumnIndex].Name == "Status" && e.Value is string s)
            {
                e.CellStyle.ForeColor = s == "Pago" ? Cores.Verde : Cores.Vinho;
                e.CellStyle.Font = fonteStatus;   // fonte criada uma vez só (criar aqui vazaria memória)
            }
        };
        g.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && g.Rows[e.RowIndex].DataBoundItem is DataRowView v)
                AbrirPagamento(atendimentos[(int)v["Indice"]]);
        };
        return g;
    }

    void PreencherTabela()
    {
        tabela.Rows.Clear();
        for (int i = 0; i < atendimentos.Count; i++)
        {
            var a = atendimentos[i];
            tabela.Rows.Add(a.Cliente, string.Join(" + ", a.Servicos.Select(s => s.Nome)), a.Profissional,
                            Formato.Dinheiro(a.Total), a.Forma ?? "—", a.Pago ? "Pago" : "Pendente", i);
        }
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

    void AplicarPeriodo(string p)
    {
        var d = periodos[p];
        string[] valores = { d.Fat, d.Atend, d.Novos, d.Ticket };
        string[] vars = { d.FatVar, d.AtendVar, d.NovosVar, d.TicketVar };
        for (int i = 0; i < 4; i++)
        {
            kpiValor[i].Text = valores[i];
            kpiVar[i].Text = vars[i] + " vs. anterior";
            kpiVar[i].ForeColor = vars[i].StartsWith("▼") ? Cores.Coral : (i == 0 ? Color.FromArgb(120, 220, 160) : Cores.Verde);
        }
        grafico.Rotulos = d.Rotulos;
        grafico.Valores = d.Valores;
        grafico.Destaque = d.Destaque;
        grafico.Animar();   // as barras "crescem" ao trocar de período

        foreach (var (nome, b) in botoesPeriodo)
        {
            bool ativo = nome == p;
            b.BackColor = ativo ? Cores.Primaria : Color.White;
            b.ForeColor = ativo ? Color.White : Cores.Texto;
        }
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
