# WitcherKM.Workflow

Заготовка приложения для описания цепочек действий: например, последовательности шагов по подключению к базе данных со скриншотами и пояснениями. Категории в будущем помогут находить цепочки. Правила создания, редактирования, хранения и поиска пока не определены и не реализованы.

## Структура

- `src/WitcherKM.Workflow.Domain` — место для будущей предметной модели.
- `src/WitcherKM.Workflow.Application` — MediatR и будущие сценарии, без зависимости от Infrastructure.
- `src/WitcherKM.Workflow.Infrastructure` — место для будущих реализаций портов и хранения.
- `src/WitcherKM.Workflow.Api` — HTTP-хост и композиция зависимостей.
- `scripts` — React-клиент, обращающийся к API.
- `docs/specs` — canonical specs и история изменений; `.agents/skills` — процедуры работы с ними.

## Локальный запуск

Требуются .NET SDK 10 и Node.js 20.19+ или 22.12+.

В одном терминале:

```powershell
dotnet run --project src/WitcherKM.Workflow.Api --urls http://127.0.0.1:5100
```

В другом терминале:

```powershell
cd scripts
npm ci
npm run dev
```

Открой `http://localhost:5173`. Vite направляет запросы `/api` на локальный API. `GET /api/health` — техническая проверка связи; предметных маршрутов пока нет.

## Проверки

```powershell
dotnet build WitcherKM.Workflow.sln
cd scripts
npm run lint
npm run build
```

Порядок работы со спецификациями описан в [docs/specs/README.md](docs/specs/README.md).
