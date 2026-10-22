# Проверки

Для текущего каркаса:

- `dotnet build WitcherKM.Workflow.sln` — компиляция backend и проверка project references.
- В `scripts/`: `npm run lint`, `npm run build`.
- Запуск API и `GET /api/health` — реальный HTTP-хост. Через Vite проверяется проксирование `/api` и показ ответа в React.

Когда появятся сценарии, добавляй проверки наблюдаемого результата и отказа на подходящем уровне: domain unit, application integration, HTTP integration и UI. Моки клиента, браузер и настоящий HTTP-хост дают разные доказательства. Правило сохранности существующих тестов — в [testing skill](../../.agents/skills/testing/SKILL.md).
