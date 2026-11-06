using System.Drawing.Drawing2D;
using System.Drawing.Text;
using dashboardforms;

namespace dashboardforms.Agenda;

// ===== Cores de cada filtro de procedimento (pastel no fundo, tom mais escuro na borda) =====
static class PaletaProcedimentos
{
    // a ordem deste vetor é a ordem em que os filtros aparecem na tela
    public static readonly (string Nome, Color Fundo, Color Borda)[] Itens =
    {
        ("Todos",       Color.White,                     Cores.Primaria),                    // neutro
        ("Corte",       Color.FromArgb(234, 222, 248),   Color.FromArgb(160, 130, 205)),     // lilás
        ("Mechas",      Color.FromArgb(255, 244, 196),   Color.FromArgb(214, 178, 62)),      // amarelo
        ("Selagem",     PaletaAgenda.VerdeFundo,         PaletaAgenda.VerdeBorda),           // verde (já existia)
        ("Coloração",   Color.FromArgb(250, 220, 232),   Color.FromArgb(222, 128, 166)),     // rosa
        ("Hidratação",  Color.FromArgb(214, 232, 250),   Color.FromArgb(110, 158, 214)),     // azul
        ("Sobrancelha", Color.FromArgb(233, 216, 200),   Color.FromArgb(160, 116, 84)),      // marrom
    };
}

// ===== Mede a largura de um texto (para os controles terem "dimensão proporcional ao conteúdo") =====
static class MedidaTexto
{
    public static int Largura(string texto, string familia, float tamanho, FontStyle estilo)
    {
        // Graphics.FromHwnd(IntPtr.Zero) = gráfico da tela inteira, com o mesmo DPI que o desenho real usa
        using var g = Graphics.FromHwnd(IntPtr.Zero);
        using var fonte = new Font(familia, tamanho, estilo);
        return (int)Math.Ceiling(g.MeasureString(texto, fonte).Width);
    }
}

// ===== Os dois valores escolhidos nos filtros (null = "Todos" = sem restrição) =====
readonly record struct FiltrosAgenda(string? Procedimento, string? Status)
{
    // Pronto para o item 7: o card do agendamento pergunta "eu passo nos filtros?".
    // O status vem de fora porque a classe Agendamento ainda não tem essa propriedade.
    public bool Aceita(Agendamento agendamento, string statusDoAgendamento)
        => (Procedimento is null || agendamento.Procedimentos.Contains(Procedimento))
        && (Status is null || Status == statusDoAgendamento);
}

// ===== Filtro de procedimento: minirretângulo arredondado com 3 estados =====
class FiltroProcedimento : Control
{
    public Color CorFundo { get; set; } = Color.White;
    public Color CorBorda { get; set; } = Cores.Primaria;
    public int Raio { get; set; } = 8;
    public float TamanhoFonte { get; set; } = 9f;
    public string FamiliaFonte { get; set; } = PaletaAgenda.FontePadrao;

    bool selecionado, sobre;

    public bool Selecionado
    {
        get => selecionado;
        set { if (selecionado == value) return; selecionado = value; Invalidate(); }
    }

    public FiltroProcedimento()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); sobre = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); sobre = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        // cada estado é a MESMA cor do procedimento, só misturada com a borda (identidade preservada)
        Color fundo = CorFundo;
        if (selecionado) fundo = PaletaAgenda.Misturar(CorFundo, CorBorda, 0.35f);
        else if (sobre) fundo = PaletaAgenda.Misturar(CorFundo, CorBorda, 0.15f);

        using var fonte = new Font(FamiliaFonte, TamanhoFonte, selecionado ? FontStyle.Bold : FontStyle.Regular);
        MiniRotulo.Desenhar(g, ClientRectangle, Text, fundo, CorBorda, Cores.Escuro, fonte, Raio);

        // selecionado: segunda borda por dentro, deixando o contorno mais grosso
        if (selecionado)
        {
            var interno = new Rectangle(1, 1, Width - 3, Height - 3);
            using var path = Util.Arredondar(interno, Math.Max(Raio - 1, 1));
            using var caneta = new Pen(CorBorda, 1f);
            g.DrawPath(caneta, path);
        }
    }
}

// ===== Tabela de cores do menu suspenso (troca o azul padrão do Windows pela paleta do projeto) =====
class TabelaCoresAgenda : ProfessionalColorTable
{
    public override Color MenuItemSelected => Cores.Rosa;
    public override Color MenuItemSelectedGradientBegin => Cores.Rosa;
    public override Color MenuItemSelectedGradientEnd => Cores.Rosa;
    public override Color MenuItemBorder => Cores.Primaria;
    public override Color MenuBorder => Cores.Primaria;
    public override Color ToolStripDropDownBackground => Color.White;
}

