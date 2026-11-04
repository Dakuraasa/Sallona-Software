using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace dashboardforms;

// ===== Configurações =====
// No topo: a conta conectada e o botão Desconectar (volta para a tela de login).
// Depois: dados do estabelecimento, senha, preferências, backup e exclusão da conta.
class ConfiguracoesPage : UserControl, IPagina
{
    // largura fixa dos cartões (centralizados): cabe na janela no tamanho mínimo (1100 px)
    const int Largura = 780;
    const int LarguraCampo = 350, Coluna2 = 380;

    readonly Usuario usuario;
    readonly UsuarioRepositorio contas = new();

    /// <summary>Pede para o Dashboard voltar para a tela de login.</summary>
    public event Action? Desconectar;
    /// <summary>Encerra a sessão sem perguntar (depois de restaurar backup ou excluir a conta).</summary>
    public event Action? SessaoEncerrada;
    /// <summary>Avisa que o nome do estabelecimento/e-mail mudou (para atualizar o título da janela).</summary>
    public event Action? ContaAlterada;

    readonly FlowLayoutPanel coluna;
    readonly AvatarIniciais avatar = new() { Size = new Size(64, 64), BackColor = Color.White };
    readonly Label lblSalao = new(), lblInfoConta = new(), lblResumo = new();

    public ConfiguracoesPage(Usuario usuario)
    {
        this.usuario = usuario;
        BackColor = Cores.Fundo;
        DoubleBuffered = true;

        coluna = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = Cores.Fundo, Padding = new Padding(20, 16, 20, 24)
        };
        coluna.Controls.Add(new Label
        {
            Text = "Configurações", Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = Cores.Escuro,
            AutoSize = true, Margin = new Padding(0, 0, 0, 12)
        });
        coluna.Controls.Add(CartaoConta());
        coluna.Controls.Add(CartaoEstabelecimento());
        coluna.Controls.Add(CartaoSenha());
        coluna.Controls.Add(CartaoPreferencias());
        coluna.Controls.Add(CartaoBackup());
        coluna.Controls.Add(CartaoPerigo());
        Controls.Add(coluna);

