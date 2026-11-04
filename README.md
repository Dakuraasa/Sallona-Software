<p align="center">
  <img src="logo.jpg" alt="Logo Sallona Software">
</p>

# Sallona Software

Sistema de gestão para salão de beleza, feito em **C# com Windows Forms (.NET 8)** pelo Grupo SinapsES
como projeto final do curso Técnico em Informática.

## Telas

| Tela | Situação |
|---|---|
| Login | Primeira tela ao abrir. Cada conta é criada em "Cadastre-se" e tem os próprios dados |
| Dashboard | Faturamento, atendimentos, novos clientes, ticket médio, gráfico e meta — tudo calculado dos dados da conta |
| Clientes | Cadastrar, editar, excluir, buscar, filtrar e ver o histórico de cada cliente |
| Serviços | Catálogo: cadastrar, editar, desativar e excluir |
| Pagamento | Aberta pelo Dashboard: clique duas vezes num atendimento **Pendente** |
| Profissionais, Financeiro, Estoque, Produtos, Agenda | Em breve |

## Dados

Ainda **não há banco de dados**. Nada vem preenchido: o programa começa zerado e quem cadastra
clientes, serviços, atendimentos e a meta do mês é a pessoa que fez login.

Os dados ficam em arquivos JSON no computador:

```
%AppData%\Sallona\usuarios.json            contas (a senha é guardada só como hash PBKDF2)
%AppData%\Sallona\contas\<id>\*.json       clientes, serviços, atendimentos e meta de cada conta
```

Para começar do zero, é só apagar a pasta `%AppData%\Sallona`.

## Como rodar

1. Instale o **Visual Studio 2022** com a carga "Desenvolvimento para desktop com .NET" (ou o .NET 8 SDK).
2. Abra `src/dashboardforms.sln`.
3. Aperte **F5**.

Pela linha de comando: `cd src` e depois `dotnet run`.

## Estrutura do código

```
src/
├── Program.cs            ponto de entrada: Login -> Dashboard (e "Sair" volta ao Login)
├── dashboardforms.cs     janela principal: menu lateral, Dashboard e troca de páginas
├── controles.cs          paleta de cores (Cores) e controles visuais compartilhados
├── Formulario.cs         base das janelas de cadastro (campos + Salvar/Cancelar)
├── Dados/                onde os dados ficam salvos (JSON por conta) e Repositorios
├── Login/                tela de login, criar conta e contas (Usuario)
├── Clientes/             tela de Clientes, janela de cadastro e repositório
├── Servicos/             tela de Serviços, janela de cadastro e repositório
├── Atendimentos/         atendimentos (modelo, repositório e janela "Novo atendimento")
└── Pagamentos/           tela de pagamento de um atendimento
```

Cada módulo acessa os dados por um **repositório** (`Repositorios.Clientes`, `Repositorios.Servicos`,
`Repositorios.Atendimentos`, `Repositorios.Configuracao`).
Quando o banco MySQL estiver pronto, basta trocar o que tem dentro dos repositórios — as telas não mudam.

## Regras do grupo

- Use as cores da classe `Cores` (em `controles.cs`) em vez de escrever cores novas.
- Use os controles compartilhados (`Card`, `Caixa`, `BotaoAnimado`, `AvatarIniciais`...) em vez de criar outros iguais.
- Envie as mudanças com **commit + push** pelo Visual Studio ou Git, não pelo "Add files via upload" do site
  (o upload ignora o `.gitignore` e manda as pastas `bin/` e `obj/` junto).
