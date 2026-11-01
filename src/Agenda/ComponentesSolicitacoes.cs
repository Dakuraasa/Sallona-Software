using System.Drawing.Drawing2D;
using System.Drawing.Text;
using dashboardforms;

namespace dashboardforms.Agenda;

// ===== Cores extras da Agenda (derivadas da paleta do projeto) =====
static class PaletaAgenda
{
    // rosa muito claro: Cores.Rosa misturado com branco (0 = cor original, 1 = branco)
    public static readonly Color FundoCard = Misturar(Cores.Rosa, Color.White, 1.0f);

    // Confirmar: verde pastel + borda mais intensa
    public static readonly Color VerdeFundo = Color.FromArgb(214, 238, 220);
    public static readonly Color VerdeBorda = Color.FromArgb(120, 190, 142);

    // Recusar: vermelho pastel + borda mais intensa
    public static readonly Color VermelhoFundo = Color.FromArgb(249, 216, 218);
    public static readonly Color VermelhoBorda = Color.FromArgb(224, 128, 138);

    // cada canal (R, G, B) anda "t" do caminho entre a cor e o alvo
    public static Color Misturar(Color cor, Color alvo, float t) => Color.FromArgb(
        (int)(cor.R + (alvo.R - cor.R) * t),
        (int)(cor.G + (alvo.G - cor.G) * t),
        (int)(cor.B + (alvo.B - cor.B) * t));
}

// ===== Linha divisória (mesma caneta da linha do minicalendário) =====
class LinhaDivisoria : Control
{
    public LinhaDivisoria()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var caneta = new Pen(Cores.Rosa, 1.0f); // igual à linha do calendário
        g.DrawLine(caneta, 0, Height / 2f, Width, Height / 2f);
    }
}

// ===== Minicard: retângulo arredondado com texto centralizado =====   
class MiniRotulo : Control
{
    public Color CorFundo { get; set; } = Color.White;
    public Color CorBorda { get; set; } = Cores.Primaria; public Color CorTexto { get; set; } = Cores.Escuro;
    public int Raio { get; set; } = 8;
    public float TamanhoFonte { get; set; } = 8f;
    public string FamiliaFonte { get; set; } = "Segoe UI";

    public MiniRotulo()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    // Método estático: o CardSolicitacao também o usa para desenhar seus botões
    public static void Desenhar(Graphics g, Rectangle area, string texto, Color fundo, Color borda,
                                Color corTexto, Font fonte, int raio)
    {
        // -1: o contorno da caneta é centrado na linha, então sobra 1 px para não cortar na borda
        var caixa = new Rectangle(area.X, area.Y, area.Width - 1, area.Height - 1);

        using var path = Util.Arredondar(caixa, raio);
        using var pincel = new SolidBrush(fundo);
        using var caneta = new Pen(borda, 1f);
        using var pTexto = new SolidBrush(corTexto);
        using var centro = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap      // nunca quebra a palavra em duas linhas
        };

        g.FillPath(pincel, path);
        g.DrawPath(caneta, path);
        g.DrawString(texto, fonte, pTexto, caixa, centro);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var fonte = new Font(FamiliaFonte, TamanhoFonte);
        Desenhar(g, ClientRectangle, Text, CorFundo, CorBorda, CorTexto, fonte, Raio);
    }
}

// ===== Card de solicitação (visual; dados fictícios vêm de fora) =====
class CardSolicitacao : Control
{
    // ----- Constantes de layout (todas as medidas em um lugar só) -----
    const int Margem = 12;            // margem interna
    const int AlturaNome = 20;
    const int AlturaInfo = 17;        // data/horário e procedimento
    const int AlturaBotao = 24;
    const int LarguraBotao = 100;
    const int EspacoBotoes = 8;
    const int RaioCard = 12;

    public string Cliente { get; set; } = "";
    public string DataHora { get; set; } = "";
    public string Procedimento { get; set; } = "";

    public CardSolicitacao()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    // altura necessária, calculada a partir das mesmas constantes do desenho
    public static int AlturaIdeal =>
        Margem + AlturaNome + AlturaInfo + AlturaInfo + 8 + AlturaBotao + Margem;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var fNome = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var fInfo = new Font("Segoe UI", 8.5f);
        using var fBotao = new Font("Segoe UI Semibold", 8.5f);

        DesenharFundo(g);
        DesenharTextos(g, fNome, fInfo);
        DesenharBotoes(g, fBotao);
    }

    // ----- Fundo rosa muito claro + borda rosada arredondada -----
    void DesenharFundo(Graphics g)
    {
        var area = new Rectangle(0, 0, Width - 1, Height - 1);
        area.Inflate(-1, -1);                       // espaço para a espessura da borda

        using var path = Util.Arredondar(area, RaioCard);
        using var fundo = new SolidBrush(PaletaAgenda.FundoCard);
        using var borda = new Pen(Cores.Primaria, 1.5f);
        g.FillPath(fundo, path);
        g.DrawPath(borda, path);
    }

    // ----- Nome, data/horário e procedimento, um abaixo do outro -----
    void DesenharTextos(Graphics g, Font fNome, Font fInfo)
    {
        using var bNome = new SolidBrush(Cores.Escuro);
        using var bData = new SolidBrush(Cores.Cinza);
        using var bProc = new SolidBrush(Cores.Texto);
        using var esquerda = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter   // texto longo termina em "..."
        };

        float largura = Width - 2 * Margem;
        float y = Margem;

        g.DrawString(Cliente, fNome, bNome, new RectangleF(Margem, y, largura, AlturaNome), esquerda);
        y += AlturaNome;
        g.DrawString(DataHora, fInfo, bData, new RectangleF(Margem, y, largura, AlturaInfo), esquerda);
        y += AlturaInfo;
        g.DrawString(Procedimento, fInfo, bProc, new RectangleF(Margem, y, largura, AlturaInfo), esquerda);
    }

    // ----- "Confirmar" e "Recusar": lado a lado, centralizados, na base -----
    void DesenharBotoes(Graphics g, Font fBotao)
    {
        int larguraConjunto = 2 * LarguraBotao + EspacoBotoes;
        int x = (Width - larguraConjunto) / 2;           // centraliza o conjunto
        int y = Height - Margem - AlturaBotao;           // distância fixa da borda inferior

        MiniRotulo.Desenhar(g, new Rectangle(x, y, LarguraBotao, AlturaBotao), "Confirmar",
            PaletaAgenda.VerdeFundo, PaletaAgenda.VerdeBorda, Cores.Escuro, fBotao, 8);

        MiniRotulo.Desenhar(g, new Rectangle(x + LarguraBotao + EspacoBotoes, y, LarguraBotao, AlturaBotao),
            "Recusar", PaletaAgenda.VermelhoFundo, PaletaAgenda.VermelhoBorda, Cores.Escuro, fBotao, 8);
    }
}