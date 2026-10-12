export function App() {
  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand"><span className="brand-mark">А</span><span>АДИМАРИП</span></div>
        <span className="semester">Учебный проект · Семинар 2</span>
      </header>
      <main>
        <section className="hero" aria-labelledby="page-title">
          <div className="hero-copy">
            <span className="eyebrow">Кооператив «Озеро» / Реестр оповещений</span>
            <h1 id="page-title">Все оповещения —<br /><span>в одном реестре.</span></h1>
            <p>Демонстрационная система для учёта адресатов, оповещений и истории их обработки. Сейчас доступна только стартовая страница.</p>
          </div>
          <div className="hero-panel" aria-label="Состояние проекта">
            <span className="panel-label">КАРКАС СИСТЕМЫ</span>
            <span className="panel-big">02<span> / учебный этап</span></span>
            <div className="panel-rule" />
            <div className="panel-row"><span>Frontend</span><strong>React · TypeScript</strong></div>
            <div className="panel-row"><span>Backend</span><strong>ASP.NET Core</strong></div>
            <div className="panel-row"><span>Реестр</span><strong>На следующем этапе</strong></div>
          </div>
        </section>
      </main>
      <footer>АДИМАРИП · Демонстрационный стенд, только тестовые данные</footer>
    </div>
  );
}
