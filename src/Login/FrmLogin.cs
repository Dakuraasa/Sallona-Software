using dashboardforms;
using dashboardforms.Login;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace TelaLogin
{
    public class FrmLogin : Form
    {
        Color corFundo = ColorTranslator.FromHtml("#F5EAE8");
        Color corRosa = ColorTranslator.FromHtml("#B04A66");
        Color corRosaEscuro = ColorTranslator.FromHtml("#983E58");
        Color corRosaClaro = ColorTranslator.FromHtml("#EBCFD6");
        Color corVinho = ColorTranslator.FromHtml("#2A0710");
        Color corTexto = ColorTranslator.FromHtml("#2B1B3A");
        Color corCinza = ColorTranslator.FromHtml("#7D7280");
        Color corBorda = ColorTranslator.FromHtml("#E6D5D8");
        Color corErro = ColorTranslator.FromHtml("#B3263E");

        Panel pnlEsquerdo;
        Panel pnlDireito;
        BotaoArredondado btnEntrar;
        BotaoArredondado btnProprietaria;
        BotaoArredondado btnCliente;
        PainelArredondado campoEmail;
        PainelArredondado campoSenha;
        TextBox txtEmail;
        TextBox txtSenha;
        Label lblMostrar;
        Label lblErro;
        CheckBox chkLembrar;
        LinkLabel lnkEsqueci;
        LinkLabel lnkCadastro;

        bool ehProprietaria = true;

        string arquivoEmail = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Sallona", "ultimo_email.txt");

        public FrmLogin()
        {
            ConfigurarForm();
            CriarPainelEsquerdo();
            CriarPainelDireito();
            MudarPerfil(true);
            CarregarEmail();
            AcceptButton = btnEntrar;
        }

        void ConfigurarForm()
        {
            Text = "Sallona - Entrar no sistema";
            ClientSize = new Size(1000, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);
            DoubleBuffered = true;
        }

        void CriarPainelEsquerdo()
        {
            pnlEsquerdo = new Panel();
            pnlEsquerdo.Left = 0;
            pnlEsquerdo.Top = 0;
            pnlEsquerdo.Width = 500;
            pnlEsquerdo.Height = 640;
            pnlEsquerdo.BackColor = corFundo;
            Controls.Add(pnlEsquerdo);

            string caminhoLogo = BuscarLogo();

            if (caminhoLogo != "")
            {
                PictureBox logo = new PictureBox();
                logo.Left = 100;
                logo.Top = 50;
                logo.Width = 300;
                logo.Height = 290;
                logo.SizeMode = PictureBoxSizeMode.Zoom;
                logo.BackColor = Color.Transparent;
                byte[] bytesLogo = File.ReadAllBytes(caminhoLogo);
                using (Bitmap original = new Bitmap(new MemoryStream(bytesLogo)))
                {
                    logo.Image = RemoverFundoBranco(original);
                }
                pnlEsquerdo.Controls.Add(logo);
            }
            else
            {
                CriarLabel(pnlEsquerdo, "Sallona", 0, 140, 500, 80,
                    new Font("Georgia", 40F, FontStyle.Italic), corVinho, ContentAlignment.MiddleCenter);
            }

            CriarLabel(pnlEsquerdo, "Instituto de Beleza\nDany Ribeiro", 50, 370, 400, 80,
                new Font("Georgia", 20F), corTexto, ContentAlignment.TopCenter);

            CriarLabel(pnlEsquerdo,
                "Agenda, ficha capilar e financeiro do salão em um só lugar, com seus dados protegidos.",
                70, 462, 360, 60, new Font("Segoe UI", 10F), corCinza, ContentAlignment.TopCenter);

            CriarLabel(pnlEsquerdo, "Sistema desenvolvido pelo Grupo SinapsES", 0, 596, 500, 24,
                new Font("Segoe UI", 9F), corCinza, ContentAlignment.MiddleCenter);

            pnlEsquerdo.Paint += DesenharArcos;
        }

        string BuscarLogo()
        {
            try
            {
                string pastaExe = Application.StartupPath;
                string[] extensoes = { "*.png", "*.jpg", "*.jpeg" };

                DirectoryInfo pastaProjeto = new DirectoryInfo(pastaExe);
                while (pastaProjeto != null && pastaProjeto.GetFiles("*.csproj").Length == 0)
                    pastaProjeto = pastaProjeto.Parent;

                DirectoryInfo[] locais = { new DirectoryInfo(pastaExe), pastaProjeto };

                foreach (DirectoryInfo local in locais)
                {
                    if (local == null)
                        continue;

                    foreach (string extensao in extensoes)
                    {
                        foreach (FileInfo arquivo in local.GetFiles(extensao, SearchOption.AllDirectories))
                        {
                            string nome = arquivo.Name.ToLower();

                            if (nome.Contains("logo") || nome.Contains("sallona"))
                                return arquivo.FullName;
                        }
                    }
                }
            }
            catch
            {
            }

            return "";
        }

        Bitmap RemoverFundoBranco(Bitmap original)
        {
            double escala = Math.Min(1.0, 600.0 / Math.Max(original.Width, original.Height));
            int largura = (int)(original.Width * escala);
            int altura = (int)(original.Height * escala);

            Bitmap reduzido = new Bitmap(original, largura, altura);
            Bitmap resultado = new Bitmap(largura, altura, PixelFormat.Format32bppArgb);

            for (int y = 0; y < altura; y++)
            {
                for (int x = 0; x < largura; x++)
                {
                    Color cor = reduzido.GetPixel(x, y);
                    int menor = Math.Min(cor.R, Math.Min(cor.G, cor.B));
                    int alfa = 255;

                    if (menor >= 245)
                        alfa = 0;
                    else if (menor > 215)
                        alfa = (245 - menor) * 255 / 30;

                    resultado.SetPixel(x, y, Color.FromArgb(alfa, cor.R, cor.G, cor.B));
                }
            }

            reduzido.Dispose();
            return resultado;
        }

        void DesenharArcos(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Pen caneta = new Pen(corRosaClaro, 14);
            caneta.StartCap = LineCap.Round;
            caneta.EndCap = LineCap.Round;
            e.Graphics.DrawArc(caneta, 390, 520, 220, 220, 180, 90);
            e.Graphics.DrawArc(caneta, -110, -110, 220, 220, 0, 90);
            caneta.Dispose();
        }

        void CriarPainelDireito()
        {
            pnlDireito = new Panel();
            pnlDireito.Left = 500;
            pnlDireito.Top = 0;
            pnlDireito.Width = 500;
            pnlDireito.Height = 640;
            pnlDireito.BackColor = Color.White;
            Controls.Add(pnlDireito);

            int x = 60;
            int largura = 380;
            Font fonteNegrito = new Font("Segoe UI", 10F, FontStyle.Bold);

            CriarLabel(pnlDireito, "Entrar no sistema", x, 55, largura, 50,
                new Font("Georgia", 26F), corTexto, ContentAlignment.TopLeft);

            CriarLabel(pnlDireito, "Acesse sua conta para continuar.", x, 108, largura, 24,
                new Font("Segoe UI", 10F), corCinza, ContentAlignment.TopLeft);

            btnProprietaria = CriarAba("Proprietária", x, 150);
            btnCliente = CriarAba("Cliente", x + 194, 150);
            btnProprietaria.Click += btnProprietaria_Click;
            btnCliente.Click += btnCliente_Click;

            CriarLabel(pnlDireito, "E-mail", x, 222, largura, 22, fonteNegrito, corTexto, ContentAlignment.TopLeft);
            campoEmail = CriarCampo(x, 246, largura, "seu@email.com", out txtEmail);

            CriarLabel(pnlDireito, "Senha", x, 308, largura, 22, fonteNegrito, corTexto, ContentAlignment.TopLeft);
            campoSenha = CriarCampo(x, 332, largura, "Sua senha", out txtSenha);
            txtSenha.UseSystemPasswordChar = true;
            txtSenha.Width = largura - 90;

            lblMostrar = new Label();
            lblMostrar.Text = "Mostrar";
            lblMostrar.Font = new Font("Segoe UI", 9F);
            lblMostrar.ForeColor = corRosa;
            lblMostrar.BackColor = Color.White;
            lblMostrar.Cursor = Cursors.Hand;
            lblMostrar.AutoSize = false;
            lblMostrar.Width = 58;
            lblMostrar.Height = 24;
            lblMostrar.Left = largura - 66;
            lblMostrar.Top = 10;
            lblMostrar.TextAlign = ContentAlignment.MiddleRight;
            lblMostrar.Click += lblMostrar_Click;
            campoSenha.Controls.Add(lblMostrar);

            chkLembrar = new CheckBox();
            chkLembrar.Text = "Lembrar-me";
            chkLembrar.ForeColor = corTexto;
            chkLembrar.BackColor = Color.White;
            chkLembrar.Cursor = Cursors.Hand;
            chkLembrar.Left = x;
            chkLembrar.Top = 392;
            chkLembrar.Width = 160;
            chkLembrar.Height = 26;
            pnlDireito.Controls.Add(chkLembrar);

            lnkEsqueci = new LinkLabel();
            lnkEsqueci.Text = "Esqueci a senha";
            lnkEsqueci.Left = x + largura - 150;
            lnkEsqueci.Top = 394;
            lnkEsqueci.Width = 150;
            lnkEsqueci.Height = 22;
            lnkEsqueci.TextAlign = ContentAlignment.MiddleRight;
            lnkEsqueci.LinkColor = corRosa;
            lnkEsqueci.ActiveLinkColor = corRosaEscuro;
            lnkEsqueci.VisitedLinkColor = corRosa;
            lnkEsqueci.LinkBehavior = LinkBehavior.HoverUnderline;
            lnkEsqueci.BackColor = Color.White;
            lnkEsqueci.Cursor = Cursors.Hand;
            lnkEsqueci.Click += lnkEsqueci_Click;
            pnlDireito.Controls.Add(lnkEsqueci);

            btnEntrar = new BotaoArredondado();
            btnEntrar.Text = "Entrar";
            btnEntrar.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnEntrar.ForeColor = Color.White;
            btnEntrar.CorNormal = corRosa;
            btnEntrar.CorHover = corRosaEscuro;
            btnEntrar.Raio = 12;
            btnEntrar.Left = x;
            btnEntrar.Top = 432;
            btnEntrar.Width = largura;
            btnEntrar.Height = 48;
            btnEntrar.Click += btnEntrar_Click;
            pnlDireito.Controls.Add(btnEntrar);

            lblErro = CriarLabel(pnlDireito, "", x, 488, largura, 22,
                new Font("Segoe UI", 9F), corErro, ContentAlignment.TopLeft);

            CriarLabel(pnlDireito,
                "Cada perfil só enxerga o que precisa. A sessão encerra após 15 minutos sem uso. Seus dados são tratados conforme a LGPD.",
                x, 516, largura, 44, new Font("Segoe UI", 9F), corCinza, ContentAlignment.TopLeft);

            lnkCadastro = new LinkLabel();
            lnkCadastro.Text = "Ainda não tem conta? Cadastre-se";
            lnkCadastro.LinkArea = new LinkArea(21, 11);
            lnkCadastro.Left = x;
            lnkCadastro.Top = 584;
            lnkCadastro.Width = largura;
            lnkCadastro.Height = 26;
            lnkCadastro.TextAlign = ContentAlignment.MiddleCenter;
            lnkCadastro.ForeColor = corCinza;
            lnkCadastro.LinkColor = corRosa;
            lnkCadastro.ActiveLinkColor = corRosaEscuro;
            lnkCadastro.LinkBehavior = LinkBehavior.HoverUnderline;
            lnkCadastro.BackColor = Color.White;
            lnkCadastro.Cursor = Cursors.Hand;
            lnkCadastro.LinkClicked += lnkCadastro_LinkClicked;
            pnlDireito.Controls.Add(lnkCadastro);
        }

        void lblMostrar_Click(object sender, EventArgs e)
        {
            txtSenha.UseSystemPasswordChar = !txtSenha.UseSystemPasswordChar;

            if (txtSenha.UseSystemPasswordChar)
                lblMostrar.Text = "Mostrar";
            else
                lblMostrar.Text = "Ocultar";

            txtSenha.Focus();
        }

        void btnEntrar_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;

            if (email == "")
            {
                MostrarErro("Informe o seu e-mail.", txtEmail);
                return;
            }

            if (!EmailValido(email))
            {
                MostrarErro("Digite um e-mail válido.", txtEmail);
                return;
            }

            if (senha == "")
            {
                MostrarErro("Informe a sua senha.", txtSenha);
                return;
            }

            if (!Autenticar(email, senha))
            {
                txtSenha.Clear();
                MostrarErro("E-mail ou senha incorretos.", txtSenha);
                return;
            }

            lblErro.Text = "";
            SalvarEmail(email);
            AbrirTelaPrincipal();
        }

        void lnkEsqueci_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();

            if (!EmailValido(email))
            {
                MostrarErro("Digite seu e-mail para recuperar a senha.", txtEmail);
                return;
            }

            MessageBox.Show(
                "Se o e-mail " + email + " estiver cadastrado, você receberá as instruções para redefinir a senha.",
                "Recuperar senha", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void lnkCadastro_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("A tela de cadastro será ligada aqui.", "Cadastro",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        bool Autenticar(string email, string senha)
        {
            if (ehProprietaria)
                return email.ToLower() == "dany@sallona.com" && senha == "123456";

            return email.ToLower() == "cliente@sallona.com" && senha == "123456";
        }

        void AbrirTelaPrincipal()
        {
            DashboardForm telaPrincipal = new DashboardForm();
            Hide();
            telaPrincipal.FormClosed += (s, e) => Close();
            telaPrincipal.Show();
        }

        void btnProprietaria_Click(object sender, EventArgs e)
        {
            MudarPerfil(true);
        }

        void btnCliente_Click(object sender, EventArgs e)
        {
            MudarPerfil(false);
        }

        void MudarPerfil(bool proprietaria)
        {
            ehProprietaria = proprietaria;
            PintarAba(btnProprietaria, proprietaria);
            PintarAba(btnCliente, !proprietaria);

            if (lblErro != null)
                lblErro.Text = "";
        }

        void PintarAba(BotaoArredondado aba, bool selecionada)
        {
            if (selecionada)
            {
                aba.CorNormal = corVinho;
                aba.CorHover = corVinho;
                aba.CorBorda = corVinho;
                aba.ForeColor = Color.White;
            }
            else
            {
                aba.CorNormal = Color.White;
                aba.CorHover = corFundo;
                aba.CorBorda = corBorda;
                aba.ForeColor = corTexto;
            }

            aba.Invalidate();
        }

        BotaoArredondado CriarAba(string texto, int x, int y)
        {
            BotaoArredondado aba = new BotaoArredondado();
            aba.Text = texto;
            aba.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            aba.Left = x;
            aba.Top = y;
            aba.Width = 186;
            aba.Height = 44;
            aba.Raio = 10;
            pnlDireito.Controls.Add(aba);
            return aba;
        }

        bool EmailValido(string email)
        {
            int arroba = email.IndexOf('@');
            if (arroba < 1)
                return false;

            int ponto = email.IndexOf('.', arroba);
            return ponto > arroba + 1 && !email.EndsWith(".") && !email.Contains(" ");
        }

        void MostrarErro(string mensagem, Control campo)
        {
            lblErro.Text = mensagem;
            campo.Focus();
        }

        void CarregarEmail()
        {
            try
            {
                if (File.Exists(arquivoEmail))
                {
                    txtEmail.Text = File.ReadAllText(arquivoEmail).Trim();
                    chkLembrar.Checked = txtEmail.Text != "";
                }
            }
            catch
            {
            }
        }

        void SalvarEmail(string email)
        {
            try
            {
                if (chkLembrar.Checked)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(arquivoEmail));
                    File.WriteAllText(arquivoEmail, email);
                }
                else if (File.Exists(arquivoEmail))
                {
                    File.Delete(arquivoEmail);
                }
            }
            catch
            {
            }
        }

        Label CriarLabel(Control pai, string texto, int x, int y, int largura, int altura,
            Font fonte, Color cor, ContentAlignment alinhamento)
        {
            Label lbl = new Label();
            lbl.Text = texto;
            lbl.Left = x;
            lbl.Top = y;
            lbl.Width = largura;
            lbl.Height = altura;
            lbl.Font = fonte;
            lbl.ForeColor = cor;
            lbl.BackColor = Color.Transparent;
            lbl.TextAlign = alinhamento;
            lbl.AutoSize = false;
            pai.Controls.Add(lbl);
            return lbl;
        }

        PainelArredondado CriarCampo(int x, int y, int largura, string dica, out TextBox caixa)
        {
            PainelArredondado painel = new PainelArredondado();
            painel.Left = x;
            painel.Top = y;
            painel.Width = largura;
            painel.Height = 44;
            painel.BackColor = Color.White;
            painel.Raio = 10;
            painel.CorBorda = corBorda;

            TextBox txt = new TextBox();
            txt.BorderStyle = BorderStyle.None;
            txt.Font = new Font("Segoe UI", 11F);
            txt.ForeColor = corTexto;
            txt.BackColor = Color.White;
            txt.PlaceholderText = dica;
            txt.Left = 14;
            txt.Top = 12;
            txt.Width = largura - 28;

            txt.Enter += (s, e) =>
            {
                painel.CorBorda = corRosa;
                painel.Invalidate();
            };

            txt.Leave += (s, e) =>
            {
                painel.CorBorda = corBorda;
                painel.Invalidate();
            };

            painel.Click += (s, e) => txt.Focus();
            painel.Controls.Add(txt);
            pnlDireito.Controls.Add(painel);

            caixa = txt;
            return painel;
        }
    }
}

    