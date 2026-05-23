# Caderno Vivo

App local em ASP.NET Razor Pages para organizar rotina de estudos: cronograma, fila do dia, linha do tempo, projetos, lembretes e exportacao para calendario.

## Funcionalidades

- Fila de estudos do dia com cronometro, conclusao de blocos e observacoes.
- Importacao de cronograma por JSON gerado por IA.
- Cadastro manual de blocos com datas `dd/mm/aaaa` e horarios `HH:mm`.
- Plano da semana com selecao e exclusao em massa de blocos.
- Linha do tempo para revisar dias anteriores, atrasos e proximos estudos.
- Projetos com checklist e blocos vinculados.
- Lembretes avulsos ou vinculados a materia/projeto.
- Dashboard com progresso semanal, metricas e proximos blocos.
- Alertas locais no navegador enquanto a tela Hoje estiver aberta.
- Exportacao `.ics` para calendario com alerta 10 minutos antes.

## Requisitos

- Linux, macOS ou Windows
- .NET SDK compativel com `net10.0`
- SQLite, usado via Entity Framework Core

## Rodando localmente

```bash
dotnet restore
dotnet run
```

Por padrao, em desenvolvimento o app usa:

```text
http://localhost:5055
```

O banco local fica em:

```text
caderno.db
```

Para usar outro caminho de banco:

```bash
CADERNO_VIVO_DB=/caminho/para/caderno.db dotnet run
```

## Servico no Linux

Este repo inclui um servico systemd de usuario em:

```text
systemd/caderno-vivo.service
```

Para instalar e iniciar automaticamente no login:

```bash
systemctl --user link "$PWD/systemd/caderno-vivo.service"
systemctl --user daemon-reload
systemctl --user enable --now caderno-vivo.service
```

Comandos uteis:

```bash
systemctl --user status caderno-vivo.service
systemctl --user restart caderno-vivo.service
systemctl --user stop caderno-vivo.service
systemctl --user disable --now caderno-vivo.service
```

## Cronograma por IA

A tela `Cronograma` tem um prompt pronto para gerar um JSON do periodo completo.

Na conversa com a IA, use datas no formato brasileiro:

```text
dd/mm/aaaa
```

No JSON final, mantenha datas no formato tecnico:

```text
aaaa-mm-dd
```

Horarios sempre em 24 horas:

```text
19:30
21:00
```

Formato esperado:

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
          "fim": "20:30",
          "titulo": "Nome claro do bloco",
          "descricao": "Acao pratica e especifica do que estudar/fazer",
          "modulo": "Faculdade",
          "materia": "Nome exato da materia",
          "artigo": null
        }
      ]
    }
  ]
}
```

## Alertas e calendario

Os alertas do app funcionam enquanto o navegador estiver aberto na tela `Hoje`.

Para alertas no celular, use a tela `Exportar` para baixar o arquivo `.ics` e importe/sincronize no seu calendario. Os eventos incluem alerta 10 minutos antes.

## Preparando para GitHub

Arquivos locais ignorados:

- `bin/`
- `obj/`
- `caderno.db`
- `caderno.db-shm`
- `caderno.db-wal`
- configuracoes locais de agentes/IDE

Antes de commitar:

```bash
dotnet build
git status
```

