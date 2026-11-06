using System.Data;

namespace dashboardforms;

// ===== Página do Financeiro: receitas, despesas, comissões e lucro do mês =====
class FinanceiroPage : UserControl, IPagina
{
    readonly DataTable tabela = new();
    readonly DataGridView grid;
    readonly Label[] kpiValor, kpiSub;
    readonly GraficoBarras grafico = new() { Dock = DockStyle.Fill, Vazio = "Sem movimento neste mês" };

    public FinanceiroPage()
    {
        BackColor = Cores.Fundo;
        DoubleBuffered = true;

        // colunas da tabela (a coluna "Id" fica escondida automaticamente)
        foreach (var (nome, tipo) in new[] { ("Id", typeof(int)), ("Data", typeof(DateTime)), ("Descrição", typeof(string)),
                                               ("Categoria", typeof(string)), ("Valor", typeof(decimal)) })
            tabela.Columns.Add(nome, tipo);
        grid = Ui.Tabela(tabela);

        var nova = Ui.BotaoPrincipal("+  Nova despesa", 150);
        nova.Click += (_, _) => NovaDespesa();

        // layout: cabeçalho (64px), KPIs (124px), conteúdo (resto)
        var raiz = Ui.Raiz(64, 124, 0);
        var cab = Ui.Cabecalho("Financeiro", nova);
        raiz.Controls.Add(cab, 0, 0); raiz.SetColumnSpan(cab, 2);

        var kpis = Ui.Kpis(new[] { "Receitas do mês", "Despesas do mês", "Comissões do mês", "Lucro do mês" }, out kpiValor, out kpiSub);
        raiz.Controls.Add(kpis, 0, 1); raiz.SetColumnSpan(kpis, 2);

        var cTab = Ui.Cartao();
        cTab.Controls.Add(grid);                          // Fill primeiro
        cTab.Controls.Add(Ui.Titulo("Despesas do mês"));  // depois o título (Top)
        raiz.Controls.Add(cTab, 0, 2);

        var cGraf = Ui.Cartao();
        cGraf.Controls.Add(grafico);
        cGraf.Controls.Add(Ui.Titulo("Receitas x despesas"));
        raiz.Controls.Add(cGraf, 1, 2);

        Controls.Add(raiz);
        Recarregar();
    }

    // chamado sempre que a página volta a aparecer
    public void Recarregar()
    {
        var ini = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var fim = ini.AddMonths(1);

        var pagos = Repositorios.Atendimentos.Todos().Where(a => a.Pago && a.Data >= ini && a.Data < fim).ToList();
        var despesas = Repositorios.Despesas.Todos().Where(d => d.Data >= ini && d.Data < fim).OrderByDescending(d => d.Data).ToList();
        var profs = Repositorios.Profissionais.Todos();

        decimal receitas = pagos.Sum(a => a.Total);
        decimal gastos = despesas.Sum(d => d.Valor);
        decimal comissoes = Math.Round(pagos.Sum(a =>
            a.Total * (profs.FirstOrDefault(p => p.Id == a.ProfissionalId)?.ComissaoPct ?? 0) / 100), 2);
        decimal lucro = receitas - gastos - comissoes;

        kpiValor[0].Text = Formato.Dinheiro(receitas); kpiSub[0].Text = $"{pagos.Count} atendimentos pagos";
        kpiValor[1].Text = Formato.Dinheiro(gastos); kpiSub[1].Text = $"{despesas.Count} lançamentos";
        kpiValor[2].Text = Formato.Dinheiro(comissoes); kpiSub[2].Text = "a pagar para a equipe";
        kpiValor[3].Text = Formato.Dinheiro(lucro); kpiSub[3].Text = lucro >= 0 ? "no azul" : "no vermelho";
        kpiSub[3].ForeColor = lucro >= 0 ? Cores.Verde : Cores.Coral;

        tabela.Rows.Clear();
        foreach (var d in despesas)
            tabela.Rows.Add(d.Id, d.Data, d.Descricao, d.Categoria, d.Valor);

        grafico.Rotulos = new[] { "Receitas", "Despesas", "Comissões" };
        grafico.Valores = new[] { (float)receitas, (float)gastos, (float)comissoes };
        grafico.Animar();
    }

    void NovaDespesa()
    {
        using var dlg = new DespesaDialog(null);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (Armazenamento.Tentar(FindForm(), () => Repositorios.Despesas.Adicionar(dlg.Resultado)))
            Recarregar();
    }
}