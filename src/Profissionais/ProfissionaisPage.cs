using System.Data;

namespace dashboardforms;

// ===== Página de Profissionais: equipe, produção do mês e comissão a pagar =====
class ProfissionaisPage : UserControl, IPagina
{
    static ProfissionalRepositorio repo => Repositorios.Profissionais;

    readonly DataTable tabela = new();
    readonly DataGridView grid;
    readonly Label[] kpiValor, kpiSub;
    readonly Label lblNome = new(), lblFuncao = new();
    readonly ListaLinhas linhas = new() { Dock = DockStyle.Top, Height = 150 };
    readonly FlowLayoutPanel acoes = new() { Dock = DockStyle.Top, Height = 46, BackColor = Color.White, Padding = new Padding(0, 6, 0, 0), WrapContents = false };
    readonly BotaoAnimado btnAtivar;
    readonly Label lblDica = Ui.Dica("", DockStyle.Fill);
    string busca = "";
    bool mostrarInativos;
    bool atualizando;

    public ProfissionaisPage()
    {
        BackColor = Cores.Fundo;
        DoubleBuffered = true;

        foreach (var (nome, tipo) in new[] { ("Id", typeof(int)), ("Profissional", typeof(string)), ("Função", typeof(string)),
                                               ("Telefone", typeof(string)), ("Comissão", typeof(string)), ("Atend. (mês)", typeof(int)),
                                               ("Faturado (mês)", typeof(decimal)), ("Comissão (mês)", typeof(decimal)), ("Status", typeof(string)) })
            tabela.Columns.Add(nome, tipo);
        grid = Ui.Tabela(tabela);
        Ui.ColorirSituacao(grid, "Status", s => s == "Ativo" ? Cores.Verde : Cores.Cinza);
        grid.SelectionChanged += (_, _) => { if (!atualizando) MostrarDetalhes(); };
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };

        var chkInativos = new CheckBox { Text = "Mostrar inativos", AutoSize = true, ForeColor = Cores.Texto, Margin = new Padding(0, 8, 8, 0) };
        chkInativos.CheckedChanged += (_, _) => { mostrarInativos = chkInativos.Checked; Recarregar(); };
        var novo = Ui.BotaoPrincipal("+  Novo profissional", 170);
        novo.Click += (_, _) => Novo();

        var raiz = Ui.Raiz(64, 124, 0);
        var cab = Ui.Cabecalho("Profissionais", Ui.Busca("Buscar profissional...", t => { busca = t; Recarregar(); }), chkInativos, novo);
        raiz.Controls.Add(cab, 0, 0); raiz.SetColumnSpan(cab, 2);

        var kpis = Ui.Kpis(new[] { "Profissionais ativos", "Atendimentos no mês", "Faturado no mês", "Comissões do mês" }, out kpiValor, out kpiSub);
        raiz.Controls.Add(kpis, 0, 1); raiz.SetColumnSpan(kpis, 2);

        var cTab = Ui.Cartao();
        cTab.Controls.Add(grid);
        cTab.Controls.Add(Ui.Titulo("Equipe"));
        cTab.Controls.Add(Ui.Dica("Comissão = % do profissional sobre os atendimentos PAGOS no mês. Clique duas vezes para editar."));
        raiz.Controls.Add(cTab, 0, 2);

