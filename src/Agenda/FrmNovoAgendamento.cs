using System.Drawing.Drawing2D;
using dashboardforms;

namespace dashboardforms.Agenda;

// =====================================================================
// MODELO: um agendamento salvo
// =====================================================================
class Agendamento
{
    public string Cliente { get; init; } = "";
    public List<string> Procedimentos { get; init; } = new();
    public DateTime Data { get; init; }
    public int InicioMin { get; init; }      // minutos desde 00:00 (08:00 = 480)
    public int FimMin { get; init; }
    public int DuracaoMin { get; init; }
    public string Observacoes { get; init; } = "";

    public string DescricaoProcedimentos => string.Join(", ", Procedimentos);
}

// Um horário de início possível e o seu término (já considerando o almoço).
// ToString é o que o ListBox mostra na tela.
readonly record struct Horario(int Inicio, int Fim)
{
    public override string ToString()
        => $"{RegrasAgenda.Formatar(Inicio)}  →  {RegrasAgenda.Formatar(Fim)}";
}

// =====================================================================
// REGRAS: durações, expediente e disponibilidade (sem nenhum desenho aqui)
// =====================================================================
static class RegrasAgenda
{
    // Tabela de durações: para somar combinações, basta consultar esta tabela
    public static readonly Dictionary<string, int> Duracoes = new()
    {
        ["Corte"] = 20,
        ["Mechas"] = 180,
        ["Selagem"] = 120,
        ["Coloração"] = 50,
        ["Hidratação"] = 40,
        ["Sobrancelha"] = 20,
    };

    // Expediente, sempre em minutos desde 00:00
    public const int Abertura = 8 * 60;       // 08:00
    public const int InicioAlmoco = 12 * 60;  // 12:00
    public const int FimAlmoco = 13 * 60;     // 13:00
    public const int Fechamento = 18 * 60;    // 18:00
    public const int Passo = 10;              // de quantos em quantos minutos começa um atendimento

    // Soma das durações dos procedimentos marcados
    public static int CalcularDuracao(IEnumerable<string> procedimentos)
        => procedimentos.Sum(p => Duracoes[p]);

    // 480 -> "08:00"
    public static string Formatar(int minutos) => $"{minutos / 60:00}:{minutos % 60:00}";

    // Dado o início e a duração, devolve a hora em que termina,
    // pulando a pausa do almoço. Devolve null se não for possível.
    public static int? CalcularFim(int inicio, int duracao)
    {
        if (duracao <= 0) return null;

        bool começaDeManha = inicio >= Abertura && inicio < InicioAlmoco;
        bool começaDeTarde = inicio >= FimAlmoco && inicio < Fechamento;
        if (!começaDeManha && !começaDeTarde) return null;   // antes de abrir, no almoço ou depois de fechar

        int restante = duracao;
        int atual = inicio;

        if (começaDeManha)
        {
            int cabeNaManha = InicioAlmoco - atual;
            if (restante <= cabeNaManha) return atual + restante;   // termina antes do almoço

            restante -= cabeNaManha;   // o que sobrou será feito à tarde
            atual = FimAlmoco;         // retoma às 13h
        }

        int fim = atual + restante;
        return fim <= Fechamento ? fim : null;   // não pode passar das 18h
    }

    // Todos os horários de início livres para a data e a duração informadas
    public static List<Horario> HorariosDisponiveis(DateTime data, int duracao,
                                                    IEnumerable<Agendamento> existentes)
    {
        var livres = new List<Horario>();
        if (data.Date < DateTime.Today || data.DayOfWeek == DayOfWeek.Sunday) return livres;

        var doDia = existentes.Where(a => a.Data.Date == data.Date).ToList();
        bool hoje = data.Date == DateTime.Today;
        int agora = DateTime.Now.Hour * 60 + DateTime.Now.Minute;

        for (int inicio = Abertura; inicio < Fechamento; inicio += Passo)
        {
            if (hoje && inicio <= agora) continue;          // hoje: não oferece horário que já passou

            int? fim = CalcularFim(inicio, duracao);
            if (fim is null) continue;                      // não cabe no expediente

            // dois intervalos se sobrepõem quando cada um começa antes de o outro terminar
            bool conflito = doDia.Any(a => inicio < a.FimMin && a.InicioMin < fim.Value);
            if (!conflito) livres.Add(new Horario(inicio, fim.Value));
        }
        return livres;
    }
}

