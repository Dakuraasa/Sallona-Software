namespace dashboardforms.Login;

class FrmCadastro : Formulario
{
    public Usuario? Criado { get; private set; }

    public FrmCadastro(UsuarioRepositorio contas) : base("Cadastrar estabelecimento", "Criar conta")
    {
        var txtSalao = AdicionarCampo("Nome do estabelecimento", NovaCaixaTexto(80));
        var txtNome = AdicionarCampo("Responsável", NovaCaixaTexto(80));
        var txtEmail = AdicionarCampo("E-mail do estabelecimento (usado para entrar)", NovaCaixaTexto(120));
        var txtSenha = AdicionarCampo("Senha (mínimo 6 caracteres)", NovaCaixaTexto(64));
        var txtConfirma = AdicionarCampo("Confirmar senha", NovaCaixaTexto(64));
        txtSenha.UseSystemPasswordChar = txtConfirma.UseSystemPasswordChar = true;

        Validar = () =>
        {
            string email = txtEmail.Text.Trim();
            if (txtSalao.Text.Trim().Length == 0) return "Informe o nome do estabelecimento.";
            if (txtNome.Text.Trim().Length == 0) return "Informe o nome do responsável.";
            if (!UsuarioRepositorio.EmailValido(email)) return "Digite um e-mail válido.";
            if (contas.EmailEmUso(email)) return "Já existe uma conta com esse e-mail.";
            if (txtSenha.Text.Length < 6) return "A senha precisa ter pelo menos 6 caracteres.";
            if (txtSenha.Text != txtConfirma.Text) return "As senhas não são iguais.";
            return null;
        };
        AoSalvar = () => Criado = contas.Cadastrar(txtNome.Text, txtSalao.Text, txtEmail.Text, txtSenha.Text);
    }
}