        raiz.Controls.Add(ConstruirDetalhes(out btnAtivar), 1, 2);
        Controls.Add(raiz);
        Recarregar();
    }

    Control ConstruirDetalhes(out BotaoAnimado ativar)
    {
        var c = Ui.Cartao();
        lblNome.Dock = DockStyle.Top; lblNome.Height = 34; lblNome.ForeColor = Cores.Escuro; lblNome.BackColor = Color.Transparent;
        lblNome.Font = new Font("Segoe UI", 14f, FontStyle.Bold); lblNome.AutoEllipsis = true;
        lblFuncao.Dock = DockStyle.Top; lblFuncao.Height = 24; lblFuncao.ForeColor = Cores.Primaria; lblFuncao.BackColor = Color.Transparent;
        lblFuncao.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

        var editar = Ui.Botao("Editar", Cores.Primaria, Color.White, 84); editar.Margin = new Padding(0, 0, 6, 0);
        ativar = Ui.Botao("Desativar", Cores.Fundo, Cores.Vinho, 90); ativar.Margin = new Padding(0, 0, 6, 0);
        var excluir = Ui.Botao("Excluir", Color.White, Cores.Coral, 80); excluir.Margin = new Padding(0);
        editar.Click += (_, _) => Editar();
        ativar.Click += (_, _) => AlternarAtivo();
        excluir.Click += (_, _) => Excluir();
        acoes.Controls.AddRange(new Control[] { editar, ativar, excluir });

        c.Controls.Add(lblDica);
        c.Controls.Add(acoes);
        c.Controls.Add(linhas);
        c.Controls.Add(lblFuncao);
        c.Controls.Add(lblNome);
        c.Controls.Add(Ui.Titulo("Detalhes"));
        return c;
    }

    // ---------- dados ----------
    public void Recarregar()
    {
        int? manter = Ui.IdSelecionado(grid);
        var mes = InicioMes();
        var atendMes = Repositorios.Atendimentos.Todos().Where(a => a.Data >= mes && a.Data < mes.AddMonths(1)).ToList();
        var todos = repo.Todos().OrderBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

        atualizando = true;
        tabela.Rows.Clear();
        foreach (var p in todos.Where(p => (mostrarInativos || p.Ativo) &&
                                           (busca.Length == 0 || p.Nome.Contains(busca, StringComparison.CurrentCultureIgnoreCase) ||
                                            p.Funcao.Contains(busca, StringComparison.CurrentCultureIgnoreCase))))
        {
            var (qtd, fat, com) = Producao(p, atendMes);
            tabela.Rows.Add(p.Id, p.Nome, p.Funcao, p.Telefone, $"{p.ComissaoPct:0.#}%", qtd, fat, com, p.Ativo ? "Ativo" : "Inativo");
        }
        Ui.Selecionar(grid, manter);
        atualizando = false;

        // KPIs (sempre de toda a equipe, não só do filtro)
        var ativos = todos.Where(p => p.Ativo).ToList();
        var producoes = todos.Select(p => Producao(p, atendMes)).ToList();
        kpiValor[0].Text = ativos.Count.ToString(); kpiSub[0].Text = $"de {todos.Count} cadastrados";
        kpiValor[1].Text = producoes.Sum(x => x.qtd).ToString(); kpiSub[1].Text = "feitos pela equipe";
        kpiValor[2].Text = Formato.Dinheiro(producoes.Sum(x => x.fat)); kpiSub[2].Text = "atendimentos pagos";
        kpiValor[3].Text = Formato.Dinheiro(producoes.Sum(x => x.com)); kpiSub[3].Text = "a pagar para a equipe";
        MostrarDetalhes();
    }

    static DateTime InicioMes() => new(DateTime.Today.Year, DateTime.Today.Month, 1);

    // Produção no mês: atendimentos, faturado (pagos) e comissão
    static (int qtd, decimal fat, decimal com) Producao(Profissional p, List<Atendimento> atendMes)
    {
        var meus = atendMes.Where(a => a.ProfissionalId == p.Id ||
                                       (a.ProfissionalId == 0 && string.Equals(a.Profissional, p.Nome, StringComparison.CurrentCultureIgnoreCase))).ToList();
        decimal fat = meus.Where(a => a.Pago).Sum(a => a.Total);
        return (meus.Count, fat, Math.Round(fat * p.ComissaoPct / 100, 2));
    }

    Profissional? Selecionado() => Ui.IdSelecionado(grid) is int id ? repo.PorId(id) : null;

    void MostrarDetalhes()
    {
        var p = Selecionado();
        linhas.Itens.Clear();
        acoes.Visible = p != null;
        if (p == null)
        {
            lblNome.Text = repo.Todos().Count == 0 ? "Nenhum profissional ainda" : "Nenhum selecionado";
            lblFuncao.Text = "";
            lblDica.Text = "Cadastre a equipe em \"+ Novo profissional\". Os profissionais aparecem na hora de registrar um atendimento.";
        }
        else
        {
            var mes = InicioMes();
            var (qtd, fat, com) = Producao(p, Repositorios.Atendimentos.Todos().Where(a => a.Data >= mes && a.Data < mes.AddMonths(1)).ToList());
            lblNome.Text = p.Nome;
            lblFuncao.Text = (p.Funcao.Length > 0 ? p.Funcao : "Profissional").ToUpper();
            linhas.Itens.Add(("Telefone", p.Telefone.Length > 0 ? p.Telefone : "—", Cores.Escuro));
            linhas.Itens.Add(("Comissão", $"{p.ComissaoPct:0.#}%", Cores.Escuro));
            linhas.Itens.Add(("Atendimentos no mês", qtd.ToString(), Cores.Escuro));
            linhas.Itens.Add(("Faturado no mês", Formato.Dinheiro(fat), Cores.Escuro));
            linhas.Itens.Add(("Comissão do mês", Formato.Dinheiro(com), Cores.Verde));
            linhas.Itens.Add(("Situação", p.Ativo ? "Ativo" : "Inativo", p.Ativo ? Cores.Verde : Cores.Cinza));
            btnAtivar.Text = p.Ativo ? "Desativar" : "Reativar";
            btnAtivar.ForeColor = p.Ativo ? Cores.Vinho : Cores.Verde;
            lblDica.Text = p.Ativo
                ? "Profissionais desativados não aparecem no \"Novo atendimento\", mas continuam no histórico."
                : "Desativado: não aparece no \"Novo atendimento\".";
        }
        linhas.Invalidate();
    }

    // ---------- ações ----------
    void Novo()
    {
        using var dlg = new ProfissionalDialog(null);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (Armazenamento.Tentar(FindForm(), () => repo.Adicionar(dlg.Resultado))) { Recarregar(); Ui.Selecionar(grid, dlg.Resultado.Id); }
    }

    void Editar()
    {
        var p = Selecionado();
        if (p == null) return;
        using var dlg = new ProfissionalDialog(p);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (Armazenamento.Tentar(FindForm(), () => repo.Atualizar(dlg.Resultado))) Recarregar();
    }

    void AlternarAtivo()
    {
        var p = Selecionado();
        if (p == null) return;
        if (Armazenamento.Tentar(FindForm(), () => repo.DefinirAtivo(p.Id, !p.Ativo))) Recarregar();
    }

    void Excluir()
    {
        var p = Selecionado();
        if (p == null || !Ui.Confirmar(FindForm(), $"Excluir \"{p.Nome}\"?\nEssa ação não pode ser desfeita.", "Excluir profissional")) return;
        if (Armazenamento.Tentar(FindForm(), () => repo.Excluir(p.Id))) Recarregar();
    }
}