// ===== Seletor de status: campo arredondado + lista suspensa =====
class SeletorStatus : Control
{
    const int Raio = 8;
    const int RecuoTexto = 10;
    const int AreaSeta = 24;

    readonly string[] itens;
    int indice;                 // posição escolhida (0 = "Todos")
    bool sobre, listaAberta;
    ContextMenuStrip? menu;
    Font? fonteItem, fonteItemSel;

    public event EventHandler? SelecaoAlterada;

    public SeletorStatus(params string[] itens)
    {
        this.itens = itens;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    public int SelectedIndex
    {
        get => indice;
        set
        {
            if (value < 0 || value >= itens.Length || value == indice) return;
            indice = value;
            Invalidate();
            SelecaoAlterada?.Invoke(this, EventArgs.Empty);
        }
    }

    public string Selecionado => itens[indice];

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); sobre = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); sobre = false; Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left) AbrirLista();
    }

    void AbrirLista()
    {
        menu ??= CriarMenu();
        menu.Items.Clear();
        menu.MinimumSize = new Size(Width, 0);   // lista com a mesma largura do campo

        for (int i = 0; i < itens.Length; i++)
        {
            int posicao = i;   // cópia: cada clique precisa lembrar a SUA posição
            var item = new ToolStripMenuItem(itens[i])
            {
                Font = i == indice ? fonteItemSel : fonteItem,   // o item atual aparece em negrito
                ForeColor = Cores.Texto
            };
            item.Click += (s, e) => SelectedIndex = posicao;
            menu.Items.Add(item);
        }

        listaAberta = true;
        Invalidate();
        menu.Show(this, new Point(0, Height + 2));   // logo abaixo do campo
    }

    ContextMenuStrip CriarMenu()
    {
        fonteItem = new Font(PaletaAgenda.FontePadrao, 9.5f);
        fonteItemSel = new Font(PaletaAgenda.FontePadrao, 9.5f, FontStyle.Bold);

        var m = new ContextMenuStrip
        {
            ShowImageMargin = false,
            BackColor = Color.White,
            DropShadowEnabled = false,
            Renderer = new ToolStripProfessionalRenderer(new TabelaCoresAgenda()) { RoundedEdges = false }
        };
        m.Closed += (s, e) => { listaAberta = false; Invalidate(); };
        return m;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        // borda discreta (primária bem clara); reforça no hover ou com a lista aberta
        Color corBorda = (sobre || listaAberta)
            ? Cores.Primaria
            : PaletaAgenda.Misturar(Cores.Primaria, Color.White, 0.5f);

        var caixa = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Util.Arredondar(caixa, Raio);
        using var fundo = new SolidBrush(Color.White);
        using var caneta = new Pen(corBorda, 1f);
        g.FillPath(fundo, path);
        g.DrawPath(caneta, path);

        // texto escolhido, alinhado à esquerda e centralizado na vertical
        using var fonte = new Font(PaletaAgenda.FontePadrao, 9.5f);
        using var pTexto = new SolidBrush(Cores.Texto);
        using var formato = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter
        };
        g.DrawString(itens[indice], fonte, pTexto,
            new RectangleF(RecuoTexto, 0, Width - RecuoTexto - AreaSeta, Height), formato);

        // setinha "v" desenhada com 3 pontos
        float cx = Width - AreaSeta / 2f - 2, cy = Height / 2f;
        using var pSeta = new Pen(Cores.Primaria, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(pSeta, new[]
        {
            new PointF(cx - 4, cy - 2), new PointF(cx, cy + 2), new PointF(cx + 4, cy - 2)
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { menu?.Dispose(); fonteItem?.Dispose(); fonteItemSel?.Dispose(); }
        base.Dispose(disposing);
    }
}

// ===== Texto clicável, sem moldura (usado em "Limpar filtros") =====
class TextoClicavel : Control
{
    public Color CorTexto { get; set; } = Cores.Primaria;
    public Color CorHover { get; set; } = PaletaAgenda.Misturar(Cores.Primaria, Color.Black, 0.25f);
    public float TamanhoFonte { get; set; } = 9.5f;
    public string FamiliaFonte { get; set; } = PaletaAgenda.FontePadrao;

    bool sobre;

    public TextoClicavel()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); sobre = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); sobre = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        // hover discreto: sublinhado + cor um pouco mais escura
        using var fonte = new Font(FamiliaFonte, TamanhoFonte, sobre ? FontStyle.Underline : FontStyle.Regular);
        using var pincel = new SolidBrush(sobre ? CorHover : CorTexto);
        using var formato = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(Text, fonte, pincel, ClientRectangle, formato);
    }
}

// ===== Faixa de filtros: monta tudo em UMA linha e guarda a seleção =====
class FaixaFiltros : Control
{
    const int EspacoChips = 6;       // entre filtros de procedimento
    const int EspacoRotulo = 8;      // entre o identificador da categoria e o controle
    const int EspacoGrupos = 18;     // entre uma categoria e a próxima
    const int PaddingChip = 14;      // respiro lateral dentro de cada filtro
    const int LarguraStatus = 130;

