namespace dashboardforms;

// ===== Base das janelas de cadastro (cliente, atendimento, conta, meta) =====
// Monta o visual padrão Sallona: título, campos um embaixo do outro, mensagem de erro
// e os botões Salvar / Cancelar. Cada janela só adiciona os campos e diz como validar.
class Formulario : Form
{
    protected const int LarguraCampo = 360;

    readonly FlowLayoutPanel corpo;
    readonly Label lblErro = new()
    {
        Width = LarguraCampo, Height = 38, ForeColor = Cores.Vinho,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold), Margin = new Padding(0, 8, 0, 0)
    };
    readonly BotaoAnimado btnSalvar;

    /// <summary>Validação: devolve a mensagem de erro, ou null se estiver tudo certo.</summary>
    protected Func<string?> Validar = () => null;

    /// <summary>O que fazer quando a validação passa (ex.: gravar). Se der exceção, a janela não fecha.</summary>
    protected Action AoSalvar = () => { };

    public Formulario(string titulo, string textoSalvar = "Salvar")
    {
        Text = titulo;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Cores.Fundo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        Padding = new Padding(16);

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(26, 20, 26, 18) };
        corpo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White
        };
        corpo.Controls.Add(new Label
        {
            Text = titulo,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Cores.Escuro,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        });
        card.Controls.Add(corpo);
        Controls.Add(card);

        btnSalvar = new BotaoAnimado
        {
            Text = textoSalvar, Size = new Size(130, 38), BackColor = Cores.Primaria, ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0)
        };
        var cancelar = new BotaoAnimado
        {
            Text = "Cancelar", Size = new Size(110, 38), BackColor = Cores.Fundo, ForeColor = Cores.Texto,
            Cursor = Cursors.Hand, Margin = new Padding(0, 0, 10, 0), DialogResult = DialogResult.Cancel
        };
        btnSalvar.Click += (_, _) => Salvar();
        AcceptButton = btnSalvar;
        CancelButton = cancelar;

        // os botões entram por último (no Load), para ficarem depois de todos os campos
        Load += (_, _) =>
        {
            corpo.Controls.Add(lblErro);
            var botoes = new FlowLayoutPanel
            {
                Width = LarguraCampo, Height = 44, FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.White, Margin = new Padding(0, 4, 0, 0)
            };
            botoes.Controls.Add(btnSalvar);
            botoes.Controls.Add(cancelar);
            corpo.Controls.Add(botoes);

            // altura da janela = soma dos campos (sem precisar calcular na mão em cada tela)
            int altura = corpo.Controls.Cast<Control>().Sum(c => c.Height + c.Margin.Vertical);
            var tela = Screen.FromPoint(Cursor.Position).WorkingArea;
            // se não couber na tela (notebook 768px), a janela fica menor e os campos rolam
            ClientSize = new Size(LarguraCampo + 32 + 52 + 8 + 18, Math.Min(altura + 32 + 38 + 8, tela.Height - 60));
        };
        // garante que a janela inteira (com os botões) fique dentro da tela
        Shown += (_, _) =>
        {
            var tela = Screen.FromControl(this).WorkingArea;
            Location = new Point(Math.Clamp(Left, tela.Left, Math.Max(tela.Left, tela.Right - Width)),
                                 Math.Clamp(Top, tela.Top, Math.Max(tela.Top, tela.Bottom - Height)));
        };
    }

    // ---------- helpers para montar os campos ----------
    protected void AdicionarRotulo(string texto) => corpo.Controls.Add(new Label
    {
        Text = texto,
        AutoSize = true,
        ForeColor = Cores.Cinza,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Margin = new Padding(0, 10, 0, 4)
    });

    protected T AdicionarCampo<T>(string rotulo, T campo) where T : Control
    {
        AdicionarRotulo(rotulo);
        campo.Width = LarguraCampo;
        campo.Font = new Font("Segoe UI", 10.5f);
        campo.Margin = new Padding(0);
        corpo.Controls.Add(campo);
        return campo;
    }

    protected void AdicionarControle(Control c) => corpo.Controls.Add(c);

    protected static TextBox NovaCaixaTexto(int limite = 100) =>
        new() { BorderStyle = BorderStyle.FixedSingle, MaxLength = limite };

    void Salvar()
    {
        string? erro = Validar();
        if (erro != null)
        {
            lblErro.Text = erro;
            return;
        }
        try
        {
            AoSalvar();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            lblErro.Text = ex.Message;
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
