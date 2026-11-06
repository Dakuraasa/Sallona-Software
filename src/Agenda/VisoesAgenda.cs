using System.Drawing.Drawing2D;
using System.Globalization;

namespace dashboardforms.Agenda;

record ColunaGrade(string Titulo, string Subtitulo, DateTime Data, int? ProfissionalId)
{
    public bool Hoje => Data.Date == DateTime.Today;
    public bool Fechado => !Expediente.Aberto(Data);
}

static class DesenhoAgenda
{
    public static readonly CultureInfo Br = new("pt-BR");

    public static void Cartao(Graphics g, RectangleF r, Agendamento a, bool hover, bool mostrarProfissional)
    {
        if (r.Width < 6 || r.Height < 6) return;
        var cor = StatusAgendamento.Cor(a.Status);
        using var path = Util.Arredondar(Rectangle.Round(r), 7);
        using (var fundo = new SolidBrush(StatusAgendamento.Fundo(a.Status))) g.FillPath(fundo, path);

        var barra = new RectangleF(r.X, r.Y, 4, r.Height);
        var clip = g.Clip;
        g.SetClip(path);
        using (var b = new SolidBrush(cor)) g.FillRectangle(b, barra);
        g.Clip = clip;

        if (hover)
        {
            using var p = new Pen(cor, 1.6f);
            g.DrawPath(p, path);
        }

        bool cancelado = a.Status == StatusAgendamento.Cancelado;
        using var fHora = new Font("Segoe UI", 7.8f, FontStyle.Bold);
        using var fNome = new Font("Segoe UI", 8.8f, cancelado ? FontStyle.Bold | FontStyle.Strikeout : FontStyle.Bold);
        using var fInfo = new Font("Segoe UI", 8f);
        using var bCor = new SolidBrush(cor);
        using var bTexto = new SolidBrush(cancelado ? Cores.Cinza : Cores.Escuro);
        using var bInfo = new SolidBrush(Cores.Cinza);
        using var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

        float x = r.X + 9, w = r.Width - 13, y = r.Y + 4;
        float linha = 15;
        if (r.Height < 34)
        {
            g.DrawString($"{a.Inicio:HH:mm}  {a.ClienteNome}", fNome, bTexto, new RectangleF(x, r.Y + (r.Height - linha) / 2, w, linha), fmt);
            return;
        }
        g.DrawString($"{a.Horario}  ·  {a.Status}", fHora, bCor, new RectangleF(x, y, w, linha), fmt); y += linha;
        g.DrawString(a.ClienteNome, fNome, bTexto, new RectangleF(x, y, w, linha + 1), fmt); y += linha + 1;
        if (y + linha <= r.Bottom - 2) { g.DrawString(a.Descricao, fInfo, bInfo, new RectangleF(x, y, w, linha), fmt); y += linha; }
        if (mostrarProfissional && y + linha <= r.Bottom - 2)
            g.DrawString(a.ProfissionalNome, fInfo, bInfo, new RectangleF(x, y, w, linha), fmt);
    }

    public static List<(Agendamento A, int Faixa, int Faixas)> DistribuirFaixas(List<Agendamento> lista)
    {
        var res = new List<(Agendamento, int, int)>();
        var ordenados = lista.OrderBy(a => a.Inicio).ThenByDescending(a => a.DuracaoMin).ToList();
        int i = 0;
        while (i < ordenados.Count)
        {
            var grupo = new List<Agendamento> { ordenados[i] };
            var fimGrupo = ordenados[i].Fim;
            int j = i + 1;
            while (j < ordenados.Count && ordenados[j].Inicio < fimGrupo)
            {
                grupo.Add(ordenados[j]);
                if (ordenados[j].Fim > fimGrupo) fimGrupo = ordenados[j].Fim;
                j++;
            }
            var fimFaixas = new List<DateTime>();
            var faixaDe = new List<int>();
            foreach (var a in grupo)
            {
                int f = fimFaixas.FindIndex(fim => fim <= a.Inicio);
                if (f < 0) { fimFaixas.Add(a.Fim); f = fimFaixas.Count - 1; }
                else fimFaixas[f] = a.Fim;
                faixaDe.Add(f);
            }
            for (int k = 0; k < grupo.Count; k++) res.Add((grupo[k], faixaDe[k], fimFaixas.Count));
            i = j;
        }
        return res;
    }
}

