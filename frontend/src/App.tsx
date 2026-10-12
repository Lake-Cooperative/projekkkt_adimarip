import { useEffect, useState } from 'react';

type ApiStatus = 'loading' | 'online' | 'offline';
const apiUrl = (import.meta.env.VITE_API_URL ?? 'http://localhost:8081').replace(/\/$/, '');

export function App() {
  const [apiStatus, setApiStatus] = useState<ApiStatus>('loading');

  useEffect(() => {
    const controller = new AbortController();
    fetch(`${apiUrl}/api/health`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error('HTTP error');
        const body: unknown = await response.json();
        if (typeof body !== 'object' || body === null || !('status' in body) || body.status !== 'ok') {
          throw new Error('Unexpected health response');
        }
        setApiStatus('online');
      })
      .catch(() => {
        if (!controller.signal.aborted) setApiStatus('offline');
      });
    return () => controller.abort();
  }, []);

  const statusText = {
    loading: 'Проверка соединения…',
    online: 'API доступен',
    offline: 'API недоступен',
  }[apiStatus];

  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand"><span className="brand-mark">А</span><span>АДИМАРИП</span></div>
        <span className="semester">Учебный проект · Лабораторная 2</span>
      </header>
      <main>
        <section className="hero" aria-labelledby="page-title">
          <div className="hero-copy">
            <span className="eyebrow">Кооператив «Озеро» / Реестр оповещений</span>
            <h1 id="page-title">Все оповещения —<br /><span>в одном реестре.</span></h1>
            <p>Демонстрационная система для учёта адресатов, оповещений и истории их обработки. Сейчас доступна стартовая страница; функциональные разделы появятся на следующих этапах.</p>
            <div className="hero-actions">
              <span className={`health-pill health-${apiStatus}`} role="status" aria-live="polite"><span className="health-dot" />{statusText}</span>
              <a href={`${apiUrl}/swagger`} target="_blank" rel="noreferrer">Документация API ↗</a>
            </div>
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
        <section className="next-section" aria-labelledby="next-title">
          <div className="section-head"><span className="eyebrow">ДАЛЬШЕ ПО ПЛАНУ</span><h2 id="next-title">Будущие возможности</h2></div>
          <div className="feature-grid">
            <article className="feature"><span className="feature-number">01</span><h3>Адресаты</h3><p>Карточки адресатов и их контактные данные.</p><span className="soon">Запланировано</span></article>
            <article className="feature"><span className="feature-number">02</span><h3>Оповещения</h3><p>Регистрация, поиск и просмотр карточек оповещений.</p><span className="soon">Запланировано</span></article>
            <article className="feature"><span className="feature-number">03</span><h3>История и аудит</h3><p>Отслеживание статусов и действий пользователей.</p><span className="soon">Запланировано</span></article>
          </div>
        </section>
      </main>
      <footer>АДИМАРИП · Демонстрационный стенд, только тестовые данные</footer>
    </div>
  );
}
