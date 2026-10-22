import ChainsPage from "./pages/ChainsPage";
import CategoriesPage from "./pages/CategoriesPage";
import ChainPage from "./pages/ChainPage";
import StepPage from "./pages/StepPage";

function App() {
  const path = location.pathname;
  const step = path.match(/^\/chains\/([a-f0-9-]+)\/steps\/([a-f0-9-]+)\/?$/i);
  const chain = path.match(/^\/chains\/([a-f0-9-]+)\/?$/i);
  return (
    <div className="app-shell">
      <header className="site-header">
        <a href="/" className="brand">
          <span className="brand-mark">W</span>
          <span>
            WitcherKM<span className="brand-accent">.Workflow</span>
          </span>
        </a>
        <nav aria-label="Основная навигация">
          <a className={path === "/" ? "active" : ""} href="/">
            Цепочки
          </a>
          <a
            className={path.startsWith("/categories") ? "active" : ""}
            href="/categories"
          >
            Категории
          </a>
        </nav>
      </header>
      <main>
        {step ? (
          <StepPage chainId={step[1]} stepId={step[2]} />
        ) : chain ? (
          <ChainPage id={chain[1]} />
        ) : path.startsWith("/categories") ? (
          <CategoriesPage />
        ) : (
          <ChainsPage />
        )}
      </main>
      <footer className="site-footer">
        <span>WitcherKM.Workflow</span>
        <span>Личная библиотека действий</span>
      </footer>
    </div>
  );
}
export default App;