// =====================================================================
// Efeito de passar o mouse nos MiniRotulos usados como botão
// =====================================================================
static class EfeitosAgenda
{
    public static void AplicarHover(MiniRotulo botao, Color corHover)
    {
        Color normal = botao.CorFundo;
        botao.Cursor = Cursors.Hand;
        botao.MouseEnter += (s, e) => { botao.CorFundo = corHover; botao.Invalidate(); };
        botao.MouseLeave += (s, e) => { botao.CorFundo = normal; botao.Invalidate(); };
    }
}

// =====================================================================
// Campo arredondado: desenha a moldura e hospeda um controle dentro
// =====================================================================
class CampoArredondado : Control
{
    public Color CorBorda { get; set; } = Cores.Primaria;
    public Color CorFundo { get; set; } = Color.White;
    public int Raio { get; set; } = 8;

    const int Recuo = 10;
    readonly Control interno;

    public CampoArredondado(Control interno)
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;

        this.interno = interno;
        Controls.Add(interno);
        Click += (s, e) => interno.Focus();   // clicar na moldura também foca o campo
    }

    // posiciona o controle interno sempre que o tamanho muda
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        if (interno is TextBox { Multiline: false })
            // TextBox de uma linha tem altura fixa pela fonte: centraliza na vertical
            interno.SetBounds(Recuo, (Height - interno.Height) / 2, Width - 2 * Recuo, interno.Height);
        else
            interno.SetBounds(Recuo, 8, Width - 2 * Recuo, Height - 16);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var caixa = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Util.Arredondar(caixa, Raio);
        using var fundo = new SolidBrush(CorFundo);
        using var caneta = new Pen(CorBorda, 1.5f);

        g.FillPath(fundo, path);
        g.DrawPath(caneta, path);
    }
}

// =====================================================================
// FORMULÁRIO "Novo agendamento"
// =====================================================================
class FrmNovoAgendamento : Form
{
    // ----- Constantes de layout (todas as medidas em um lugar só) -----
    const int Largura = 560;
    const int Margem = 20;
    const int LarguraConteudo = Largura - 2 * Margem;   // 520
    const int RaioForm = 18;
    const int AlturaRotulo = 20;
    const int AlturaCampo = 34;
    const int EspacoLinhas = 10;
    const int LarguraCalendario = 260;
    const int AlturaCalendario = 288;                   // mesma medida do card da FrmAgenda

    readonly IReadOnlyList<Agendamento> existentes;

    TextBox txtCliente = null!;
    CheckedListBox clbProcedimentos = null!;
    CheckBox chkTodos = null!;
    MiniCalendario calendario = null!;
    ListBox lstHorarios = null!;
    Label lblAviso = null!;
    TextBox txtDuracao = null!;
    TextBox txtObservacoes = null!;

    bool atualizandoLista;   // true enquanto "Selecionar todos" marca os itens por código

    // o FrmAgenda lê isto depois que o formulário fecha com OK
    public Agendamento? Resultado { get; private set; }

    public FrmNovoAgendamento(IReadOnlyList<Agendamento> existentes)
    {
        this.existentes = existentes;

        FormBorderStyle = FormBorderStyle.None;     // sem barra: o contorno é desenhado por nós
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = Color.White;
        DoubleBuffered = true;

        MontarCampos();
        AtualizarDuracaoEHorarios();                // estado inicial: mensagens de orientação
    }