class GradeAgenda : UserControl
{
    const int Gutter = 58;
    const int AlturaCabecalho = 54;
    const int AlturaHora = 64;

    readonly Panel cabecalho;
    readonly Panel rolagem;
    readonly CorpoGrade corpo;

    public List<ColunaGrade> Colunas { get; private set; } = new();
    public List<Agendamento> Agendamentos { get; private set; } = new();
    public bool MostrarProfissional { get; set; }

    public event Action<Agendamento>? AgendamentoClicado;
    public event Action<Agendamento>? AgendamentoDuploClique;
    public event Action<DateTime, int?>? HorarioClicado;

    int horaInicio = 8, horaFim = 18;

    public GradeAgenda()
    {
        DoubleBuffered = true;
        BackColor = Color.White;

        corpo = new CorpoGrade(this) { Location = new Point(0, 0) };
        rolagem = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        rolagem.Controls.Add(corpo);
        rolagem.Resize += (_, _) => AjustarCorpo();

        cabecalho = new CabecalhoGrade(this) { Dock = DockStyle.Top, Height = AlturaCabecalho };
        Controls.Add(rolagem);
        Controls.Add(cabecalho);
    }

    public void Mostrar(List<ColunaGrade> colunas, List<Agendamento> agendamentos)
    {
        Colunas = colunas;
        Agendamentos = agendamentos;

        int ini = Expediente.Abertura / 60, fim = (Expediente.Fechamento + 59) / 60;
        foreach (var a in agendamentos)
        {
            ini = Math.Min(ini, a.Inicio.Hour);
            fim = Math.Max(fim, a.Fim.Date > a.Inicio.Date ? 24 : (a.Fim.Hour * 60 + a.Fim.Minute + 59) / 60);
        }
        horaInicio = Math.Clamp(ini, 0, 23);
        horaFim = Math.Clamp(Math.Max(fim, horaInicio + 1), 1, 24);

        AjustarCorpo();
        cabecalho.Invalidate();
        corpo.Invalidate();
    }

    public void RolarParaAgora()
    {
        var visiveis = Agendamentos.Where(a => Colunas.Any(c => c.Data.Date == a.Inicio.Date)).ToList();
        int alvo = Colunas.Any(c => c.Hoje) ? Math.Max(horaInicio, DateTime.Now.Hour - 1)
                 : visiveis.Count > 0 ? Math.Max(horaInicio, visiveis.Min(a => a.Inicio.Hour)) : horaInicio;
        int y = (alvo - horaInicio) * AlturaHora;
        rolagem.AutoScrollPosition = new Point(0, Math.Max(0, y));
    }

    void AjustarCorpo()
    {
        int largura = Math.Max(200, rolagem.ClientSize.Width);
        corpo.Size = new Size(largura, (horaFim - horaInicio) * AlturaHora + 12);
        cabecalho.Invalidate();
    }

    float LarguraColuna(int larguraTotal) => Colunas.Count == 0 ? 0 : (larguraTotal - Gutter) / (float)Colunas.Count;

    int YDe(DateTime t) => (int)Math.Round(((t.Hour - horaInicio) * 60 + t.Minute + t.Second / 60.0) * AlturaHora / 60.0) + 6;
    int YDeMinuto(int minuto) => (int)Math.Round((minuto - horaInicio * 60) * AlturaHora / 60.0) + 6;

