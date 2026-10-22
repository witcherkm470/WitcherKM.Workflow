import { useEffect, useState } from 'react'

type ApiStatus = 'checking' | 'online' | 'offline'

function App() {
  const [apiStatus, setApiStatus] = useState<ApiStatus>('checking')

  useEffect(() => {
    const controller = new AbortController()

    async function checkApi() {
      try {
        const response = await fetch('/api/health', { signal: controller.signal })
        if (!response.ok) throw new Error('API unavailable')

        const body: unknown = await response.json()
        if (
          typeof body !== 'object' ||
          body === null ||
          !('status' in body) ||
          body.status !== 'ok'
        ) {
          throw new Error('Unexpected API response')
        }

        setApiStatus('online')
      } catch {
        if (!controller.signal.aborted) setApiStatus('offline')
      }
    }

    void checkApi()
    return () => controller.abort()
  }, [])

  const statusText = {
    checking: 'Проверяем связь с API',
    online: 'API доступен',
    offline: 'Нет связи с API',
  }[apiStatus]

  return (
    <div className="app-shell">
      <header className="site-header">
        <div className="brand" aria-label="WitcherKM.Workflow">
          <span className="brand-mark" aria-hidden="true">W</span>
          <span>WitcherKM<span className="brand-accent">.Workflow</span></span>
        </div>
        <span className={`api-status api-status--${apiStatus}`} role="status" aria-live="polite">
          <span className="status-dot" aria-hidden="true" />
          {statusText}
        </span>
      </header>

      <main>
        <section className="hero" aria-labelledby="hero-title">
          <p className="eyebrow">РАБОЧЕЕ ПРОСТРАНСТВО ДЛЯ ИНСТРУКЦИЙ</p>
          <h1 id="hero-title">Порядок действий,<br /><em>который легко повторить.</em></h1>
          <p className="hero-description">
            Здесь будут храниться цепочки шагов для практических задач — от настройки
            подключения к базе данных до любых процессов, к которым хочется возвращаться.
          </p>
          <div className="hero-note">
            <span className="note-line" aria-hidden="true" />
            Каркас готов. Сценарии работы с цепочками появятся после согласования спецификаций.
          </div>
        </section>

        <section className="preview" aria-labelledby="preview-title">
          <div className="section-heading">
            <div>
              <p className="eyebrow">НАПРАВЛЕНИЕ ПРОЕКТА</p>
              <h2 id="preview-title">Что будет внутри</h2>
            </div>
            <span className="section-caption">01 / ОСНОВА</span>
          </div>
          <div className="preview-grid">
            <article className="preview-card">
              <span className="card-number">01</span>
              <h3>Цепочки</h3>
              <p>Отдельные инструкции для результатов, к которым нужно прийти шаг за шагом.</p>
            </article>
            <article className="preview-card">
              <span className="card-number">02</span>
              <h3>Шаги</h3>
              <p>Понятный порядок действий с пояснениями и примерами.</p>
            </article>
            <article className="preview-card">
              <span className="card-number">03</span>
              <h3>Категории</h3>
              <p>Группировка, которая поможет быстрее находить нужные цепочки.</p>
            </article>
          </div>
        </section>
      </main>

      <footer className="site-footer">
        <span>WitcherKM.Workflow</span>
        <span>Основа для следующих шагов</span>
      </footer>
    </div>
  )
}

export default App
