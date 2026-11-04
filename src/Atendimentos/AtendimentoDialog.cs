namespace dashboardforms;

// ===== Registrar um atendimento: cliente + profissional + serviços realizados =====
// (enquanto a Agenda não estiver pronta, é por aqui que os atendimentos entram no sistema)
class AtendimentoDialog : Formulario
{
    public Atendimento Resultado { get; } = new();

    public AtendimentoDialog(List<Cliente> clientes, List<Servico> servicosAtivos, IEnumerable<string> profissionaisAnteriores)
        : base("Novo atendimento")
    {
        var cbCliente = AdicionarCampo("Cliente", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        cbCliente.DisplayMember = nameof(Cliente.Nome);
        cbCliente.DataSource = clientes.OrderBy(c => c.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

        var txtProf = AdicionarCampo("Profissional", NovaCaixaTexto(60));
        txtProf.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        txtProf.AutoCompleteSource = AutoCompleteSource.CustomSource;
        txtProf.AutoCompleteCustomSource.AddRange(profissionaisAnteriores.ToArray());

        var dtData = AdicionarCampo("Data e hora", new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy  HH:mm",
            Value = DateTime.Now
        });

        var lista = AdicionarCampo("Serviços realizados", new CheckedListBox { Height = 150, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle });
        foreach (var s in servicosAtivos)
            lista.Items.Add(new ItemLista(s));

        var lblTotal = new Label
        {
            AutoSize = false, Width = LarguraCampo, Height = 28, TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Cores.Escuro, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Margin = new Padding(0, 6, 0, 0),
            Text = "Total: " + Formato.Dinheiro(0)
        };
        AdicionarControle(lblTotal);

        IEnumerable<Servico> Marcados() => lista.CheckedItems.Cast<ItemLista>().Select(i => i.Servico);
        // ItemCheck acontece ANTES da marcação mudar, então recalcula logo depois
        lista.ItemCheck += (_, _) => BeginInvoke(new Action(() => lblTotal.Text = "Total: " + Formato.Dinheiro(Marcados().Sum(s => s.Preco))));

        Validar = () =>
        {
            if (cbCliente.SelectedItem is not Cliente) return "Escolha o cliente.";
            if (txtProf.Text.Trim().Length == 0) return "Informe quem fez o atendimento.";
            if (!Marcados().Any()) return "Marque pelo menos um serviço.";
            if (dtData.Value > DateTime.Now.AddMinutes(5)) return "A data não pode estar no futuro (para agendar, use a Agenda).";
            return null;
        };
        AoSalvar = () =>
        {
            var c = (Cliente)cbCliente.SelectedItem!;
            Resultado.ClienteId = c.Id;
            Resultado.ClienteNome = c.Nome;
            Resultado.Profissional = txtProf.Text.Trim();
            Resultado.Data = dtData.Value;
            Resultado.Itens = Marcados().Select(s => new ItemAtendimento(s.Nome, s.Preco)).ToList();
        };
    }

    // item da lista: mostra "Nome — R$ 0,00 (duração)"
    sealed record ItemLista(Servico Servico)
    {
        public override string ToString() =>
            $"{Servico.Nome}  —  {Formato.Dinheiro(Servico.Preco)}  ({Formato.Duracao(Servico.DuracaoMin)})";
    }
}

// ===== Definir a meta de faturamento do mês =====
class MetaDialog : Formulario
{
    public decimal Valor { get; private set; }

    public MetaDialog(decimal atual) : base("Meta do mês")
    {
        var num = AdicionarCampo("Quanto você quer faturar por mês? (R$)", new NumericUpDown
        {
            DecimalPlaces = 2, Maximum = 10_000_000, Increment = 500, ThousandsSeparator = true,
            Value = Math.Min(10_000_000, Math.Max(0, atual))
        });
        AdicionarControle(new Label
        {
            Text = "Use 0 para não ter meta.", AutoSize = true, ForeColor = Cores.Cinza, Margin = new Padding(0, 6, 0, 0)
        });
        AoSalvar = () => Valor = num.Value;
    }
}