// ===== Janela para cadastrar ou editar um profissional =====
class ProfissionalDialog : Formulario
{
    static readonly string[] Funcoes = { "Cabeleireiro(a)", "Manicure / Pedicure", "Esteticista", "Maquiador(a)", "Barbeiro", "Designer de sobrancelha", "Recepcionista", "Auxiliar" };

    public Profissional Resultado { get; }

    public ProfissionalDialog(Profissional? existente) : base(existente == null ? "Novo profissional" : "Editar profissional")
    {
        Resultado = existente ?? new Profissional { ComissaoPct = Repositorios.Configuracao.ComissaoPadrao };

        var txtNome = AdicionarCampo("Nome", NovaCaixaTexto(80));
        var cbFuncao = AdicionarCampo("Função", new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, MaxLength = 60 });
        cbFuncao.Items.AddRange(Funcoes);
        var txtTel = AdicionarCampo("Telefone (opcional)", NovaCaixaTexto(20));
        txtTel.PlaceholderText = "(27) 99999-9999";
        var numCom = AdicionarCampo("Comissão sobre os atendimentos (%)", new NumericUpDown { DecimalPlaces = 1, Maximum = 100, Increment = 5 });
        var chkAtivo = new CheckBox { Text = "Ativo (aparece no \"Novo atendimento\")", AutoSize = true, ForeColor = Cores.Texto, Margin = new Padding(0, 14, 0, 0) };
        AdicionarControle(chkAtivo);

        txtNome.Text = Resultado.Nome;
        cbFuncao.Text = Resultado.Funcao;
        txtTel.Text = Resultado.Telefone;
        numCom.Value = Math.Clamp(Resultado.ComissaoPct, 0, 100);
        chkAtivo.Checked = Resultado.Ativo;

        Validar = () =>
        {
            string nome = txtNome.Text.Trim();
            int digitos = txtTel.Text.Count(char.IsDigit);
            if (nome.Length == 0) return "Informe o nome.";
            if (Repositorios.Profissionais.NomeEmUso(nome, Resultado.Id)) return "Já existe um profissional com esse nome.";
            if (digitos > 0 && digitos is < 10 or > 11) return "Telefone com DDD (10 ou 11 números), ou deixe em branco.";
            return null;
        };
        AoSalvar = () =>
        {
            Resultado.Nome = txtNome.Text.Trim();
            Resultado.Funcao = cbFuncao.Text.Trim();
            Resultado.Telefone = txtTel.Text.Trim();
            Resultado.ComissaoPct = Math.Round(numCom.Value, 1);
            Resultado.Ativo = chkAtivo.Checked;
        };
    }
}
