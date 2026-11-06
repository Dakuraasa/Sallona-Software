using System.Drawing.Drawing2D;
using System.Drawing.Text;
using dashboardforms;

namespace dashboardforms.Agenda;

// ===== Nomes dos status de um agendamento =====
// Os valores são EXATAMENTE os textos do seletor de status da FaixaFiltros,
// assim o filtro (FiltrosAgenda.Aceita) compara strings iguais sem nenhuma conversão.
static class StatusAgenda
{
    public const string Pendentes = "Pendentes";
    public const string Confirmados = "Confirmados";
    public const string Concluidos = "Concluídos";
    public const string Cancelados = "Cancelados";
    public const string Alteracoes = "Alterações";

    // texto mostrado no card (o filtro fala no plural; o card, de um agendamento só, no singular)
    public static string Rotulo(string status) => status switch
    {
        Pendentes => "Pendente",
        Confirmados => "Confirmado",
        Concluidos => "Concluído",
        Cancelados => "Cancelado",
        Alteracoes => "Alteração",
        _ => status
    };
}

// ===== Item 8: card de UM agendamento (cor do procedimento, textos e status) =====
// A posição e a altura NÃO são decididas aqui: quem calcula é a AreaAgendaDia,
// a partir de InicioMin e FimMin. O card só desenha o que cabe na altura que recebeu.
class CardAgendamento : Control
{
    // ----- Constantes de layout (todas as medidas em um lugar só) -----
    const int MargemX = 12;            // margem lateral interna
    const int MargemY = 8;             // margem superior interna
    const int LimiteInferior = 6;      // folga mínima antes da borda de baixo
    const int AlturaHorario = 15;
    const int AlturaNome = 19;
    const int AlturaInfo = 16;         // procedimento, duração e observações
    const int AlturaChip = 16;         // retângulo branco do status
    const int TopoChip = 6;
    const int PaddingChip = 14;        // respiro lateral dentro do chip (soma dos dois lados)
    const int EspacoChip = 8;          // entre o texto e o chip de status
    const int RaioCard = 12;           // mesmo raio do CardSolicitacao
    const int RaioChip = 6;
    const int LarguraFaixa = 6;        // faixa lateral (só quando há mais de um procedimento); 0 desliga

    // abaixo desta altura não cabem 2 linhas legíveis: o card usa o modo compacto (1 linha)
    const int AlturaDetalhada = MargemY + AlturaHorario + AlturaNome + LimiteInferior;

    public Agendamento Dados { get; }

