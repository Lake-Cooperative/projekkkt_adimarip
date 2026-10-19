import { useCallback, useEffect, useState } from 'react';
import { api, asJson, type Notice, type Office, type Page, type Session } from './api';

const statuses = ['Created', 'Sent', 'Delivered', 'Read', 'Closed', 'Cancelled', 'Failed'];

export function NoticesPanel({ session, onSelect, onCreate }: { session: Session; onSelect: (id: number) => void; onCreate: () => void }) {
  const [data, setData] = useState<Page<Notice>>({ items: [], total: 0, page: 1, pageSize: 15, totalPages: 0 });
  const [offices, setOffices] = useState<Office[]>([]);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [officeId, setOfficeId] = useState('');
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [bulkChannel, setBulkChannel] = useState('Email');
  const [bulkAddress, setBulkAddress] = useState('demo@example.invalid');
  const [bulkResult, setBulkResult] = useState('');
  useEffect(() => { void api<Office[]>('/api/offices', session).then(setOffices).catch(() => setError('Не удалось загрузить подразделения')); }, [session]);
  const load = useCallback(async () => {
    setLoading(true);
    const params = new URLSearchParams({ page: String(page), pageSize: '15' });
    if (search.trim()) params.set('search', search.trim());
    if (status) params.set('status', status);
    if (officeId) params.set('officeId', officeId);
    try { setData(await api<Page<Notice>>(`/api/notices?${params}`, session)); setError(''); }
    catch (e) { setError(e instanceof Error ? e.message : 'Ошибка загрузки'); }
    finally { setLoading(false); }
  }, [session, page, search, status, officeId]);
  useEffect(() => { void load(); }, [load]);
  const allowWrite = session.role === 'Operator' || session.role === 'Manager';
  function toggle(id: number) { setSelectedIds(before => before.includes(id) ? before.filter(x => x !== id) : [...before, id]); }
  async function bulkSend() {
    setError(''); setBulkResult('');
    try {
      const result = await api<{ count: number }>('/api/notices/notifications/bulk', session,
        asJson({ noticeIds: selectedIds, channel: bulkChannel, recipientAddress: bulkAddress }));
      setBulkResult(`Смоделирована доставка: ${result.count} оповещений`); setSelectedIds([]);
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка массовой операции'); }
  }
  const isUnfiltered = page === 1 && !search && !status && !officeId;
  return <>
    <div className="toolbar"><input aria-label="Поиск оповещений" placeholder="Номер, ФИО, причина…" value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} />
      <select aria-label="Статус" value={status} onChange={e => { setStatus(e.target.value); setPage(1); }}><option value="">Все статусы</option>{statuses.map(x => <option key={x}>{x}</option>)}</select>
      <select aria-label="Подразделение" value={officeId} onChange={e => { setOfficeId(e.target.value); setPage(1); }}><option value="">Все подразделения</option>{offices.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select>
      {allowWrite && <button className="button primary" onClick={onCreate}>+ Создать оповещение</button>}
    </div>
    {error && <p className="alert error" role="alert">{error}</p>}
    {bulkResult && <p className="alert success">{bulkResult}</p>}
    {allowWrite && selectedIds.length > 0 && <section className="panel toolbar"><strong>Выбрано: {selectedIds.length}</strong>
      <select aria-label="Канал массовой отправки" value={bulkChannel} onChange={e => setBulkChannel(e.target.value)}><option value="Email">E-mail</option><option value="Sms">SMS</option><option value="Portal">Портал</option><option value="Max">MAX</option></select>
      <input aria-label="Тестовый адрес" value={bulkAddress} onChange={e => setBulkAddress(e.target.value)} />
      <button className="button primary" onClick={() => void bulkSend()}>Симулировать массовую отправку</button></section>}
    <section className="panel"><h2>Реестр <span className="muted">· {data.total} записей</span></h2>
      <div className="table-wrap"><table className="registry-table"><thead><tr>{allowWrite && <th>Выбор</th>}<th>Номер</th><th>Адресат</th><th>Подразделение</th><th>Создано</th><th>Срок</th><th>Статус</th></tr></thead><tbody>
        {data.items.map((n, i) => <tr className="clickable" tabIndex={0} key={n.id} onClick={() => onSelect(n.id)} onKeyDown={e => { if (e.key === 'Enter') onSelect(n.id); }}>
          {allowWrite && <td><input type="checkbox" aria-label={`Выбрать ${n.number}`} checked={selectedIds.includes(n.id)} onClick={e => e.stopPropagation()} onChange={() => toggle(n.id)} /></td>}
          <td><strong>{n.number}</strong>{isUnfiltered && i === 0 && <span className="latest-tag">Последнее</span>}</td><td>{n.recipientName}</td><td>{n.officeName}</td><td>{n.createdAt}</td><td>{n.processingDeadline}</td><td><span className={`status-chip ${n.currentStatus}`}>{n.currentStatus}</span></td>
        </tr>)}
      </tbody></table></div>
      {loading ? <p className="empty">Загрузка данных…</p> : data.items.length === 0 && <p className="empty">По выбранным фильтрам ничего нет</p>}
      <div className="pagination"><span>Страница {page} из {Math.max(1, data.totalPages)}</span><button className="button" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Назад</button><button className="button" disabled={page >= data.totalPages} onClick={() => setPage(p => p + 1)}>Далее →</button></div>
    </section>
  </>;
}
