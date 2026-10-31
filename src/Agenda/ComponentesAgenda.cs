using System.Drawing.Drawing2D;
using System.Globalization;
using dashboardforms;

namespace dashboardforms.Agenda;

// ===== Card com borda (herda do Card do projeto) =====
class CardBorda : Card
{
    public Color CorBorda { get; set; } = Cores.Primaria;
    public int EspessuraBorda { get; set; } = 3;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var area = new Rectangle(0, 0, Width - 1, Height - 1);
        area.Inflate(-EspessuraBorda / 2, -EspessuraBorda / 2);

        using var path = Util.Arredondar(area, Raio);
        using var br = new SolidBrush(Preenchimento);
        using var caneta = new Pen(CorBorda, EspessuraBorda);

        g.FillPath(br, path);
        g.DrawPath(caneta, path);
    }
}

// ===== Minicalendário =====
class MiniCalendario : Control
{
    const int AlturaTopo = 34;
    const int AlturaSemana = 26;
    const int AlturaLinha = 32;

    // mês exibido (sempre o dia 1)
    DateTime mes = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public MiniCalendario()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        float larguraCasa = Width / 7f;

        using var fTitulo = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var fSemana = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fDia = new Font("Segoe UI", 9.5f);
        using var bTitulo = new SolidBrush(Cores.Escuro);
        using var bSemana = new SolidBrush(Cores.Cinza);
        using var bDia = new SolidBrush(Cores.Texto);
        using var bApagado = new SolidBrush(Color.FromArgb(110, Cores.Cinza));
        using var centro = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        // título: mês/ano
        string titulo = mes.ToString("MMMM/yyyy", new CultureInfo("pt-BR"));
        g.DrawString(titulo, fTitulo, bTitulo, 0, 4);

        // linha separadora abaixo do título
        using var linha = new Pen(Cores.Rosa, 1.5f);
        g.DrawLine(linha, 0, AlturaTopo - 4, Width, AlturaTopo - 4);

        // letras dos dias da semana
        string[] letras = { "D", "S", "T", "Q", "Q", "S", "S" };
        for (int c = 0; c < 7; c++)
        {
            var r = new RectangleF(c * larguraCasa, AlturaTopo, larguraCasa, AlturaSemana);
            g.DrawString(letras[c], fSemana, bSemana, r, centro);
        }

        // números: grade fixa de 42 casas (6 linhas x 7 colunas)
        DateTime inicio = mes.AddDays(-(int)mes.DayOfWeek);
        for (int i = 0; i < 42; i++)
        {
            DateTime data = inicio.AddDays(i);
            var r = new RectangleF(
                (i % 7) * larguraCasa,
                AlturaTopo + AlturaSemana + (i / 7) * AlturaLinha,
                larguraCasa, AlturaLinha);

            bool doMes = data.Month == mes.Month;
            g.DrawString(data.Day.ToString(), fDia, doMes ? bDia : bApagado, r, centro);
        }
    }
}