    public CardAgendamento(Agendamento dados)
    {
        Dados = dados;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    // altura que o card precisa para mostrar TUDO (a área usa para decidir se oferece a dica com o texto completo)
    public static int AlturaNecessaria(Agendamento a)
        => MargemY + AlturaHorario + AlturaNome + AlturaInfo + AlturaInfo
         + (a.Observacoes.Length > 0 ? AlturaInfo : 0) + LimiteInferior;

    string Horario
        => $"{RegrasAgenda.Formatar(Dados.InicioMin)} – {RegrasAgenda.Formatar(Dados.FimMin)}";

    bool VariosProcedimentos => Dados.Procedimentos.Count > 1;

    int RecuoEsquerdo => MargemX + (VariosProcedimentos ? LarguraFaixa : 0);

    // texto completo, usado como dica quando o card é pequeno demais para mostrar tudo
    public string DescricaoCompleta
    {
        get
        {
            string texto = $"{Dados.Cliente}\n{Dados.DescricaoProcedimentos}\n" +
                           $"{Horario} ({Dados.DuracaoMin} min)\n{StatusAgenda.Rotulo(Dados.Status)}";
            return Dados.Observacoes.Length > 0 ? texto + "\n" + Dados.Observacoes : texto;
        }
    }

    // cor do procedimento = a MESMA da paleta dos filtros do item 6 (nada de cor nova)
    static (Color Fundo, Color Borda) ObterCores(string procedimento)
    {
        foreach (var (nome, fundo, borda) in PaletaProcedimentos.Itens)
            if (nome == procedimento) return (fundo, borda);

        var neutro = PaletaProcedimentos.Itens[0];   // "Todos": só se o nome não existir na paleta
        return (neutro.Fundo, neutro.Borda);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;   // suaviza as letras, sem encaixar na grade de pixels

        // o fundo segue o primeiro procedimento; com vários, a faixa lateral mostra todos
        var (fundo, borda) = ObterCores(Dados.Procedimentos.FirstOrDefault() ?? "");
        bool compacto = Height < AlturaDetalhada;

        using var fNome = new Font(PaletaAgenda.FontePadrao, 10f, FontStyle.Bold);
        using var fInfo = new Font(PaletaAgenda.FontePadrao, 8.5f);
        using var fChip = new Font(PaletaAgenda.FontePadrao, 8f);

        DesenharFundo(g, fundo, borda);
        int xChip = DesenharStatus(g, fChip, borda, compacto);

        if (compacto) DesenharCompacto(g, fNome, fInfo, xChip - EspacoChip);
        else DesenharDetalhado(g, fNome, fInfo, xChip - EspacoChip);
    }

    // ----- Fundo na cor do procedimento + borda mais intensa, cantos arredondados -----
    void DesenharFundo(Graphics g, Color fundo, Color borda)
    {
        var area = new Rectangle(0, 0, Width - 1, Height - 1);
        area.Inflate(-1, -1);                       // espaço para a espessura da borda

        using var path = Util.Arredondar(area, RaioCard);
        using var pincel = new SolidBrush(fundo);
        using var caneta = new Pen(borda, 1.5f);

        g.FillPath(pincel, path);
        if (VariosProcedimentos && LarguraFaixa > 0) DesenharFaixaProcedimentos(g, path, area);
        g.DrawPath(caneta, path);
    }

    // Vários procedimentos no mesmo agendamento: a faixa da esquerda é dividida em partes iguais,
    // cada parte na cor (borda) de um procedimento, na ordem em que foram registrados.
    void DesenharFaixaProcedimentos(Graphics g, GraphicsPath path, Rectangle area)
    {
        var estado = g.Save();
        g.SetClip(path, CombineMode.Intersect);     // a faixa respeita os cantos arredondados

        int n = Dados.Procedimentos.Count;
        for (int i = 0; i < n; i++)
        {
            int y0 = area.Top + area.Height * i / n;
            int y1 = area.Top + area.Height * (i + 1) / n;
            using var pincel = new SolidBrush(ObterCores(Dados.Procedimentos[i]).Borda);
            g.FillRectangle(pincel, area.Left, y0, LarguraFaixa, y1 - y0);
        }

        g.Restore(estado);
    }

    // ----- Status: retângulo branco arredondado no canto superior direito -----
    // Devolve o X onde o chip começa, para os textos não passarem por baixo dele.
    int DesenharStatus(Graphics g, Font fChip, Color borda, bool compacto)
    {
        string rotulo = StatusAgenda.Rotulo(Dados.Status);
        int largura = (int)Math.Ceiling(g.MeasureString(rotulo, fChip).Width) + PaddingChip;
        int x = Width - MargemX - largura;
        int y = compacto ? (Height - AlturaChip) / 2 : TopoChip;   // card baixo: centraliza; senão, canto superior

        MiniRotulo.Desenhar(g, new Rectangle(x, y, largura, AlturaChip), rotulo,
            Color.White, borda, Cores.Escuro, fChip, RaioChip);
        return x;
    }

    // ----- Modo detalhado: horário, nome, procedimento, duração e observações, de cima para baixo -----
    // Cada linha só é desenhada se couber inteira (nada é cortado pela metade).
    void DesenharDetalhado(Graphics g, Font fNome, Font fInfo, int limiteDireito)
    {
        using var bNome = new SolidBrush(Cores.Escuro);
        using var bTexto = new SolidBrush(Cores.Texto);
        using var linha = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter   // texto longo termina em "..."
        };

        float x = RecuoEsquerdo;
        float largura = Width - MargemX - x;
        float limite = Height - LimiteInferior;
        float y = MargemY;

        // horário de início e término: na região superior, dividindo a linha com o status
        g.DrawString(Horario, fInfo, bTexto, new RectangleF(x, y, limiteDireito - x, AlturaHorario), linha);
        y += AlturaHorario;

        g.DrawString(Dados.Cliente, fNome, bNome, new RectangleF(x, y, largura, AlturaNome), linha);
        y += AlturaNome;

        if (y + AlturaInfo > limite) return;
        g.DrawString(Dados.DescricaoProcedimentos, fInfo, bTexto, new RectangleF(x, y, largura, AlturaInfo), linha);
        y += AlturaInfo;

        if (y + AlturaInfo > limite) return;
        g.DrawString($"Duração: {Dados.DuracaoMin} min", fInfo, bTexto, new RectangleF(x, y, largura, AlturaInfo), linha);
        y += AlturaInfo;

        // observações: ocupam o que sobrar do card, podendo quebrar em mais de uma linha
        if (Dados.Observacoes.Length == 0 || limite - y < AlturaInfo) return;
        using var quebra = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisWord
        };
        g.DrawString(Dados.Observacoes, fInfo, bTexto, new RectangleF(x, y, largura, limite - y), quebra);
    }

    // ----- Modo compacto (atendimentos curtos): tudo em UMA linha centralizada -----
    // "08:00 – 08:20   Cliente · Procedimento"; o texto completo fica na dica do mouse.
    void DesenharCompacto(Graphics g, Font fNome, Font fInfo, int limiteDireito)
    {
        using var bNome = new SolidBrush(Cores.Escuro);
        using var bTexto = new SolidBrush(Cores.Texto);
        using var formato = new StringFormat(StringFormat.GenericTypographic)   // medida sem folga extra
        {
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter
        };

        float x = RecuoEsquerdo;

        // desenha um pedaço de texto e avança o X pela largura dele
        void Segmento(string texto, Font fonte, Brush pincel, float espacoDepois)
        {
            if (x >= limiteDireito) return;
            g.DrawString(texto, fonte, pincel, new RectangleF(x, 0, limiteDireito - x, Height), formato);
            x += g.MeasureString(texto, fonte, PointF.Empty, formato).Width + espacoDepois;
        }

        Segmento(Horario, fInfo, bTexto, 10);
        Segmento(Dados.Cliente, fNome, bNome, 6);
        Segmento("· " + Dados.DescricaoProcedimentos, fInfo, bTexto, 0);
    }
}

