using System.Text.Json.Serialization;

namespace dashboardforms;

public static class StatusAgendamento
{
    public const string Pendente = "Pendente";
    public const string Confirmado = "Confirmado";
    public const string Concluido = "Concluído";
    public const string Cancelado = "Cancelado";

    public static readonly string[] Todos = { Pendente, Confirmado, Concluido, Cancelado };

    public static Color Cor(string status) => status switch
    {
        Pendente => Cores.Coral,
        Confirmado => Cores.Primaria,
        Concluido => Cores.Verde,
        _ => Cores.Cinza
    };

    public static Color Fundo(string status) => status switch
    {
        Pendente => Color.FromArgb(253, 236, 230),
        Confirmado => Color.FromArgb(248, 228, 235),
        Concluido => Color.FromArgb(225, 243, 232),
        _ => Color.FromArgb(240, 237, 236)
    };
}

public class Agendamento : IEntidade
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNome { get; set; } = "";
    public int ProfissionalId { get; set; }
    public string ProfissionalNome { get; set; } = "";
    public DateTime Inicio { get; set; }
    public int DuracaoMin { get; set; }
    public List<ItemAtendimento> Itens { get; set; } = new();
    public string Status { get; set; } = StatusAgendamento.Confirmado;
    public string Observacoes { get; set; } = "";
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public int? AtendimentoId { get; set; }

    [JsonIgnore] public DateTime Fim => Inicio.AddMinutes(DuracaoMin);
    [JsonIgnore] public decimal Total => Itens.Sum(i => i.Valor);
    [JsonIgnore] public string Descricao => string.Join(" + ", Itens.Select(i => i.Servico));
    [JsonIgnore] public bool OcupaHorario => Status != StatusAgendamento.Cancelado;
    [JsonIgnore] public string Horario => $"{Inicio:HH:mm} – {Fim:HH:mm}";
}

static class Expediente
{
    public const int Passo = 10;

    static Configuracao Cfg => Repositorios.Configuracao.Dados;

    public static int Abertura => Cfg.AberturaMin;
    public static int Fechamento => Cfg.FechamentoMin;
    public static int AlmocoInicio => Cfg.AlmocoInicioMin;
    public static int AlmocoFim => Cfg.AlmocoFimMin;
    public static bool TemAlmoco => AlmocoFim > AlmocoInicio;

    public static bool Aberto(DateTime dia) => dia.DayOfWeek != DayOfWeek.Sunday;

    public static string Formatar(int minutos) => $"{minutos / 60:00}:{minutos % 60:00}";

    public static bool CabeNoExpediente(int inicio, int duracao)
    {
        int fim = inicio + duracao;
        if (inicio < Abertura || fim > Fechamento) return false;
        if (TemAlmoco && inicio < AlmocoFim && AlmocoInicio < fim) return false;
        return true;
    }

    public static List<DateTime> HorariosLivres(DateTime dia, int duracao, int profissionalId, int ignorarId)
    {
        var livres = new List<DateTime>();
        dia = dia.Date;
        if (duracao <= 0 || dia < DateTime.Today || !Aberto(dia)) return livres;

        var ocupados = Repositorios.Agendamentos.DoDia(dia)
            .Where(a => a.OcupaHorario && a.ProfissionalId == profissionalId && a.Id != ignorarId)
            .ToList();
        int agora = DateTime.Now.Hour * 60 + DateTime.Now.Minute;

        for (int inicio = Abertura; inicio + duracao <= Fechamento; inicio += Passo)
        {
            if (dia == DateTime.Today && inicio <= agora) continue;
            if (!CabeNoExpediente(inicio, duracao)) continue;
            var ini = dia.AddMinutes(inicio);
            var fim = ini.AddMinutes(duracao);
            if (ocupados.Any(a => ini < a.Fim && a.Inicio < fim)) continue;
            livres.Add(ini);
        }
        return livres;
    }

    public static Agendamento? Conflito(Agendamento novo) =>
        Repositorios.Agendamentos.DoDia(novo.Inicio.Date)
            .FirstOrDefault(a => a.OcupaHorario && a.Id != novo.Id && a.ProfissionalId == novo.ProfissionalId &&
                                 novo.Inicio < a.Fim && a.Inicio < novo.Fim);
}

class AgendamentoRepositorio : RepositorioJson<Agendamento>
{
    public AgendamentoRepositorio(string pasta) : base(pasta, "agendamentos.json") { }

    public List<Agendamento> NoPeriodo(DateTime inicio, DateTime fim) =>
        itens.Where(a => a.Inicio >= inicio && a.Inicio < fim).OrderBy(a => a.Inicio).Select(Copiar).ToList();

    public List<Agendamento> DoDia(DateTime dia) => NoPeriodo(dia.Date, dia.Date.AddDays(1));

    public List<Agendamento> Solicitacoes() =>
        itens.Where(a => a.Status == StatusAgendamento.Pendente && a.Fim >= DateTime.Today)
             .OrderBy(a => a.Inicio).Select(Copiar).ToList();

    public bool UsaCliente(int id) => itens.Any(a => a.ClienteId == id && a.OcupaHorario && a.Inicio >= DateTime.Today);
    public bool UsaProfissional(int id) => itens.Any(a => a.ProfissionalId == id);
    public bool UsaServico(string nome) =>
        itens.Any(a => a.Itens.Any(i => string.Equals(i.Servico, nome, StringComparison.CurrentCultureIgnoreCase)));

    public void Salvar(Agendamento a)
    {
        if (Expediente.Conflito(a) is { } c)
            throw new InvalidOperationException($"{a.ProfissionalNome} já tem um horário das {c.Horario} com {c.ClienteNome}.");
        if (a.Id == 0) Adicionar(a);
        else Atualizar(a);
    }

    public void MudarStatus(int id, string status)
    {
        var a = PorId(id) ?? throw new InvalidOperationException("Agendamento não encontrado.");
        if (a.Status == StatusAgendamento.Concluido)
            throw new InvalidOperationException("Este agendamento já foi concluído e virou atendimento.");
        a.Status = status;
        if (status != StatusAgendamento.Cancelado && Expediente.Conflito(a) is { } c)
            throw new InvalidOperationException($"O horário já está ocupado por {c.ClienteNome} ({c.Horario}).");
        Atualizar(a);
    }

    public Atendimento Concluir(int id)
    {
        var a = PorId(id) ?? throw new InvalidOperationException("Agendamento não encontrado.");
        if (a.Status == StatusAgendamento.Concluido)
            throw new InvalidOperationException("Este agendamento já foi concluído.");
        if (a.Status == StatusAgendamento.Cancelado)
            throw new InvalidOperationException("Não dá para concluir um agendamento cancelado.");

        var atendimento = new Atendimento
        {
            ClienteId = a.ClienteId,
            ClienteNome = a.ClienteNome,
            ProfissionalId = a.ProfissionalId,
            Profissional = a.ProfissionalNome,
            Data = a.Inicio,
            Itens = a.Itens.Select(i => i with { }).ToList()
        };
        Repositorios.Atendimentos.Adicionar(atendimento);

        a.Status = StatusAgendamento.Concluido;
        a.AtendimentoId = atendimento.Id;
        try { Atualizar(a); }
        catch { Repositorios.Atendimentos.Excluir(atendimento.Id); throw; }
        return atendimento;
    }
}
