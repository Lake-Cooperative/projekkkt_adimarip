import { useEffect, useState, type FormEvent } from 'react';
import { api, asJson, type RecipientOption, type Role, type Session } from './api';

export function AdminPanel({ session }: { session: Session }) {
  const [username, setUsername] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<Role>('Operator');
  const [recipientId, setRecipientId] = useState('');
  const [recipients, setRecipients] = useState<RecipientOption[]>([]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    if (role !== 'Observer') return;
    void api<RecipientOption[]>('/api/admin/recipients', session).then(setRecipients)
      .catch(() => setError('Не удалось загрузить список адресатов'));
  }, [role, session]);
  function pickRecipient(value: string) {
    setRecipientId(value);
    const chosen = recipients.find(r => String(r.id) === value);
    // The observer's account takes the recipient's name, so the surname in the header matches the recipient.
    if (chosen) setDisplayName(chosen.fullName);
  }
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(''); setSuccess('');
    try {
      await api('/api/admin/users', session, asJson({ username, displayName, password, role, recipientId: role === 'Observer' ? Number(recipientId) : null }));
      setSuccess(`Пользователь ${username} создан`); setUsername(''); setDisplayName(''); setPassword(''); setRecipientId('');
    } catch (e) { setError(e instanceof Error ? e.message : 'Не удалось создать учётную запись'); }
    finally { setBusy(false); }
  }
  return <section className="panel"><h2>Создание учётной записи</h2><p>Доступно только инженеру автоматизации. Логин уникален; самостоятельная регистрация отсутствует.</p>
    {error && <p className="alert error" role="alert">{error}</p>}{success && <p className="alert success">{success}</p>}
    <form className="form-grid" onSubmit={submit}>
      <label>Логин<input required pattern="[a-zA-Z0-9_.-]{3,40}" value={username} onChange={e => setUsername(e.target.value)} /></label>
      <label>Отображаемое имя<input required minLength={2} value={displayName} onChange={e => setDisplayName(e.target.value)} /></label>
      <label>Роль<select value={role} onChange={e => setRole(e.target.value as Role)}><option value="Operator">Секретарь</option><option value="Manager">Управитель</option><option value="Observer">Служащий</option><option value="AutomationEngineer">Инженер автоматизации</option></select></label>
      {role === 'Observer' && <label>Адресат (его ID станет ID служащего)<select required value={recipientId} onChange={e => pickRecipient(e.target.value)}><option value="">— выберите адресата —</option>{recipients.map(r => <option key={r.id} value={r.id}>ID {r.id} · {r.fullName} ({r.externalIdentifier})</option>)}</select></label>}
      <label>Первоначальный пароль<input type="password" required minLength={10} value={password} onChange={e => setPassword(e.target.value)} autoComplete="new-password" /></label>
      <div className="form-actions"><button className="button primary" disabled={busy} type="submit">{busy ? 'Создание…' : 'Создать пользователя'}</button></div>
    </form></section>;
}
