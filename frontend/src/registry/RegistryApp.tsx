import { useEffect, useState, type FormEvent } from 'react';
import { api, apiBase, loadSession, saveSession, type Session } from './api';
import './registry.css';
import { RecipientsPanel } from './RecipientsPanel';
import { NoticesPanel } from './NoticesPanel';
import { NoticeDetailPanel } from './NoticeDetailPanel';
import { CreateNoticePanel } from './CreateNoticePanel';
import { AdminPanel } from './AdminPanel';

type Section = 'notices' | 'recipients' | 'admin';

export function RegistryApp() {
  const [session, setSession] = useState<Session | null>(loadSession);
  const [section, setSection] = useState<Section>(() => loadSession()?.role === 'AutomationEngineer' ? 'admin' : 'notices');
  const [noticeView, setNoticeView] = useState<'list' | 'create' | 'detail'>('list');
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [health, setHealth] = useState<'loading' | 'ok' | 'error'>('loading');

  useEffect(() => {
    const expire = () => {
      setSession(null);
      setSelectedId(null);
      setNoticeView('list');
      setSection('notices');
    };
    window.addEventListener('adimarip:session-expired', expire);
    return () => window.removeEventListener('adimarip:session-expired', expire);
  }, []);

  useEffect(() => {
    fetch(`${apiBase}/api/health`)
      .then(response => setHealth(response.ok ? 'ok' : 'error'))
      .catch(() => setHealth('error'));
  }, []);

  function enter(next: Session) { saveSession(next); setSession(next); setSection(next.role === 'AutomationEngineer' ? 'admin' : 'notices'); }
  function logout() { saveSession(null); setSession(null); setSection('notices'); setNoticeView('list'); setSelectedId(null); }

  return <div className="registry-root">
    <header className="registry-header">
      <div className="registry-brand"><span className="registry-monogram">А</span><div><strong>АДИМАРИП</strong><small>Единый реестр оповещений · учебная версия</small></div></div>
      <div className="header-end"><span className={`service-badge ${health}`}>● API {health === 'ok' ? 'подключён' : health === 'loading' ? 'проверка' : 'не доступен'}</span>
        {session && <><span className="account-name">{session.role === 'Observer' ? (session.surname || session.displayName) : session.displayName} · {session.role}{session.role === 'Observer' && session.recipientId ? ` · ID ${session.recipientId}` : ''}</span><button className="button ghost" onClick={logout}>Выйти</button></>}
      </div>
    </header>
    {!session ? <Login onLogin={enter} /> : <div className="registry-layout">
      <aside className="registry-sidebar">
        <p className="side-caption">РАБОЧЕЕ МЕСТО</p>
        {session.role !== 'AutomationEngineer' && <>
          <button className={section === 'notices' ? 'nav active' : 'nav'} onClick={() => { setSection('notices'); setNoticeView('list'); }}>
            {session.role === 'Observer' ? '▤ Мои оповещения' : '▤ Реестр оповещений'}
          </button>
          {session.role !== 'Observer' && <button className={section === 'recipients' ? 'nav active' : 'nav'} onClick={() => setSection('recipients')}>♙ Адресаты</button>}
        </>}
        {session.role === 'AutomationEngineer' && <button className="nav active" onClick={() => setSection('admin')}>⚙ Учётные записи</button>}
        <p className="side-help">Учебная версия. Демонстрационные каналы не подтверждают реальную доставку. Для промышленного использования требуется внешний сервис.</p>
      </aside>
      <main className="registry-main">
        <div className="page-heading"><p className="eyebrow">КООПЕРАТИВ «ОЗЕРО» / РЕЕСТР</p><h1>{section === 'notices' ? (session.role === 'Observer' ? 'Мои оповещения' : 'Оповещения') : section === 'recipients' ? 'Адресаты' : 'Управление доступом'}</h1></div>
        {section === 'recipients' && session.role !== 'Observer' && <RecipientsPanel session={session} />}
        {section === 'admin' && <AdminPanel session={session} />}
        {section === 'notices' && noticeView === 'list' && <NoticesPanel session={session} onSelect={id => { setSelectedId(id); setNoticeView('detail'); }} onCreate={() => setNoticeView('create')} />}
        {section === 'notices' && noticeView === 'create' && <CreateNoticePanel session={session} onBack={() => setNoticeView('list')} onSaved={id => { setSelectedId(id); setNoticeView('detail'); }} />}
        {section === 'notices' && noticeView === 'detail' && selectedId !== null && <NoticeDetailPanel session={session} id={selectedId} onBack={() => setNoticeView('list')} />}
      </main>
    </div>}
  </div>;
}

function Login({ onLogin }: { onLogin: (next: Session) => void }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setBusy(true);
    try { onLogin(await api<Session>('/api/auth/login', null, { method: 'POST', body: JSON.stringify({ username, password }) })); }
    catch (e) { setError(e instanceof Error ? e.message : 'Не удалось войти'); }
    finally { setBusy(false); }
  }
  return <main className="login-page"><div className="login-intro"><span className="eyebrow">ЭЛЕКТРОННЫЙ РЕЕСТР</span><h1>Управление оповещениями без лишних действий.</h1><p>Единое пространство для операторов, руководителей и служащих. Доступ определяется ролью.</p><div className="demo-note">Демонстрационный стенд · используются только вымышленные данные</div></div>
    <form className="login-card" onSubmit={submit}><span className="eyebrow">ВХОД В СИСТЕМУ</span><h2>Авторизация</h2><label>Логин<input value={username} onChange={e => setUsername(e.target.value)} autoComplete="username" required /></label><label>Пароль<input type="password" value={password} onChange={e => setPassword(e.target.value)} autoComplete="current-password" required /></label>{error && <p role="alert" className="alert error">{error}</p>}<button disabled={busy} className="button primary" type="submit">{busy ? 'Проверка…' : 'Войти →'}</button><small>Создание аккаунтов доступно инженеру автоматизации.</small></form>
  </main>;
}
