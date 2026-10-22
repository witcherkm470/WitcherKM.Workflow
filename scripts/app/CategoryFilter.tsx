import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import type { Category } from "./api";

type Props = {
  categories: Category[];
  value: string;
  onChange: (value: string) => void;
};

export default function CategoryFilter({ categories, value, onChange }: Props) {
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const optionRefs = useRef<(HTMLButtonElement | null)[]>([]);
  const id = useId();
  const options = [{ id: "", name: "Все категории" }, ...categories];
  const selectedIndex = Math.max(
    0,
    options.findIndex((option) => option.id === value),
  );

  useEffect(() => {
    if (open) optionRefs.current[activeIndex]?.focus();
  }, [open, activeIndex]);

  useEffect(() => {
    if (!open) return;
    const closeOutside = (event: PointerEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("pointerdown", closeOutside);
    return () => document.removeEventListener("pointerdown", closeOutside);
  }, [open]);

  function openMenu() {
    setActiveIndex(selectedIndex);
    setOpen(true);
  }

  function choose(index: number) {
    if (options[index].id !== value) onChange(options[index].id);
    setOpen(false);
    triggerRef.current?.focus();
  }

  function onTriggerKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      openMenu();
    }
  }

  function onOptionKeyDown(
    event: KeyboardEvent<HTMLButtonElement>,
    index: number,
  ) {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex(
        (index + (event.key === "ArrowDown" ? 1 : -1) + options.length) %
          options.length,
      );
    } else if (event.key === "Home" || event.key === "End") {
      event.preventDefault();
      setActiveIndex(event.key === "Home" ? 0 : options.length - 1);
    } else if (event.key === "Escape") {
      event.preventDefault();
      setOpen(false);
      triggerRef.current?.focus();
    }
  }

  return (
    <div
      className="category-filter"
      ref={rootRef}
      onBlurCapture={(event) => {
        if (!rootRef.current?.contains(event.relatedTarget)) setOpen(false);
      }}
    >
      <span className="filter-label" id={`${id}-label`}>
        Категория
      </span>
      <button
        type="button"
        className="category-filter-trigger"
        ref={triggerRef}
        id={`${id}-trigger`}
        aria-labelledby={`${id}-label ${id}-trigger`}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? `${id}-options` : undefined}
        onClick={() => (open ? setOpen(false) : openMenu())}
        onKeyDown={onTriggerKeyDown}
      >
        <span className="category-filter-value">
          {options[selectedIndex].name}
        </span>
        <span
          className={`category-filter-chevron${open ? " is-open" : ""}`}
          aria-hidden="true"
        />
      </button>
      {open && (
        <div
          className="category-filter-menu"
          id={`${id}-options`}
          role="listbox"
          aria-labelledby={`${id}-label`}
        >
          {options.map((option, index) => (
            <button
              type="button"
              className="category-filter-option"
              key={option.id}
              role="option"
              aria-selected={index === selectedIndex}
              tabIndex={index === activeIndex ? 0 : -1}
              ref={(element) => {
                optionRefs.current[index] = element;
              }}
              onClick={() => choose(index)}
              onKeyDown={(event) => onOptionKeyDown(event, index)}
            >
              <span>{option.name}</span>
              {index === selectedIndex && (
                <span className="category-filter-check" aria-hidden="true">
                  ✓
                </span>
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
