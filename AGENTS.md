# Правила работы в WitcherKM.Workflow

- `src/` — backend, `scripts/` — React-клиент, `tests/` — будущие проверки. Перед изменением изучай `git status` и diff; сохраняй пользовательские изменения.
- [Canonical specs](docs/specs/README.md) — источник согласованного поведения. `domain` хранит словарь и инварианты, `application` — сценарии, `ui` — наблюдаемые действия. Не описывай как требование случайную особенность кода.
- [Архитектура](docs/architecture/overview.md) задаёт технические границы, [дизайн](docs/design/frontend.md) — визуальные. Skills задают процедуры, а не дублируют требования.
- Порядок работы: `docs/specs/changes/active/<change>.md` → canonical specs → код → проверки → `changes/archive`. Незавершённое остаётся active. Продуктовые неоднозначности фиксируй в [questions](docs/specs/questions.md), продолжая независимую работу.
- Основной маршрут — [implement-change](.agents/skills/implement-change/SKILL.md). По задаче подключай [spec-management](.agents/skills/spec-management/SKILL.md), [domain-modeling](.agents/skills/domain-modeling/SKILL.md), [testing](.agents/skills/testing/SKILL.md) и другие профильные skills. Не загружай все процедуры заранее.
- Не подгоняй существующие тесты ради зелёного прогона. Команды и уровни проверки — в [architecture/testing](docs/architecture/testing.md).
- Сейчас реализован только каркас. Не добавляй бизнес-сущности, сценарии цепочек, шагов или категорий без отдельной согласованной spec.
