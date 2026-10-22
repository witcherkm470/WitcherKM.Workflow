import { useEffect, useState, type FormEvent, type DragEvent } from "react";
import {
  api,
  json,
  message,
  type Category,
  type Chain,
  type Step,
} from "../api";
import { Title, Notice, CategoryChoices, ConfirmDelete } from "../components";

export default function ChainPage({ id }: { id: string }) {
  const [chain, setChain] = useState<Chain | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [name, setName] = useState("");
  const [selected, setSelected] = useState<string[]>([]);
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [revision, setRevision] = useState(0);
  const [dragId, setDragId] = useState<string | null>(null);
  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      api<Chain>(`/chains/${id}`, { signal: controller.signal }),
      api<Category[]>("/categories", { signal: controller.signal }),
    ])
      .then(([data, cats]) => {
        setChain(data);
        setCategories(cats);
        setName(data.name);
        setSelected(data.categories.map((c) => c.id));
        setError("");
      })
      .catch((e) => {
        if (!controller.signal.aborted) setError(message(e));
      });
    return () => controller.abort();
  }, [id, revision]);
  async function save(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await api(`/chains/${id}`, json({ name, categoryIds: selected }, "PUT"));
      setEditing(false);
      setRevision((v) => v + 1);
    } catch (e) {
      setError(message(e));
    } finally {
      setBusy(false);
    }
  }
  async function remove() {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await api(`/chains/${id}`, { method: "DELETE" });
      location.href = "/";
    } catch (e) {
      setError(message(e));
      setBusy(false);
    }
  }
  async function addStep() {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      const step = await api<Step>(
        `/chains/${id}/steps`,
        json({ title: null, description: "" }),
      );
      location.href = `/chains/${id}/steps/${step.id}`;
    } catch (e) {
      setError(message(e));
      setBusy(false);
    }
  }
  async function move(stepId: string, position: number) {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await api(
        `/chains/${id}/steps/${stepId}/position`,
        json({ position }, "PUT"),
      );
      setRevision((v) => v + 1);
    } catch (e) {
      setError(message(e));
    } finally {
      setBusy(false);
      setDragId(null);
    }
  }
  function drop(event: DragEvent, position: number) {
    event.preventDefault();
    if (dragId) void move(dragId, position);
  }
  return (
    <div className="page">
      <a className="back" href="/">
        ← Все цепочки
      </a>
      {error && (
        <Notice retry={() => setRevision((v) => v + 1)}>{error}</Notice>
      )}
      {!chain ? (
        error ? null : (
          <p className="muted">Загружаем цепочку…</p>
        )
      ) : (
        <>
          <Title
            eyebrow="КАРТОЧКА ЦЕПОЧКИ"
            aside={
              <div className="actions">
                <button
                  className="ghost"
                  onClick={() => {
                    if (editing) {
                      setName(chain.name);
                      setSelected(chain.categories.map((c) => c.id));
                    }
                    setEditing((v) => !v);
                  }}
                >
                  {editing ? "Отмена" : "Настроить"}
                </button>
                <button
                  className="ghost danger-text"
                  onClick={() => setDeleting((v) => !v)}
                >
                  Удалить
                </button>
              </div>
            }
          >
            {chain.name}
          </Title>
          <div className="chips page-chips">
            {chain.categories.map((c) => (
              <a className="chip" href={`/?categoryId=${c.id}`} key={c.id}>
                {c.name}
              </a>
            ))}
          </div>
          {deleting && (
            <ConfirmDelete
              busy={busy}
              onConfirm={() => void remove()}
              label="Да, удалить цепочку и шаги"
            />
          )}
          {editing && (
            <form className="panel form-panel" onSubmit={save}>
              <h2>Настройки цепочки</h2>
              <label>
                Название
                <input value={name} onChange={(e) => setName(e.target.value)} />
              </label>
              <CategoryChoices
                categories={categories}
                selected={selected}
                onChange={setSelected}
              />
              <button
                className="primary"
                disabled={busy || !name.trim() || selected.length === 0}
              >
                Сохранить
              </button>
            </form>
          )}
          <div className="section-heading">
            <div>
              <p className="eyebrow">ПОСЛЕДОВАТЕЛЬНОСТЬ</p>
              <h2>
                Шаги <span className="count">{chain.steps.length}</span>
              </h2>
            </div>
            <button
              className="primary"
              onClick={() => void addStep()}
              disabled={busy}
            >
              + Добавить шаг
            </button>
          </div>
          {chain.steps.length === 0 ? (
            <div className="empty">
              Пока нет шагов. Добавьте первый, когда будете готовы.
            </div>
          ) : (
            <div className="step-list">
              {chain.steps.map((s, i) => (
                <article
                  key={s.id}
                  className="step-row"
                  draggable
                  onDragStart={() => setDragId(s.id)}
                  onDragOver={(e) => e.preventDefault()}
                  onDrop={(e) => drop(e, i + 1)}
                  onDragEnd={() => setDragId(null)}
                >
                  <span className="step-index">
                    {String(s.number).padStart(2, "0")}
                  </span>
                  <div className="step-summary">
                    <a href={`/chains/${id}/steps/${s.id}`}>{s.displayTitle}</a>
                    <p>
                      {s.description
                        ? s.description
                            .replace(/[#*`![\]()]/g, "")
                            .slice(0, 120)
                        : "Описание пока пусто"}
                    </p>
                  </div>
                  <div className="step-actions">
                    <button
                      className="icon-button"
                      aria-label={`Поднять ${s.displayTitle}`}
                      disabled={busy || i === 0}
                      onClick={() => void move(s.id, i)}
                    >
                      ↑
                    </button>
                    <button
                      className="icon-button"
                      aria-label={`Опустить ${s.displayTitle}`}
                      disabled={busy || i === chain.steps.length - 1}
                      onClick={() => void move(s.id, i + 2)}
                    >
                      ↓
                    </button>
                    <span className="drag-hint" title="Перетащить">
                      ⋮⋮
                    </span>
                  </div>
                </article>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}
