import { useEffect, useState, type FormEvent } from "react";
import {
  api,
  json,
  message,
  type Category,
  type Chain,
  type ChainList,
} from "../api";
import { Title, Notice, CategoryChoices } from "../components";

export default function ChainsPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [chains, setChains] = useState<ChainList[]>([]);
  const [name, setName] = useState("");
  const [categoryId, setCategoryId] = useState(
    new URLSearchParams(location.search).get("categoryId") ?? "",
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [revision, setRevision] = useState(0);
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [selected, setSelected] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [formError, setFormError] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    void api<Category[]>("/categories", { signal: controller.signal })
      .then(setCategories)
      .catch((e) => {
        if (!controller.signal.aborted) setError(message(e));
      });
    return () => controller.abort();
  }, [revision]);
  useEffect(() => {
    const controller = new AbortController();
    const query = new URLSearchParams();
    if (name) query.set("name", name);
    if (categoryId) query.set("categoryId", categoryId);
    void api<ChainList[]>(`/chains?${query}`, { signal: controller.signal })
      .then((data) => {
        setChains(data);
        setLoading(false);
        setError("");
      })
      .catch((e) => {
        if (!controller.signal.aborted) {
          setError(message(e));
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [name, categoryId, revision]);

  async function create(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setFormError("");
    try {
      const chain = await api<Chain>(
        "/chains",
        json({ name: newName, categoryIds: selected }),
      );
      location.href = `/chains/${chain.id}`;
    } catch (e) {
      setFormError(message(e));
      setBusy(false);
    }
  }

  return (
    <div className="page">
      <Title
        eyebrow="БИБЛИОТЕКА / ЦЕПОЧКИ"
        aside={
          <button className="primary" onClick={() => setCreating((v) => !v)}>
            {creating ? "Закрыть" : "+ Новая цепочка"}
          </button>
        }
      >
        Цепочки действий
      </Title>
      <p className="intro">
        Сохраняйте последовательности, к которым нужно возвращаться. Каждая
        цепочка может объединять несколько категорий.
      </p>
      {creating && (
        <form className="panel form-panel" onSubmit={create}>
          <h2>Новая цепочка</h2>
          <label>
            Название <span className="required">*</span>
            <input
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Например, Подключение к БД"
              autoFocus
            />
          </label>
          <CategoryChoices
            categories={categories}
            selected={selected}
            onChange={setSelected}
          />
          {formError && <Notice>{formError}</Notice>}
          <div className="actions">
            <button
              className="primary"
              disabled={busy || !newName.trim() || selected.length === 0}
            >
              Создать цепочку
            </button>
          </div>
        </form>
      )}
      <div className="filters">
        <label className="search-label">
          Поиск по названию
          <input
            type="search"
            value={name}
            onChange={(e) => {
              setName(e.target.value);
              setLoading(true);
            }}
            placeholder="Найти цепочку"
          />
        </label>
        <label>
          Категория
          <select
            value={categoryId}
            onChange={(e) => {
              setCategoryId(e.target.value);
              setLoading(true);
            }}
          >
            <option value="">Все категории</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>
      </div>
      {error && (
        <Notice
          retry={() => {
            setLoading(true);
            setRevision((v) => v + 1);
          }}
        >
          {error}
        </Notice>
      )}
      {loading ? (
        <p className="muted">Загружаем цепочки…</p>
      ) : chains.length === 0 ? (
        <div className="empty">
          Цепочки не найдены. Попробуйте другой поиск или создайте новую.
        </div>
      ) : (
        <div className="chain-grid">
          {chains.map((c) => (
            <a className="chain-card" href={`/chains/${c.id}`} key={c.id}>
              <div className="chain-card-top">
                <span className="card-symbol">↗</span>
                <span className="muted">{c.stepCount} шагов</span>
              </div>
              <h2>{c.name}</h2>
              <div className="chips">
                {c.categories.map((x) => (
                  <span className="chip" key={x.id}>
                    {x.name}
                  </span>
                ))}
              </div>
              <span className="card-link">Открыть цепочку →</span>
            </a>
          ))}
        </div>
      )}
    </div>
  );
}
