using System.Globalization;
using dashboardforms;

namespace dashboardforms.Agenda;

public partial class FrmAgenda : Form
{
    // ----- Constantes de espaçamento da coluna esquerda -----
    const int EspacoAcimaDivisor = 12;   // distância entre o card do calendário e a linha horizontal
    const int EspacoDepoisDivisor = 3;   // distância entre a linha horizontal e o título "Solicitações"
    const int AlturaDivisor = 2;
    const int AlturaTitulo = 24;
    const int EspacoFiltros = 4;       // entre os 3 minicards de categoria
    const int AlturaFiltro = 26;
    const int EspacoCards = 8;         // entre os 4 cards de solicitação

    // ----- Constantes do divisor vertical -----
    const int EspacoAteDivisor = 20;   // distância entre a coluna esquerda e a linha vertical
    const int LarguraDivisorVertical = 2;
    const int ExtensaoDivisor = 120;// pixels a mais abaixo do último card (0 = termina na base dele)

    // ----- Constantes do título principal da agenda -----
    const int AlturaTituloAgenda = 48; // altura do label do título grande
    const int MargemDireita = 24;      // respiro entre o título e a borda direita da janela

    // =====================================================================
    // >>> NOVO 1/3: AJUSTE DA DIVISÓRIA ABAIXO DO TÍTULO (edite só aqui) <<<
    // =====================================================================
    const int EspacoTituloDivisor = 8;   // distância entre o título e a linha (maior = linha mais baixa)
    // =====================================================================

    // =====================================================================
    // >>> NOVO (item 5): constantes do botão "+ Novo Agendamento" e a lista de agendamentos <<<
    // =====================================================================
    const int LarguraBotaoNovo = 190;
    const int AlturaBotaoNovo = 38;
    const int EspacoBotaoTitulo = 16;    // respiro entre o fim do título e o botão

    // agendamentos salvos (por enquanto só em memória; depois virá do banco)
    readonly List<Agendamento> agendamentos = new();
    // =====================================================================

    // =====================================================================
    // >>> NOVO (item 6): constantes e campo da faixa de filtros <<<
    // =====================================================================
    const int EspacoAcimaFaixa = 10;    // entre a divisória do título e a faixa de filtros
    const int EspacoAbaixoFaixa = 10;   // entre a faixa de filtros e a divisória de baixo

    // a faixa fica em um campo para o item 7 conseguir consultar os filtros escolhidos
    FaixaFiltros faixaFiltros = null!;

    // o item 7 lê os filtros por aqui (null em um campo = "Todos")
    internal FiltrosAgenda FiltrosAtuais => faixaFiltros.Filtros;
    // =====================================================================

    // =====================================================================
    // >>> NOVO (itens 7 e 8): constantes e campos da área dos agendamentos do dia <<<
    // =====================================================================
    const int EspacoAcimaAgenda = 10;   // entre a divisória de baixo dos filtros e a área do dia
    const int MargemInferior = 24;      // respiro entre a área do dia e a borda de baixo da janela

    // o calendário vira campo para o formulário saber a data escolhida a qualquer momento
    MiniCalendario calendario = null!;

    // a área fica em um campo para ser atualizada quando data, filtros ou agendamentos mudarem
    AreaAgendaDia areaAgenda = null!;

    // data mostrada na agenda: a escolhida no calendário (ou hoje, se não houver seleção)
    // — a mesma regra que o título já usava
    DateTime DataAtual => calendario.DataSelecionada ?? DateTime.Today;
    // =====================================================================

    // guardamos o label em um campo para o método AtualizarTitulo conseguir alterá-lo
    Label lblTituloAgenda = null!;

    public FrmAgenda()
    {
        InitializeComponent();
        Text = "Agenda";
        BackColor = Cores.Fundo;

        var cardCalendario = new CardBorda
        {
            Location = new Point(24, 24),
            Size = new Size(260, 288),
            CorBorda = Cores.Primaria,
            EspessuraBorda = 2
        };

        // >>> ALTERADO (itens 7 e 8): deixou de ser variável local para virar o campo "calendario" <<<
        calendario = new MiniCalendario { Dock = DockStyle.Fill };

        // quando o usuário escolhe uma data, o título acompanha
        // (se a seleção for limpa, volta para a data de hoje)
        // >>> ALTERADO (itens 7 e 8): a área dos agendamentos também é atualizada <<<
        calendario.DataSelecionadaAlterada += (s, e) =>
        {
            AtualizarTitulo(DataAtual);
            AtualizarAgendaDia();
        };

        cardCalendario.Controls.Add(calendario);
        Controls.Add(cardCalendario);

        // primeiro monta a coluna e descobre onde ela termina...
        int baseDaColuna = MontarSolicitacoes(cardCalendario);

        // ...depois cria a linha ao lado, com a altura da coluna inteira
        MontarDivisorVertical(cardCalendario, baseDaColuna);

        // por fim, o título grande à direita da linha (já abre mostrando a data de hoje)
        MontarTituloAgenda(cardCalendario);

        // >>> NOVO 2/3: linha horizontal abaixo do título <<<
        MontarDivisorTitulo(cardCalendario);

        // >>> NOVO (item 5): botão "+ Novo Agendamento" <<<
        MontarBotaoNovoAgendamento(cardCalendario);

        // >>> NOVO (item 6): faixa de filtros + divisória inferior <<<
        MontarFaixaFiltros(cardCalendario);

        // >>> NOVO (itens 7 e 8): área dos agendamentos do dia, abaixo da divisória dos filtros <<<
        MontarAreaAgenda(cardCalendario);
    }

