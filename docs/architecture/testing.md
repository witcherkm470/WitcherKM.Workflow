# Проверки

Для затронутого backend: `dotnet build WitcherKM.Workflow.sln` и `dotnet run --project tests/ImageCacheSmoke.csproj` для кэша изображений. Для React в `scripts/`: `npm run lint`, `npm run build`. Поведение новых HTTP-сценариев проверяет `node tests/http-smoke.mjs` против запущенного API на отдельной временной базе (`WORKFLOW_API_URL` задаёт адрес, по умолчанию `http://127.0.0.1:52771`). Скрипт проверяет создание, отказ, фильтрацию, порядок шагов и изображения. Браузерный проход проверяет реальный React с API и Vite proxy. Docker проверяется через `docker compose config`, сборку целевых образов `test` и HTTP-ответы после запуска.

Не повторяй проверки функциональности, которую изменение не затронуло. HTTP, браузер и сборка доказывают разные вещи. Правило сохранности существующих тестов — в [testing skill](../../.agents/skills/testing/SKILL.md).