    // ----- Construção visual, de cima para baixo -----
    void MontarCampos()
    {
        int y = 18;

        // 1) Título
        Controls.Add(new Label
        {
            Text = "Novo agendamento",
            Font = new Font(PaletaAgenda.FontePadrao, 18f, FontStyle.Bold),
            ForeColor = Cores.Primaria,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(Margem, y),
            Size = new Size(LarguraConteudo, 36)
        });
        y += 36 + 8;

        // 2) Cliente
        AdicionarRotulo("Cliente", Margem, y, LarguraConteudo);
        y += AlturaRotulo;
        txtCliente = CriarTextBox();
        Controls.Add(new CampoArredondado(txtCliente)
        {
            Location = new Point(Margem, y),
            Size = new Size(LarguraConteudo, AlturaCampo)
        });
        y += AlturaCampo + EspacoLinhas;

        // 3) Procedimentos (rótulo à esquerda, "Selecionar todos" à direita)
        AdicionarRotulo("Procedimento a ser realizado", Margem, y, 300);

        chkTodos = new CheckBox
        {
            Text = "Selecionar todos",
            Font = new Font(PaletaAgenda.FontePadrao, 9f),
            ForeColor = Cores.Texto,
            BackColor = Color.Transparent,
            AutoSize = false,
            Cursor = Cursors.Hand,
            Location = new Point(Margem + LarguraConteudo - 150, y),
            Size = new Size(150, AlturaRotulo)
        };
        chkTodos.Click += (s, e) =>
        {
            atualizandoLista = true;   // impede o ItemCheck de recalcular 6 vezes
            for (int i = 0; i < clbProcedimentos.Items.Count; i++)
                clbProcedimentos.SetItemChecked(i, chkTodos.Checked);
            atualizandoLista = false;
            AtualizarDuracaoEHorarios();   // recalcula uma vez só
        };
        Controls.Add(chkTodos);
        y += AlturaRotulo;

        clbProcedimentos = new CheckedListBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Cores.Texto,
            Font = new Font(PaletaAgenda.FontePadrao, 10f),
            CheckOnClick = true,          // um clique já marca/desmarca
            MultiColumn = true,           // 2 colunas: ocupa menos altura
            ColumnWidth = 240,
            IntegralHeight = false
        };
        foreach (string nome in RegrasAgenda.Duracoes.Keys) clbProcedimentos.Items.Add(nome);

        // ItemCheck dispara ANTES de o item mudar; adiamos o cálculo para depois com BeginInvoke
        clbProcedimentos.ItemCheck += (s, e) =>
        {
            if (atualizandoLista) return;
            BeginInvoke(new Action(() =>
            {
                SincronizarSelecionarTodos();
                AtualizarDuracaoEHorarios();
            }));
        };

        Controls.Add(new CampoArredondado(clbProcedimentos)
        {
            Location = new Point(Margem, y),
            Size = new Size(LarguraConteudo, 84)
        });
        y += 84 + EspacoLinhas;

        // 4) Data (esquerda) e Horário (direita)
        int xDireita = Margem + LarguraCalendario + 20;
        int larguraDireita = Largura - Margem - xDireita;   // 240

        AdicionarRotulo("Data", Margem, y, LarguraCalendario);
        AdicionarRotulo("Horário", xDireita, y, larguraDireita);
        y += AlturaRotulo;

        var cardCalendario = new CardBorda
        {
            Location = new Point(Margem, y),
            Size = new Size(LarguraCalendario, AlturaCalendario),
            CorBorda = Cores.Primaria,
            EspessuraBorda = 2
        };
        calendario = new MiniCalendario { Dock = DockStyle.Fill };
        calendario.DataSelecionadaAlterada += (s, e) => AtualizarHorarios();   // trocou a data: recalcula
        cardCalendario.Controls.Add(calendario);
        Controls.Add(cardCalendario);