    sealed class CabecalhoGrade : Panel
    {
        readonly GradeAgenda g;
        public CabecalhoGrade(GradeAgenda grade) { g = grade; DoubleBuffered = true; BackColor = Color.White; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var gr = e.Graphics;
            gr.SmoothingMode = SmoothingMode.AntiAlias;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int largura = g.corpo.Width;
            float lc = g.LarguraColuna(largura);
            using var fTit = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var fSub = new Font("Segoe UI", 8.5f);
            using var bTit = new SolidBrush(Cores.Escuro);
            using var bSub = new SolidBrush(Cores.Cinza);
            using var bBranco = new SolidBrush(Color.White);
            using var bRosa = new SolidBrush(Cores.Rosa);
            using var centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            using var linha = new Pen(Cores.Borda);

            for (int i = 0; i < g.Colunas.Count; i++)
            {
                var c = g.Colunas[i];
                float x = Gutter + i * lc;
                if (c.Hoje)
                {
                    using var path = Util.Arredondar(Rectangle.Round(new RectangleF(x + 6, 6, lc - 12, Height - 12)), 10);
                    using var b = new SolidBrush(Cores.Escuro);
                    gr.FillPath(b, path);
                }
                gr.DrawString(c.Titulo, fTit, c.Hoje ? bBranco : bTit, new RectangleF(x, 8, lc, 22), centro);
                gr.DrawString(c.Subtitulo, fSub, c.Hoje ? bRosa : bSub, new RectangleF(x, 28, lc, 18), centro);
                if (i > 0) gr.DrawLine(linha, x, 10, x, Height - 10);
            }
            gr.DrawLine(linha, 0, Height - 1, Width, Height - 1);
        }
    }

    sealed class CorpoGrade : Control
    {
        readonly GradeAgenda g;
        readonly List<(RectangleF R, Agendamento A)> areas = new();
        Agendamento? hover;

        public CorpoGrade(GradeAgenda grade)
        {
            g = grade;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var gr = e.Graphics;
            gr.SmoothingMode = SmoothingMode.AntiAlias;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            areas.Clear();

            float lc = g.LarguraColuna(Width);
            using var fHora = new Font("Segoe UI", 8.5f);
            using var fPequena = new Font("Segoe UI", 8f, FontStyle.Italic);
            using var bHora = new SolidBrush(Cores.Cinza);
            using var linhaHora = new Pen(Color.FromArgb(236, 228, 226));
            using var linhaMeia = new Pen(Color.FromArgb(246, 240, 239)) { DashStyle = DashStyle.Dash };
            using var linhaCol = new Pen(Cores.Borda);
            using var fechado = new HatchBrush(HatchStyle.WideUpwardDiagonal, Color.FromArgb(30, Cores.Cinza), Color.FromArgb(250, 247, 246));
            using var foraExpediente = new SolidBrush(Color.FromArgb(250, 247, 246));
            using var almoco = new SolidBrush(Color.FromArgb(252, 244, 240));

            for (int i = 0; i < g.Colunas.Count; i++)
            {
                var c = g.Colunas[i];
                float x = Gutter + i * lc;
                if (c.Fechado)
                {
                    gr.FillRectangle(fechado, x, 0, lc, Height);
                    continue;
                }
                int yAbre = g.YDeMinuto(Expediente.Abertura), yFecha = g.YDeMinuto(Expediente.Fechamento);
                if (yAbre > 0) gr.FillRectangle(foraExpediente, x, 0, lc, yAbre);
                if (yFecha < Height) gr.FillRectangle(foraExpediente, x, yFecha, lc, Height - yFecha);
                if (Expediente.TemAlmoco)
                {
                    int y1 = g.YDeMinuto(Expediente.AlmocoInicio), y2 = g.YDeMinuto(Expediente.AlmocoFim);
                    gr.FillRectangle(almoco, x, y1, lc, y2 - y1);
                    gr.DrawString("Almoço", fPequena, bHora, x + 8, y1 + 4);
                }
            }

            for (int h = g.horaInicio; h <= g.horaFim; h++)
            {
                int y = g.YDeMinuto(h * 60);
                gr.DrawLine(linhaHora, Gutter - 6, y, Width, y);
                if (h < g.horaFim)
                {
                    int ym = g.YDeMinuto(h * 60 + 30);
                    gr.DrawLine(linhaMeia, Gutter, ym, Width, ym);
                    gr.DrawString($"{h:00}:00", fHora, bHora, 8, y - 8);
                }
            }
            for (int i = 0; i <= g.Colunas.Count; i++)
                gr.DrawLine(linhaCol, Gutter + i * lc, 0, Gutter + i * lc, Height);

            for (int i = 0; i < g.Colunas.Count; i++)
            {
                var c = g.Colunas[i];
                float x = Gutter + i * lc;
                var doCol = g.Agendamentos.Where(a => a.Inicio.Date == c.Data.Date &&
                                                      (c.ProfissionalId == null || a.ProfissionalId == c.ProfissionalId)).ToList();
                foreach (var (a, faixa, faixas) in DesenhoAgenda.DistribuirFaixas(doCol))
                {
                    float wFaixa = (lc - 8) / faixas;
                    int y1 = g.YDe(a.Inicio), y2 = g.YDe(a.Fim);
                    var r = new RectangleF(x + 4 + faixa * wFaixa, y1 + 1, wFaixa - 3, Math.Max(18, y2 - y1 - 2));
                    DesenhoAgenda.Cartao(gr, r, a, hover?.Id == a.Id, g.MostrarProfissional);
                    areas.Add((r, a));
                }

                if (c.Hoje)
                {
                    int y = g.YDe(DateTime.Now);
                    if (y > 0 && y < Height)
                    {
                        using var p = new Pen(Cores.Coral, 2f);
                        using var b = new SolidBrush(Cores.Coral);
                        gr.DrawLine(p, x, y, x + lc, y);
                        gr.FillEllipse(b, x - 4, y - 4, 8, 8);
                    }
                }
            }
        }

