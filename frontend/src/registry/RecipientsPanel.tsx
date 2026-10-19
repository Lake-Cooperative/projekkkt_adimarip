import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api, asJson, type Recipient, type Session } from './api';

export function RecipientsPanel({ session }: { session: Session }) {
  const [recipients, setRecipients] = useState<Recipient[]>([]);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [create, setCreate] = useState(false);
  const [identifier, setIdentifier] = useState('');
  const [fullName, setFullName] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [city, setCity] = useState('');

  const refresh = useCallback(async () => {
    try { setRecipients(await api<Recipient[]>(`/api/recipients?search=${encodeURIComponent(search)}`, session)); setError(''); }
    catch (e) { setError(e instanceof Error ? e.message : 'Не удалось загрузить адресатов'); }
  }, [session, search]);
  useEffect(() => { void refresh(); }, [refresh]);

  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setMessage('');
    try {
      await api('/api/recipients', session, asJson({ externalIdentifier: identifier, fullName, dateOfBirth, email, phone, address: { city } }));
      setMessage('Адресат добавлен'); setCreate(false); setIdentifier(''); setFullName(''); setDateOfBirth(''); setEmail(''); setPhone(''); setCity('');
      await refresh();
    } catch (e) { setError(e instanceof Error ? e.message : 'Не удалось сохранить адресата'); }
  }
  return <>
    <div className="toolbar"><input aria-label="Поиск адресата" placeholder="Поиск по ФИО или идентификатору" value={search} onChange={e => setSearch(e.target.value)} />
      {session.role !== 'Observer' && <button className="button primary" onClick={() => setCreate(!create)}>{create ? 'Скрыть форму' : '+ Новый адресат'}</button>}</div>
    {error && <p className="alert error" role="alert">{error}</p>}{message && <p className="alert success">{message}</p>}
    {create && <form className="panel form-grid" onSubmit={submit}>
      <label>Идентификатор<input required minLength={2} maxLength={100} value={identifier} onChange={e => setIdentifier(e.target.value)} placeholder="DEMO-014" /></label>
      <label>ФИО<input required minLength={2} value={fullName} onChange={e => setFullName(e.target.value)} /></label>
      <label>Дата рождения<input required type="date" value={dateOfBirth} onChange={e => setDateOfBirth(e.target.value)} /></label>
      <label>E-mail<input type="email" value={email} onChange={e => setEmail(e.target.value)} /></label>
      <label>Телефон<input type="tel" value={phone} onChange={e => setPhone(e.target.value)} /></label>
      <label>Город<input value={city} onChange={e => setCity(e.target.value)} /></label>
      <div className="form-actions"><button className="button primary" type="submit">Сохранить адресата</button></div>
    </form>}
    <section className="panel"><h2>Список адресатов</h2><div className="table-wrap"><table className="registry-table"><thead><tr><th>Идентификатор</th><th>ФИО</th><th>Дата рождения</th><th>Контакты</th></tr></thead><tbody>
      {recipients.map(r => <tr key={r.id}><td>{r.externalIdentifier}</td><td><strong>{r.fullName}</strong></td><td>{r.dateOfBirth}</td><td>{r.email || r.phone || '—'}</td></tr>)}
    </tbody></table></div>{!recipients.length && <p className="empty">Адресаты не найдены</p>}</section>
  </>;
}
