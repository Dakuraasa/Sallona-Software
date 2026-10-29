using System.Drawing.Drawing2D;

namespace dashboardforms;

// ===== Paleta Sallona =====
static class Cores
{
    public static readonly Color Escuro = ColorTranslator.FromHtml("#260108");
    public static readonly Color Primaria = ColorTranslator.FromHtml("#BF4163");
    public static readonly Color Rosa = ColorTranslator.FromHtml("#D99D8F");
    public static readonly Color Vinho = ColorTranslator.FromHtml("#8C2C23");
    public static readonly Color Coral = ColorTranslator.FromHtml("#BF5F56");
    public static readonly Color Fundo = ColorTranslator.FromHtml("#F6ECEA");
    public static readonly Color Texto = ColorTranslator.FromHtml("#3A1A1F");
    public static readonly Color Cinza = ColorTranslator.FromHtml("#8E7A7D");
    public static readonly Color Verde = ColorTranslator.FromHtml("#2E9E5B");
}

static class Util
{
    public static GraphicsPath Arredondar(Rectangle r, int raio)
    {
        int d = raio * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

// ===== Painel com cantos arredondados (cards) =====
class Card : Panel
{
    public int Raio { get; set; } = 18;
    public Color Preenchimento { get; set; } = Color.White;

    public Card()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Padding = new Padding(18);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var b = new SolidBrush(Parent?.BackColor ?? Cores.Fundo);
        e.Graphics.FillRectangle(b, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Util.Arredondar(new Rectangle(0, 0, Width - 1, Height - 1), Raio);
        using var br = new SolidBrush(Preenchimento);
        e.Graphics.FillPath(br, path);
        base.OnPaint(e);
    }
}

// ===== Botão com animação de hover e clique =====
class BotaoAnimado : Button
{
    Color corNormal = Color.White;
    float t, alvo;
    bool pressionado;
    readonly System.Windows.Forms.Timer timer = new() { Interval = 15 };
    public int Raio { get; set; } = 10;

    public BotaoAnimado()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        timer.Tick += (_, _) =>
        {
            t += (alvo - t) * 0.3f;
            if (Math.Abs(alvo - t) < 0.02f) { t = alvo; timer.Stop(); }
            Invalidate();
        };
    }

    public override Color BackColor
    {
        get => base.BackColor;
        set { base.BackColor = value; corNormal = value; Invalidate(); }
    }

    Color CorHover => corNormal.GetBrightness() > 0.6f
        ? Misturar(corNormal, Cores.Rosa, 0.35f)
        : Misturar(corNormal, Color.White, 0.15f);

    static Color Misturar(Color a, Color b, float k) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * k), (int)(a.G + (b.G - a.G) * k), (int)(a.B + (b.B - a.B) * k));

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); alvo = 1; timer.Start(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); alvo = 0; pressionado = false; timer.Start(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); pressionado = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pressionado = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var fundo = new SolidBrush(Parent?.BackColor ?? Color.White))
            g.FillRectangle(fundo, ClientRectangle);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (pressionado) r.Inflate(-2, -2);
        using var path = Util.Arredondar(r, Raio);
        using var br = new SolidBrush(Misturar(corNormal, CorHover, t));
        g.FillPath(br, path);

        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        flags |= TextAlign == ContentAlignment.MiddleLeft ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter;
        var area = new Rectangle(Padding.Left, 0, Width - Padding.Horizontal, Height);
        TextRenderer.DrawText(g, Text, Font, area, ForeColor, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}

// ===== Gráfico de barras (com animação de crescimento) =====
class GraficoBarras : Control
{
    public string[] Rotulos = Array.Empty<string>();
    public float[] Valores = Array.Empty<float>();
    public int Destaque = -1;
    int hover = -1;

    float progresso = 1f;
    readonly System.Windows.Forms.Timer anim = new() { Interval = 15 };

    public void Animar()
    {
        progresso = 0f;
        anim.Tick -= Passo;
        anim.Tick += Passo;
        anim.Start();
    }

    void Passo(object? s, EventArgs e)
    {
        progresso += 0.045f;
        if (progresso >= 1f) { progresso = 1f; anim.Stop(); }
        Invalidate();
    }

    public GraficoBarras()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    Rectangle Area => new(52, 8, Math.Max(10, Width - 60), Math.Max(10, Height - 40));

    int IndiceEm(Point p)
    {
        if (Valores.Length == 0) return -1;
        var a = Area;
        float slot = a.Width / (float)Valores.Length;
        int i = (int)((p.X - a.X) / slot);
        return p.X >= a.X && i >= 0 && i < Valores.Length ? i : -1;
    }