        Agendamento? NoPonto(Point p)
        {
            for (int i = areas.Count - 1; i >= 0; i--)
                if (areas[i].R.Contains(p)) return areas[i].A;
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var a = NoPonto(e.Location);
            int col = ColunaEm(e.X);
            Cursor = a != null || (col >= 0 && !g.Colunas[col].Fechado) ? Cursors.Hand : Cursors.Default;
            if (a?.Id != hover?.Id) { hover = a; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hover != null) { hover = null; Invalidate(); }
        }

        int ColunaEm(int x)
        {
            float lc = g.LarguraColuna(Width);
            if (x < Gutter || lc <= 0) return -1;
            int i = (int)((x - Gutter) / lc);
            return i >= 0 && i < g.Colunas.Count ? i : -1;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            if (NoPonto(e.Location) is { } a) { g.AgendamentoClicado?.Invoke(a); return; }
            int col = ColunaEm(e.X);
            if (col < 0 || g.Colunas[col].Fechado) return;
            int minuto = (int)((e.Y - 6) * 60.0 / AlturaHora) + g.horaInicio * 60;
            minuto = minuto / 30 * 30;
            g.HorarioClicado?.Invoke(g.Colunas[col].Data.Date.AddMinutes(minuto), g.Colunas[col].ProfissionalId);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (NoPonto(e.Location) is { } a) g.AgendamentoDuploClique?.Invoke(a);
        }
    }
}

class VisaoMes : Control
{
    const int AlturaSemana = 30;

    DateTime mes = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    List<Agendamento> agendamentos = new();
    int hoverCelula = -1;

    public event Action<DateTime>? DiaClicado;

    public VisaoMes()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    public static DateTime InicioGrade(DateTime mes) => mes.AddDays(-(int)mes.DayOfWeek);

    public void Mostrar(DateTime mes, List<Agendamento> lista)
    {
        this.mes = new DateTime(mes.Year, mes.Month, 1);
        agendamentos = lista;
        Invalidate();
    }

