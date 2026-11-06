using System.Text.Json;

namespace dashboardforms;

public interface IEntidade
{
    int Id { get; set; }
}

class RepositorioJson<T> where T : class, IEntidade
{
    protected readonly string arquivo;
    protected readonly List<T> itens;

    public RepositorioJson(string pasta, string nomeArquivo)
    {
        arquivo = Path.Combine(pasta, nomeArquivo);
        itens = Armazenamento.Carregar<T>(arquivo);
    }

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
