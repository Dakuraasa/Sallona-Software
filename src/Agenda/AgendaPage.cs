namespace dashboardforms.Agenda;

class AgendaPage : UserControl, IPagina
{
    enum Modo { Dia, Semana, Mes, Ano }

    Modo modo = Modo.Dia;
    DateTime dataRef = DateTime.Today;
    int? filtroProfissional;
    string? filtroStatus;
    bool carregando;

    readonly MiniCalendario calendario = new() { Dock = DockStyle.Fill };
    readonly ComboBox cbProfissional = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Font = new Font("Segoe UI", 10f) };
    readonly ComboBox cbStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Font = new Font("Segoe UI", 10f) };
    readonly FlowLayoutPanel listaSolicitacoes = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.White };
    readonly Label tituloSolicitacoes = Ui.Titulo("Solicitações");
    readonly Label lblTitulo = new(), lblResumo = new();
    readonly Dictionary<Modo, Button> botoesModo = new();
    readonly Card areaVisao = new() { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(10) };
    readonly GradeAgenda grade = new() { Dock = DockStyle.Fill };
    readonly VisaoMes visaoMes = new() { Dock = DockStyle.Fill };
    readonly VisaoAno visaoAno = new() { Dock = DockStyle.Fill };

    public event Action<Atendimento>? PagamentoSolicitado;
    public event Action<string>? NavegarPara;

    public AgendaPage()
    {
        BackColor = Cores.Fundo;
        DoubleBuffered = true;

        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(20, 16, 20, 16), BackColor = Cores.Fundo };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        raiz.Controls.Add(MontarColunaEsquerda(), 0, 0);
        raiz.Controls.Add(MontarAreaPrincipal(), 1, 0);
        Controls.Add(raiz);

        calendario.DataSelecionadaAlterada += (_, _) =>
        {
            if (carregando || calendario.DataSelecionada is not DateTime d) return;
            dataRef = d;
            if (modo is Modo.Mes or Modo.Ano) modo = Modo.Dia;
            Renderizar();
        };
        grade.AgendamentoClicado += MostrarAcoes;
        grade.AgendamentoDuploClique += Editar;
        grade.HorarioClicado += (dt, prof) => NovoAgendamento(dt, prof);
        visaoMes.DiaClicado += d => { dataRef = d; modo = Modo.Dia; Renderizar(); };
        visaoAno.DiaClicado += d => { dataRef = d; modo = Modo.Dia; Renderizar(); };
        visaoAno.MesClicado += m => { dataRef = m; modo = Modo.Mes; Renderizar(); };

        Recarregar();
        HandleCreated += (_, _) => BeginInvoke(new Action(grade.RolarParaAgora));
    }

    Control MontarColunaEsquerda()
    {
        var col = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0, 0, 14, 0), BackColor = Cores.Fundo };
        col.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));
        col.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        col.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cCal = new CardBorda { Dock = DockStyle.Fill, CorBorda = Cores.Primaria, EspessuraBorda = 2, Padding = new Padding(14, 10, 14, 6), Margin = new Padding(0, 0, 0, 12) };
        cCal.Controls.Add(calendario);
        col.Controls.Add(cCal, 0, 0);

        var cFiltros = new Card { Dock = DockStyle.Fill, Padding = new Padding(16, 10, 16, 10), Margin = new Padding(0, 0, 0, 12) };
        cbStatus.Items.Add("Todos os status");
        cbStatus.Items.AddRange(StatusAgendamento.Todos);
        cbStatus.SelectedIndex = 0;
        cbStatus.SelectedIndexChanged += (_, _) =>
        {
            if (carregando) return;
            filtroStatus = cbStatus.SelectedIndex <= 0 ? null : (string)cbStatus.SelectedItem!;
            Renderizar();
        };
        cbProfissional.SelectedIndexChanged += (_, _) =>
        {
            if (carregando) return;
            filtroProfissional = cbProfissional.SelectedItem is Profissional p ? p.Id : null;
            Renderizar();
        };
        cFiltros.Controls.Add(cbStatus);
        cFiltros.Controls.Add(Ui.Dica("Status", DockStyle.Top));
        cFiltros.Controls.Add(cbProfissional);
        cFiltros.Controls.Add(Ui.Dica("Profissional", DockStyle.Top));
        cFiltros.Controls.Add(Ui.Titulo("Filtros"));
        col.Controls.Add(cFiltros, 0, 1);

        var cSol = new Card { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 8, 10), Margin = new Padding(0) };
        cSol.Controls.Add(listaSolicitacoes);
        cSol.Controls.Add(Ui.Dica("Pedidos de horário ainda não confirmados.", DockStyle.Top));
        cSol.Controls.Add(tituloSolicitacoes);
        col.Controls.Add(cSol, 0, 2);
        return col;
    }

    Control MontarAreaPrincipal()
    {
        var area = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0), BackColor = Cores.Fundo };
        area.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        area.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topo = new Panel { Dock = DockStyle.Fill, BackColor = Cores.Fundo };
        lblTitulo.AutoSize = true; lblTitulo.Location = new Point(0, 0);
        lblTitulo.Font = new Font("Segoe UI", 19f, FontStyle.Bold); lblTitulo.ForeColor = Cores.Escuro;
        lblResumo.AutoSize = true; lblResumo.Location = new Point(2, 40); lblResumo.ForeColor = Cores.Cinza;
        var novo = Ui.BotaoPrincipal("+  Novo agendamento", 180);
        novo.Height = 38;
        novo.Click += (_, _) => NovoAgendamento(null, filtroProfissional);
        topo.Controls.Add(novo);
        topo.Controls.Add(lblTitulo);
        topo.Controls.Add(lblResumo);
        topo.Resize += (_, _) => novo.Location = new Point(topo.Width - novo.Width, 4);
        area.Controls.Add(topo, 0, 0);

        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = Cores.Fundo, Padding = new Padding(0, 4, 0, 0) };
        barra.Controls.Add(BotaoNav("‹", -1));
        var hoje = Ui.Botao("Hoje", Color.White, Cores.Texto, 70);
        hoje.Margin = new Padding(0, 0, 6, 0);
        hoje.Click += (_, _) => { dataRef = DateTime.Today; Renderizar(); grade.RolarParaAgora(); };
        barra.Controls.Add(hoje);
        barra.Controls.Add(BotaoNav("›", +1));
        barra.Controls.Add(new Panel { Width = 18, Height = 1, BackColor = Cores.Fundo });
        foreach (var (m, txt) in new[] { (Modo.Dia, "Dia"), (Modo.Semana, "Semana"), (Modo.Mes, "Mês"), (Modo.Ano, "Ano") })
        {
            var b = new Button { Text = txt, Size = new Size(78, 34), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0), Font = new Font("Segoe UI", 9.5f) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => { modo = m; Renderizar(); };
            botoesModo[m] = b;
            barra.Controls.Add(b);
        }
        area.Controls.Add(barra, 0, 1);
        area.Controls.Add(areaVisao, 0, 2);
        return area;
    }

    Button BotaoNav(string txt, int direcao)
    {
        var b = Ui.Botao(txt, Color.White, Cores.Escuro, 38);
        b.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
        b.Margin = new Padding(0, 0, 6, 0);
        b.Click += (_, _) =>
        {
            dataRef = modo switch
            {
                Modo.Dia => dataRef.AddDays(direcao),
                Modo.Semana => dataRef.AddDays(7 * direcao),
                Modo.Mes => dataRef.AddMonths(direcao),
                _ => dataRef.AddYears(direcao)
            };
            Renderizar();
        };
        return b;
    }

    public void Recarregar()
    {
        carregando = true;
        var profs = Repositorios.Profissionais.Ativos();
        cbProfissional.Items.Clear();
        cbProfissional.Items.Add("Todos os profissionais");
        foreach (var p in profs) cbProfissional.Items.Add(p);
        cbProfissional.DisplayMember = nameof(Profissional.Nome);
        var sel = profs.FirstOrDefault(p => p.Id == filtroProfissional);
        if (sel != null) cbProfissional.SelectedItem = sel;
        else { cbProfissional.SelectedIndex = 0; filtroProfissional = null; }
        carregando = false;
        Renderizar();
    }

    static DateTime InicioSemana(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    List<Agendamento> Filtrar(IEnumerable<Agendamento> lista) =>
        lista.Where(a => (filtroProfissional == null || a.ProfissionalId == filtroProfissional) &&
                         (filtroStatus == null || a.Status == filtroStatus)).ToList();

    void Renderizar()
    {
        var br = DesenhoAgenda.Br;
        var inicioMes = new DateTime(dataRef.Year, dataRef.Month, 1);
        (DateTime ini, DateTime fim) = modo switch
        {
            Modo.Dia => (dataRef.Date, dataRef.Date.AddDays(1)),
            Modo.Semana => (InicioSemana(dataRef), InicioSemana(dataRef).AddDays(7)),
            Modo.Mes => (VisaoMes.InicioGrade(inicioMes), VisaoMes.InicioGrade(inicioMes).AddDays(42)),
            _ => (new DateTime(dataRef.Year, 1, 1), new DateTime(dataRef.Year + 1, 1, 1))
        };
        var lista = Filtrar(Repositorios.Agendamentos.NoPeriodo(ini, fim));

        Control visao;
        switch (modo)
        {
            case Modo.Dia:
            {
                var profs = Repositorios.Profissionais.Ativos();
                var extras = lista.Where(a => profs.All(p => p.Id != a.ProfissionalId))
                                  .GroupBy(a => a.ProfissionalId)
                                  .Select(g => new Profissional { Id = g.Key, Nome = g.First().ProfissionalNome });
                var colunas = profs.Concat(extras)
                    .Where(p => filtroProfissional == null || p.Id == filtroProfissional)
                    .Select(p => new ColunaGrade(p.Nome, p.Funcao.Length > 0 ? p.Funcao : "Profissional", dataRef.Date, p.Id)).ToList();
                if (colunas.Count == 0) colunas.Add(new ColunaGrade("Agenda", "cadastre profissionais", dataRef.Date, null));
                grade.MostrarProfissional = false;
                grade.Mostrar(colunas, lista);
                lblTitulo.Text = br.TextInfo.ToTitleCase(dataRef.ToString("dddd, d 'de' MMMM 'de' yyyy", br));
                visao = grade;
                break;
            }
            case Modo.Semana:
            {
                var colunas = Enumerable.Range(0, 7).Select(i => ini.AddDays(i))
                    .Select(d => new ColunaGrade(br.TextInfo.ToTitleCase(d.ToString("ddd", br).TrimEnd('.')), d.ToString("dd/MM"), d, null)).ToList();
                grade.MostrarProfissional = true;
                grade.Mostrar(colunas, lista);
                var f = ini.AddDays(6);
                lblTitulo.Text = ini.Month == f.Month
                    ? $"{ini.Day} a {f.Day} de {f.ToString("MMMM 'de' yyyy", br)}"
                    : $"{ini.ToString("d 'de' MMM", br)} a {f.ToString("d 'de' MMM 'de' yyyy", br)}";
                visao = grade;
                break;
            }
            case Modo.Mes:
                visaoMes.Mostrar(dataRef, lista);
                lblTitulo.Text = br.TextInfo.ToTitleCase(dataRef.ToString("MMMM 'de' yyyy", br));
                visao = visaoMes;
                lista = lista.Where(a => a.Inicio.Month == dataRef.Month && a.Inicio.Year == dataRef.Year).ToList();
                break;
            default:
                visaoAno.Mostrar(dataRef.Year, lista);
                lblTitulo.Text = $"Ano de {dataRef.Year}";
                visao = visaoAno;
                break;
        }

        int ativos = lista.Count(a => a.OcupaHorario);
        int pend = lista.Count(a => a.Status == StatusAgendamento.Pendente);
        decimal previsto = lista.Where(a => a.OcupaHorario).Sum(a => a.Total);
        lblResumo.Text = ativos == 0
            ? "Nenhum agendamento neste período. Clique num horário da grade ou em \"+ Novo agendamento\"."
            : $"{ativos} agendamento(s)  ·  {pend} pendente(s)  ·  {Formato.Dinheiro(previsto)} previsto";

        if (areaVisao.Controls.Count == 0 || areaVisao.Controls[0] != visao)
        {
            areaVisao.Controls.Clear();
            areaVisao.Controls.Add(visao);
        }
        foreach (var (m, b) in botoesModo)
        {
            bool at = m == modo;
            b.BackColor = at ? Cores.Primaria : Color.White;
            b.ForeColor = at ? Color.White : Cores.Texto;
        }

        carregando = true;
        if (Expediente.Aberto(dataRef)) calendario.DataSelecionada = dataRef;
        carregando = false;

        MontarSolicitacoes();
    }

    void MontarSolicitacoes()
    {
        var sols = Repositorios.Agendamentos.Solicitacoes();
        tituloSolicitacoes.Text = sols.Count == 0 ? "Solicitações" : $"Solicitações ({sols.Count})";
        listaSolicitacoes.SuspendLayout();
        var antigos = listaSolicitacoes.Controls.Cast<Control>().ToList();
        listaSolicitacoes.Controls.Clear();
        foreach (var c in antigos) c.Dispose();
        if (sols.Count == 0)
            listaSolicitacoes.Controls.Add(new Label
            {
                Text = "Nenhuma solicitação pendente.\n\nPara registrar um pedido de horário (WhatsApp, telefone...), use \"+ Novo agendamento\" e escolha \"Solicitação\".",
                AutoSize = true, MaximumSize = new Size(240, 0), ForeColor = Cores.Cinza, Margin = new Padding(0, 6, 0, 0)
            });
        foreach (var a in sols) listaSolicitacoes.Controls.Add(CartaoSolicitacao(a));
        listaSolicitacoes.ResumeLayout();
    }

    Control CartaoSolicitacao(Agendamento a)
    {
        var c = new Card { Size = new Size(240, 118), Preenchimento = StatusAgendamento.Fundo(a.Status), Raio = 12, Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 0, 0, 8), Cursor = Cursors.Hand };
        var nome = new Label { Text = a.ClienteNome, Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Cores.Escuro, BackColor = Color.Transparent, AutoEllipsis = true };
        var quando = new Label { Text = $"{a.Inicio.ToString("ddd dd/MM", DesenhoAgenda.Br)}  ·  {a.Horario}", Dock = DockStyle.Top, Height = 18, ForeColor = Cores.Coral, Font = Ui.Negrito, BackColor = Color.Transparent };
        var oque = new Label { Text = $"{a.Descricao} · {a.ProfissionalNome}", Dock = DockStyle.Top, Height = 18, ForeColor = Cores.Cinza, BackColor = Color.Transparent, AutoEllipsis = true };
        var botoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.Transparent, WrapContents = false };
        var ok = Ui.Botao("Confirmar", Cores.Verde, Color.White, 100); ok.Height = 30; ok.Margin = new Padding(0, 2, 8, 0);
        var nao = Ui.Botao("Recusar", Color.White, Cores.Vinho, 90); nao.Height = 30; nao.Margin = new Padding(0, 2, 0, 0);
        ok.Click += (_, _) => MudarStatus(a, StatusAgendamento.Confirmado);
        nao.Click += (_, _) =>
        {
            if (Ui.Confirmar(FindForm(), $"Recusar o pedido de {a.ClienteNome} ({a.Inicio:dd/MM HH:mm})?", "Recusar solicitação"))
                MudarStatus(a, StatusAgendamento.Cancelado);
        };
        botoes.Controls.Add(ok);
        botoes.Controls.Add(nao);
        c.Controls.Add(botoes);
        c.Controls.Add(oque);
        c.Controls.Add(quando);
        c.Controls.Add(nome);
        foreach (Control ctl in new Control[] { c, nome, quando, oque })
            ctl.Click += (_, _) => BeginInvoke(new Action(() => { dataRef = a.Inicio.Date; modo = Modo.Dia; Renderizar(); }));
        return c;
    }

    bool PodeAgendar()
    {
        var (falta, oque) = Repositorios.Profissionais.Ativos().Count == 0 ? ("Profissionais", "um profissional ativo")
                          : !Repositorios.Servicos.Todos().Any(s => s.Ativo) ? ("Serviços", "um serviço ativo")
                          : ("", "");
        if (falta.Length == 0) return true;
        if (MessageBox.Show(FindForm(), $"Para usar a agenda, cadastre pelo menos {oque}.\n\nAbrir a página de {falta} agora?",
                "Agenda", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            NavegarPara?.Invoke(falta);
        return false;
    }

    void NovoAgendamento(DateTime? quando, int? profissional)
    {
        if (!PodeAgendar()) return;
        var sugestao = quando ?? (dataRef.Date >= DateTime.Today ? dataRef.Date : DateTime.Today);
        using var dlg = new AgendamentoDialog(null, sugestao, profissional);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (Repositorios.Agendamentos.PorId(dlg.Resultado.Id) is { } salvo) dataRef = salvo.Inicio.Date;
        if (modo is Modo.Mes or Modo.Ano) modo = Modo.Dia;
        Renderizar();
    }

    void Editar(Agendamento a)
    {
        if (a.Status is StatusAgendamento.Concluido or StatusAgendamento.Cancelado) { MostrarAcoes(a); return; }
        using var dlg = new AgendamentoDialog(Repositorios.Agendamentos.PorId(a.Id) ?? a);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK) Renderizar();
    }

    void MudarStatus(Agendamento a, string status)
    {
        if (Armazenamento.Tentar(FindForm(), () => Repositorios.Agendamentos.MudarStatus(a.Id, status)))
            BeginInvoke(new Action(Renderizar));
    }

    void MostrarAcoes(Agendamento a)
    {
        var menu = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5f), ShowImageMargin = false };
        var cab = menu.Items.Add($"{a.Horario}  ·  {a.ClienteNome}");
        cab.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold); cab.Enabled = false;
        var info = menu.Items.Add($"{a.Descricao}  ·  {a.ProfissionalNome}  ·  {Formato.Dinheiro(a.Total)}"); info.Enabled = false;
        var st = menu.Items.Add($"Status: {a.Status}"); st.Enabled = false;
        if (a.Observacoes.Length > 0) { var o = menu.Items.Add($"Obs.: {a.Observacoes}"); o.Enabled = false; }
        menu.Items.Add(new ToolStripSeparator());

        bool aberto = a.Status is StatusAgendamento.Pendente or StatusAgendamento.Confirmado;
        if (a.Status == StatusAgendamento.Pendente) menu.Items.Add("✓  Confirmar", null, (_, _) => MudarStatus(a, StatusAgendamento.Confirmado));
        if (aberto) menu.Items.Add("✔  Concluir e gerar atendimento", null, (_, _) => BeginInvoke(new Action(() => Concluir(a))));
        if (aberto) menu.Items.Add("✎  Editar...", null, (_, _) => BeginInvoke(new Action(() => Editar(a))));
        if (aberto) menu.Items.Add("✕  Cancelar agendamento", null, (_, _) =>
        {
            if (Ui.Confirmar(FindForm(), $"Cancelar o horário de {a.ClienteNome} ({a.Inicio:dd/MM HH:mm})?", "Cancelar agendamento"))
                MudarStatus(a, StatusAgendamento.Cancelado);
        });
        if (a.Status == StatusAgendamento.Cancelado) menu.Items.Add("↺  Reativar", null, (_, _) => MudarStatus(a, StatusAgendamento.Confirmado));
        if (a.Status != StatusAgendamento.Concluido) menu.Items.Add("Excluir", null, (_, _) =>
        {
            if (Ui.Confirmar(FindForm(), $"Excluir o agendamento de {a.ClienteNome}?\nEssa ação não pode ser desfeita.", "Excluir agendamento") &&
                Armazenamento.Tentar(FindForm(), () => Repositorios.Agendamentos.Excluir(a.Id)))
                BeginInvoke(new Action(Renderizar));
        });
        if (a.Status == StatusAgendamento.Concluido)
        {
            var c = menu.Items.Add("Já virou atendimento (veja no Dashboard)"); c.Enabled = false;
        }
        menu.Closed += (_, _) => BeginInvoke(new Action(menu.Dispose));
        menu.Show(Cursor.Position);
    }

    void Concluir(Agendamento a)
    {
        Atendimento? at = null;
        if (!Armazenamento.Tentar(FindForm(), () => at = Repositorios.Agendamentos.Concluir(a.Id)) || at == null) return;
        Renderizar();
        if (MessageBox.Show(FindForm(), "Atendimento gerado como pendente de pagamento.\n\nRegistrar o pagamento agora?",
                "Concluir agendamento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            PagamentoSolicitado?.Invoke(at);
    }
}
