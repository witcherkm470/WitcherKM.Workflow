import { useEffect, useState, type FormEvent } from "react";
import { api, json, message, type Category } from "../api";
import { Title, Notice, ConfirmDelete } from "../components";

export default function CategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [name, setName] = useState("");
  const [newName, setNewName] = useState("");
  const [editing, setEditing] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [deleting, setDeleting] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    const query = new URLSearchParams();
    if (name) query.set("name", name);
    void api<Category[]>(`/categories?${query}`, { signal: controller.signal })
      .then((data) => {
        setCategories(data);
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
  }, [name, revision]);
  async function create(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await api("/categories", json({ name: newName }));
      setNewName("");
      setRevision((v) => v + 1);
    } catch (e) {
      setError(message(e));
    } finally {
      setBusy(false);
    }
  }
  async function rename(id: string, e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await api(`/categories/${id}`, json({ name: editName }, "PUT"));
      setEditing(null);
      setRevision((v) => v + 1);
    } catch (e) {
      setError(message(e));
    } finally {
      setBusy(false);
    }
  }
  async function remove(id: string) {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await api(`/categories/${id}`, { method: "DELETE" });
      setDeleting(null);
      setRevision((v) => v + 1);
    } catch (e) {
      setError(message(e));
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="page">
      <Title eyebrow="БИБЛИОТЕКА / КАТЕГОРИИ">Категории</Title>
      <p className="intro">
        Назовите области так, как удобно вам. Нажмите «Цепочки», чтобы
        посмотреть всё внутри категории.
      </p>
      <form className="panel create-row" onSubmit={create}>
        <label>
          Новая категория
          <input
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="Название категории"
          />
        </label>
        <button className="primary" disabled={busy || !newName.trim()}>
          Добавить
        </button>
      </form>
      <div className="filters">
        <label className="search-label">
          Поиск по имени
          <input
            type="search"
            value={name}
            onChange={(e) => {
              setName(e.target.value);
              setLoading(true);
            }}
            placeholder="Найти категорию"
          />
        </label>
      </div>
      {error && (
        <Notice retry={() => setRevision((v) => v + 1)}>{error}</Notice>
      )}
      {loading ? (
        <p className="muted">Загружаем категории…</p>
      ) : categories.length === 0 ? (
        <div className="empty">Категории не найдены.</div>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Название</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              {categories.map((c) => (
                <tr key={c.id}>
                  <td>
                    {editing === c.id ? (
                      <form
                        className="inline-form"
                        onSubmit={(e) => void rename(c.id, e)}
                      >
                        <input
                          value={editName}
                          onChange={(e) => setEditName(e.target.value)}
                          autoFocus
                        />
                        <button disabled={busy || !editName.trim()}>
                          Сохранить
                        </button>
                        <button
                          type="button"
                          className="ghost"
                          onClick={() => setEditing(null)}
                        >
                          Отмена
                        </button>
                      </form>
                    ) : (
                      <strong>{c.name}</strong>
                    )}
                  </td>
                  <td>
                    <div className="row-actions">
                      <a href={`/?categoryId=${c.id}`}>Цепочки →</a>
                      <button
                        className="text-button"
                        onClick={() => {
                          setEditing(c.id);
                          setEditName(c.name);
                          setDeleting(null);
                        }}
                      >
                        Изменить
                      </button>
                      <button
                        className="text-button danger-text"
                        onClick={() => {
                          setDeleting(deleting === c.id ? null : c.id);
                          setEditing(null);
                        }}
                      >
                        Удалить
                      </button>
                    </div>
                    {deleting === c.id && (
                      <ConfirmDelete
                        busy={busy}
                        onConfirm={() => void remove(c.id)}
                      />
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
