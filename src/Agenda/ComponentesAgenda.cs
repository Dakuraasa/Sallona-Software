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
    const int TamBotao = 24;

    // mês exibido (sempre o dia 1)
    DateTime mes = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    // data escolhida pelo usuário (null = nenhuma)
    DateTime? dataSelecionada;

    // controle do efeito de passar o mouse nos botões < >
    bool sobreAnterior, sobreProximo;

    // avisa quem estiver interessado (outros componentes) que a data mudou
    public event EventHandler? DataSelecionadaAlterada;

    public MiniCalendario()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    // ----- Propriedade pública: o formulário lê (e pode atribuir) a data -----
    public DateTime? DataSelecionada
    {
        get => dataSelecionada;
        set => Selecionar(value);   // passa pela mesma validação do clique
    }

    // ----- Regra central: domingo nunca é selecionável -----
    // (o desenho do círculo rasurado também usa este método)
    static bool EhSelecionavel(DateTime data) => data.DayOfWeek != DayOfWeek.Sunday;

    // ----- Único lugar onde a seleção realmente muda -----
    void Selecionar(DateTime? nova)
    {
        if (nova.HasValue)
        {
            DateTime dia = nova.Value.Date;          // descarta a hora
            if (!EhSelecionavel(dia)) return;        // domingo: ignora, nada muda
            nova = dia;
            mes = new DateTime(dia.Year, dia.Month, 1); // mostra o mês da data escolhida
        }

        bool mudou = dataSelecionada != nova;
        dataSelecionada = nova;
        Invalidate();                                // pede para redesenhar
        if (mudou) OnDataSelecionadaAlterada();
    }

    protected virtual void OnDataSelecionadaAlterada()
        => DataSelecionadaAlterada?.Invoke(this, EventArgs.Empty);

    // ----- Navegação: -1 = mês anterior, +1 = próximo (AddMonths cuida do ano) -----
    void MudarMes(int delta)
    {
        mes = mes.AddMonths(delta);
        Invalidate();
    }

    // ----- Auxiliares de geometria (usados no desenho E no clique) -----
    float LarguraCasa => Width / 7f;

    Rectangle BotaoAnterior => new(Width - 2 * TamBotao - 4, 4, TamBotao, TamBotao);
    Rectangle BotaoProximo => new(Width - TamBotao, 4, TamBotao, TamBotao);

    DateTime InicioGrade() => mes.AddDays(-(int)mes.DayOfWeek);

    RectangleF RetanguloDoDia(int i) => new(
        (i % 7) * LarguraCasa,
        AlturaTopo + AlturaSemana + (i / 7) * AlturaLinha,
        LarguraCasa, AlturaLinha);

    // círculo proporcional à casa, centralizado nela
    // (usado pela seleção rosa e pelo círculo rasurado dos domingos)
    static RectangleF CirculoDaCasa(RectangleF r)
    {
        float d = Math.Min(r.Width, r.Height) - 7  ;
        return new RectangleF(r.X + (r.Width - d) / 2, r.Y + (r.Height - d) / 2, d, d);
    }

    // converte um ponto do mouse na data da casa clicada (ou null se fora da grade)
    DateTime? DiaNaPosicao(Point p)
    {
        int topoGrade = AlturaTopo + AlturaSemana;
        if (p.Y < topoGrade || p.X < 0 || p.X >= Width) return null;

        int coluna = (int)(p.X / LarguraCasa);
        int linha = (p.Y - topoGrade) / AlturaLinha;
        if (linha > 5) return null;

        return InicioGrade().AddDays(linha * 7 + coluna);
    }

    // ----- Mouse -----
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;

        if (BotaoAnterior.Contains(e.Location)) { MudarMes(-1); return; }
        if (BotaoProximo.Contains(e.Location)) { MudarMes(+1); return; }

        DateTime? data = DiaNaPosicao(e.Location);
        if (data.HasValue) Selecionar(data);   // Selecionar recusa domingos
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        bool ant = BotaoAnterior.Contains(e.Location);
        bool prox = BotaoProximo.Contains(e.Location);
        DateTime? dia = DiaNaPosicao(e.Location);

        // mãozinha só onde o clique faz algo
        Cursor = (ant || prox || (dia.HasValue && EhSelecionavel(dia.Value)))
            ? Cursors.Hand : Cursors.Default;

        if (ant != sobreAnterior || prox != sobreProximo)
        {
            sobreAnterior = ant;
            sobreProximo = prox;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        sobreAnterior = sobreProximo = false;
        Invalidate();
    }

    // ----- Desenho de um botão discreto (< ou >) -----
    static void DesenharBotao(Graphics g, Rectangle area, string texto, bool hover,
                              Font fonte, Brush corTexto, StringFormat centro)
    {
        if (hover)
        {
            using var path = Util.Arredondar(area, 8);
            using var fundo = new SolidBrush(Cores.Rosa);
            g.FillPath(fundo, path);
        }
        g.DrawString(texto, fonte, corTexto, area, centro);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var fTitulo = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var fSemana = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fDia = new Font("Segoe UI", 9.5f);
        using var bTitulo = new SolidBrush(Cores.Escuro);
        using var bSemana = new SolidBrush(Cores.Cinza);
        using var bDia = new SolidBrush(Cores.Texto);
        using var bApagado = new SolidBrush(Color.FromArgb(110, Cores.Cinza));
        using var bSelecao = new SolidBrush(Cores.Rosa);      // círculo do dia selecionado
        using var bDiaSel = new SolidBrush(Cores.Escuro);     // número sobre o círculo (contraste)
        using var bRasurado = new HatchBrush(HatchStyle.WideUpwardDiagonal,
                                             Color.FromArgb(90, Cores.Cinza), Color.Transparent); // listras dos domingos
        using var pBloqueado = new Pen(Color.FromArgb(110, Cores.Cinza), 1f);                     // contorno dos domingos
        using var centro = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        // título: mês/ano
        string titulo = mes.ToString("MMMM/yyyy", new CultureInfo("pt-BR"));
        g.DrawString(titulo, fTitulo, bTitulo, 0, 4);

        // botões de navegação (canto superior direito)
        DesenharBotao(g, BotaoAnterior, "<", sobreAnterior, fTitulo, bTitulo, centro);
        DesenharBotao(g, BotaoProximo, ">", sobreProximo, fTitulo, bTitulo, centro);

        // linha separadora abaixo do título
        using var linha = new Pen(Cores.Rosa, 1.5f);
        g.DrawLine(linha, 0, AlturaTopo - 4, Width, AlturaTopo - 4);

        // letras dos dias da semana
        string[] letras = { "D", "S", "T", "Q", "Q", "S", "S" };
        for (int c = 0; c < 7; c++)
        {
            var r = new RectangleF(c * LarguraCasa, AlturaTopo, LarguraCasa, AlturaSemana);
            g.DrawString(letras[c], fSemana, bSemana, r, centro);
        }

        // números: grade fixa de 42 casas (6 linhas x 7 colunas)
        DateTime inicio = InicioGrade();
        for (int i = 0; i < 42; i++)
        {
            DateTime data = inicio.AddDays(i);
            RectangleF r = RetanguloDoDia(i);

            bool doMes = data.Month == mes.Month;
            bool selecionado = dataSelecionada.HasValue && data.Date == dataSelecionada.Value.Date;

            if (selecionado)
            {
                g.FillEllipse(bSelecao, CirculoDaCasa(r));       // círculo rosa (antes do número)
            }
            else if (!EhSelecionavel(data))
            {
                RectangleF c = CirculoDaCasa(r);
                g.FillEllipse(bRasurado, c);                      // listras cinza
                g.DrawEllipse(pBloqueado, c);                     // contorno fino
            }

            g.DrawString(data.Day.ToString(), fDia,
                         selecionado ? bDiaSel : (doMes ? bDia : bApagado), r, centro);
        }
    }
}   