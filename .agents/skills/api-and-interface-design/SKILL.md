---
name: api-and-interface-design
description: Сверяет или изменяет HTTP и прикладные контракты Workflow, DTO и frontend-клиент по утверждённому сценарию.
---

Начни с `docs/specs/application/<область>.md` и связанной domain spec. Проследи маршрут API, request/handler, обработку ошибок и вызов клиента в `scripts/`. Зафиксируй вход, результат, отсутствие объекта, отказ и сохранность данных при отказе.

Перед реализацией закрепи контракт в canonical через [spec-management](../spec-management/SKILL.md). Не заимствуй контракты Planner, pagination, формат ошибок или idempotency как неявные требования.

Валидация HTTP-входа не заменяет локальные доменные инварианты. Проследи всех потребителей изменённого поля. Проверь round trip, ошибку и фактический HTTP-маршрут по [testing](../testing/SKILL.md).