    private void FrmAgenda_Load(object sender, EventArgs e)
    {
    }

    // Título principal: fica à direita da linha vertical, alinhado ao topo do card do calendário.
    // A posição vem das mesmas constantes do divisor; a largura, do espaço que sobra na janela.
    void MontarTituloAgenda(Control referencia)
    {
        // onde a linha vertical termina (mesma conta usada em MontarDivisorVertical)
        int fimDoDivisor = referencia.Right + EspacoAteDivisor + LarguraDivisorVertical;

        // mesmo respiro que existe entre o card e a linha, agora entre a linha e o título
        int x = fimDoDivisor + EspacoAteDivisor;

        lblTituloAgenda = new Label
        {
            Font = new Font(PaletaAgenda.FontePadrao, 28f),
            ForeColor = Cores.Vinho,                 // mesma cor do "Solicitações"
            BackColor = Color.Transparent,
            AutoSize = false,                         // nós controlamos o tamanho
            AutoEllipsis = true,                      // se não couber, termina em "..."
            TextAlign = ContentAlignment.MiddleLeft,
            UseCompatibleTextRendering = false,       // motor GDI (ClearType): bordas mais suaves em fonte grande
            Location = new Point(x, referencia.Top),
            // >>> ALTERADO (item 5): reserva espaço à direita para o botão <<<
            Size = new Size(ClientSize.Width - x - MargemDireita - LarguraBotaoNovo - EspacoBotaoTitulo,
                            AlturaTituloAgenda),
            // acompanha a janela: fixo no topo/esquerda e estica até a direita
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        Controls.Add(lblTituloAgenda);

        AtualizarTitulo(DateTime.Today);              // estado inicial
    }

    // >>> NOVO (item 5): botão no canto superior direito, acima da linha divisória <<<
    // Usa o MiniRotulo como botão: a classe já desenha retângulo arredondado com texto centralizado.
    void MontarBotaoNovoAgendamento(Control referencia)
    {
        // encostado na margem direita da janela
        int x = ClientSize.Width - MargemDireita - LarguraBotaoNovo;
        // centralizado na vertical dentro da faixa do título (fica acima da linha)
        int y = referencia.Top + (AlturaTituloAgenda - AlturaBotaoNovo) / 2;

        var botao = new MiniRotulo
        {
            Text = "+ Novo Agendamento",
            CorFundo = Cores.Primaria,
            CorBorda = Cores.Primaria,
            CorTexto = Color.White,
            Raio = 12,
            TamanhoFonte = 11f,
            Location = new Point(x, y),
            Size = new Size(LarguraBotaoNovo, AlturaBotaoNovo),
            Anchor = AnchorStyles.Top | AnchorStyles.Right   // acompanha a borda direita ao redimensionar
        };

        // hover: a cor primária fica 15% mais escura enquanto o mouse está em cima
        EfeitosAgenda.AplicarHover(botao, PaletaAgenda.Misturar(Cores.Primaria, Color.Black, 0.15f));
        botao.Click += (s, e) => AbrirNovoAgendamento();

        Controls.Add(botao);
    }

    // >>> NOVO (item 5): abre o miniformulário e recebe o agendamento confirmado <<<
    void AbrirNovoAgendamento()
    {
        using var frm = new FrmNovoAgendamento(agendamentos);

        // ShowDialog = modal: a agenda fica travada até o formulário fechar.
        // TopLevelControl = a janela de verdade (o Dashboard, quando a agenda está encaixada nele)
        if (frm.ShowDialog(TopLevelControl ?? this) != DialogResult.OK || frm.Resultado == null) return;

        agendamentos.Add(frm.Resultado);   // a partir daqui o horário passa a contar como ocupado

        // >>> ALTERADO (itens 7 e 8): o card do novo agendamento aparece sozinho na agenda <<<
        AtualizarAgendaDia();

        // A confirmação provisória só continua existindo quando o card NÃO aparece agora
        // (data diferente da que está na tela, ou escondido pelos filtros): sem ela, a pessoa
        // não teria nenhum retorno visual de que o agendamento foi salvo.
        bool apareceNaTela = frm.Resultado.Data.Date == DataAtual.Date
                          && FiltrosAtuais.Aceita(frm.Resultado, frm.Resultado.Status);
        if (apareceNaTela) return;

        MessageBox.Show(TopLevelControl ?? this,
            $"{frm.Resultado.Cliente}\n{frm.Resultado.DescricaoProcedimentos}\n" +
            $"{frm.Resultado.Data:dd/MM/yyyy} — {RegrasAgenda.Formatar(frm.Resultado.InicioMin)} " +
            $"às {RegrasAgenda.Formatar(frm.Resultado.FimMin)}",
            "Agendamento salvo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // >>> NOVO 3/3: método da divisória abaixo do título <<<
    // Começa colada na linha vertical e vai até o fim da janela.
    // Usa a mesma classe (LinhaDivisoria) das outras divisórias: cor e espessura idênticas.
    void MontarDivisorTitulo(Control referencia)
    {
        // mesmo X da linha vertical (ver MontarDivisorVertical): é aqui que a nova linha "cola"
        int x = referencia.Right + EspacoAteDivisor;

        // logo abaixo do título: topo + altura do título + espaço ajustável
        int y = referencia.Top + AlturaTituloAgenda + EspacoTituloDivisor;

        Controls.Add(new LinhaDivisoria
        {
            Location = new Point(x, y),
            Size = new Size(ClientSize.Width - x, AlturaDivisor),   // até a borda direita da janela
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        });
    }

    // >>> NOVO (item 6): faixa de filtros logo abaixo da divisória do título + divisória inferior <<<
    // Todas as posições vêm das constantes que já existem; nenhuma coordenada solta.
    void MontarFaixaFiltros(Control referencia)
    {
        // X da faixa = mesmo X do título principal (alinhamento inicial com ele)
        int x = referencia.Right + 2 * EspacoAteDivisor + LarguraDivisorVertical;

        // Y da divisória do título (mesma conta de MontarDivisorTitulo) + a divisória + um respiro
        int yDivisorTitulo = referencia.Top + AlturaTituloAgenda + EspacoTituloDivisor;
        int y = yDivisorTitulo + AlturaDivisor + EspacoAcimaFaixa;

        faixaFiltros = new FaixaFiltros(AlturaFiltro);   // mesma altura dos minicards da coluna esquerda

        // largura = espaço que sobra na janela, mas nunca menor que o necessário (nada fica cortado)
        int larguraDisponivel = ClientSize.Width - x - MargemDireita;
        faixaFiltros.Location = new Point(x, y);
        faixaFiltros.Size = new Size(Math.Max(larguraDisponivel, faixaFiltros.LarguraNecessaria), AlturaFiltro);
        faixaFiltros.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // sempre que um filtro mudar, o formulário é avisado
        faixaFiltros.FiltrosAlterados += (s, e) => AoMudarFiltros();
        Controls.Add(faixaFiltros);

        // divisória de baixo: mesmo X da divisória do título, até a borda direita da janela
        int xDivisor = referencia.Right + EspacoAteDivisor;
        int yDivisor = y + AlturaFiltro + EspacoAbaixoFaixa;

        Controls.Add(new LinhaDivisoria
        {
            Location = new Point(xDivisor, yDivisor),
            Size = new Size(ClientSize.Width - xDivisor, AlturaDivisor),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        });
    }

    // >>> NOVO (itens 7 e 8): área dos agendamentos, logo abaixo da divisória de baixo dos filtros <<<
    // Todas as posições vêm das constantes que já existem; a conta é a mesma de MontarFaixaFiltros.
    void MontarAreaAgenda(Control referencia)
    {
        // mesmo X da faixa de filtros e do título principal
        int x = referencia.Right + 2 * EspacoAteDivisor + LarguraDivisorVertical;

        // Y da divisória de baixo dos filtros (mesma conta de MontarFaixaFiltros) + a divisória + um respiro
        int yDivisorTitulo = referencia.Top + AlturaTituloAgenda + EspacoTituloDivisor;
        int yFaixa = yDivisorTitulo + AlturaDivisor + EspacoAcimaFaixa;
        int yDivisorFiltros = yFaixa + AlturaFiltro + EspacoAbaixoFaixa;
        int y = yDivisorFiltros + AlturaDivisor + EspacoAcimaAgenda;

        areaAgenda = new AreaAgendaDia
        {
            Location = new Point(x, y),
            Size = new Size(ClientSize.Width - x - MargemDireita,
                            Math.Max(ClientSize.Height - y - MargemInferior, AreaAgendaDia.AlturaMinima)),
            // acompanha a janela nas quatro direções: a escala se ajusta à altura disponível
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        Controls.Add(areaAgenda);

        AtualizarAgendaDia();                         // estado inicial
    }

    // >>> NOVO (itens 7 e 8): ponto de encontro com os itens 5 e 6 e com o calendário <<<
    // Escolhe, entre os agendamentos salvos, só os da data na tela que passam nos dois filtros.
    // A lista original (agendamentos) nunca é alterada: a área recebe apenas uma consulta sobre ela.
    void AtualizarAgendaDia()
    {
        var filtros = FiltrosAtuais;
        DateTime data = DataAtual.Date;

        areaAgenda.Definir(agendamentos
            .Where(a => a.Data.Date == data && filtros.Aceita(a, a.Status))
            .OrderBy(a => a.InicioMin));
    }

    // >>> NOVO (item 6): ponto de encontro com o item 7 <<<
    // >>> ALTERADO (itens 7 e 8): agora refaz os cards com os filtros escolhidos <<<
    void AoMudarFiltros()
    {
        AtualizarAgendaDia();
    }

    // Único lugar que formata a data do título.
    void AtualizarTitulo(DateTime data)
    {
        // dd = dia com 2 dígitos | 'de' entre aspas = texto literal | MMMM = mês por extenso (minúsculo em pt-BR)
        lblTituloAgenda.Text = "Agenda — " +
            data.ToString("dd 'de' MMMM 'de' yyyy", new CultureInfo("pt-BR"));
    }

    // Linha vertical colada ao lado direito da coluna (calendário + solicitações).
    // Começa no topo do card do calendário e termina na base do último card,
    // mais a ExtensaoDivisor.
    void MontarDivisorVertical(Control referencia, int baseDaColuna)
    {
        Controls.Add(new LinhaVertical
        {
            // logo à direita do card, com um respiro pequeno
            Location = new Point(referencia.Right + EspacoAteDivisor, referencia.Top),
            // altura = do topo da coluna até a base do último card + extensão
            Size = new Size(LarguraDivisorVertical, baseDaColuna - referencia.Top + ExtensaoDivisor)
        });
    }

    // Monta a seção abaixo do card de referência (o card do minicalendário).
    // Tudo é calculado a partir da posição e da largura dele.
    // Devolve a base (coordenada Y) do último card, para o divisor vertical usar.
    int MontarSolicitacoes(Control referencia)
    {
        int x = referencia.Left;
        int largura = referencia.Width;
        int y = referencia.Bottom + EspacoAcimaDivisor;

        // 1) linha divisória
        Controls.Add(new LinhaDivisoria
        {
            Location = new Point(x, y),
            Size = new Size(largura, AlturaDivisor)
        });
        y += AlturaDivisor + EspacoDepoisDivisor;

        // 2) título "Solicitações" (mesma fonte do título do calendário: 11 bold)
        Controls.Add(new Label
        {
            Text = "Solicitações",
            Font = new Font(PaletaAgenda.FontePadrao, 11f, FontStyle.Bold),
            ForeColor = Cores.Escuro,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            UseCompatibleTextRendering = false,   // motor GDI (ClearType), como o título grande
            Location = new Point(x, y),
            Size = new Size(largura, AlturaTitulo)
        });
        y += AlturaTitulo + 6;

        // 3) três minicards de categoria, mesma largura cada um
        string[] filtros = { "Pendentes", "Alterações", "Cancelamentos" };
        int larguraFiltro = (largura - 2 * EspacoFiltros) / 3;

        for (int i = 0; i < filtros.Length; i++)
        {
            Controls.Add(new MiniRotulo
            {
                Text = filtros[i],
                Location = new Point(x + i * (larguraFiltro + EspacoFiltros), y),
                Size = new Size(larguraFiltro, AlturaFiltro)
            });
        }
        y += AlturaFiltro + 12;

        // 4) quatro cards fictícios, empilhados, todos com a mesma largura e altura
        var dados = new (string Nome, string Data, string Procedimento)[]
        {
            ("Mariana Oliveira", "05/11/2026 — 09:00", "Corte feminino"),
            ("Camila Santos",    "05/11/2026 — 10:30", "Coloração"),
            ("Fernanda Costa",   "06/11/2026 — 14:00", "Manicure e pedicure"),
            ("Beatriz Almeida",  "07/11/2026 — 11:00", "Hidratação capilar"),
        };

        foreach (var d in dados)
        {
            Controls.Add(new CardSolicitacao
            {
                Cliente = d.Nome,
                DataHora = d.Data,
                Procedimento = d.Procedimento,
                Location = new Point(x, y),
                Size = new Size(largura, CardSolicitacao.AlturaIdeal)
            });
            y += CardSolicitacao.AlturaIdeal + EspacoCards;
        }

        // o laço soma EspacoCards depois de cada card, inclusive o último; desfazemos isso
        return y - EspacoCards;
    }
}
