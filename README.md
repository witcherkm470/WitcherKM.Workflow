# WitcherKM.Workflow

Личное локальное приложение для цепочек действий. Стартовая страница показывает цепочки с поиском по названию и фильтром по категориям. В цепочке можно вести упорядоченные шаги с Markdown, кодом и скриншотами. Категории управляются в отдельном справочнике.

## Структура

- `src/WitcherKM.Workflow.Domain` — модель и инварианты.
- `src/WitcherKM.Workflow.Application` — MediatR-сценарии и порты.
- `src/WitcherKM.Workflow.Infrastructure` — SQLite, миграции и файлы изображений.
- `src/WitcherKM.Workflow.Api` — HTTP-хост.
- `scripts` — React-клиент через API.
- `docs/specs` — canonical specs и история изменений; `.agents/skills` — процедуры.

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

Открой `http://localhost:5173`. Vite направляет `/api` на локальный API. При первом запуске миграция создаёт SQLite в `src/WitcherKM.Workflow.Api/App_Data/workflow.db`; скриншоты хранятся в `App_Data/images`. Сохраняйте эту папку при переносе или обновлении приложения.

## Проверки

```powershell
dotnet build WitcherKM.Workflow.sln
cd scripts
npm run lint
npm run build
```

Для HTTP-проверки новых сценариев запустите API на отдельной временной базе и выполните `node tests/http-smoke.mjs` из корня репозитория. Подробности — в [документации проверок](docs/architecture/testing.md).

Правила продукта — в [спецификациях](docs/specs/README.md).

## Docker

Для отдельного запуска Workflow нужен Docker Desktop с Docker Compose. Из папки проекта выполните:

```powershell
powershell -ExecutionPolicy Bypass -File .\docker.ps1
```

Скрипт проверяет образы, собирает и запускает API и UI. Откройте `http://localhost:8081`; Planner на порту 8080 не затрагивается. UI и API доступны только с этого компьютера. SQLite и изображения сохраняются в томе `workflow-data` при пересоздании контейнеров. Исходники и инструменты .NET/Node.js на хосте для этого запуска не нужны.

`.\docker.ps1 check` проверяет сборку без запуска, `.\docker.ps1 stop` останавливает контейнеры с сохранением данных, `.\docker.ps1 logs` показывает логи.
