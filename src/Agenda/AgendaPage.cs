namespace dashboardforms.Agenda;

// ===== Página da Agenda dentro do Dashboard =====
// A FrmAgenda foi feita como uma janela própria. Aqui ela é "encaixada" na área de páginas,
// sem borda e ocupando todo o espaço, igual às outras páginas (Clientes, Serviços...).
// Assim o layout da agenda continua do jeito que foi feito, só que dentro do menu lateral.
class AgendaPage : UserControl
{
    readonly FrmAgenda agenda;

    public AgendaPage()
    {
        BackColor = Cores.Fundo;
        DoubleBuffered = true;

        agenda = new FrmAgenda
        {
            TopLevel = false,                         // deixa de ser janela e vira um controle comum
            FormBorderStyle = FormBorderStyle.None,   // sem barra de título
            Dock = DockStyle.Fill
        };

        Controls.Add(agenda);
        agenda.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) agenda.Dispose();
        base.Dispose(disposing);
    }
}
