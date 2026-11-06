namespace dashboardforms;

class AtendimentoDialog : Formulario
{
    public Atendimento Resultado { get; } = new();

    public AtendimentoDialog(List<Cliente> clientes, List<Servico> servicosAtivos, List<Profissional> profissionaisAtivos)
        : base("Novo atendimento")
    {
        var cbCliente = AdicionarCampo("Cliente", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        cbCliente.DisplayMember = nameof(Cliente.Nome);
        cbCliente.DataSource = clientes.OrderBy(c => c.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

        var cbProf = AdicionarCampo("Profissional", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        cbProf.DisplayMember = nameof(Profissional.Nome);
        cbProf.DataSource = profissionaisAtivos;

        var dtData = AdicionarCampo("Data e hora", new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy  HH:mm",
            Value = DateTime.Now
        });

        var lista = AdicionarCampo("Serviços realizados", new CheckedListBox { Height = 110, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle });
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

        lista.ItemCheck += (_, _) => BeginInvoke(new Action(() => lblTotal.Text = "Total: " + Formato.Dinheiro(Marcados().Sum(s => s.Preco))));

        Validar = () =>
        {
            if (cbCliente.SelectedItem is not Cliente) return "Escolha o cliente.";
            if (cbProf.SelectedItem is not Profissional) return "Escolha quem fez o atendimento.";
            if (!Marcados().Any()) return "Marque pelo menos um serviço.";
            if (dtData.Value > DateTime.Now.AddMinutes(5)) return "A data não pode estar no futuro (para agendar, use a Agenda).";
            return null;
        };
        AoSalvar = () =>
        {
            var c = (Cliente)cbCliente.SelectedItem!;
            Resultado.ClienteId = c.Id;
            Resultado.ClienteNome = c.Nome;
            var p = (Profissional)cbProf.SelectedItem!;
            Resultado.ProfissionalId = p.Id;
            Resultado.Profissional = p.Nome;
            Resultado.Data = dtData.Value;
            Resultado.Itens = Marcados().Select(s => new ItemAtendimento(s.Nome, s.Preco)).ToList();
        };
    }

    sealed record ItemLista(Servico Servico)
    {
        public override string ToString() =>
            $"{Servico.Nome}  —  {Formato.Dinheiro(Servico.Preco)}  ({Formato.Duracao(Servico.DuracaoMin)})";
    }
}

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
        AoSalvar = () => Valor = Math.Round(num.Value, 2);
    }
}
