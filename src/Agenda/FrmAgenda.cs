using System.Globalization;
using dashboardforms;

namespace dashboardforms.Agenda;

public partial class FrmAgenda : Form
{
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

        // antes o calendário era criado direto dentro do Add; agora vai para uma variável
        var calendario = new MiniCalendario { Dock = DockStyle.Fill };

        // TESTE: reage ao evento mostrando a data no título da janela
        calendario.DataSelecionadaAlterada += (s, e) =>
            Text = "Agenda — " + calendario.DataSelecionada?.ToString("dddd, dd/MM/yyyy", new CultureInfo("pt-BR"));

        cardCalendario.Controls.Add(calendario);
        Controls.Add(cardCalendario);
    }

    private void FrmAgenda_Load(object sender, EventArgs e)
    {
    }
}