    static readonly string[] OpcoesStatus =
        { "Todos", "Pendentes", "Confirmados", "Concluídos", "Cancelados", "Alterações" };

    readonly List<FiltroProcedimento> chips = new();
    readonly SeletorStatus seletorStatus;
    FiltroProcedimento chipAtivo = null!;
    bool limpando;                   // true enquanto o "Limpar" mexe nos controles por código

    // soma de tudo que foi montado: a janela usa para nunca deixar a faixa menor que isso
    public int LarguraNecessaria { get; private set; }

    // avisa quem estiver interessado (item 7) que algum filtro mudou
    public event EventHandler? FiltrosAlterados;

    // o item 7 lê os valores aqui (null = "Todos")
    public FiltrosAgenda Filtros => new(
        chipAtivo.Text == "Todos" ? null : chipAtivo.Text,
        seletorStatus.SelectedIndex == 0 ? null : seletorStatus.Selecionado);

    public FaixaFiltros(int altura)
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Height = altura;

        int x = 0;

        // 1) identificador "Procedimentos" + os 7 filtros
        x = AdicionarRotulo("Procedimentos", x, altura) + EspacoRotulo;

        foreach (var (nome, fundo, borda) in PaletaProcedimentos.Itens)
        {
            int largura = MedidaTexto.Largura(nome, PaletaAgenda.FontePadrao, 9f, FontStyle.Bold) + 2 * PaddingChip;
            var chip = new FiltroProcedimento
            {
                Text = nome,
                CorFundo = fundo,
                CorBorda = borda,
                Location = new Point(x, 0),
                Size = new Size(largura, altura)
            };
            chip.Click += (s, e) => SelecionarProcedimento((FiltroProcedimento)s!, true);
            chips.Add(chip);
            Controls.Add(chip);
            x += largura + EspacoChips;
        }

        chipAtivo = chips[0];          // "Todos" começa selecionado
        chipAtivo.Selecionado = true;
        x += EspacoGrupos - EspacoChips;   // troca o espaço entre filtros pelo espaço entre categorias

        // 2) identificador "Status" + seletor logo ao lado
        x = AdicionarRotulo("Status", x, altura) + EspacoRotulo;

        seletorStatus = new SeletorStatus(OpcoesStatus)
        {
            Location = new Point(x, 0),
            Size = new Size(LarguraStatus, altura)
        };
        seletorStatus.SelecaoAlterada += (s, e) => { if (!limpando) FiltrosAlterados?.Invoke(this, EventArgs.Empty); };
        Controls.Add(seletorStatus);
        x += LarguraStatus + EspacoGrupos;

        // 3) "Limpar filtros", à direita do seletor
        int larguraLimpar = MedidaTexto.Largura("Limpar filtros", PaletaAgenda.FontePadrao, 9.5f, FontStyle.Underline) + 8;
        var limpar = new TextoClicavel
        {
            Text = "Limpar filtros",
            Location = new Point(x, 0),
            Size = new Size(larguraLimpar, altura)
        };
        limpar.Click += (s, e) => Limpar();
        Controls.Add(limpar);
        x += larguraLimpar;

        LarguraNecessaria = x;
        Width = x;
    }

    // Identificador de categoria: texto puro, sem moldura. Devolve onde ele termina.
    int AdicionarRotulo(string texto, int x, int altura)
    {
        int largura = MedidaTexto.Largura(texto, PaletaAgenda.FontePadrao, 10f, FontStyle.Bold) + 8;
        Controls.Add(new Label
        {
            Text = texto,
            Font = new Font(PaletaAgenda.FontePadrao, 10f, FontStyle.Bold),
            ForeColor = Cores.Escuro,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            UseCompatibleTextRendering = false,
            Location = new Point(x, 0),
            Size = new Size(largura, altura)
        });
        return x + largura;
    }

    // Um procedimento por vez: desmarca o anterior, marca o novo e avisa.
    void SelecionarProcedimento(FiltroProcedimento novo, bool notificar)
    {
        if (novo == chipAtivo) return;
        chipAtivo.Selecionado = false;
        chipAtivo = novo;
        chipAtivo.Selecionado = true;
        if (notificar) FiltrosAlterados?.Invoke(this, EventArgs.Empty);
    }

    // Volta os dois grupos para "Todos" e avisa UMA vez só (se algo realmente mudou).
    public void Limpar()
    {
        bool mudou = chipAtivo != chips[0] || seletorStatus.SelectedIndex != 0;

        limpando = true;
        SelecionarProcedimento(chips[0], false);
        seletorStatus.SelectedIndex = 0;
        limpando = false;

        if (mudou) FiltrosAlterados?.Invoke(this, EventArgs.Empty);
    }
}