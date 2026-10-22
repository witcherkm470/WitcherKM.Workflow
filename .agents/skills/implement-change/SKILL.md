---
name: implement-change
description: Реализует согласованное изменение Workflow или исправляет подтверждённое расхождение с canonical specs; выбирает нужные процедуры вместо загрузки всех skills.
---

Прочитай AGENTS.md, запрос, git status/diff и релевантный срез specs/architecture. Сохрани изменения пользователя. Источник поведения — canonical; баг не разрешает переписать требование.

1. Через [spec-management](../spec-management/SKILL.md) зафиксируй change и критерии, затем обнови затронутые canonical specs **до кода** в пределах запроса. Для bug fix достаточно ссылки на неизменённое требование. Неоднозначное запиши в questions и продолжай независимую часть.
2. Выбери только нужные маршруты: [domain-modeling](../domain-modeling/SKILL.md) для инвариантов; [api-and-interface-design](../api-and-interface-design/SKILL.md) для контрактов; [frontend-ui-engineering](../frontend-ui-engineering/SKILL.md) для страниц/форм; [incremental-implementation](../incremental-implementation/SKILL.md) для нескольких срезов.
3. Следуй [testing](../testing/SKILL.md); при падении используй [debugging-and-error-recovery](../debugging-and-error-recovery/SKILL.md). После реализации выполни [code-review-and-quality](../code-review-and-quality/SKILL.md).
4. Свяжи каждый критерий с наблюдаемым результатом. Spec-management архивирует только завершённое. Сообщи изменения, реальные проверки, границы и оставшиеся вопросы.
