using System.Security.Cryptography;

namespace dashboardforms;

// ===== Conta do ESTABELECIMENTO =====
// O login é por salão (não por pessoa): dono(a) e funcionários entram com a mesma conta.
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";        // responsável pelo estabelecimento
    public string NomeSalao { get; set; } = "";   // nome do estabelecimento
    public string Email { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Documento { get; set; } = "";   // CNPJ ou CPF (opcional)
    public DateTime CriadoEm { get; set; } = DateTime.Now;

    // A senha NUNCA é guardada: guardamos só um "hash" dela (PBKDF2 + sal aleatório).
    public string SenhaHash { get; set; } = "";
    public string SenhaSal { get; set; } = "";
}

// ===== Contas cadastradas neste computador =====
// Ficam em %AppData%\Sallona\usuarios.json. Cada conta tem a sua pasta de dados
// (clientes, serviços, atendimentos), então uma conta não vê os dados da outra.
// TODO: quando o banco MySQL estiver pronto, as contas passam a ficar lá.
class UsuarioRepositorio
{
    const int Iteracoes = 100_000;
    static readonly string Arquivo = Path.Combine(Armazenamento.PastaRaiz, "usuarios.json");

    readonly List<Usuario> usuarios = Armazenamento.Carregar<Usuario>(Arquivo);

    public bool Vazio => usuarios.Count == 0;

    public bool EmailEmUso(string email, int ignorarId = 0) =>
        usuarios.Any(u => u.Id != ignorarId && string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

    public Usuario? PorId(int id) => usuarios.FirstOrDefault(u => u.Id == id);

    Usuario? PorEmail(string email) =>
        usuarios.FirstOrDefault(u => string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

    // Devolve a conta se e-mail e senha conferem; senão, null.
    public Usuario? Autenticar(string email, string senha)
    {
        var u = PorEmail(email);
        return u != null && SenhaConfere(u, senha) ? u : null;
    }

    static bool SenhaConfere(Usuario u, string senha)
    {
        byte[] sal = Convert.FromBase64String(u.SenhaSal);
        byte[] esperado = Convert.FromBase64String(u.SenhaHash);
        byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    static void DefinirSenha(Usuario u, string senha)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(16);
        u.SenhaSal = Convert.ToBase64String(sal);
        u.SenhaHash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, 32));
    }

    // Atualiza os dados do estabelecimento (não mexe na senha)
    public void AtualizarDados(int id, string nomeSalao, string responsavel, string email, string telefone, string endereco, string documento)
    {
        var u = PorId(id) ?? throw new InvalidOperationException("Conta não encontrada.");
        if (EmailEmUso(email, id)) throw new InvalidOperationException("Já existe outra conta com esse e-mail.");
        var antes = (u.NomeSalao, u.Nome, u.Email, u.Telefone, u.Endereco, u.Documento);
        (u.NomeSalao, u.Nome, u.Email, u.Telefone, u.Endereco, u.Documento) =
            (nomeSalao.Trim(), responsavel.Trim(), email.Trim(), telefone.Trim(), endereco.Trim(), documento.Trim());
        try { Salvar(); }
        catch { (u.NomeSalao, u.Nome, u.Email, u.Telefone, u.Endereco, u.Documento) = antes; throw; }
    }

    public void AlterarSenha(int id, string senhaAtual, string novaSenha)
    {
        var u = PorId(id) ?? throw new InvalidOperationException("Conta não encontrada.");
        if (!SenhaConfere(u, senhaAtual)) throw new InvalidOperationException("A senha atual está incorreta.");
        var (sal, hash) = (u.SenhaSal, u.SenhaHash);
        DefinirSenha(u, novaSenha);
        try { Salvar(); }
        catch { (u.SenhaSal, u.SenhaHash) = (sal, hash); throw; }
    }

    // Exclui a conta e TODOS os dados dela (pede a senha para confirmar)
    public void Excluir(int id, string senha)
    {
        var u = PorId(id) ?? throw new InvalidOperationException("Conta não encontrada.");
        if (!SenhaConfere(u, senha)) throw new InvalidOperationException("Senha incorreta.");
        usuarios.Remove(u);
        try { Salvar(); }
        catch { usuarios.Add(u); throw; }
        try { Directory.Delete(Armazenamento.PastaDaConta(id), recursive: true); } catch (DirectoryNotFoundException) { }
    }

    void Salvar() => Armazenamento.Salvar(Arquivo, usuarios);

    public Usuario Cadastrar(string nome, string nomeSalao, string email, string senha)
    {
        if (EmailEmUso(email))
            throw new InvalidOperationException("Já existe uma conta com esse e-mail.");

        var u = new Usuario
        {
            // nunca reaproveita o Id de uma conta excluída (a pasta de dados usa o Id)
            Id = Math.Max(usuarios.Count == 0 ? 0 : usuarios.Max(x => x.Id), MaiorPastaDeConta()) + 1,
            Nome = nome.Trim(),
            NomeSalao = nomeSalao.Trim(),
            Email = email.Trim()
        };
        DefinirSenha(u, senha);
        usuarios.Add(u);
        try { Salvar(); }
        catch { usuarios.Remove(u); throw; }
        return u;
    }

    static int MaiorPastaDeConta()
    {
        var pasta = Path.Combine(Armazenamento.PastaRaiz, "contas");
        if (!Directory.Exists(pasta)) return 0;
        return Directory.GetDirectories(pasta).Select(d => int.TryParse(Path.GetFileName(d), out int n) ? n : 0).DefaultIfEmpty(0).Max();
    }

    // Validação simples de e-mail (tem algo@algo.algo, sem espaços)
    public static bool EmailValido(string email)
    {
        int arroba = email.IndexOf('@');
        if (arroba < 1 || email.Contains(' ')) return false;
        int ponto = email.IndexOf('.', arroba);
        return ponto > arroba + 1 && !email.EndsWith(".");
    }
}