// ===== Item 7: área do dia = divisória vertical + escala temporal proporcional + cards =====
// Não existe grade de horários: cada card é posicionado por InicioMin e dimensionado por
// (FimMin - InicioMin), sempre na MESMA escala (pixels por minuto). Os horários do eixo
// esquerdo são apenas os de início e término dos agendamentos mostrados.
class AreaAgendaDia : Control
{
    // altura mínima que o formulário reserva para esta área
    public const int AlturaMinima = 200;

    // ----- Constantes de layout (todas as medidas em um lugar só) -----
    const int LarguraHorarios = 54;      // coluna dos horários, à esquerda da linha vertical
    const int LarguraLinha = 2;          // mesma espessura da LinhaVertical já existente
    const int EspacoAteCards = 12;       // entre a linha vertical e os cards
    const int MargemTopo = 10;           // respiro acima de 08:00 (e abaixo de 18:00)
    const int MargemDireita = 14;        // respiro à direita; também é onde fica a barra de rolagem
    const int AlturaRotulo = 14;         // altura de cada horário escrito no eixo
    const int TamTick = 4;               // tracinho entre o horário e a linha
    const int RaioAlmoco = 8;
    const int AlturaPilulaAlmoco = 22;
    const int LarguraBarra = 6;
    const int AlturaMinimaBarra = 30;
    const int PassoRolagem = 48;

    // Escala: o mínimo garante cards legíveis (20 min = 40 px). Se a janela for alta o bastante,
    // a escala cresce para o expediente inteiro caber sem rolagem; senão, aparece a rolagem.
    const float MinPxPorMinuto = 2f;

    const string TextoAlmoco = "Intervalo de almoço";

    readonly List<Agendamento> itens = new();
    readonly List<CardAgendamento> cards = new();
    readonly LinhaVertical linha = new();      // reaproveita o controle da divisória vertical
    readonly ToolTip dica = new();

    int deslocamento;                           // quanto a área está rolada (pixels)
    bool arrastandoBarra;
    int ancoraMouse, ancoraDeslocamento;

