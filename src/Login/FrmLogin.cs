using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace dashboardforms.Login
{
    public class FrmLogin : Form
    {
        Color corFundo = Cores.Fundo;
        Color corRosa = Cores.Primaria;
        Color corRosaEscuro = ColorTranslator.FromHtml("#983E58");
        Color corRosaClaro = ColorTranslator.FromHtml("#EBCFD6");
        Color corVinho = Cores.Escuro;
        Color corTexto = Cores.Texto;
        Color corCinza = Cores.Cinza;
        Color corBorda = Cores.Borda;
        Color corErro = Cores.Vinho;

        readonly UsuarioRepositorio contas = new();

        public Usuario? Usuario { get; private set; }

        Panel pnlEsquerdo = null!;
        Panel pnlDireito = null!;
        BotaoArredondado btnEntrar = null!;
        PainelArredondado campoEmail = null!;
        PainelArredondado campoSenha = null!;
        TextBox txtEmail = null!;
        TextBox txtSenha = null!;
        Label lblMostrar = null!;
        Label lblErro = null!;
        CheckBox chkLembrar = null!;
        LinkLabel lnkEsqueci = null!;
        LinkLabel lnkCadastro = null!;

        string arquivoEmail = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Sallona", "ultimo_email.txt");

        Label lblSubtitulo = null!;

        public FrmLogin()
        {
            ConfigurarForm();
            CriarPainelEsquerdo();
            CriarPainelDireito();
            CarregarEmail();
            AcceptButton = btnEntrar;

            if (contas.Vazio)
                lblSubtitulo.Text = "Primeiro acesso? Cadastre o estabelecimento no link abaixo.";
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

            CriarLabel(pnlEsquerdo,
                "Clientes, serviços, atendimentos e financeiro do salão em um só lugar.",
                70, 380, 360, 60, new Font("Segoe UI", 10F), corCinza, ContentAlignment.TopCenter);

            CriarLabel(pnlEsquerdo, "Sistema desenvolvido pelo Grupo SinapsES", 0, 596, 500, 24,
                new Font("Segoe UI", 9F), corCinza, ContentAlignment.MiddleCenter);

            pnlEsquerdo.Paint += DesenharArcos;
        }

        static string BuscarLogo()
        {
            string caminho = Path.Combine(AppContext.BaseDirectory, "logo.jpg");
            return File.Exists(caminho) ? caminho : "";
        }

        static Bitmap RemoverFundoBranco(Bitmap original)
        {
            double escala = Math.Min(1.0, 600.0 / Math.Max(original.Width, original.Height));
            int largura = Math.Max(1, (int)(original.Width * escala));
            int altura = Math.Max(1, (int)(original.Height * escala));

            var resultado = new Bitmap(largura, altura, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(resultado))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, largura, altura);
            }

            var area = new Rectangle(0, 0, largura, altura);
            var dados = resultado.LockBits(area, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int total = Math.Abs(dados.Stride) * altura;
                var bytes = new byte[total];
                Marshal.Copy(dados.Scan0, bytes, 0, total);
                for (int i = 0; i + 3 < total; i += 4)
                {
                    int menor = Math.Min(bytes[i], Math.Min(bytes[i + 1], bytes[i + 2]));
                    if (menor >= 245) bytes[i + 3] = 0;
                    else if (menor > 215) bytes[i + 3] = (byte)((245 - menor) * 255 / 30);
                }
                Marshal.Copy(bytes, 0, dados.Scan0, total);
            }
            finally
            {
                resultado.UnlockBits(dados);
            }
            return resultado;
        }

        void DesenharArcos(object? sender, PaintEventArgs e)
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

            lblSubtitulo = CriarLabel(pnlDireito, "Acesse sua conta para continuar.", x, 108, largura, 24,
                new Font("Segoe UI", 10F), corCinza, ContentAlignment.TopLeft);

            var aviso = new PainelArredondado
            {
                Left = x, Top = 150, Width = largura, Height = 44, Raio = 10,
                BackColor = corFundo, CorBorda = corFundo
            };
            aviso.Controls.Add(new Label
            {
                Text = "🏪   Acesso do estabelecimento — dono(a) e equipe",
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = corVinho, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            });
            pnlDireito.Controls.Add(aviso);

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
                "Uma conta por estabelecimento, com dados salvos neste computador. A senha é guardada de forma protegida (hash).",
                x, 516, largura, 44, new Font("Segoe UI", 9F), corCinza, ContentAlignment.TopLeft);

            lnkCadastro = new LinkLabel();
            lnkCadastro.Text = "Estabelecimento novo? Cadastre-se";
            lnkCadastro.LinkArea = new LinkArea(22, 11);
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

        void lblMostrar_Click(object? sender, EventArgs e)
        {
            txtSenha.UseSystemPasswordChar = !txtSenha.UseSystemPasswordChar;

            if (txtSenha.UseSystemPasswordChar)
                lblMostrar.Text = "Mostrar";
            else
                lblMostrar.Text = "Ocultar";

            txtSenha.Focus();
        }

        void btnEntrar_Click(object? sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;

            if (email == "")
            {
                MostrarErro("Informe o seu e-mail.", txtEmail);
                return;
            }

            if (!UsuarioRepositorio.EmailValido(email))
            {
                MostrarErro("Digite um e-mail válido.", txtEmail);
                return;
            }

            if (senha == "")
            {
                MostrarErro("Informe a sua senha.", txtSenha);
                return;
            }

            var usuario = contas.Autenticar(email, senha);
            if (usuario == null)
            {
                txtSenha.Clear();
                MostrarErro(contas.Vazio ? "Nenhuma conta cadastrada ainda. Clique em \"Cadastre-se\"."
                                         : "E-mail ou senha incorretos.", txtSenha);
                return;
            }

            lblErro.Text = "";
            SalvarEmail(email);
            Usuario = usuario;
            DialogResult = DialogResult.OK;
            Close();
        }

        void lnkEsqueci_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "A recuperação de senha por e-mail ainda não está disponível nesta versão.\n" +
                "Quem estiver conectado pode trocar a senha em Configurações.",
                "Recuperar senha", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void lnkCadastro_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            using var cadastro = new FrmCadastro(contas);
            if (cadastro.ShowDialog(this) != DialogResult.OK || cadastro.Criado == null) return;

            txtEmail.Text = cadastro.Criado.Email;
            txtSenha.Clear();
            lblSubtitulo.Text = "Conta criada! Digite sua senha para entrar.";
            txtSenha.Focus();
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
                    Directory.CreateDirectory(Path.GetDirectoryName(arquivoEmail)!);
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
