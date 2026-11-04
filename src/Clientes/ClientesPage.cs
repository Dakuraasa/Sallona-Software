using System.Drawing.Drawing2D;

namespace dashboardforms;

public class ClientesPage : UserControl
{
    // ---------- paleta: tudo vem de Cores (controles.cs) para ficar igual às outras telas ----------
    static readonly Color Primaria = Cores.Primaria;
    static readonly Color Texto = Cores.Texto;
    static readonly Color Suave = Cores.Cinza;
    static readonly Color Borda = Cores.Borda;
    static readonly Color RosaClaro = Cores.RosaClaro;
    static readonly Color AvatarFundo = Cores.AvatarFundo;

    const int AlturaLinha = 72;

    // Fontes das linhas da lista: criadas uma vez só e reaproveitadas
    // (criar uma Font nova para cada linha a cada atualização vaza recursos do Windows).
    static readonly Font FonteColuna = new("Segoe UI", 9.5F);
    static readonly Font FonteColunaNegrito = new("Segoe UI", 9.5F, FontStyle.Bold);
    static readonly Font FonteSeta = new("Segoe UI", 16F);
    int porPaginaAtual;
   
    int PorPagina => Math.Max(1, linhas.Height / AlturaLinha);

    // Os clientes vêm do repositório compartilhado (Cliente.cs), como na tela de Serviços.
    readonly List<Cliente> todos = Repositorios.Clientes.Todos();

    Cliente? selecionado;
    bool ordemAZ = true;
    int filtroVisita;      
    int pagina = 1;
    int totalPaginas = 1;

    readonly TextBox busca = new();
    readonly Panel linhas = new() { Dock = DockStyle.Fill, BackColor = Color.White };
    readonly Label lblBadge = new(), lblMostrando = new(), lblPagina = new();
    readonly Label lblNome = new(), lblTelefone = new(), lblEmail = new(), lblVisita = new(), lblPillVisita = new();
    readonly AvatarIniciais avatarFicha = new() { Size = new Size(56, 56), BackColor = Color.White };
    Caixa ordenar = null!;

    public ClientesPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Cores.Fundo;
        Padding = new Padding(36, 20, 36, 12);
        DoubleBuffered = true;

