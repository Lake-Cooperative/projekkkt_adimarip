import { useCallback, useEffect, useState } from 'react';
import { api, asJson, type Recipient, type Session } from './api';

export function RecipientsPanel({ session }: { session: Session }) {
  const [recipients, setRecipients] = useState<Recipient[]>([]);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [archiveId, setArchiveId] = useState<number | null>(null);
  const [archiveReason, setArchiveReason] = useState('');

  const refresh = useCallback(async () => {
    try { setRecipients(await api<Recipient[]>(`/api/recipients?search=${encodeURIComponent(search)}`, session)); setError(''); }
    catch (e) { setError(e instanceof Error ? e.message : 'Не удалось загрузить адресатов'); }
  }, [session, search]);
  useEffect(() => { void refresh(); }, [refresh]);

  async function archive() {
    if (archiveId === null || archiveReason.trim().length < 3) return;
    try {
      await api(`/api/recipients/${archiveId}/archive`, session, asJson({ reason: archiveReason }));
      setArchiveId(null); setArchiveReason(''); setMessage('Адресат списан, исторические записи сохранены'); await refresh();
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка списания'); }
  }
  return <>
    <div className="toolbar"><input aria-label="Поиск адресата" placeholder="Поиск по ФИО или идентификатору" value={search} onChange={e => setSearch(e.target.value)} /></div>
    {error && <p className="alert error" role="alert">{error}</p>}{message && <p className="alert success">{message}</p>}
    {archiveId !== null && <section className="panel"><h2>Списание адресата № {archiveId}</h2><p>Адресат будет скрыт из рабочего справочника, но связанные оповещения и история сохранятся.</p>
      <div className="toolbar"><input placeholder="Причина списания" aria-label="Причина списания" value={archiveReason} onChange={e => setArchiveReason(e.target.value)} />
      <button className="button danger" onClick={() => void archive()}>Подтвердить списание</button><button className="button" onClick={() => setArchiveId(null)}>Отмена</button></div></section>}
    <section className="panel"><h2>Список адресатов</h2><div className="table-wrap"><table className="registry-table"><thead><tr><th>Идентификатор</th><th>ФИО</th><th>Дата рождения</th><th>Контакты</th>{session.role !== 'Observer' && <th>Действия</th>}</tr></thead><tbody>
      {recipients.map(r => <tr key={r.id}><td>{r.externalIdentifier}</td><td><strong>{r.fullName}</strong></td><td>{r.dateOfBirth}</td><td>{r.email || r.phone || '—'}</td>{session.role !== 'Observer' && <td><button className="button danger" onClick={() => { setArchiveId(r.id); setMessage(''); }}>Списать</button></td>}</tr>)}
    </tbody></table></div>{!recipients.length && <p className="empty">Адресаты не найдены</p>}</section>
  </>;
}
