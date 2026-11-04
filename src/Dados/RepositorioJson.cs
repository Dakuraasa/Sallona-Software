using System.Text.Json;

namespace dashboardforms;

// Tudo que é guardado num repositório tem um Id
public interface IEntidade
{
    int Id { get; set; }
}

// ===== Repositório genérico: uma lista de T salva em um arquivo JSON da conta =====
// Faz o básico que todo cadastro precisa (listar, buscar, adicionar, atualizar, excluir),
// sempre devolvendo CÓPIAS — a tela não consegue alterar os dados sem passar por aqui.
// Se a gravação falhar, a lista em memória volta ao que era (não fica diferente do arquivo).
// TODO: quando o MySQL estiver pronto, esta classe vira as consultas SQL.
class RepositorioJson<T> where T : class, IEntidade
{
    protected readonly string arquivo;
    protected readonly List<T> itens;

    public RepositorioJson(string pasta, string nomeArquivo)
    {
        arquivo = Path.Combine(pasta, nomeArquivo);
        itens = Armazenamento.Carregar<T>(arquivo);
    }

    // cópia "de verdade" (inclusive listas dentro do objeto)
    protected static T Copiar(T item) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(item))!;

    public List<T> Todos() => itens.Select(Copiar).ToList();

    public T? PorId(int id) => itens.FirstOrDefault(i => i.Id == id) is { } i ? Copiar(i) : null;

    public void Adicionar(T item)
    {
        item.Id = itens.Count == 0 ? 1 : itens.Max(x => x.Id) + 1;
        itens.Add(Copiar(item));
        try { Salvar(); }
        catch { itens.RemoveAt(itens.Count - 1); throw; }
    }

    public void Atualizar(T item)
    {
        int i = itens.FindIndex(x => x.Id == item.Id);
        if (i < 0) return;
        var antigo = itens[i];
        itens[i] = Copiar(item);
        try { Salvar(); }
        catch { itens[i] = antigo; throw; }
    }

    public virtual void Excluir(int id)
    {
        int i = itens.FindIndex(x => x.Id == id);
        if (i < 0) return;
        var removido = itens[i];
        itens.RemoveAt(i);
        try { Salvar(); }
        catch { itens.Insert(i, removido); throw; }
    }

    protected void Salvar() => Armazenamento.Salvar(arquivo, itens);
}
