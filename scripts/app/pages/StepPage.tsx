import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type ClipboardEvent,
} from "react";
import { api, json, message, type Chain, type Step } from "../api";
import { Title, Notice, Markdown, ConfirmDelete } from "../components";

export default function StepPage({
  chainId,
  stepId,
}: {
  chainId: string;
  stepId: string;
}) {
  const [chain, setChain] = useState<Chain | null>(null);
  const [step, setStep] = useState<Step | null>(null);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [busy, setBusy] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState("");
  const [warnings, setWarnings] = useState<string[]>([]);
  const [revision, setRevision] = useState(0);
  const editor = useRef<HTMLTextAreaElement>(null);
  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      api<Chain>(`/chains/${chainId}`, { signal: controller.signal }),
      api<Step>(`/chains/${chainId}/steps/${stepId}`, {
        signal: controller.signal,
      }),
    ])
      .then(([c, s]) => {
        setChain(c);
        setStep(s);
        setTitle(s.title ?? "");
        setDescription(s.description);
        setError("");
      })
      .catch((e) => {
        if (!controller.signal.aborted) setError(message(e));
      });
    return () => controller.abort();
  }, [chainId, stepId, revision]);
  async function save(e: FormEvent) {
    e.preventDefault();
    if (busy || uploading) return;
    setBusy(true);
    setError("");
    setWarnings([]);
    try {
      const saved = await api<Step>(
        `/chains/${chainId}/steps/${stepId}`,
        json({ title, description }, "PUT"),
      );
      setWarnings(saved.imageWarnings);
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
      await api(`/chains/${chainId}/steps/${stepId}`, { method: "DELETE" });
      location.href = `/chains/${chainId}`;
    } catch (e) {
      setError(message(e));
      setBusy(false);
    }
  }
  async function paste(e: ClipboardEvent<HTMLTextAreaElement>) {
    const image =
      Array.from(e.clipboardData.files).find((file) =>
        file.type.startsWith("image/"),
      ) ??
      Array.from(e.clipboardData.items)
        .find((item) => item.type.startsWith("image/"))
        ?.getAsFile();
    if (!image) {
      const value = e.clipboardData.getData("text/plain").trim();
      if (!/^https?:\/\/\S+$/i.test(value)) return;
      e.preventDefault();
      const start = editor.current?.selectionStart ?? description.length;
      const end = editor.current?.selectionEnd ?? start;
      const link = `![Изображение](${value})`;
      setDescription(
        (current) => current.slice(0, start) + link + current.slice(end),
      );
      requestAnimationFrame(() => {
        editor.current?.focus();
        editor.current?.setSelectionRange(
          start + link.length,
          start + link.length,
        );
      });
      return;
    }
    e.preventDefault();
    setUploading(true);
    setError("");
    const start = editor.current?.selectionStart ?? description.length;
    const end = editor.current?.selectionEnd ?? start;
    const data = new FormData();
    data.append("image", image);
    try {
      const result = await api<{ url: string }>("/images", {
        method: "POST",
        body: data,
      });
      const link = `![Скриншот](${result.url})`;
      setDescription(
        (current) => current.slice(0, start) + link + current.slice(end),
      );
      requestAnimationFrame(() => {
        editor.current?.focus();
        editor.current?.setSelectionRange(
          start + link.length,
          start + link.length,
        );
      });
    } catch (err) {
      setError(message(err));
    } finally {
      setUploading(false);
    }
  }
  return (
    <div className="page">
      <a className="back" href={`/chains/${chainId}`}>
        ← {chain?.name ?? "К цепочке"}
      </a>
      {error && (
        <Notice retry={() => setRevision((v) => v + 1)}>{error}</Notice>
      )}
      {warnings.length > 0 && (
        <Notice>
          <strong>
            Шаг сохранён, но некоторые изображения остались по внешней ссылке:
          </strong>
          <ul>
            {warnings.map((warning) => (
              <li key={warning}>{warning}</li>
            ))}
          </ul>
        </Notice>
      )}
      {!step ? (
        error ? null : (
          <p className="muted">Загружаем шаг…</p>
        )
      ) : (
        <>
          <Title
            eyebrow={`ШАГ ${String(step.number).padStart(2, "0")} / КАРТОЧКА`}
            aside={
              <div className="actions">
                <button
                  className="ghost"
                  onClick={() => {
                    if (editing) {
                      setTitle(step.title ?? "");
                      setDescription(step.description);
                    }
                    setEditing((v) => !v);
                  }}
                >
                  {editing ? "Отмена" : "Редактировать"}
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
            {step.displayTitle}
          </Title>
          {deleting && (
            <ConfirmDelete
              busy={busy}
              onConfirm={() => void remove()}
              label="Да, удалить шаг"
            />
          )}
          {editing ? (
            <form className="step-editor" onSubmit={save}>
              <div className="panel editor-pane">
                <label>
                  Название шага <span className="muted">(необязательно)</span>
                  <input
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    placeholder={`Шаг ${step.number}`}
                  />
                </label>
                <label>
                  Описание Markdown
                  <textarea
                    ref={editor}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    onPaste={(e) => void paste(e)}
                    placeholder="Опишите действие, вставьте скриншот или блок кода…"
                    rows={18}
                  />
                </label>
                <p className="hint">
                  Скриншот и ссылку на изображение можно вставить из буфера.
                  Внешние изображения сохраняются локально при сохранении шага.
                  Для кода используйте тройные обратные кавычки и язык, например
                  cs.
                </p>
                <button className="primary" disabled={busy || uploading}>
                  {uploading ? "Загружаем скриншот…" : "Сохранить шаг"}
                </button>
              </div>
              <div className="panel preview-pane">
                <h2>Предпросмотр</h2>
                <Markdown text={description} />
              </div>
            </form>
          ) : (
            <div className="panel reading-pane">
              <Markdown text={step.description} />
            </div>
          )}
          <div className="step-navigation">
            {chain?.steps
              .filter((s) => Math.abs(s.number - step.number) === 1)
              .map((s) => (
                <a key={s.id} href={`/chains/${chainId}/steps/${s.id}`}>
                  {s.number < step.number ? "← " : "→ "}
                  {s.displayTitle}
                </a>
              ))}
          </div>
        </>
      )}
    </div>
  );
}
