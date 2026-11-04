using System.Security.Cryptography;

namespace dashboardforms;

// ===== Conta de quem usa o sistema (a proprietária do salão) =====
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string NomeSalao { get; set; } = "";
    public string Email { get; set; } = "";
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

    public bool EmailEmUso(string email) => PorEmail(email) != null;

    Usuario? PorEmail(string email) =>
        usuarios.FirstOrDefault(u => string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

    // Devolve a conta se e-mail e senha conferem; senão, null.
    public Usuario? Autenticar(string email, string senha)
    {
        var u = PorEmail(email);
        if (u == null) return null;
        byte[] sal = Convert.FromBase64String(u.SenhaSal);
        byte[] esperado = Convert.FromBase64String(u.SenhaHash);
        byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado) ? u : null;
    }

    public Usuario Cadastrar(string nome, string nomeSalao, string email, string senha)
    {
        if (EmailEmUso(email))
            throw new InvalidOperationException("Já existe uma conta com esse e-mail.");

        byte[] sal = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, 32);
        var u = new Usuario
        {
            Id = usuarios.Count == 0 ? 1 : usuarios.Max(x => x.Id) + 1,
            Nome = nome.Trim(),
            NomeSalao = nomeSalao.Trim(),
            Email = email.Trim(),
            SenhaSal = Convert.ToBase64String(sal),
            SenhaHash = Convert.ToBase64String(hash)
        };
        usuarios.Add(u);
        try { Armazenamento.Salvar(Arquivo, usuarios); }
        catch { usuarios.Remove(u); throw; }
        return u;
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
