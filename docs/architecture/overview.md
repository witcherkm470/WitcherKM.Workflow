# Обзор архитектуры

## Проекты

| Проект | Ответственность |
| --- | --- |
| `WitcherKM.Workflow.Domain` | Будущая предметная модель и локальные инварианты. Сейчас пуст. |
| `WitcherKM.Workflow.Application` | Будущие команды, запросы, обработчики MediatR и порты. Сейчас только регистрация MediatR. |
| `WitcherKM.Workflow.Infrastructure` | Будущие реализации портов, база данных и интеграции. Сейчас пуст. |
| `WitcherKM.Workflow.Api` | HTTP-хост, технический health endpoint и composition root. |
| `scripts/` | React-клиент, обращающийся к API через `/api`. |

Направление project references:

```text
Api ──→ Application ──→ Domain
 │
 └────→ Infrastructure ──→ Application, Domain
```

Domain не зависит от других проектов. Application не зависит от Infrastructure или Api. Infrastructure реализует контракты Application при появлении хранения. Api связывает слои через DI. Пользовательские сценарии будут проходить через MediatR, но пока их нет.

Технический `GET /api/health` проверяет доступность HTTP-хоста и не содержит предметного поведения. UI использует его для проверки связи. Клиент не подключается к базе напрямую.

При развитии модели сохраняй эти границы; выбор БД, файлового хранилища и формата вложений требует отдельного решения. Не копируй связанность `Application → Infrastructure` из текущего Planner.
