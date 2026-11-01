using System.Globalization;
using dashboardforms;

namespace dashboardforms.Agenda;

public partial class FrmAgenda : Form
{
    // ----- Constantes de espaçamento da coluna esquerda -----
    const int EspacoAcimaDivisor = 12;
    const int AlturaDivisor = 2;
    const int AlturaTitulo = 24;
    const int EspacoFiltros = 4;       // entre os 3 minicards de categoria
    const int AlturaFiltro = 26;
    const int EspacoCards = 8;         // entre os 4 cards de solicitação

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

        calendario.DataSelecionadaAlterada += (s, e) =>
            Text = "Agenda — " + calendario.DataSelecionada?.ToString("dddd, dd/MM/yyyy", new CultureInfo("pt-BR"));

        cardCalendario.Controls.Add(calendario);
        Controls.Add(cardCalendario);

        MontarSolicitacoes(cardCalendario);   // itens 2 e 3 (só visual)
    }

    private void FrmAgenda_Load(object sender, EventArgs e)
    {
    }

    // Monta a seção abaixo do card de referência (o card do minicalendário).
    // Tudo é calculado a partir da posição e da largura dele.
    void MontarSolicitacoes(Control referencia)
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
        y += AlturaDivisor + 8;

        // 2) título "Solicitações" (mesma fonte do título do calendário: Segoe UI 11 bold)
        Controls.Add(new Label
        {
            Text = "Solicitações",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Cores.Escuro,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            UseCompatibleTextRendering = true,   // mesmo motor de texto (GDI+) do DrawString
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
    }
}