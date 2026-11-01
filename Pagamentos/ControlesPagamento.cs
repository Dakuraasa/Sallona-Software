using System.Drawing.Drawing2D;
using dashboardforms.Clientes;   // Formas.Caminho

namespace dashboardforms.Pagamentos;

// Cores usadas pelos controles desta tela (iguais às da tela de Clientes)
static class CoresPagamento
{
    public static readonly Color Primaria = Color.FromArgb(191, 65, 99);
    public static readonly Color Texto = Color.FromArgb(43, 42, 76);
    public static readonly Color Suave = Color.FromArgb(120, 118, 135);
    public static readonly Color Borda = Color.FromArgb(232, 226, 223);
    public static readonly Color RosaClaro = Color.FromArgb(246, 232, 237);
    public static readonly Color Rosa = Color.FromArgb(217, 157, 143);
    public static readonly Color Verde = Color.FromArgb(46, 158, 91);
}

// ===== Opção de pagamento clicável (estilo "radio card") =====
class OpcaoPagamento : Control
{
    public string Icone { get; set; } = "";
    public string Titulo { get; set; } = "Opção";
    public string Descricao { get; set; } = "";
    public bool Selecionada { get; set; }
    bool hover;

    public OpcaoPagamento()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Size = new Size(300, 52);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 60 || Height < 40) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var r = new Rectangle(1, 1, Width - 3, Height - 3);
        using (var caminho = Formas.Caminho(r, 12))
        using (var fundo = new SolidBrush(Selecionada ? CoresPagamento.RosaClaro : Color.White))
        using (var borda = new Pen(Selecionada ? CoresPagamento.Primaria : hover ? CoresPagamento.Rosa : CoresPagamento.Borda,
                                   Selecionada ? 1.6f : 1f))
        {
            g.FillPath(fundo, caminho);
            g.DrawPath(borda, caminho);
        }

        // ícone
        var ri = new Rectangle(12, (Height - 34) / 2, 34, 34);
        using (var ci = Formas.Caminho(ri, 8))
        using (var fi = new SolidBrush(Selecionada ? Color.White : Color.FromArgb(247, 243, 241)))
            g.FillPath(fi, ci);
        using (var fonteIcone = new Font("Segoe UI Emoji", 11F))
            TextRenderer.DrawText(g, Icone, fonteIcone, ri, CoresPagamento.Texto,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        // textos
        using (var ft = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            TextRenderer.DrawText(g, Titulo, ft, new Point(58, Height / 2 - 19), CoresPagamento.Texto);
        using (var fd = new Font("Segoe UI", 8.5F))
            TextRenderer.DrawText(g, Descricao, fd, new Point(58, Height / 2 + 1), CoresPagamento.Suave);

        // "radio" à direita
        var rr = new Rectangle(Width - 34, Height / 2 - 9, 18, 18);
        using (var anel = new Pen(Selecionada ? CoresPagamento.Primaria : CoresPagamento.Borda, Selecionada ? 2f : 1.4f))
        using (var branco = new SolidBrush(Color.White))
        {
            g.FillEllipse(branco, rr);
            g.DrawEllipse(anel, rr);
        }
        if (Selecionada)
        {
            using var miolo = new SolidBrush(CoresPagamento.Primaria);
            g.FillEllipse(miolo, rr.X + 5, rr.Y + 5, 8, 8);
        }
    }
}

// ===== Indicador de etapas: Atendimento -> Pagamento -> Concluído =====
class Etapas : Control
{
    readonly string[] nomes = { "Atendimento", "Pagamento", "Concluído" };
    public int Atual { get; set; } = 1;

    public Etapas()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Size = new Size(540, 26);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var fonte = new Font("Segoe UI", 8.5F);
        using var fonteAtual = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        using var fonteNum = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        using var linha = new Pen(CoresPagamento.Borda, 1.2f);
        var centro = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;

        int x = 0, cy = Height / 2;
        for (int i = 0; i < nomes.Length; i++)
        {
            var c = new Rectangle(x, cy - 10, 20, 20);
            bool feita = i < Atual || (i == Atual && i == nomes.Length - 1);
            bool atual = i == Atual && !feita;

            if (feita)
            {
                using var p = new Pen(CoresPagamento.Verde, 1.5f);
                g.DrawEllipse(p, c);
                TextRenderer.DrawText(g, "✓", fonteNum, c, CoresPagamento.Verde, centro);
            }
            else if (atual)
            {
                using var b = new SolidBrush(CoresPagamento.Primaria);
                g.FillEllipse(b, c);
                TextRenderer.DrawText(g, (i + 1).ToString(), fonteNum, c, Color.White, centro);
            }
            else
            {
                using var p = new Pen(CoresPagamento.Borda, 1.4f);
                g.DrawEllipse(p, c);
                TextRenderer.DrawText(g, (i + 1).ToString(), fonteNum, c, CoresPagamento.Suave, centro);
            }

            var f = atual ? fonteAtual : fonte;
            var tam = TextRenderer.MeasureText(nomes[i], f);
            TextRenderer.DrawText(g, nomes[i], f, new Point(x + 26, cy - tam.Height / 2),
                atual ? CoresPagamento.Primaria : CoresPagamento.Suave);
            x += 26 + tam.Width + 12;

            if (i < nomes.Length - 1)
            {
                g.DrawLine(linha, x, cy, x + 50, cy);
                x += 62;
            }
        }
    }
}

// ===== Painel com só os cantos de baixo arredondados (bloco do total) =====
class BlocoInferior : Panel
{
    public int Raio { get; set; } = 13;
    public Color Preenchimento { get; set; } = Color.FromArgb(46, 48, 84);

    public BlocoInferior()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < Raio * 2 || Height < Raio * 2) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int d = Raio * 2;
        using var p = new GraphicsPath();
        p.AddLine(0, 0, Width, 0);
        p.AddArc(Width - d - 1, Height - d - 1, d, d, 0, 90);
        p.AddArc(0, Height - d - 1, d, d, 90, 90);
        p.CloseFigure();
        using var b = new SolidBrush(Preenchimento);
        g.FillPath(b, p);
    }
}
