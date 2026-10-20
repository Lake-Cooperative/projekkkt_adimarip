import { useState, type FormEvent } from 'react';
import { api, asJson, type Role, type Session } from './api';

export function AdminPanel({ session }: { session: Session }) {
  const [username, setUsername] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<Role>('Operator');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [city, setCity] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  const isObserver = role === 'Observer';
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(''); setSuccess('');
    try {
      const result = await api<{ recipientId?: number }>('/api/admin/users', session, asJson({
        username, displayName, password, role,
        ...(isObserver ? { dateOfBirth, email: email || null, phone: phone || null, city: city || null } : {})
      }));
      setSuccess(isObserver
        ? `Служащий ${username} создан. Он же добавлен в адресаты, его ID: ${result?.recipientId ?? '—'}`
        : `Пользователь ${username} создан`);
      setUsername(''); setDisplayName(''); setPassword(''); setDateOfBirth(''); setEmail(''); setPhone(''); setCity('');
    } catch (e) { setError(e instanceof Error ? e.message : 'Не удалось создать учётную запись'); }
    finally { setBusy(false); }
  }
  return <section className="panel"><h2>Создание учётной записи</h2><p>Доступно только инженеру автоматизации. Логин уникален; самостоятельная регистрация отсутствует. Служащий и адресат — одно лицо: при создании служащего автоматически создаётся адресат, и его ID становится ID служащего.</p>
    {error && <p className="alert error" role="alert">{error}</p>}{success && <p className="alert success">{success}</p>}
    <form className="form-grid" onSubmit={submit}>
      <label>Логин<input required pattern="[a-zA-Z0-9_.-]{3,40}" value={username} onChange={e => setUsername(e.target.value)} /></label>
      <label>{isObserver ? 'ФИО (Фамилия Имя Отчество)' : 'Отображаемое имя'}<input required minLength={2} value={displayName} onChange={e => setDisplayName(e.target.value)} /></label>
      <label>Роль<select value={role} onChange={e => setRole(e.target.value as Role)}><option value="Operator">Секретарь</option><option value="Manager">Управитель</option><option value="Observer">Служащий</option><option value="AutomationEngineer">Инженер автоматизации</option></select></label>
      {isObserver && <>
        <label>Дата рождения<input required type="date" value={dateOfBirth} onChange={e => setDateOfBirth(e.target.value)} /></label>
        <label>E-mail<input type="email" value={email} onChange={e => setEmail(e.target.value)} /></label>
        <label>Телефон<input type="tel" value={phone} onChange={e => setPhone(e.target.value)} /></label>
        <label>Город<input value={city} onChange={e => setCity(e.target.value)} /></label>
      </>}
      <label>Первоначальный пароль<input type="password" required minLength={10} value={password} onChange={e => setPassword(e.target.value)} autoComplete="new-password" /></label>
      <div className="form-actions"><button className="button primary" disabled={busy} type="submit">{busy ? 'Создание…' : 'Создать пользователя'}</button></div>
    </form></section>;
}
