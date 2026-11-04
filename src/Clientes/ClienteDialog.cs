namespace dashboardforms;

// ===== Janela para cadastrar ou editar um cliente =====
class ClienteDialog : Formulario
{
    public Cliente Resultado { get; }

    public ClienteDialog(Cliente? existente) : base(existente == null ? "Novo cliente" : "Editar cliente")
    {
        Resultado = existente?.Copia() ?? new Cliente();

        var txtNome = AdicionarCampo("Nome completo", NovaCaixaTexto(80));
        var txtTelefone = AdicionarCampo("Telefone", NovaCaixaTexto(20));
        var txtEmail = AdicionarCampo("E-mail (opcional)", NovaCaixaTexto(120));
        txtTelefone.PlaceholderText = "(27) 99999-9999";

        txtNome.Text = Resultado.Nome;
        txtTelefone.Text = Resultado.Telefone;
        txtEmail.Text = Resultado.Email ?? "";

        Validar = () =>
        {
            string nome = txtNome.Text.Trim();
            string email = txtEmail.Text.Trim();
            int digitos = txtTelefone.Text.Count(char.IsDigit);
            if (nome.Length == 0) return "Informe o nome do cliente.";
            if (Repositorios.Clientes.NomeEmUso(nome, Resultado.Id)) return "Já existe um cliente com esse nome.";
            if (digitos is < 10 or > 11) return "Informe o telefone com DDD (10 ou 11 números).";
            if (email.Length > 0 && !UsuarioRepositorio.EmailValido(email)) return "E-mail inválido (ou deixe em branco).";
            return null;
        };
        AoSalvar = () =>
        {
            Resultado.Nome = txtNome.Text.Trim();
            Resultado.Telefone = txtTelefone.Text.Trim();
            Resultado.Email = txtEmail.Text.Trim() is { Length: > 0 } e ? e : null;
        };
    }
}
