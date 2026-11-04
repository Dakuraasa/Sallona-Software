namespace dashboardforms.Login;

// ===== Criar conta (aberta pelo link "Cadastre-se" da tela de login) =====
class FrmCadastro : Formulario
{
    public Usuario? Criado { get; private set; }

    public FrmCadastro(UsuarioRepositorio contas) : base("Criar conta", "Criar conta")
    {
        var txtNome = AdicionarCampo("Seu nome", NovaCaixaTexto(80));
        var txtSalao = AdicionarCampo("Nome do salão", NovaCaixaTexto(80));
        var txtEmail = AdicionarCampo("E-mail (usado para entrar)", NovaCaixaTexto(120));
        var txtSenha = AdicionarCampo("Senha (mínimo 6 caracteres)", NovaCaixaTexto(64));
        var txtConfirma = AdicionarCampo("Confirmar senha", NovaCaixaTexto(64));
        txtSenha.UseSystemPasswordChar = txtConfirma.UseSystemPasswordChar = true;

        Validar = () =>
        {
            string email = txtEmail.Text.Trim();
            if (txtNome.Text.Trim().Length == 0) return "Informe o seu nome.";
            if (!UsuarioRepositorio.EmailValido(email)) return "Digite um e-mail válido.";
            if (contas.EmailEmUso(email)) return "Já existe uma conta com esse e-mail.";
            if (txtSenha.Text.Length < 6) return "A senha precisa ter pelo menos 6 caracteres.";
            if (txtSenha.Text != txtConfirma.Text) return "As senhas não são iguais.";
            return null;
        };
        AoSalvar = () => Criado = contas.Cadastrar(txtNome.Text, txtSalao.Text, txtEmail.Text, txtSenha.Text);
    }
}
