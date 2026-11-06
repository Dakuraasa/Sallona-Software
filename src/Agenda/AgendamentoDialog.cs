namespace dashboardforms.Agenda;

class AgendamentoDialog : Form
{
    readonly Agendamento resultado;
    readonly bool novo;
    readonly DateTime? inicioOriginal;

    readonly ComboBox cbCliente = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10.5f) };
    readonly ComboBox cbProfissional = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10.5f) };
    readonly DateTimePicker dtData = new() { Format = DateTimePickerFormat.Long, Font = new Font("Segoe UI", 10.5f) };
    readonly TextBox txtObs = new() { Multiline = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f), MaxLength = 300 };
    readonly CheckedListBox lstServicos = new() { CheckOnClick = true, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f), IntegralHeight = false };
    readonly FlowLayoutPanel chips = new() { AutoScroll = true, BackColor = Color.White, Padding = new Padding(0, 2, 0, 0) };
    readonly Label lblResumo1 = new(), lblResumo2 = new();
    readonly Label lblErro = new() { ForeColor = Cores.Vinho, Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };
    readonly BotaoAnimado btnConfirmado, btnSolicitacao;

    DateTime? horarioEscolhido;
    string status;

    public Agendamento Resultado => resultado;

    public AgendamentoDialog(Agendamento? existente, DateTime? sugestao = null, int? profissionalSugerido = null)
    {
        novo = existente == null;
        resultado = existente ?? new Agendamento();
        status = novo ? StatusAgendamento.Confirmado : resultado.Status;
        if (!novo) inicioOriginal = resultado.Inicio;

        Text = novo ? "Novo agendamento" : "Editar agendamento";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Cores.Fundo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        var tela = Screen.FromPoint(Cursor.Position).WorkingArea;
        ClientSize = new Size(Math.Min(900, tela.Width - 40), Math.Min(620, tela.Height - 40));
        Padding = new Padding(18, 14, 18, 14);

        btnConfirmado = Ui.Botao("✓  Confirmado", Color.White, Cores.Texto, 150);
        btnSolicitacao = Ui.Botao("⏳  Solicitação", Color.White, Cores.Texto, 150);
        btnConfirmado.Margin = new Padding(0, 0, 8, 0);
        btnSolicitacao.Margin = new Padding(0);
        btnConfirmado.Click += (_, _) => { status = StatusAgendamento.Confirmado; PintarTipo(); };
        btnSolicitacao.Click += (_, _) => { status = StatusAgendamento.Pendente; PintarTipo(); };

        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = Cores.Fundo };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var cab = new Panel { Dock = DockStyle.Fill, BackColor = Cores.Fundo };
        cab.Controls.Add(new Label
        {
            Text = novo ? "Novo agendamento" : "Editar agendamento", AutoSize = true, Location = new Point(0, 0),
            Font = new Font("Segoe UI", 17f, FontStyle.Bold), ForeColor = Cores.Escuro
        });
        cab.Controls.Add(new Label
        {
            Text = "Escolha o cliente, os serviços e um dos horários livres do profissional.", AutoSize = true,
            Location = new Point(2, 36), ForeColor = Cores.Cinza
        });
        raiz.Controls.Add(cab, 0, 0); raiz.SetColumnSpan(cab, 2);

        raiz.Controls.Add(MontarDados(), 0, 1);
        raiz.Controls.Add(MontarServicos(), 1, 1);
        var rodape = MontarRodape();
        raiz.Controls.Add(rodape, 0, 2); raiz.SetColumnSpan(rodape, 2);
        Controls.Add(raiz);

        CarregarListas(sugestao, profissionalSugerido);
        PintarTipo();

        cbProfissional.SelectedIndexChanged += (_, _) => AtualizarHorarios();
        dtData.ValueChanged += (_, _) => AtualizarHorarios();
        lstServicos.ItemCheck += (_, _) => BeginInvoke(new Action(AtualizarHorarios));
        AtualizarHorarios();
    }

    static Label Rotulo(string t) => new()
    {
        Text = t, AutoSize = true, ForeColor = Cores.Cinza, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Margin = new Padding(0, 10, 0, 4)
    };

    Control MontarDados()
    {
        var card = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(20, 16, 20, 16) };
        var fluxo = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.White, AutoScroll = true };
        card.Controls.Add(fluxo);

        int largura = 320;
        var linhaCliente = new FlowLayoutPanel { Width = largura, Height = 32, WrapContents = false, Margin = new Padding(0), BackColor = Color.White };
        cbCliente.Width = largura - 44;
        cbCliente.Margin = new Padding(0, 2, 6, 0);
        var maisCliente = Ui.Botao("+", Cores.Primaria, Color.White, 36);
        maisCliente.Height = 30; maisCliente.Margin = new Padding(0);
        new ToolTip().SetToolTip(maisCliente, "Cadastrar novo cliente");
        maisCliente.Click += (_, _) => NovoCliente();
        linhaCliente.Controls.Add(cbCliente);
        linhaCliente.Controls.Add(maisCliente);

        cbProfissional.Width = largura;
        dtData.Width = largura;
        txtObs.Size = new Size(largura, 70);
        var tipo = new FlowLayoutPanel { Width = largura, Height = 38, WrapContents = false, Margin = new Padding(0), BackColor = Color.White };
        tipo.Controls.Add(btnConfirmado);
        tipo.Controls.Add(btnSolicitacao);

        fluxo.Controls.Add(new Label { Text = "Dados do agendamento", AutoSize = true, ForeColor = Cores.Escuro, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), Margin = new Padding(0, 0, 0, 2) });
        fluxo.Controls.Add(Rotulo("Cliente"));
        fluxo.Controls.Add(linhaCliente);
        fluxo.Controls.Add(Rotulo("Profissional"));
        fluxo.Controls.Add(cbProfissional);
        fluxo.Controls.Add(Rotulo("Data"));
        fluxo.Controls.Add(dtData);
        fluxo.Controls.Add(Rotulo("Tipo"));
        fluxo.Controls.Add(tipo);
        fluxo.Controls.Add(Rotulo("Observações (opcional)"));
        fluxo.Controls.Add(txtObs);
        return card;
    }

    Control MontarServicos()
    {
        var card = new Card { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(20, 16, 20, 16) };
        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = Color.White };
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        card.Controls.Add(tabela);

        tabela.Controls.Add(new Label { Text = "Serviços", Dock = DockStyle.Fill, ForeColor = Cores.Escuro, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold) }, 0, 0);
        lstServicos.Dock = DockStyle.Fill;
        tabela.Controls.Add(lstServicos, 0, 1);
        tabela.Controls.Add(new Label { Text = "Horários livres", Dock = DockStyle.Fill, ForeColor = Cores.Escuro, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft }, 0, 2);
        chips.Dock = DockStyle.Fill;
        tabela.Controls.Add(chips, 0, 3);

        var resumo = new Card { Dock = DockStyle.Fill, Preenchimento = Cores.Escuro, Padding = new Padding(16, 8, 16, 8), Margin = new Padding(0, 6, 0, 0) };
        lblResumo1.Dock = DockStyle.Top; lblResumo1.Height = 24; lblResumo1.ForeColor = Color.White; lblResumo1.BackColor = Color.Transparent;
        lblResumo1.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        lblResumo2.Dock = DockStyle.Top; lblResumo2.Height = 20; lblResumo2.ForeColor = Cores.Rosa; lblResumo2.BackColor = Color.Transparent;
        resumo.Controls.Add(lblResumo2);
        resumo.Controls.Add(lblResumo1);
        tabela.Controls.Add(resumo, 0, 4);
        return card;
    }

    Control MontarRodape()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Cores.Fundo, Padding = new Padding(0, 12, 0, 0) };
        var salvar = Ui.Botao(novo ? "Agendar" : "Salvar", Cores.Primaria, Color.White, 140);
        var cancelar = Ui.Botao("Cancelar", Color.White, Cores.Texto, 110);
        salvar.Dock = DockStyle.Right; cancelar.Dock = DockStyle.Right;
        salvar.Height = cancelar.Height = 40;
        var espaco = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Cores.Fundo };
        lblErro.Dock = DockStyle.Fill;
        salvar.Click += (_, _) => Salvar();
        cancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        AcceptButton = salvar;
        CancelButton = cancelar;
        p.Controls.Add(lblErro);
        p.Controls.Add(cancelar);
        p.Controls.Add(espaco);
        p.Controls.Add(salvar);
        return p;
    }

    sealed record ItemServico(Servico S)
    {
        public override string ToString() => $"{S.Nome}   ·   {Formato.Dinheiro(S.Preco)}   ·   {Formato.Duracao(S.DuracaoMin)}";
    }

    void CarregarListas(DateTime? sugestao, int? profissionalSugerido)
    {
        var clientes = Repositorios.Clientes.Todos().OrderBy(c => c.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
        cbCliente.DisplayMember = nameof(Cliente.Nome);
        cbCliente.DataSource = clientes;
        var cli = clientes.FirstOrDefault(c => c.Id == resultado.ClienteId);
        if (cli != null) cbCliente.SelectedItem = cli;

        var profs = Repositorios.Profissionais.Ativos();
        if (!novo && profs.All(p => p.Id != resultado.ProfissionalId) && Repositorios.Profissionais.PorId(resultado.ProfissionalId) is { } antigo)
            profs.Add(antigo);
        cbProfissional.DisplayMember = nameof(Profissional.Nome);
        cbProfissional.DataSource = profs;
        int? profAlvo = novo ? profissionalSugerido : resultado.ProfissionalId;
        if (profs.FirstOrDefault(p => p.Id == profAlvo) is { } pr) cbProfissional.SelectedItem = pr;

        var marcados = resultado.Itens.Select(i => i.Servico).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        foreach (var s in Repositorios.Servicos.Todos().Where(s => s.Ativo || marcados.Contains(s.Nome)))
            lstServicos.Items.Add(new ItemServico(s), marcados.Contains(s.Nome));

        var data = novo ? sugestao ?? DateTime.Today : resultado.Inicio;
        dtData.MinDate = novo ? DateTime.Today : new DateTime(Math.Min(DateTime.Today.Ticks, resultado.Inicio.Date.Ticks));
        dtData.Value = data.Date < dtData.MinDate ? dtData.MinDate : data.Date;
        horarioEscolhido = novo ? sugestao is { } s1 && s1.TimeOfDay.TotalMinutes > 0 ? s1 : null : resultado.Inicio;
        txtObs.Text = resultado.Observacoes;
    }

    List<Servico> ServicosMarcados() => lstServicos.CheckedItems.Cast<ItemServico>().Select(i => i.S).ToList();

    void AtualizarHorarios()
    {
        var servs = ServicosMarcados();
        int duracao = servs.Sum(s => s.DuracaoMin);
        decimal total = servs.Sum(s => s.Preco);
        chips.SuspendLayout();
        chips.Controls.Clear();

        if (cbProfissional.SelectedItem is not Profissional prof)
            chips.Controls.Add(Aviso("Cadastre um profissional ativo para ver os horários."));
        else if (duracao == 0)
            chips.Controls.Add(Aviso("Marque pelo menos um serviço para ver os horários livres."));
        else if (!Expediente.Aberto(dtData.Value))
            chips.Controls.Add(Aviso("O salão não abre aos domingos. Escolha outra data."));
        else
        {
            var livres = Expediente.HorariosLivres(dtData.Value, duracao, prof.Id, resultado.Id);
            if (inicioOriginal is { } orig && orig.Date == dtData.Value.Date && prof.Id == resultado.ProfissionalId &&
                !livres.Contains(orig) && Expediente.Conflito(new Agendamento { Id = resultado.Id, ProfissionalId = prof.Id, Inicio = orig, DuracaoMin = duracao }) == null)
                livres.Insert(0, orig);
            livres.Sort();

            if (horarioEscolhido is { } h && !livres.Contains(h)) horarioEscolhido = null;
            if (livres.Count == 0)
                chips.Controls.Add(Aviso("Nenhum horário livre nesse dia para essa duração. Tente outra data ou profissional."));
            else
            {
                string? periodo = null;
                foreach (var t in livres)
                {
                    string p = t.Hour < 12 ? "Manhã" : t.Hour < 18 ? "Tarde" : "Noite";
                    if (p != periodo)
                    {
                        periodo = p;
                        chips.Controls.Add(new Label
                        {
                            Text = p, Width = Math.Max(200, chips.ClientSize.Width - 24), Height = 22, ForeColor = Cores.Cinza,
                            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft, Margin = new Padding(2, 2, 0, 2)
                        });
                    }
                    chips.Controls.Add(Chip(t));
                }
            }
        }
        chips.ResumeLayout();

        lblResumo1.Text = duracao == 0 ? "Nenhum serviço marcado" : $"{Formato.Duracao(duracao)}   ·   {Formato.Dinheiro(total)}";
        lblResumo2.Text = horarioEscolhido is { } esc
            ? $"{esc.ToString("dddd, dd/MM", DesenhoAgenda.Br)}   ·   {esc:HH:mm} → {esc.AddMinutes(duracao):HH:mm}"
            : "Escolha um horário acima";
    }

    static Label Aviso(string t) => new() { Text = t, AutoSize = true, MaximumSize = new Size(380, 0), ForeColor = Cores.Cinza, Margin = new Padding(2, 8, 2, 2) };

    Button Chip(DateTime t)
    {
        bool sel = horarioEscolhido == t;
        var b = new Button
        {
            Text = t.ToString("HH:mm"), Size = new Size(66, 32), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = sel ? Cores.Primaria : Color.White, ForeColor = sel ? Color.White : Cores.Texto,
            Font = new Font("Segoe UI", 9.5f, sel ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(2, 2, 6, 6)
        };
        b.FlatAppearance.BorderColor = sel ? Cores.Primaria : Cores.Borda;
        b.FlatAppearance.MouseOverBackColor = sel ? Cores.Primaria : Cores.RosaClaro;
        b.Click += (_, _) => { horarioEscolhido = t; lblErro.Text = ""; AtualizarHorarios(); };
        return b;
    }

    void PintarTipo()
    {
        bool conf = status != StatusAgendamento.Pendente;
        btnConfirmado.BackColor = conf ? Cores.Escuro : Color.White;
        btnConfirmado.ForeColor = conf ? Color.White : Cores.Texto;
        btnSolicitacao.BackColor = !conf ? Cores.Coral : Color.White;
        btnSolicitacao.ForeColor = !conf ? Color.White : Cores.Texto;
        btnConfirmado.Invalidate(); btnSolicitacao.Invalidate();
        if (!novo && (resultado.Status == StatusAgendamento.Concluido || resultado.Status == StatusAgendamento.Cancelado))
            btnConfirmado.Enabled = btnSolicitacao.Enabled = false;
    }

    void NovoCliente()
    {
        using var dlg = new ClienteDialog(null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!Armazenamento.Tentar(this, () => Repositorios.Clientes.Adicionar(dlg.Resultado))) return;
        var clientes = Repositorios.Clientes.Todos().OrderBy(c => c.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
        cbCliente.DataSource = clientes;
        cbCliente.SelectedItem = clientes.First(c => c.Id == dlg.Resultado.Id);
    }

    void Salvar()
    {
        var servs = ServicosMarcados();
        string? erro =
            cbCliente.SelectedItem is not Cliente ? "Escolha o cliente (ou cadastre no botão +)." :
            cbProfissional.SelectedItem is not Profissional ? "Escolha o profissional." :
            servs.Count == 0 ? "Marque pelo menos um serviço." :
            horarioEscolhido == null ? "Escolha um dos horários livres." :
            null;
        if (erro != null) { lblErro.Text = erro; return; }

        var c = (Cliente)cbCliente.SelectedItem!;
        var p = (Profissional)cbProfissional.SelectedItem!;
        var copia = new Agendamento
        {
            Id = resultado.Id,
            ClienteId = c.Id, ClienteNome = c.Nome,
            ProfissionalId = p.Id, ProfissionalNome = p.Nome,
            Inicio = horarioEscolhido!.Value,
            DuracaoMin = servs.Sum(s => s.DuracaoMin),
            Itens = servs.Select(s => new ItemAtendimento(s.Nome, s.Preco)).ToList(),
            Status = resultado.Status is StatusAgendamento.Concluido or StatusAgendamento.Cancelado ? resultado.Status : status,
            Observacoes = txtObs.Text.Trim(),
            CriadoEm = novo ? DateTime.Now : resultado.CriadoEm,
            AtendimentoId = resultado.AtendimentoId
        };
        try
        {
            Repositorios.Agendamentos.Salvar(copia);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            lblErro.Text = ex.Message;
            return;
        }
        resultado.Id = copia.Id;
        DialogResult = DialogResult.OK;
        Close();
    }
}
