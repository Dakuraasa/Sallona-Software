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
        cardCalendario.Controls.Add(new MiniCalendario { Dock = DockStyle.Fill });
        Controls.Add(cardCalendario);
    }

    private void FrmAgenda_Load(object sender, EventArgs e)
    {
    }
}