        var conteudo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 8) };
        conteudo.Controls.Add(ConstruirLista());   
        conteudo.Controls.Add(ConstruirFicha());   

        Controls.Add(conteudo);
        Controls.Add(ConstruirRodape());
        Controls.Add(ConstruirBarra());
        Controls.Add(ConstruirCabecalho());     
       
        linhas.Resize += (_, _) => { if (PorPagina != porPaginaAtual) Aplicar(); };

        Aplicar();
    }

    // ================= cabeçalho =================
    Control ConstruirCabecalho()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 108 };

        var textos = new Panel { Dock = DockStyle.Fill };
        textos.Controls.Add(Rotulo("RELAÇÕES QUE FLORESCEM", 7.5f, FontStyle.Bold, Primaria, 0, 0));
        textos.Controls.Add(Rotulo("Clientes", 26, FontStyle.Regular, Texto, 0, 14, "Georgia"));
        textos.Controls.Add(Rotulo("Cada pessoa, uma história. Tenha todos os detalhes por perto.", 9.5f, FontStyle.Regular, Suave, 0, 66));

        var botao = new Button
        {
            Text = "＋   Novo cliente",
            Dock = DockStyle.Top,
            Height = 46,
            FlatStyle = FlatStyle.Flat,
            BackColor = Primaria,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F),
            Cursor = Cursors.Hand
        };
        botao.FlatAppearance.BorderSize = 0;
        botao.Click += (_, _) => MessageBox.Show("TODO: abrir o cadastro de novo cliente");
        Formas.Arredondar(botao, 10);

        var direita = new Panel { Dock = DockStyle.Right, Width = 180, Padding = new Padding(0, 8, 0, 0) };
        direita.Controls.Add(botao);

        p.Controls.Add(textos);
        p.Controls.Add(direita);
        return p;
    }

    // ================= busca + filtros + ordenação =================
    Control ConstruirBarra()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(0, 0, 0, 26) };

        var caixaBusca = new Caixa { Dock = DockStyle.Fill, Raio = 10 };
        busca.BorderStyle = BorderStyle.None;
        busca.Font = new Font("Segoe UI", 10F);
        busca.BackColor = Color.White;
        busca.ForeColor = Texto;
        busca.PlaceholderText = "Buscar por nome ou telefone";
        busca.Location = new Point(48, 14);
        busca.TextChanged += (_, _) => { pagina = 1; Aplicar(); };
        caixaBusca.Resize += (_, _) => busca.Width = Math.Max(50, caixaBusca.Width - 66);
        caixaBusca.Controls.Add(Rotulo("🔍", 11, FontStyle.Regular, Suave, 16, 11, "Segoe UI Emoji"));
        caixaBusca.Controls.Add(busca);

        var menu = new ContextMenuStrip();
        void Opcao(string texto, int valor) =>
            menu.Items.Add(texto, null, (_, _) => { filtroVisita = valor; pagina = 1; Aplicar(); });
        Opcao("Todos os clientes", 0);
        Opcao("Visitaram nos últimos 30 dias", 1);
        Opcao("Sem visita há mais de 30 dias", 2);

        var filtros = BotaoContorno("⚙   Filtros", c => menu.Show(c, new Point(0, c.Height)));
        ordenar = BotaoContorno("⇅   Nome: A–Z", _ => { ordemAZ = !ordemAZ; pagina = 1; Aplicar(); });

        var ladoFiltros = new Panel { Dock = DockStyle.Right, Width = 134, Padding = new Padding(14, 0, 0, 0) };
        ladoFiltros.Controls.Add(filtros);
        var ladoOrdenar = new Panel { Dock = DockStyle.Right, Width = 164, Padding = new Padding(14, 0, 0, 0) };
        ladoOrdenar.Controls.Add(ordenar);

        p.Controls.Add(caixaBusca);
        p.Controls.Add(ladoFiltros);
        p.Controls.Add(ladoOrdenar);
        return p;
    }

    // ================= lista =================
    Control ConstruirLista()
    {
        var card = new Caixa { Dock = DockStyle.Fill, Padding = new Padding(1) };

        // título + badge com a contagem
        var titulo = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 66,
            WrapContents = false,
            Padding = new Padding(24, 20, 0, 0),
            BackColor = Color.Transparent
        };
        var tit = Rotulo("Todos os clientes", 11, FontStyle.Bold, Texto, 0, 0);
        tit.Margin = new Padding(0, 1, 12, 0);
        var pill = new Caixa { Raio = 12, Preenchimento = RosaClaro, Borda = RosaClaro, Size = new Size(100, 26), Margin = new Padding(0) };
        Estilo(lblBadge, 8.5f, FontStyle.Regular, Primaria, 0, 0);
        lblBadge.AutoSize = false; lblBadge.Dock = DockStyle.Fill; lblBadge.TextAlign = ContentAlignment.MiddleCenter;
        pill.Controls.Add(lblBadge);
        titulo.Controls.Add(tit);
        titulo.Controls.Add(pill);

        // cabeçalho das colunas
        var cab = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(247, 243, 241) };
        cab.Controls.Add(Rotulo("NOME", 7.5f, FontStyle.Bold, Suave, 24, 14));
        cab.Controls.Add(Rotulo("TELEFONE", 7.5f, FontStyle.Bold, Suave, 238, 14));
        cab.Controls.Add(Rotulo("ÚLTIMA VISITA", 7.5f, FontStyle.Bold, Suave, 422, 14));

        // rodapé: "Mostrando..." + paginação
        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.Transparent };
        Estilo(lblMostrando, 8.5f, FontStyle.Regular, Suave, 24, 20);

        var anterior = BotaoPagina("‹", () => { if (pagina > 1) { pagina--; Aplicar(); } });
        var proxima = BotaoPagina("›", () => { if (pagina < totalPaginas) { pagina++; Aplicar(); } });
        lblPagina.AutoSize = false; lblPagina.Size = new Size(30, 28); lblPagina.Margin = new Padding(0);
        lblPagina.TextAlign = ContentAlignment.MiddleCenter;
        lblPagina.BackColor = RosaClaro; lblPagina.ForeColor = Primaria;
        lblPagina.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        var pag = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 120,
            WrapContents = false,
            Padding = new Padding(20, 14, 0, 0),
            BackColor = Color.Transparent
        };
        pag.Controls.Add(anterior);
        pag.Controls.Add(lblPagina);
        pag.Controls.Add(proxima);
        rodape.Controls.Add(lblMostrando);
        rodape.Controls.Add(pag);

        card.Controls.Add(linhas);
        card.Controls.Add(rodape);
        card.Controls.Add(cab);
        card.Controls.Add(titulo);
        return card;
    }

    static Label BotaoPagina(string simbolo, Action aoClicar)
    {
        var l = new Label
        {
            Text = simbolo,
            AutoSize = false,
            Size = new Size(26, 28),
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Suave,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12F),
            Cursor = Cursors.Hand
        };
        l.Click += (_, _) => aoClicar();
        return l;
    }

    Control Linha(Cliente c)
    {
        bool sel = c == selecionado;
        var fundo = sel ? RosaClaro : Color.White;

        var l = new Panel { Dock = DockStyle.Top, Height = AlturaLinha, BackColor = fundo, Cursor = Cursors.Hand };
        l.Paint += (_, e) =>
        {
            using var caneta = new Pen(Borda);
            e.Graphics.DrawLine(caneta, 0, l.Height - 1, l.Width, l.Height - 1);
            if (sel)
            {
                using var barra = new SolidBrush(Primaria);
                e.Graphics.FillRectangle(barra, 0, 0, 3, l.Height - 1);
            }
        };

        l.Controls.Add(new AvatarIniciais
        {
            Iniciais = Iniciais(c.Nome),
            Size = new Size(38, 38),
            Location = new Point(24, 17),
            BackColor = fundo,
            Fundo = AvatarFundo,
            Cor = Primaria
        });
        l.Controls.Add(Coluna(c.Nome, 73, 150, true));
        l.Controls.Add(Coluna(c.Telefone, 238, 170, false));
        l.Controls.Add(Coluna(c.UltimaVisita?.ToString("dd/MM/yyyy") ?? "—", 422, 130, false));
        l.Controls.Add(new Label
        {
            Text = "›",
            Dock = DockStyle.Right,
            Width = 50,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Primaria,
            Font = FonteSeta,
            BackColor = Color.Transparent
        });

        
        void Clicar(object? s, EventArgs e) => BeginInvoke(new Action(() => { selecionado = c; Aplicar(); }));
        l.Click += Clicar;
        foreach (Control k in l.Controls) k.Click += Clicar;
        return l;
    }

    static Label Coluna(string texto, int x, int largura, bool negrito) => new()
    {
        Text = texto,
        Location = new Point(x, 0),
        Size = new Size(largura, AlturaLinha),
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Texto,
        BackColor = Color.Transparent,
        Font = negrito ? FonteColunaNegrito : FonteColuna
    };

    // ================= ficha do cliente =================
    Control ConstruirFicha()
    {
        var lado = new Panel { Dock = DockStyle.Right, Width = 354, Padding = new Padding(24, 0, 0, 0) };
        var card = new Caixa { Dock = DockStyle.Fill };
        lado.Controls.Add(card);

        var mais = Rotulo("•••", 8, FontStyle.Regular, Suave, 0, 20);

        Estilo(lblNome, 18, FontStyle.Regular, Texto, 24, 104, "Georgia");
        lblNome.AutoSize = false; lblNome.TextAlign = ContentAlignment.MiddleCenter;

        var pill = new Caixa { Raio = 12, Preenchimento = RosaClaro, Borda = RosaClaro, Size = new Size(170, 24), Location = new Point(0, 140) };
        Estilo(lblPillVisita, 8.5f, FontStyle.Regular, Primaria, 0, 0);
        lblPillVisita.AutoSize = false; lblPillVisita.Dock = DockStyle.Fill; lblPillVisita.TextAlign = ContentAlignment.MiddleCenter;
        pill.Controls.Add(lblPillVisita);

        Panel Divisor(int y) => new() { Location = new Point(24, y), Size = new Size(100, 1), BackColor = Borda };
        var div1 = Divisor(178);
        var div2 = Divisor(244);

        Estilo(lblTelefone, 9.5f, FontStyle.Regular, Texto, 58, 190);
        Estilo(lblEmail, 9.5f, FontStyle.Regular, Suave, 58, 216);
        Estilo(lblVisita, 9.5f, FontStyle.Regular, Suave, 24, 278);

        var historico = BotaoContorno("🕘   Ver histórico", _ => MessageBox.Show("TODO: abrir o histórico do cliente"));
        historico.Dock = DockStyle.None;

        var editar = Rotulo("Editar cadastro", 9, FontStyle.Bold, Primaria, 0, 0);
        editar.AutoSize = false; editar.TextAlign = ContentAlignment.MiddleCenter; editar.Cursor = Cursors.Hand;
        editar.Click += (_, _) => MessageBox.Show("TODO: editar o cadastro do cliente");

        card.Controls.AddRange(new Control[]
        {
            Rotulo("FICHA DO CLIENTE", 7.5f, FontStyle.Bold, Suave, 24, 22), mais, avatarFicha, lblNome, pill,
            div1, Rotulo("📞", 10, FontStyle.Regular, Suave, 24, 186, "Segoe UI Emoji"), lblTelefone,
            Rotulo("✉", 10, FontStyle.Regular, Suave, 24, 212, "Segoe UI Emoji"), lblEmail, div2,
            Rotulo("Última visita", 9.5f, FontStyle.Bold, Texto, 24, 256), lblVisita, historico, editar
        });

        card.Resize += (_, _) =>
        {
            int w = card.Width, h = card.Height;
            if (w < 100) return;
            mais.Left = w - 24 - mais.Width;
            avatarFicha.Location = new Point((w - avatarFicha.Width) / 2, 44);
            lblNome.SetBounds(24, 104, w - 48, 34);
            pill.Left = (w - pill.Width) / 2;
            div1.Width = div2.Width = w - 48;

            int baseY = Math.Max(306, h - 96);
            historico.SetBounds(24, baseY, w - 48, 42);
            editar.SetBounds(24, baseY + 50, w - 48, 22);
        };
        return lado;
    }

    void PreencherFicha()
    {
        var c = selecionado;
        avatarFicha.Iniciais = c is null ? "" : Iniciais(c.Nome);
        avatarFicha.Invalidate();
        lblNome.Text = c?.Nome ?? "Nenhum cliente";
        lblTelefone.Text = c?.Telefone ?? "—";
        lblEmail.Text = c?.Email is { Length: > 0 } e ? e : "E-mail não informado";
        var visita = c?.UltimaVisita?.ToString("dd/MM/yyyy") ?? "—";
        lblVisita.Text = visita;
        lblPillVisita.Text = $"Última visita: {visita}";
    }

    // ================= rodapé da página =================
    Control ConstruirRodape()
    {
        var p = new Panel { Dock = DockStyle.Bottom, Height = 32 };
        p.Controls.Add(Rotulo("♡", 11, FontStyle.Bold, Primaria, 0, 4));
        p.Controls.Add(Rotulo("Cuidar dos detalhes é o primeiro passo para um atendimento memorável.", 8.5f, FontStyle.Regular, Suave, 24, 6));
        return p;
    }

    // ================= filtro + ordenação + paginação =================
    void Aplicar()
    {
        var limite = DateTime.Today.AddDays(-30);
        IEnumerable<Cliente> q = todos;

        var t = busca.Text.Trim();
        if (t.Length > 0)
            q = q.Where(c => c.Nome.Contains(t, StringComparison.CurrentCultureIgnoreCase) || c.Telefone.Contains(t));
        if (filtroVisita == 1) q = q.Where(c => c.UltimaVisita >= limite);
        if (filtroVisita == 2) q = q.Where(c => c.UltimaVisita is null || c.UltimaVisita < limite);

        q = ordemAZ ? q.OrderBy(c => c.Nome, StringComparer.CurrentCultureIgnoreCase)
                    : q.OrderByDescending(c => c.Nome, StringComparer.CurrentCultureIgnoreCase);

        var lista = q.ToList();
        int porPagina = PorPagina;
        porPaginaAtual = porPagina;
        totalPaginas = Math.Max(1, (int)Math.Ceiling(lista.Count / (double)porPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        var visiveis = lista.Skip((pagina - 1) * porPagina).Take(porPagina).ToList();

        if (selecionado is null || !lista.Contains(selecionado))
            selecionado = visiveis.FirstOrDefault();

  
        linhas.SuspendLayout();
        foreach (var antigo in linhas.Controls.Cast<Control>().ToList()) antigo.Dispose();
        for (int i = visiveis.Count - 1; i >= 0; i--) linhas.Controls.Add(Linha(visiveis[i]));
        linhas.ResumeLayout();

        string plural = lista.Count == 1 ? "cliente" : "clientes";
        int primeiro = (pagina - 1) * porPagina + 1;
        lblBadge.Text = $"{lista.Count} {plural}";
        lblMostrando.Text = lista.Count == 0
            ? "Nenhum cliente encontrado"
            : $"Mostrando {primeiro}–{primeiro + visiveis.Count - 1} de {lista.Count} {plural}";
        lblPagina.Text = pagina.ToString();
        ((Label)ordenar.Controls[0]).Text = ordemAZ ? "⇅   Nome: A–Z" : "⇅   Nome: Z–A";

        PreencherFicha();
    }

    // ================= utilitários =================
    static string Iniciais(string nome) => Util.Iniciais(nome);

    static void Estilo(Label l, float tam, FontStyle estilo, Color cor, int x, int y, string fonte = "Segoe UI")
    {
        l.AutoSize = true;
        l.Location = new Point(x, y);
        l.ForeColor = cor;
        l.BackColor = Color.Transparent;
        l.Font = new Font(fonte, tam, estilo);
    }

    static Label Rotulo(string texto, float tam, FontStyle estilo, Color cor, int x, int y, string fonte = "Segoe UI")
    {
        var l = new Label { Text = texto };
        Estilo(l, tam, estilo, cor, x, y, fonte);
        return l;
    }


    static Caixa BotaoContorno(string texto, Action<Control> aoClicar)
    {
        var caixa = new Caixa { Raio = 10, Cursor = Cursors.Hand, Dock = DockStyle.Fill, Padding = new Padding(1) };
        var rotulo = new Label
        {
            Text = texto,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Texto,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 10F)
        };
        caixa.Controls.Add(rotulo);
        caixa.Click += (_, _) => aoClicar(caixa);
        rotulo.Click += (_, _) => aoClicar(caixa);
        return caixa;
    }
}