    public AreaAgendaDia()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);   // Selectable: para a roda do mouse chegar aqui
        BackColor = Color.Transparent;
        TabStop = false;

        Controls.Add(linha);
    }

    // ----- Recebe os agendamentos JÁ filtrados (data, procedimento e status) e refaz os cards -----
    // Os dados originais não são alterados: cada card só aponta para o seu agendamento.
    public void Definir(IEnumerable<Agendamento> agendamentos)
    {
        foreach (var card in cards)
        {
            dica.SetToolTip(card, null);
            Controls.Remove(card);
            card.Dispose();
        }
        cards.Clear();
        itens.Clear();
        itens.AddRange(agendamentos.OrderBy(a => a.InicioMin));

        foreach (var agendamento in itens)
        {
            var card = new CardAgendamento(agendamento);
            card.MouseEnter += (s, e) => Focus();     // para a roda do mouse rolar também sobre os cards
            cards.Add(card);
            Controls.Add(card);
        }

        Reposicionar();
        Invalidate();
    }

    // ----- Geometria da escala (usada no desenho E no posicionamento dos cards) -----
    int MinutosDoExpediente => RegrasAgenda.Fechamento - RegrasAgenda.Abertura;

    float PxPorMinuto => Math.Max(MinPxPorMinuto, (Height - 2 * MargemTopo) / (float)MinutosDoExpediente);

    // Round (e não Ceiling): quando a escala é calculada para caber na janela, a sobra de
    // casas decimais não pode criar 1 pixel de rolagem à toa
    int AlturaConteudo => (int)Math.Round(2 * MargemTopo + MinutosDoExpediente * PxPorMinuto);

    int MaxDeslocamento => Math.Max(0, AlturaConteudo - Height);

    // minuto do dia (480 = 08:00) -> coordenada Y; sem arredondar para nenhum múltiplo de minutos
    int Y(int minutos)
        => (int)Math.Round(MargemTopo + (minutos - RegrasAgenda.Abertura) * PxPorMinuto) - deslocamento;

    int XCards => LarguraHorarios + LarguraLinha + EspacoAteCards;

    int LarguraCards => Math.Max(Width - MargemDireita - XCards, 40);

    // Cada card vai do Y de InicioMin ao Y de FimMin. Como o fim de um e o início do seguinte
    // usam a mesma conta, atendimentos consecutivos se encostam e os vazios ficam do tamanho real.
    void Reposicionar()
    {
        deslocamento = Math.Clamp(deslocamento, 0, MaxDeslocamento);
        linha.SetBounds(LarguraHorarios, 0, LarguraLinha, Height);

        foreach (var card in cards)
        {
            int topo = Y(card.Dados.InicioMin);
            int baixo = Y(card.Dados.FimMin);
            card.SetBounds(XCards, topo, LarguraCards, Math.Max(baixo - topo, 1));

            // card pequeno demais para mostrar tudo: o texto completo aparece ao passar o mouse
            dica.SetToolTip(card,
                card.Height < CardAgendamento.AlturaNecessaria(card.Dados) ? card.DescricaoCompleta : null);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Reposicionar();
        Invalidate();
    }

    // ----- Rolagem (roda do mouse e arrasto da barra) -----
    void AplicarDeslocamento(int novo)
    {
        novo = Math.Clamp(novo, 0, MaxDeslocamento);
        if (novo == deslocamento) return;
        deslocamento = novo;
        Reposicionar();
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Focus(); }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        AplicarDeslocamento(deslocamento + (e.Delta > 0 ? -PassoRolagem : PassoRolagem));
    }

    bool TemBarra => MaxDeslocamento > 0;

    Rectangle Barra()
    {
        int altura = Math.Max(AlturaMinimaBarra, (int)((long)Height * Height / AlturaConteudo));
        int livre = Height - altura;
        int y = MaxDeslocamento == 0 ? 0 : (int)((long)deslocamento * livre / MaxDeslocamento);
        return new Rectangle(Width - MargemDireita + (MargemDireita - LarguraBarra) / 2, y, LarguraBarra, altura);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !TemBarra || !Barra().Contains(e.Location)) return;

        arrastandoBarra = true;
        ancoraMouse = e.Y;
        ancoraDeslocamento = deslocamento;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!arrastandoBarra) return;

        int livre = Height - Barra().Height;
        if (livre > 0)
            AplicarDeslocamento(ancoraDeslocamento + (int)((long)(e.Y - ancoraMouse) * MaxDeslocamento / livre));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        arrastandoBarra = false;
    }

    // ----- Desenho -----
    // Ordem: almoço (fundo) -> horários -> barra. Os cards e a linha vertical são controles filhos,
    // desenhados por cima do almoço (um atendimento que atravessa o almoço cobre a faixa).
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;   // suaviza as letras, sem encaixar na grade de pixels

        DesenharAlmoco(g);
        DesenharHorarios(g);
        if (TemBarra) DesenharBarra(g);
    }

    // Pausa na disponibilidade (não é um agendamento): fundo listrado + "Intervalo de almoço".
    // Só ocupa o espaço que o relógio manda (12:00 a 13:00); não interfere na posição dos cards.
    void DesenharAlmoco(Graphics g)
    {
        int topo = Y(RegrasAgenda.InicioAlmoco);
        int baixo = Y(RegrasAgenda.FimAlmoco);

        // -1: o contorno da caneta é centrado na linha, então sobra 1 px para não cortar na borda
        var caixa = new Rectangle(XCards, topo, LarguraCards - 1, baixo - topo - 1);

        using var path = Util.Arredondar(caixa, RaioAlmoco);
        using var listras = new HatchBrush(HatchStyle.WideUpwardDiagonal,
                                           Color.FromArgb(90, Cores.Cinza), Color.Transparent);   // mesmas listras dos domingos
        using var caneta = new Pen(Color.FromArgb(110, Cores.Cinza), 1f);
        g.FillPath(listras, path);
        g.DrawPath(caneta, path);

        // indicação centralizada, sobre uma pastilha para o texto não se perder nas listras
        using var fonte = new Font(PaletaAgenda.FontePadrao, 9.5f, FontStyle.Bold);
        int larguraTexto = (int)Math.Ceiling(g.MeasureString(TextoAlmoco, fonte).Width) + 20;
        var pastilha = new Rectangle(
            caixa.X + (caixa.Width - larguraTexto) / 2,
            caixa.Y + (caixa.Height - AlturaPilulaAlmoco) / 2,
            larguraTexto, AlturaPilulaAlmoco);

        MiniRotulo.Desenhar(g, pastilha, TextoAlmoco, Cores.Fundo,
            Color.FromArgb(110, Cores.Cinza), Cores.Cinza, fonte, 10);
    }

    // Eixo dos horários: o início e o fim do almoço (regra do expediente, sempre visíveis)
    // + os inícios e términos dos agendamentos mostrados. Nenhuma outra hora é escrita.
    // Se dois horários ficarem colados demais, o de menor prioridade não é escrito
    // (o do almoço tem prioridade; o dos agendamentos continua dentro do próprio card).
    void DesenharHorarios(Graphics g)
    {
        using var fonte = new Font(PaletaAgenda.FontePadrao, 8.5f);
        using var pincel = new SolidBrush(Cores.Cinza);
        using var tick = new Pen(Cores.Primaria, 1f);
        using var direita = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };

        // ordem de prioridade: almoço primeiro, depois os agendamentos em ordem cronológica
        var minutos = new[] { RegrasAgenda.InicioAlmoco, RegrasAgenda.FimAlmoco }
            .Concat(itens.SelectMany(a => new[] { a.InicioMin, a.FimMin }).OrderBy(m => m))
            .Distinct();

        var desenhados = new List<int>();   // Y de cada horário já escrito

        foreach (int m in minutos)
        {
            int y = Y(m);
            if (desenhados.Any(outro => Math.Abs(y - outro) < AlturaRotulo)) continue;
            desenhados.Add(y);

            g.DrawLine(tick, LarguraHorarios - TamTick, y, LarguraHorarios, y);
            g.DrawString(RegrasAgenda.Formatar(m), fonte, pincel,
                new RectangleF(0, y - AlturaRotulo / 2f, LarguraHorarios - TamTick - 4, AlturaRotulo), direita);
        }
    }

    // Barra de rolagem discreta (só aparece quando o expediente não cabe na altura da janela)
    void DesenharBarra(Graphics g)
    {
        using var path = Util.Arredondar(Barra(), LarguraBarra / 2);
        using var pincel = new SolidBrush(Color.FromArgb(140, Cores.Primaria));
        g.FillPath(pincel, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) dica.Dispose();
        base.Dispose(disposing);
    }
}