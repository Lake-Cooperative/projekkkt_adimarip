import { useState, type FormEvent } from 'react';
import { errorText } from '../api/client';
import { authApi } from '../api/endpoints';
import type { Session } from '../api/types';

export function LoginPage({ onLogin }: { onLogin: (session: Session) => void }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setBusy(true);
    try {
      onLogin(await authApi.login(username, password));
    } catch (e) {
      setError(errorText(e, 'Не удалось войти'));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="login-page">
      <div className="login-intro">
        <span className="eyebrow">ЭЛЕКТРОННЫЙ РЕЕСТР</span>
        <h1>Управление оповещениями без лишних действий.</h1>
        <p>Единое пространство для операторов, руководителей и служащих. Доступ определяется набором прав роли.</p>
        <div className="demo-note">Демонстрационный стенд · используются только вымышленные данные</div>
      </div>
      <form className="login-card" onSubmit={submit}>
        <span className="eyebrow">ВХОД В СИСТЕМУ</span>
        <h2>Авторизация</h2>
        <label>
          Логин
          <input value={username} onChange={e => setUsername(e.target.value)} autoComplete="username" required />
        </label>
        <label>
          Пароль
          <input
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            autoComplete="current-password"
            required
          />
        </label>
        {error && (
          <p role="alert" className="alert error">
            {error}
          </p>
        )}
        <button disabled={busy} className="button primary" type="submit">
          {busy ? 'Проверка…' : 'Войти →'}
        </button>
        <small>Создание аккаунтов доступно инженеру автоматизации.</small>
      </form>
    </main>
  );
}
