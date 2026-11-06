using dashboardforms.Login;

namespace dashboardforms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        while (true)
        {
            Usuario? usuario;
            using (var login = new FrmLogin())
            {
                if (login.ShowDialog() != DialogResult.OK || login.Usuario == null) return;
                usuario = login.Usuario;
            }

            Repositorios.Abrir(usuario);
            using var principal = new DashboardForm(usuario);
            Application.Run(principal);
            if (!principal.SaiuDaConta) return;
        }
    }
}
