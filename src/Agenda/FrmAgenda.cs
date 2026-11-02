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

        var calendario = new MiniCalendario { Dock = DockStyle.Fill };

        // quando o usuário escolhe uma data, o título acompanha
        // (se a seleção for limpa, volta para a data de hoje)
        calendario.DataSelecionadaAlterada += (s, e) =>
            AtualizarTitulo(calendario.DataSelecionada ?? DateTime.Today);

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
            Size = new Size(ClientSize.Width - x - MargemDireita, AlturaTituloAgenda),
            // acompanha a janela: fixo no topo/esquerda e estica até a direita
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        Controls.Add(lblTituloAgenda);

        AtualizarTitulo(DateTime.Today);              // estado inicial
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