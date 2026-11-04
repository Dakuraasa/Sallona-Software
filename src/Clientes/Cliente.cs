namespace dashboardforms;

// ===== Modelo: um cliente do salão =====
// É um record: dois clientes com os mesmos dados são considerados iguais,
// e ninguém altera um cliente "sem querer" (para mudar, cria-se uma cópia com `with`).
public record Cliente(string Nome, string Telefone, string? Email, DateTime? UltimaVisita);

// ===== Repositório de clientes =====
// Segue o mesmo padrão do ServicoRepositorio: a tela só conversa com estes métodos.
// TODO: quando o banco MySQL estiver pronto, trocar a lista abaixo por consultas SQL,
// mantendo os mesmos métodos públicos — assim a tela não precisa mudar.
class ClienteRepositorio
{
    readonly List<Cliente> clientes = Exemplos();

    public List<Cliente> Todos() => clientes.ToList();

    public Cliente? PorNome(string nome) =>
        clientes.FirstOrDefault(c => string.Equals(c.Nome, nome, StringComparison.CurrentCultureIgnoreCase));

    // Dados de exemplo — os mesmos nomes que aparecem no Dashboard e no Pagamento.
    static List<Cliente> Exemplos() => new()
    {
        new("Camila Souza",   "(27) 99999-0001", "camila.souza@email.com", new DateTime(2026, 11, 3)),
        new("Fernanda Lima",  "(27) 99999-0002", null,                     new DateTime(2026, 11, 3)),
        new("Marcos Pereira", "(27) 99999-0003", null,                     new DateTime(2026, 11, 2)),
        new("Aline Costa",    "(27) 99999-0004", "aline.costa@email.com",  new DateTime(2026, 11, 2)),
        new("Renata Alves",   "(27) 99999-0005", null,                     new DateTime(2026, 10, 31)),
        new("Beatriz Nunes",  "(27) 99999-0006", "bia.nunes@email.com",    new DateTime(2026, 10, 30)),
        new("Lucas Martins",  "(27) 99999-0007", null,                     new DateTime(2026, 10, 29)),
        new("Júlia Reis",     "(27) 99999-0008", "julia.reis@email.com",   new DateTime(2026, 10, 21)),
        new("Maria Souza",    "(27) 99999-0009", null,                     new DateTime(2026, 10, 14)),
        new("Paulo André",    "(27) 99999-0010", null,                     new DateTime(2026, 9, 15)),
        new("Cleusa Maria",   "(27) 99999-0011", null,                     new DateTime(2026, 8, 22)),
        new("Pedro Henrique", "(27) 99999-0012", null,                     null),
    };
}
