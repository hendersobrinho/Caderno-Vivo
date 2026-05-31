# 📓 Caderno Vivo

> App local de organização de estudos construído com ASP.NET Razor Pages + SQLite. Sem nuvem, sem conta, sem assinatura — roda na sua máquina e abre no navegador.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![SQLite](https://img.shields.io/badge/SQLite-EF%20Core-003B57?logo=sqlite)
![Plataforma](https://img.shields.io/badge/plataforma-Linux%20%7C%20macOS%20%7C%20Windows-informational)

---

## O que é

Caderno Vivo é um caderno de estudos interativo que roda localmente no seu computador. Você planeja seus blocos de estudo, acompanha o progresso em tempo real com um cronômetro, registra onde parou, dúvidas e observações, e exporta tudo para o calendário do celular.

A proposta é simples: um painel centralizado para quem estuda com seriedade e quer manter o controle sem depender de ferramentas online.

---

## Funcionalidades

### Fila do dia (`/Hoje`)
- Lista os blocos de estudo do dia em ordem de horário
- Cronômetro integrado por bloco com suporte a pausa
- Registro de onde parou, próximo passo, dúvidas e observações ao concluir
- Alertas no navegador 5 minutos antes de cada bloco começar
- Ações rápidas: concluir, marcar como não feito, reagendar para amanhã ou sábado

### Dashboard
- Visão geral da semana: blocos concluídos, minutos planejados vs. realizados
- Gráfico de barras por dia da semana
- Métricas: blocos no prazo, antecipados, depois do prazo, hora extra, média real por bloco (histórico completo)
- Painel de atenção com blocos atrasados e ações diretas
- Projetos em foco, lembretes importantes, próximos blocos e observações recentes

### Cronograma por IA (`/Cronograma`)
- Gera um prompt pronto para colar em qualquer IA (ChatGPT, Claude etc.)
- A IA devolve um JSON com o plano completo do período
- Você cola o JSON no app e importa todos os blocos de uma vez

### Linha do tempo (`/LinhaDoTempo`)
- Navegação por data: passado, hoje e futuro
- Revisão de dias anteriores com status de cada bloco
- Detalhe de bloco com observações, tempo real gasto, dúvidas registradas

### Faculdade
- Cadastro de matérias com carga horária e período
- Tarefas com prazo, prioridade e status
- Pendências e dúvidas por matéria
- Histórico de aulas
- Plano da semana com seleção e exclusão em massa de blocos

### Projetos
- Projetos com prazo, prioridade e status
- Checklist de itens
- Roadmap com fases e marcos
- Blocos de estudo vinculados ao projeto
- Importação de cronograma de projeto via JSON

### Artigos
- Controle de artigos e textos em produção (ideia → desenvolvimento → revisão → publicado)
- Tarefas por artigo com prazo e prioridade
- Aulas e materiais de referência vinculados
- Dúvidas por artigo

### Lembretes
- Lembretes avulsos ou vinculados a matéria, projeto ou artigo
- Destaque, prazo, prioridade e escopo
- Painel de lembretes importantes no dashboard

### Exportação
- Exporta os blocos para arquivo `.ics` compatível com Google Calendar, Outlook e qualquer app de calendário
- Cada evento inclui alerta 10 minutos antes

---

## Stack

| Camada | Tecnologia |
|---|---|
| Framework | ASP.NET Core Razor Pages (.NET 10) |
| Banco de dados | SQLite via Entity Framework Core 8 |
| Frontend | Bootstrap 5 + Bootstrap Icons |
| Persistência | Arquivo local `caderno.db` |

---

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (ou compatível com `net10.0`)
- Linux, macOS ou Windows

SQLite já vem incluso via NuGet — não precisa instalar nada além do SDK.

---

## Rodando localmente

```bash
git clone https://github.com/seu-usuario/CadernoVivo.git
cd CadernoVivo

dotnet restore
dotnet run
```

Acesse em: **http://localhost:5055**

O banco de dados é criado automaticamente em `caderno.db` na raiz do projeto.

Para usar um caminho de banco diferente:

```bash
CADERNO_VIVO_DB=/outro/caminho/caderno.db dotnet run
```

---

## Serviço no Linux (systemd)

Para o app iniciar automaticamente quando você fizer login, instale o serviço de usuário incluso:

```bash
systemctl --user link "$PWD/systemd/caderno-vivo.service"
systemctl --user daemon-reload
systemctl --user enable --now caderno-vivo.service
```

Comandos úteis:

```bash
systemctl --user status  caderno-vivo.service
systemctl --user restart caderno-vivo.service
systemctl --user stop    caderno-vivo.service
```

---

## Importação de cronograma via IA

A tela **Cronograma** gera um prompt completo para você colar em qualquer IA. A IA devolve um JSON que o app importa diretamente como blocos de estudo.

**Formato esperado:**

```json
{
  "tipo": "plano",
  "semana_inicio": "2026-05-25",
  "periodo": "2026-05-25 a 2026-06-30",
  "dias": [
    {
      "data": "2026-05-25",
      "blocos": [
        {
          "inicio": "19:30",
          "fim": "21:00",
          "titulo": "Revisão acumulada — Aulas 1 e 2",
          "descricao": "Revisar slides e resolver questões dos módulos 1 e 2",
          "modulo": "Faculdade",
          "materia": "Probabilidade e Estatística",
          "artigo": null
        }
      ]
    }
  ]
}
```

> Datas no JSON: `yyyy-MM-dd` | Horários: `HH:mm` em 24h

---

## Estrutura do projeto

```
CadernoVivo/
├── Data/               # AppDbContext (Entity Framework)
├── Helpers/            # Utilitários de data, status e importação
├── Models/             # Entidades: BlocoEstudo, Materia, Projeto, Artigo...
├── Pages/
│   ├── Hoje/           # Fila do dia + cronômetro
│   ├── Index           # Dashboard
│   ├── LinhaDoTempo/   # Navegação por data
│   ├── Faculdade/      # Matérias, tarefas, pendências
│   ├── Projetos/       # Projetos, checklist, roadmap
│   ├── Artigos/        # Artigos em produção
│   ├── Lembretes/      # Lembretes e alertas
│   ├── PlanoImportar/  # Importação de cronograma via JSON
│   └── Exportar/       # Exportação .ics
├── systemd/            # Serviço de usuário para Linux
└── caderno.db          # Banco local (gerado automaticamente, ignorado pelo git)
```

---

## .gitignore recomendado

```gitignore
bin/
obj/
caderno.db
caderno.db-shm
caderno.db-wal
*.user
.vs/
.vscode/
.idea/
```

---

## Licença

Uso pessoal. Faça um fork e adapte à sua rotina.
