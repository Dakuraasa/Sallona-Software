<p align="center">
  <img src="logo.jpg" alt="Logo Sallona Software">
</p>

# Sallona Software

Sistema de gestão para salão de beleza, feito em **C# com Windows Forms (.NET 8)** pelo Grupo SinapsES
como projeto final do curso Técnico em Informática.

## Telas

| Tela | Situação |
|---|---|
| Dashboard | Pronta (dados de exemplo) — faturamento, atendimentos, metas e alertas |
| Clientes | Lista, busca, filtros e ficha do cliente (cadastro/edição ainda por fazer) |
| Serviços | Catálogo completo: cadastrar, editar, desativar e excluir (salvo em JSON) |
| Pagamento | Aberta pelo Dashboard: clique duas vezes num atendimento **Pendente** |
| Profissionais, Financeiro, Estoque, Produtos, Agenda | Em breve |

## Como rodar

1. Instale o **Visual Studio 2022** com a carga "Desenvolvimento para desktop com .NET" (ou o .NET 8 SDK).
2. Abra `src/dashboardforms.sln`.
3. Aperte **F5**.

Pela linha de comando: `cd src` e depois `dotnet run`.

## Estrutura do código

```
src/
├── Program.cs            ponto de entrada
├── dashboardforms.cs     janela principal: menu lateral, Dashboard e troca de páginas
├── controles.cs          paleta de cores (Cores) e controles visuais compartilhados
├── Clientes/             tela de Clientes + modelo/repositório de clientes
├── Servicos/             tela, janela de cadastro e repositório de serviços
└── Pagamentos/           tela de pagamento de um atendimento
```

Cada módulo acessa os dados por um **repositório** (`Repositorios.Servicos`, `Repositorios.Clientes`).
Quando o banco MySQL estiver pronto, basta trocar o que tem dentro dos repositórios — as telas não mudam.

## Regras do grupo

- Use as cores da classe `Cores` (em `controles.cs`) em vez de escrever cores novas.
- Use os controles compartilhados (`Card`, `Caixa`, `BotaoAnimado`, `AvatarIniciais`...) em vez de criar outros iguais.
- Envie as mudanças com **commit + push** pelo Visual Studio ou Git, não pelo "Add files via upload" do site
  (o upload ignora o `.gitignore` e manda as pastas `bin/` e `obj/` junto).
