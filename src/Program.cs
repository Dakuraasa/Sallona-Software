using dashboardforms.Login;

namespace dashboardforms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 1) Tela de login primeiro  2) depois do login, o Dashboard com os dados daquela conta
        // 3) se clicar em "Sair", volta para o login; se fechar a janela, encerra o programa
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