        lstHorarios = new ListBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Cores.Texto,
            Font = new Font(PaletaAgenda.FontePadrao, 10f),
            IntegralHeight = false
        };
        Controls.Add(new CampoArredondado(lstHorarios)
        {
            Location = new Point(xDireita, y),
            Size = new Size(larguraDireita, 150)
        });

        lblAviso = new Label
        {
            Font = new Font(PaletaAgenda.FontePadrao, 8.5f),
            ForeColor = Cores.Cinza,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.TopLeft,
            Location = new Point(xDireita, y + 154),
            Size = new Size(larguraDireita, 38)
        };
        Controls.Add(lblAviso);

        // 5) Duração, na coluna da direita, abaixo do aviso
        AdicionarRotulo("Duração (minutos)", xDireita, y + 198, larguraDireita);
        txtDuracao = CriarTextBox();
        txtDuracao.ReadOnly = true;            // calculada, não digitada
        txtDuracao.BackColor = Color.White;    // ReadOnly deixaria cinza
        txtDuracao.TabStop = false;
        Controls.Add(new CampoArredondado(txtDuracao)
        {
            Location = new Point(xDireita, y + 198 + AlturaRotulo),
            Size = new Size(larguraDireita, AlturaCampo)
        });

        y += AlturaCalendario + EspacoLinhas;

        // 6) Observações (multilinha, bem maior que os outros campos)
        AdicionarRotulo("Observações", Margem, y, LarguraConteudo);
        y += AlturaRotulo;
        txtObservacoes = CriarTextBox();
        txtObservacoes.Multiline = true;
        txtObservacoes.AcceptsReturn = true;
        txtObservacoes.ScrollBars = ScrollBars.Vertical;
        Controls.Add(new CampoArredondado(txtObservacoes)
        {
            Location = new Point(Margem, y),
            Size = new Size(LarguraConteudo, 78)
        });
        y += 78 + 14;

        // 7) Botões: lado a lado, centralizados
        const int larguraCancelar = 140, larguraSalvar = 190, espacoBotoes = 14, alturaBotao = 36;
        int xBotoes = (Largura - (larguraCancelar + espacoBotoes + larguraSalvar)) / 2;

        var btnCancelar = new MiniRotulo
        {
            Text = "Cancelar",
            CorFundo = PaletaAgenda.VermelhoFundo,    // estética do "Recusar"
            CorBorda = PaletaAgenda.VermelhoBorda,
            CorTexto = Cores.Escuro,
            Raio = 10,
            TamanhoFonte = 10f,
            Location = new Point(xBotoes, y),
            Size = new Size(larguraCancelar, alturaBotao)
        };
        EfeitosAgenda.AplicarHover(btnCancelar,
            PaletaAgenda.Misturar(PaletaAgenda.VermelhoFundo, Color.Black, 0.06f));
        btnCancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;   // fecha sem salvar

        var btnSalvar = new MiniRotulo
        {
            Text = "Salvar agendamento",
            CorFundo = PaletaAgenda.VerdeFundo,       // estética do "Confirmar"
            CorBorda = PaletaAgenda.VerdeBorda,
            CorTexto = Cores.Escuro,
            Raio = 10,
            TamanhoFonte = 10f,
            Location = new Point(xBotoes + larguraCancelar + espacoBotoes, y),
            Size = new Size(larguraSalvar, alturaBotao)
        };
        EfeitosAgenda.AplicarHover(btnSalvar,
            PaletaAgenda.Misturar(PaletaAgenda.VerdeFundo, Color.Black, 0.06f));
        btnSalvar.Click += (s, e) => Salvar();

        Controls.Add(btnCancelar);
        Controls.Add(btnSalvar);
        y += alturaBotao + 18;

        // altura final = tudo que foi empilhado; depois recorta os cantos da janela
        ClientSize = new Size(Largura, y);
        using var moldura = Util.Arredondar(new Rectangle(0, 0, Width, Height), RaioForm);
        Region = new Region(moldura);
    }

    // ----- Fábricas pequenas, para não repetir código -----
    void AdicionarRotulo(string texto, int x, int y, int largura)
    {
        Controls.Add(new Label
        {
            Text = texto,
            Font = new Font(PaletaAgenda.FontePadrao, 9.5f, FontStyle.Bold),
            ForeColor = Cores.Escuro,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.BottomLeft,
            Location = new Point(x, y),
            Size = new Size(largura, AlturaRotulo)
        });
    }

    static TextBox CriarTextBox() => new()
    {
        BorderStyle = BorderStyle.None,    // a moldura é do CampoArredondado
        BackColor = Color.White,
        ForeColor = Cores.Texto,
        Font = new Font(PaletaAgenda.FontePadrao, 10f)
    };

    // ----- Lógica -----
    List<string> ProcedimentosMarcados()
        => clbProcedimentos.CheckedItems.Cast<string>().ToList();

    // o "Selecionar todos" fica marcado só quando os 6 itens estão marcados
    void SincronizarSelecionarTodos()
        => chkTodos.Checked = clbProcedimentos.CheckedItems.Count == clbProcedimentos.Items.Count;

    void AtualizarDuracaoEHorarios()
    {
        int duracao = RegrasAgenda.CalcularDuracao(ProcedimentosMarcados());
        txtDuracao.Text = $"{duracao} min";
        AtualizarHorarios();
    }

    void AtualizarHorarios()
    {
        lstHorarios.Items.Clear();

        var procedimentos = ProcedimentosMarcados();
        if (procedimentos.Count == 0)
        {
            lblAviso.Text = "Escolha ao menos um procedimento para ver os horários.";
            return;
        }
        if (calendario.DataSelecionada is not DateTime data)
        {
            lblAviso.Text = "Escolha uma data no calendário para ver os horários.";
            return;
        }
        if (data.Date < DateTime.Today)
        {
            lblAviso.Text = "Essa data já passou. Escolha uma data de hoje em diante.";
            return;
        }

        int duracao = RegrasAgenda.CalcularDuracao(procedimentos);
        var livres = RegrasAgenda.HorariosDisponiveis(data, duracao, existentes);

        if (livres.Count == 0)
        {
            lblAviso.Text = "Não há horários livres para esses procedimentos neste dia. " +
                            "Tente outra data ou menos procedimentos.";
            return;
        }

        foreach (var h in livres) lstHorarios.Items.Add(h);
        lblAviso.Text = $"{livres.Count} horário(s) disponível(is). Mostrando início → término.";
    }

    void Salvar()
    {
        string cliente = txtCliente.Text.Trim();
        var procedimentos = ProcedimentosMarcados();

        if (cliente.Length == 0) { Avisar("Informe o nome da cliente.", txtCliente); return; }
        if (procedimentos.Count == 0) { Avisar("Selecione ao menos um procedimento.", clbProcedimentos); return; }
        if (calendario.DataSelecionada is not DateTime data) { Avisar("Escolha a data do atendimento.", null); return; }
        if (lstHorarios.SelectedItem is not Horario horario) { Avisar("Escolha um horário disponível.", lstHorarios); return; }

        // segunda checagem: garante que o horário continua livre
        int duracao = RegrasAgenda.CalcularDuracao(procedimentos);
        if (!RegrasAgenda.HorariosDisponiveis(data, duracao, existentes).Contains(horario))
        {
            Avisar("Esse horário não está mais disponível. Escolha outro.", null);
            AtualizarHorarios();
            return;
        }

        Resultado = new Agendamento
        {
            Cliente = cliente,
            Procedimentos = procedimentos,
            Data = data.Date,
            InicioMin = horario.Inicio,
            FimMin = horario.Fim,
            DuracaoMin = duracao,
            Observacoes = txtObservacoes.Text.Trim()
        };
        DialogResult = DialogResult.OK;   // fecha o formulário devolvendo OK
    }

    void Avisar(string mensagem, Control? foco)
    {
        MessageBox.Show(this, mensagem, "Novo agendamento", MessageBoxButtons.OK, MessageBoxIcon.Information);
        foco?.Focus();
    }

    // ----- Contorno do formulário + tecla Esc -----
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var area = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = Util.Arredondar(area, RaioForm);
        using var caneta = new Pen(Cores.Primaria, 2.5f);
        g.DrawPath(caneta, path);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { DialogResult = DialogResult.Cancel; return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}