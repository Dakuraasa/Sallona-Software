namespace dashboardforms;

// ===== Janela para adicionar ou editar um serviço =====
class ServicoDialog : Form
{
    public static readonly string[] Categorias = { "Cabelo", "Unhas", "Estética", "Maquiagem", "Barba", "Outros" };

    public Servico Resultado { get; }

    readonly Func<string, bool> nomeEmUso;
    readonly TextBox txtNome = new() { Width = 360, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f) };
    readonly ComboBox cbCategoria = new() { Width = 360, DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10.5f) };
    readonly NumericUpDown numPreco = new() { Width = 170, DecimalPlaces = 2, Maximum = 100000, Increment = 5, ThousandsSeparator = true, Font = new Font("Segoe UI", 10.5f) };
    readonly NumericUpDown numDuracao = new() { Width = 170, Minimum = 5, Maximum = 600, Increment = 5, Value = 30, Font = new Font("Segoe UI", 10.5f) };
    readonly CheckBox chkAtivo = new() { Text = "Serviço ativo (aparece na hora de agendar)", AutoSize = true, Checked = true, ForeColor = Cores.Texto, Margin = new Padding(0, 14, 0, 0) };
    readonly Label lblErro = new() { Width = 360, Height = 38, ForeColor = Cores.Vinho, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Margin = new Padding(0, 8, 0, 0) };

    // nomeEmUso: função que diz se já existe outro serviço com esse nome
    public ServicoDialog(Servico? existente, Func<string, bool> nomeEmUso)
    {
        this.nomeEmUso = nomeEmUso;
        Resultado = existente?.Copia() ?? new Servico();
        bool novo = existente == null;

        Text = novo ? "Novo serviço" : "Editar serviço";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Cores.Fundo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(448, 486);
        Padding = new Padding(16);

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(26, 20, 26, 18) };
        var corpo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.White
        };

        corpo.Controls.Add(new Label
        {
            Text = novo ? "Novo serviço" : "Editar serviço",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Cores.Escuro,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        });

        corpo.Controls.Add(Rotulo("Nome do serviço"));
        corpo.Controls.Add(txtNome);

        corpo.Controls.Add(Rotulo("Categoria"));
        cbCategoria.Items.AddRange(Categorias);
        corpo.Controls.Add(cbCategoria);

        var linha = new TableLayoutPanel { Width = 360, Height = 58, ColumnCount = 2, RowCount = 2, Margin = new Padding(0), BackColor = Color.White };
        linha.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        linha.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        linha.Controls.Add(Rotulo("Preço (R$)"), 0, 0);
        linha.Controls.Add(Rotulo("Duração (minutos)"), 1, 0);
        linha.Controls.Add(numPreco, 0, 1);
        linha.Controls.Add(numDuracao, 1, 1);
        corpo.Controls.Add(linha);

        corpo.Controls.Add(chkAtivo);
        corpo.Controls.Add(lblErro);

        var botoes = new FlowLayoutPanel { Width = 360, Height = 44, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.White, Margin = new Padding(0, 4, 0, 0) };
        var salvar = new BotaoAnimado { Text = "Salvar", Size = new Size(110, 38), BackColor = Cores.Primaria, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0) };
        var cancelar = new BotaoAnimado { Text = "Cancelar", Size = new Size(110, 38), BackColor = Cores.Fundo, ForeColor = Cores.Texto, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 10, 0), DialogResult = DialogResult.Cancel };
        salvar.Click += (_, _) => Salvar();
        botoes.Controls.Add(salvar);
        botoes.Controls.Add(cancelar);
        corpo.Controls.Add(botoes);

        card.Controls.Add(corpo);
        Controls.Add(card);
        AcceptButton = salvar;
        CancelButton = cancelar;

        // Preenche com os dados do serviço que está sendo editado
        txtNome.Text = Resultado.Nome;
        cbCategoria.Text = string.IsNullOrWhiteSpace(Resultado.Categoria) ? Categorias[0] : Resultado.Categoria;
        numPreco.Value = Math.Min(numPreco.Maximum, Math.Max(0, Resultado.Preco));
        numDuracao.Value = Math.Min(numDuracao.Maximum, Math.Max(numDuracao.Minimum, Resultado.DuracaoMin));
        chkAtivo.Checked = Resultado.Ativo;
    }

    static Label Rotulo(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        ForeColor = Cores.Cinza,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Margin = new Padding(0, 10, 0, 4)
    };

    void Salvar()
    {
        string nome = txtNome.Text.Trim();
        string categoria = cbCategoria.Text.Trim();

        string? erro =
            nome.Length == 0 ? "Informe o nome do serviço." :
            nomeEmUso(nome) ? "Já existe um serviço com esse nome." :
            categoria.Length == 0 ? "Escolha uma categoria." :
            numPreco.Value <= 0 ? "Informe um preço maior que zero." :
            null;

        if (erro != null)
        {
            lblErro.Text = erro;
            return;
        }

        Resultado.Nome = nome;
        Resultado.Categoria = categoria;
        Resultado.Preco = numPreco.Value;
        Resultado.DuracaoMin = (int)numDuracao.Value;
        Resultado.Ativo = chkAtivo.Checked;
        DialogResult = DialogResult.OK;
        Close();
    }
}