    RectangleF Celula(int i)
    {
        float w = Width / 7f, h = (Height - AlturaSemana) / 6f;
        return new RectangleF(i % 7 * w, AlturaSemana + i / 7 * h, w, h);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var fSemana = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fDia = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var fItem = new Font("Segoe UI", 8f);
        using var fMais = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var bCinza = new SolidBrush(Cores.Cinza);
        using var bEscuro = new SolidBrush(Cores.Escuro);
        using var bApagado = new SolidBrush(Color.FromArgb(120, Cores.Cinza));
        using var bBranco = new SolidBrush(Color.White);
        using var bHoje = new SolidBrush(Cores.Primaria);
        using var bHover = new SolidBrush(Color.FromArgb(252, 246, 245));
        using var fechado = new HatchBrush(HatchStyle.WideUpwardDiagonal, Color.FromArgb(25, Cores.Cinza), Color.FromArgb(251, 249, 248));
        using var linha = new Pen(Cores.Borda);
        using var centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var corte = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

        string[] nomes = { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };
        float w = Width / 7f;
        for (int c = 0; c < 7; c++)
            g.DrawString(nomes[c], fSemana, bCinza, new RectangleF(c * w, 0, w, AlturaSemana), centro);

        var inicio = InicioGrade(mes);
        var porDia = agendamentos.GroupBy(a => a.Inicio.Date).ToDictionary(x => x.Key, x => x.OrderBy(a => a.Inicio).ToList());

        for (int i = 0; i < 42; i++)
        {
            var dia = inicio.AddDays(i);
            var r = Celula(i);
            bool doMes = dia.Month == mes.Month;
            if (!Expediente.Aberto(dia)) g.FillRectangle(fechado, r);
            else if (i == hoverCelula) g.FillRectangle(bHover, r);
            g.DrawRectangle(linha, r.X, r.Y, r.Width, r.Height);

            var rNum = new RectangleF(r.X + 6, r.Y + 5, 26, 24);
            if (dia == DateTime.Today)
            {
                g.FillEllipse(bHoje, rNum);
                g.DrawString(dia.Day.ToString(), fDia, bBranco, rNum, centro);
            }
            else g.DrawString(dia.Day.ToString(), fDia, doMes ? bEscuro : bApagado, rNum, centro);

            if (!porDia.TryGetValue(dia, out var lista)) continue;
            float y = r.Y + 32;
            int cabe = Math.Max(0, (int)((r.Bottom - y - 2) / 18));
            int mostrar = lista.Count > cabe ? Math.Max(0, cabe - 1) : lista.Count;
            for (int k = 0; k < mostrar; k++)
            {
                var a = lista[k];
                var rItem = new RectangleF(r.X + 4, y, r.Width - 8, 16);
                using (var path = Util.Arredondar(Rectangle.Round(rItem), 5))
                using (var b = new SolidBrush(StatusAgendamento.Fundo(a.Status))) g.FillPath(b, path);
                using (var bc = new SolidBrush(StatusAgendamento.Cor(a.Status))) g.FillEllipse(bc, rItem.X + 4, rItem.Y + 5, 6, 6);
                g.DrawString($"{a.Inicio:HH:mm} {a.ClienteNome}", fItem, doMes ? bEscuro : bApagado,
                             new RectangleF(rItem.X + 13, rItem.Y + 1, rItem.Width - 15, 15), corte);
                y += 18;
            }
            if (lista.Count > mostrar)
                g.DrawString($"+ {lista.Count - mostrar} mais", fMais, bHoje, r.X + 8, y);
        }
    }

    int CelulaEm(Point p)
    {
        if (p.Y < AlturaSemana) return -1;
        for (int i = 0; i < 42; i++) if (Celula(i).Contains(p)) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int c = CelulaEm(e.Location);
        Cursor = c >= 0 ? Cursors.Hand : Cursors.Default;
        if (c != hoverCelula) { hoverCelula = c; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hoverCelula = -1;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int c = CelulaEm(e.Location);
        if (c >= 0) DiaClicado?.Invoke(InicioGrade(mes).AddDays(c));
    }
}

class VisaoAno : Control
{
    int ano = DateTime.Today.Year;
    Dictionary<DateTime, int> contagem = new();
    readonly List<(RectangleF R, DateTime Dia)> dias = new();
    readonly List<(RectangleF R, DateTime Mes)> meses = new();

    public event Action<DateTime>? DiaClicado;
    public event Action<DateTime>? MesClicado;

