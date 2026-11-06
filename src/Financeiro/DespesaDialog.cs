namespace dashboardforms;

// ===== Janela para cadastrar ou editar uma despesa =====
class DespesaDialog : Formulario
{
    public Despesa Resultado { get; }

    public DespesaDialog(Despesa? existente) : base(existente == null ? "Nova despesa" : "Editar despesa")
    {
        Resultado = existente ?? new Despesa();

        var txtDesc = AdicionarCampo("Descrição", NovaCaixaTexto(80));
        var cbCat = AdicionarCampo("Categoria", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        cbCat.Items.AddRange(DespesaRepositorio.Categorias);
        var dtData = AdicionarCampo("Data", new DateTimePicker { Format = DateTimePickerFormat.Short });
        var numValor = AdicionarCampo("Valor (R$)", new NumericUpDown { DecimalPlaces = 2, Maximum = 1_000_000, ThousandsSeparator = true });

        txtDesc.Text = Resultado.Descricao;
        cbCat.SelectedItem = Resultado.Categoria;
        dtData.Value = Resultado.Data;
        numValor.Value = Resultado.Valor;

        Validar = () =>
            txtDesc.Text.Trim().Length == 0 ? "Informe a descrição." :
            Math.Round(numValor.Value, 2) <= 0 ? "Informe um valor maior que zero." :
            null;

        AoSalvar = () =>
        {
            Resultado.Descricao = txtDesc.Text.Trim();
            Resultado.Categoria = (string)cbCat.SelectedItem!;
            Resultado.Data = dtData.Value.Date;
            Resultado.Valor = Math.Round(numValor.Value, 2);
        };
    }
}