    protected override void OnMouseMove(MouseEventArgs e) { int i = IndiceEm(e.Location); if (i != hover) { hover = i; Invalidate(); } }
    protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        if (Valores.Length == 0) return;

        var a = Area;
        float max = Valores.Max();
        float passo = (float)Math.Pow(10, Math.Floor(Math.Log10(Math.Max(1, max))));
        float topo = (float)Math.Ceiling(max / passo) * passo;
        using var fonte = new Font("Segoe UI", 8.5f);
        using var pincelTxt = new SolidBrush(Cores.Cinza);
        using var linha = new Pen(Color.FromArgb(235, 225, 223));

        for (int k = 0; k <= 4; k++)
        {
            float y = a.Bottom - a.Height * k / 4f;
            g.DrawLine(linha, a.X, y, a.Right, y);
            string t = $"{topo * k / 4000f:0.#}k";
            var sz = g.MeasureString(t, fonte);
            g.DrawString(t, fonte, pincelTxt, a.X - sz.Width - 6, y - sz.Height / 2);
        }

        float slotW = a.Width / (float)Valores.Length;
        float larg = Math.Min(48, slotW * 0.55f);
        int ativo = hover >= 0 ? hover : Destaque;

        for (int i = 0; i < Valores.Length; i++)
        {
            float h = a.Height * Valores[i] / topo * (1 - (float)Math.Pow(1 - progresso, 3));
            float x = a.X + slotW * i + (slotW - larg) / 2;
            var r = new Rectangle((int)x, (int)(a.Bottom - h), (int)larg, (int)h);
            if (r.Height < 6) r.Height = 6;
            using var path = Util.Arredondar(r, 8);
            using var br = new SolidBrush(i == ativo ? Cores.Primaria : Cores.Rosa);
            g.FillPath(br, path);

            using var fn = new Font("Segoe UI", 9f, i == ativo ? FontStyle.Bold : FontStyle.Regular);
            using var bt = new SolidBrush(i == ativo ? Cores.Escuro : Cores.Cinza);
            var s = g.MeasureString(Rotulos[i], fn);
            g.DrawString(Rotulos[i], fn, bt, x + larg / 2 - s.Width / 2, a.Bottom + 8);

            if (i == ativo)
            {
                string v = $"R$ {Valores[i]:N0}";
                using var fv = new Font("Segoe UI", 9f, FontStyle.Bold);
                var sv = g.MeasureString(v, fv);
                g.DrawString(v, fv, bt, x + larg / 2 - sv.Width / 2, r.Y - sv.Height - 2);
            }
        }
    }
}

// ===== Lista com barras de progresso =====
class ListaProgresso : Control
{
    public List<(string Nome, int Pct, Color Cor)> Itens = new();

    public ListaProgresso()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (Itens.Count == 0) return;
        float linhaH = Height / (float)Itens.Count;
        using var f = new Font("Segoe UI", 9.5f);
        using var fb = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var t = new SolidBrush(Cores.Texto);
        using var trilho = new SolidBrush(Color.FromArgb(240, 230, 228));

        for (int i = 0; i < Itens.Count; i++)
        {
            float y = i * linhaH + 2;
            var (nome, pct, cor) = Itens[i];
            g.DrawString(nome, f, t, 0, y);
            string p = pct + "%";
            var sz = g.MeasureString(p, fb);
            g.DrawString(p, fb, t, Width - sz.Width, y);
            var rt = new Rectangle(0, (int)(y + 24), Width, 8);
            using var pt = Util.Arredondar(rt, 4);
            g.FillPath(trilho, pt);
            var rp = new Rectangle(0, rt.Y, Math.Max(8, Width * pct / 100), 8);
            using var pp = Util.Arredondar(rp, 4);
            using var bp = new SolidBrush(cor);
            g.FillPath(bp, pp);
        }
    }
}

// ===== Lista simples (título de seção / item + valor à direita) =====
class ListaLinhas : Control
{
    public List<(string Texto, string? Direita, Color Cor)> Itens = new();

    public ListaLinhas()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using var f = new Font("Segoe UI", 9.5f);
        using var fs = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var t = new SolidBrush(Cores.Texto);
        using var c = new SolidBrush(Cores.Cinza);
        float y = 0;
        foreach (var (txt, dir, cor) in Itens)
        {
            if (dir == null) { y += 4; g.DrawString(txt.ToUpper(), fs, c, 0, y); y += 22; continue; }
            g.DrawString(txt, f, t, 0, y);
            using var bd = new SolidBrush(cor);
            var sz = g.MeasureString(dir, f);
            g.DrawString(dir, f, bd, Width - sz.Width, y);
            y += 22;
        }
    }
}