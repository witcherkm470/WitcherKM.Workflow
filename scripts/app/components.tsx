import type { ReactNode } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import "highlight.js/styles/github.css";
import type { Category } from "./api";

export function Title({
  eyebrow,
  children,
  aside,
}: {
  eyebrow: string;
  children: ReactNode;
  aside?: ReactNode;
}) {
  return (
    <div className="page-heading">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h1>{children}</h1>
      </div>
      {aside}
    </div>
  );
}
export function Notice({
  children,
  retry,
}: {
  children: ReactNode;
  retry?: () => void;
}) {
  return (
    <div className="notice" role="alert">
      {children}
      {retry && (
        <button className="text-button" onClick={retry}>
          Повторить
        </button>
      )}
    </div>
  );
}
export function Markdown({ text }: { text: string }) {
  return (
    <div className="markdown">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeHighlight]}
      >
        {text || "*Описание пока пусто.*"}
      </ReactMarkdown>
    </div>
  );
}
export function ConfirmDelete({
  onConfirm,
  busy,
  label = "Да, удалить",
}: {
  onConfirm: () => void;
  busy: boolean;
  label?: string;
}) {
  return (
    <div className="confirm" role="group" aria-label="Подтверждение удаления">
      <strong>Точно удалить?</strong>
      <button className="danger" onClick={onConfirm} disabled={busy}>
        {label}
      </button>
    </div>
  );
}
export function CategoryChoices({
  categories,
  selected,
  onChange,
}: {
  categories: Category[];
  selected: string[];
  onChange: (ids: string[]) => void;
}) {
  return (
    <fieldset className="choice-field">
      <legend>
        Категории <span className="required">*</span>
      </legend>
      <div className="choice-list">
        {categories.map((c) => (
          <label className="choice" key={c.id}>
            <input
              type="checkbox"
              checked={selected.includes(c.id)}
              onChange={(e) =>
                onChange(
                  e.target.checked
                    ? [...selected, c.id]
                    : selected.filter((id) => id !== c.id),
                )
              }
            />
            {c.name}
          </label>
        ))}
      </div>
      {categories.length === 0 && (
        <p className="muted">Сначала создайте категорию в справочнике.</p>
      )}
    </fieldset>
  );
}