    public VisaoAno()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    public void Mostrar(int ano, List<Agendamento> lista)
    {
        this.ano = ano;
        contagem = lista.Where(a => a.OcupaHorario).GroupBy(a => a.Inicio.Date).ToDictionary(g => g.Key, g => g.Count());
        Invalidate();
    }

    static Color CorDe(int n) => n switch
    {
        0 => Color.Transparent,
        <= 2 => Color.FromArgb(246, 222, 228),
        <= 5 => Color.FromArgb(226, 160, 180),
        _ => Cores.Primaria
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        dias.Clear(); meses.Clear();

        using var fMes = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        using var fLetra = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var fDia = new Font("Segoe UI", 8f);
        using var fLegenda = new Font("Segoe UI", 8.5f);
        using var bMes = new SolidBrush(Cores.Escuro);
        using var bCinza = new SolidBrush(Cores.Cinza);
        using var bTexto = new SolidBrush(Cores.Texto);
        using var bBranco = new SolidBrush(Color.White);
        using var pHoje = new Pen(Cores.Coral, 2f);
        using var centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        const int legenda = 30;
        float wBloco = Width / 4f, hBloco = (Height - legenda) / 3f;
        string[] letras = { "D", "S", "T", "Q", "Q", "S", "S" };

        for (int m = 0; m < 12; m++)
        {
            var primeiro = new DateTime(ano, m + 1, 1);
            float bx = m % 4 * wBloco + 10, by = m / 4 * hBloco + 6;
            float bw = wBloco - 20;
            var rTitulo = new RectangleF(bx, by, bw, 22);
            string nome = DesenhoAgenda.Br.TextInfo.ToTitleCase(primeiro.ToString("MMMM", DesenhoAgenda.Br));
            g.DrawString(nome, fMes, bMes, rTitulo);
            meses.Add((rTitulo, primeiro));

            float cw = bw / 7f, ch = Math.Min(cw, (hBloco - 52) / 6f);
            for (int c = 0; c < 7; c++)
                g.DrawString(letras[c], fLetra, bCinza, new RectangleF(bx + c * cw, by + 24, cw, 14), centro);

            var ini = VisaoMes.InicioGrade(primeiro);
            for (int i = 0; i < 42; i++)
            {
                var d = ini.AddDays(i);
                if (d.Month != primeiro.Month) continue;
                var r = new RectangleF(bx + i % 7 * cw, by + 40 + i / 7 * ch, cw, ch);
                float tam = Math.Min(r.Width, r.Height) - 3;
                var circ = new RectangleF(r.X + (r.Width - tam) / 2, r.Y + (r.Height - tam) / 2, tam, tam);
                contagem.TryGetValue(d, out int n);
                if (n > 0) using (var b = new SolidBrush(CorDe(n))) g.FillEllipse(b, circ);
                if (d == DateTime.Today) g.DrawEllipse(pHoje, circ);
                var cor = n > 5 ? bBranco : !Expediente.Aberto(d) ? bCinza : bTexto;
                g.DrawString(d.Day.ToString(), fDia, cor, r, centro);
                dias.Add((r, d));
            }
        }

        float lx = 10, ly = Height - legenda + 6;
        g.DrawString("Agendamentos por dia:", fLegenda, bCinza, lx, ly);
        lx += 150;
        foreach (var (n, txt) in new[] { (1, "1–2"), (3, "3–5"), (6, "6 ou mais") })
        {
            using var b = new SolidBrush(CorDe(n));
            g.FillEllipse(b, lx, ly + 2, 14, 14);
            g.DrawString(txt, fLegenda, bCinza, lx + 18, ly);
            lx += 90;
        }
        g.DrawEllipse(pHoje, lx, ly + 2, 14, 14);
        g.DrawString("hoje", fLegenda, bCinza, lx + 18, ly);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = dias.Any(d => d.R.Contains(e.Location)) || meses.Any(m => m.R.Contains(e.Location)) ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        foreach (var (r, d) in dias) if (r.Contains(e.Location)) { DiaClicado?.Invoke(d); return; }
        foreach (var (r, m) in meses) if (r.Contains(e.Location)) { MesClicado?.Invoke(m); return; }
    }
}