        // centraliza os cartões quando a janela é larga
        coluna.Resize += (_, _) =>
        {
            int sobra = Math.Max(20, (coluna.ClientSize.Width - Largura) / 2);
            coluna.Padding = new Padding(sobra, 16, sobra, 24);
        };
        Recarregar();
    }

    public void Recarregar()
    {
        string salao = usuario.NomeSalao.Length > 0 ? usuario.NomeSalao : usuario.Nome;
        avatar.Iniciais = Util.Iniciais(salao);
        avatar.Invalidate();
        lblSalao.Text = salao;
        lblInfoConta.Text = $"Responsável: {usuario.Nome}   ·   {usuario.Email}\nConta criada em {usuario.CriadoEm:dd/MM/yyyy}";
        lblResumo.Text =
            $"{Repositorios.Clientes.Todos().Count} clientes  ·  {Repositorios.Servicos.Todos().Count} serviços  ·  " +
            $"{Repositorios.Profissionais.Todos().Count} profissionais  ·  {Repositorios.Produtos.Todos().Count} produtos  ·  " +
            $"{Repositorios.Atendimentos.Todos().Count} atendimentos";
    }

    // =====================================================================
    // Montagem dos cartões
    // =====================================================================
    static Card NovoCartao(string titulo, string descricao, int altura, out Panel corpo, Color? fundo = null)
    {
        var c = new Card { Width = Largura, Height = altura, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(24, 18, 24, 18), Preenchimento = fundo ?? Color.White };
        corpo = new Panel { Dock = DockStyle.Fill, BackColor = fundo ?? Color.White };
        c.Controls.Add(corpo);
        if (descricao.Length > 0)
            c.Controls.Add(new Label { Text = descricao, Dock = DockStyle.Top, Height = 26, ForeColor = Cores.Cinza, BackColor = Color.Transparent });
        c.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Top, Height = 30, ForeColor = Cores.Escuro, BackColor = Color.Transparent, Font = new Font("Segoe UI", 12f, FontStyle.Bold) });
        return c;
    }

    // rótulo + campo, posicionados em grade (coluna 0 ou 1)
    static T Campo<T>(Panel corpo, string rotulo, T campo, int col, int linha, int largura = LarguraCampo) where T : Control
    {
        int x = col * Coluna2, y = linha * 62;
        corpo.Controls.Add(new Label { Text = rotulo, Location = new Point(x, y), AutoSize = true, ForeColor = Cores.Cinza, Font = Ui.Negrito, BackColor = Color.Transparent });
        campo.Location = new Point(x, y + 22);
        campo.Width = largura;
        campo.Font = new Font("Segoe UI", 10.5f);
        corpo.Controls.Add(campo);
        return campo;
    }

    static TextBox Texto(int limite = 100, bool senha = false) =>
        new() { BorderStyle = BorderStyle.FixedSingle, MaxLength = limite, UseSystemPasswordChar = senha };

    static Label Status(Panel corpo, int x, int y) =>
        Adicionar(corpo, new Label { Location = new Point(x, y), AutoSize = true, BackColor = Color.Transparent, Font = Ui.Negrito });

    static T Adicionar<T>(Panel corpo, T c) where T : Control { corpo.Controls.Add(c); return c; }

    static void Avisar(Label l, string texto, bool ok)
    {
        l.Text = texto;
        l.ForeColor = ok ? Cores.Verde : Cores.Vinho;
    }

    // ---------- 1) Conta conectada ----------
    Control CartaoConta()
    {
        var c = new Card { Width = Largura, Height = 120, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(24, 20, 24, 20), Preenchimento = Cores.Escuro };
        avatar.BackColor = Cores.Escuro;
        avatar.Fundo = Cores.Primaria;
        avatar.Cor = Color.White;
        avatar.Location = new Point(24, 28);
        lblSalao.Location = new Point(106, 26); lblSalao.AutoSize = true; lblSalao.ForeColor = Color.White;
        lblSalao.BackColor = Color.Transparent; lblSalao.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
        lblInfoConta.Location = new Point(108, 60); lblInfoConta.AutoSize = true; lblInfoConta.ForeColor = Cores.Rosa;
        lblInfoConta.BackColor = Color.Transparent;

        var conectado = new Label { Text = "● Conectado", AutoSize = true, ForeColor = Color.FromArgb(120, 220, 160), BackColor = Color.Transparent, Font = Ui.Negrito };
        var sair = Ui.Botao("⏻   Desconectar", Cores.Primaria, Color.White, 150);
        sair.Click += (_, _) => Desconectar?.Invoke();
        c.Controls.AddRange(new Control[] { avatar, lblSalao, lblInfoConta, conectado, sair });
        // o cartão tem largura fixa, então dá para posicionar direto (à direita)
        sair.Location = new Point(Largura - 24 - sair.Width, 52);
        conectado.Location = new Point(Largura - 24 - 90, 24);
        return c;
    }

    // ---------- 2) Dados do estabelecimento ----------
    Control CartaoEstabelecimento()
    {
        var c = NovoCartao("Dados do estabelecimento", "Aparecem no sistema e identificam a conta. O e-mail é o usado para entrar.", 330, out var corpo);
        var salao = Campo(corpo, "Nome do estabelecimento", Texto(80), 0, 0);
        var resp = Campo(corpo, "Responsável", Texto(80), 1, 0);
        var email = Campo(corpo, "E-mail (login)", Texto(120), 0, 1);
        var tel = Campo(corpo, "Telefone", Texto(20), 1, 1);
        var end = Campo(corpo, "Endereço", Texto(150), 0, 2);
        var doc = Campo(corpo, "CNPJ ou CPF (opcional)", Texto(20), 1, 2);
        tel.PlaceholderText = "(27) 99999-9999";

        void Preencher()
        {
            salao.Text = usuario.NomeSalao; resp.Text = usuario.Nome; email.Text = usuario.Email;
            tel.Text = usuario.Telefone; end.Text = usuario.Endereco; doc.Text = usuario.Documento;
        }
        Preencher();

        var salvar = Adicionar(corpo, Ui.Botao("Salvar dados", Cores.Primaria, Color.White, 130));
        salvar.Location = new Point(0, 196);
        var status = Status(corpo, 146, 204);
        salvar.Click += (_, _) =>
        {
            int dTel = tel.Text.Count(char.IsDigit), dDoc = doc.Text.Count(char.IsDigit);
            string? erro =
                salao.Text.Trim().Length == 0 ? "Informe o nome do estabelecimento." :
                resp.Text.Trim().Length == 0 ? "Informe o responsável." :
                !UsuarioRepositorio.EmailValido(email.Text.Trim()) ? "E-mail inválido." :
                dTel > 0 && dTel is < 10 or > 11 ? "Telefone com DDD (10 ou 11 números)." :
                dDoc > 0 && dDoc != 11 && dDoc != 14 ? "CPF tem 11 números e CNPJ tem 14." :
                null;
            if (erro != null) { Avisar(status, erro, false); return; }
            try
            {
                contas.AtualizarDados(usuario.Id, salao.Text, resp.Text, email.Text, tel.Text, end.Text, doc.Text);
                // atualiza também a conta que está em uso nesta sessão
                var atual = contas.PorId(usuario.Id)!;
                (usuario.NomeSalao, usuario.Nome, usuario.Email, usuario.Telefone, usuario.Endereco, usuario.Documento) =
                    (atual.NomeSalao, atual.Nome, atual.Email, atual.Telefone, atual.Endereco, atual.Documento);
                Avisar(status, "✓ Dados salvos.", true);
                Recarregar();
                ContaAlterada?.Invoke();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Avisar(status, ex.Message, false);
            }
        };
        return c;
    }

    // ---------- 3) Senha ----------
    Control CartaoSenha()
    {
        var c = NovoCartao("Segurança", "Troque a senha de acesso do estabelecimento (todos que usam a conta precisarão da nova).", 260, out var corpo);
        var atual = Campo(corpo, "Senha atual", Texto(64, true), 0, 0);
        var nova = Campo(corpo, "Nova senha (mínimo 6 caracteres)", Texto(64, true), 0, 1);
        var conf = Campo(corpo, "Confirmar nova senha", Texto(64, true), 1, 1);

        var alterar = Adicionar(corpo, Ui.Botao("Alterar senha", Cores.Primaria, Color.White, 130));
        alterar.Location = new Point(0, 134);
        var status = Status(corpo, 146, 142);
        alterar.Click += (_, _) =>
        {
            if (nova.Text.Length < 6) { Avisar(status, "A nova senha precisa ter 6 caracteres ou mais.", false); return; }
            if (nova.Text != conf.Text) { Avisar(status, "As senhas novas não são iguais.", false); return; }
            try
            {
                contas.AlterarSenha(usuario.Id, atual.Text, nova.Text);
                atual.Clear(); nova.Clear(); conf.Clear();
                Avisar(status, "✓ Senha alterada.", true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Avisar(status, ex.Message, false);
            }
        };
        return c;
    }

    // ---------- 4) Preferências ----------
    Control CartaoPreferencias()
    {
        var c = NovoCartao("Preferências", "Valores usados pelo Dashboard e sugeridos nos cadastros.", 260, out var corpo);
        var cfg = Repositorios.Configuracao;
        var meta = Campo(corpo, "Meta de faturamento do mês (R$) — 0 = sem meta", new NumericUpDown { DecimalPlaces = 2, Maximum = 10_000_000, Increment = 500, ThousandsSeparator = true }, 0, 0);
        var periodo = Campo(corpo, "Período que o Dashboard abre", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList }, 1, 0);
        periodo.Items.AddRange(new object[] { "Dia", "Semana", "Mês", "Ano" });
        var comissao = Campo(corpo, "Comissão padrão de novos profissionais (%)", new NumericUpDown { DecimalPlaces = 1, Maximum = 100, Increment = 5 }, 0, 1);
        var minimo = Campo(corpo, "Estoque mínimo padrão de novos produtos", new NumericUpDown { Maximum = 100000 }, 1, 1);

        meta.Value = Math.Clamp(cfg.MetaMensal, 0, meta.Maximum);
        periodo.SelectedItem = periodo.Items.Contains(cfg.PeriodoPadrao) ? cfg.PeriodoPadrao : "Mês";
        comissao.Value = Math.Clamp(cfg.ComissaoPadrao, 0, 100);
        minimo.Value = Math.Clamp(cfg.EstoqueMinimoPadrao, 0, 100000);

        var salvar = Adicionar(corpo, Ui.Botao("Salvar preferências", Cores.Primaria, Color.White, 170));
        salvar.Location = new Point(0, 134);
        var status = Status(corpo, 186, 142);
        salvar.Click += (_, _) =>
        {
            if (Armazenamento.Tentar(FindForm(), () => cfg.SalvarPreferencias(Math.Round(meta.Value, 2), (string)periodo.SelectedItem!,
                                                                               Math.Round(comissao.Value, 1), (int)minimo.Value)))
                Avisar(status, "✓ Preferências salvas.", true);
        };
        return c;
    }

    // ---------- 5) Backup ----------
    static readonly string[] ArquivosDaConta =
        { "clientes.json", "servicos.json", "atendimentos.json", "profissionais.json", "produtos.json", "movimentos_estoque.json", "configuracao.json" };

    Control CartaoBackup()
    {
        var c = NovoCartao("Backup e dados",
            "Ainda não há banco de dados: tudo fica neste computador. Faça backup com frequência (pen drive, nuvem...).", 200, out var corpo);
        lblResumo.Location = new Point(0, 0); lblResumo.AutoSize = true; lblResumo.ForeColor = Cores.Texto; lblResumo.BackColor = Color.Transparent;
        corpo.Controls.Add(lblResumo);
        var pasta = Adicionar(corpo, new Label
        {
            Text = Armazenamento.PastaDaConta(usuario.Id), Location = new Point(0, 26), AutoSize = true,
            ForeColor = Cores.Cinza, BackColor = Color.Transparent, Font = Ui.Pequena
        });

        var backup = Adicionar(corpo, Ui.Botao("Fazer backup (.zip)", Cores.Escuro, Color.White, 170));
        var restaurar = Adicionar(corpo, Ui.Botao("Restaurar backup", Cores.Fundo, Cores.Texto, 160));
        var abrir = Adicionar(corpo, Ui.Botao("Abrir pasta dos dados", Color.White, Cores.Primaria, 180));
        backup.Location = new Point(0, 60); restaurar.Location = new Point(180, 60); abrir.Location = new Point(350, 60);
        var status = Status(corpo, 0, 104);

        backup.Click += (_, _) => FazerBackup(status);
        restaurar.Click += (_, _) => RestaurarBackup(status);
        abrir.Click += (_, _) =>
        {
            var dir = Armazenamento.PastaDaConta(usuario.Id);
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        };
        return c;
    }

    void FazerBackup(Label status)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Salvar backup do Sallona",
            Filter = "Backup do Sallona (*.zip)|*.zip",
            FileName = $"Sallona-backup-{Seguro(usuario.NomeSalao)}-{DateTime.Now:yyyy-MM-dd_HHmm}.zip"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            CriarZip(dlg.FileName);
            Avisar(status, $"✓ Backup salvo em {dlg.FileName}", true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Avisar(status, "Não foi possível salvar o backup: " + ex.Message, false);
        }
    }

    void CriarZip(string destino)
    {
        string dir = Armazenamento.PastaDaConta(usuario.Id);
        string temp = destino + ".tmp";
        using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
        {
            foreach (var nome in ArquivosDaConta)
            {
                var caminho = Path.Combine(dir, nome);
                if (File.Exists(caminho)) zip.CreateEntryFromFile(caminho, nome);
            }
            var info = zip.CreateEntry("backup-info.json");
            using var w = new StreamWriter(info.Open());
            w.Write(JsonSerializer.Serialize(new { Sistema = "Sallona", Estabelecimento = usuario.NomeSalao, usuario.Email, Data = DateTime.Now }));
        }
        File.Move(temp, destino, overwrite: true);
    }

    void RestaurarBackup(Label status)
    {
        using var dlg = new OpenFileDialog { Title = "Escolha o backup do Sallona", Filter = "Backup do Sallona (*.zip)|*.zip" };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        // 1) lê e confere o backup ANTES de mexer em qualquer coisa
        var conteudo = new Dictionary<string, string>();
        try
        {
            using var zip = ZipFile.OpenRead(dlg.FileName);
            if (zip.GetEntry("backup-info.json") == null) throw new InvalidDataException("Este arquivo não é um backup do Sallona.");
            foreach (var e in zip.Entries)
            {
                if (!ArquivosDaConta.Contains(e.FullName)) continue;   // ignora qualquer outro arquivo (e caminhos estranhos)
                if (e.Length > 50_000_000) throw new InvalidDataException($"{e.FullName} é grande demais.");
                using var r = new StreamReader(e.Open());
                string json = r.ReadToEnd();
                ValidarJson(e.FullName, json);
                conteudo[e.FullName] = json;
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
        {
            Avisar(status, "Backup inválido: " + ex.Message, false);
            return;
        }

        if (!Ui.Confirmar(FindForm(),
                "Restaurar este backup vai SUBSTITUIR todos os dados atuais desta conta " +
                "(clientes, serviços, atendimentos, profissionais, produtos e estoque).\n\n" +
                "Antes disso, uma cópia dos dados atuais será guardada automaticamente.\n\nContinuar?",
                "Restaurar backup"))
            return;

        // 2) cópia de segurança automática do que existe hoje
        string dir = Armazenamento.PastaDaConta(usuario.Id);
        string pastaBackups = Path.Combine(Armazenamento.PastaRaiz, "backups");
        try
        {
            Directory.CreateDirectory(pastaBackups);
            CriarZip(Path.Combine(pastaBackups, $"conta{usuario.Id}-antes-de-restaurar-{DateTime.Now:yyyyMMdd-HHmmss}.zip"));

            // 3) troca os arquivos (os que não estão no backup são apagados, para não misturar dados)
            foreach (var nome in ArquivosDaConta)
            {
                var caminho = Path.Combine(dir, nome);
                if (conteudo.TryGetValue(nome, out var json)) Armazenamento.SalvarTexto(caminho, json);
                else if (File.Exists(caminho)) File.Delete(caminho);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Avisar(status, "Não foi possível restaurar: " + ex.Message + $"\nOs dados anteriores estão em {pastaBackups}", false);
            return;
        }

        MessageBox.Show(FindForm(), "Backup restaurado!\n\nEntre de novo para carregar os dados restaurados.",
            "Restaurar backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        SessaoEncerrada?.Invoke();
    }

    // Confere se o JSON tem o formato certo para aquele arquivo
    static void ValidarJson(string nome, string json)
    {
        object? lido = nome switch
        {
            "clientes.json" => JsonSerializer.Deserialize<List<Cliente>>(json),
            "servicos.json" => JsonSerializer.Deserialize<List<Servico>>(json),
            "atendimentos.json" => JsonSerializer.Deserialize<List<Atendimento>>(json),
            "profissionais.json" => JsonSerializer.Deserialize<List<Profissional>>(json),
            "produtos.json" => JsonSerializer.Deserialize<List<Produto>>(json),
            "movimentos_estoque.json" => JsonSerializer.Deserialize<List<MovimentoEstoque>>(json),
            "configuracao.json" => JsonSerializer.Deserialize<Configuracao>(json),
            _ => null
        };
        if (lido == null) throw new InvalidDataException($"{nome} está vazio ou corrompido.");
    }

    static string Seguro(string texto)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var limpo = new string(texto.Where(ch => !invalidos.Contains(ch)).ToArray()).Trim().Replace(' ', '-');
        return limpo.Length == 0 ? "salao" : limpo;
    }

    // ---------- 6) Excluir conta ----------
    Control CartaoPerigo()
    {
        var c = NovoCartao("Excluir conta do estabelecimento",
            "Apaga a conta e TODOS os dados dela deste computador. Não dá para desfazer — faça um backup antes.", 140, out var corpo);
        var excluir = Adicionar(corpo, Ui.Botao("Excluir conta...", Color.White, Cores.Vinho, 150));
        excluir.Location = new Point(0, 4);
        excluir.Click += (_, _) =>
        {
            using var dlg = new ConfirmarSenhaDialog(usuario.NomeSalao);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            try
            {
                contas.Excluir(usuario.Id, dlg.Senha);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                MessageBox.Show(FindForm(), ex.Message, "Excluir conta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show(FindForm(), "A conta foi excluída.", "Excluir conta", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SessaoEncerrada?.Invoke();
        };
        return c;
    }
}

// ===== Pede a senha para confirmar a exclusão da conta =====
class ConfirmarSenhaDialog : Formulario
{
    public string Senha { get; private set; } = "";

    public ConfirmarSenhaDialog(string salao) : base("Excluir conta", "Excluir tudo")
    {
        AdicionarControle(new Label
        {
            Text = $"Isso apaga \"{salao}\" e todos os dados.\nDigite a senha da conta para confirmar.",
            AutoSize = true, ForeColor = Cores.Vinho, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4)
        });
        var txt = AdicionarCampo("Senha", NovaCaixaTexto(64));
        txt.UseSystemPasswordChar = true;
        Validar = () => txt.Text.Length == 0 ? "Digite a senha." : null;
        AoSalvar = () => Senha = txt.Text;
    }
}
