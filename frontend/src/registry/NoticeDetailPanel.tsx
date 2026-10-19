import { useCallback, useEffect, useState } from 'react';
import { api, type NoticeDetail, type Session } from './api';
import { NoticeActions } from './NoticeActions';

const dateTime = (value: string) => new Date(value).toLocaleString('ru-RU');

export function NoticeDetailPanel({ session, id, onBack }: { session: Session; id: number; onBack: () => void }) {
  const [notice, setNotice] = useState<NoticeDetail | null>(null);
  const [error, setError] = useState('');
  const load = useCallback(async () => {
    try { setNotice(await api<NoticeDetail>(`/api/notices/${id}`, session)); setError(''); }
    catch (e) { setError(e instanceof Error ? e.message : 'Не удалось открыть карточку'); }
  }, [id, session]);
  useEffect(() => { void load(); }, [load]);
  return <>
    <button className="button ghost" onClick={onBack}>← К реестру</button>
    {error && <p className="alert error" role="alert">{error}</p>}
    {!notice ? <section className="panel"><p>Загрузка карточки…</p></section> : <>
      <section className="panel"><div className="toolbar"><h2 style={{ flex: 1, margin: 0 }}>Оповещение № {notice.number}</h2><span className={`status-chip ${notice.currentStatus}`}>{notice.currentStatus}</span></div>
        <div className="detail-grid">
          <div className="detail-item"><small>Адресат</small><strong>{notice.recipientName}</strong></div>
          <div className="detail-item"><small>Подразделение</small><strong>{notice.officeName}</strong></div>
          <div className="detail-item"><small>Создано</small><strong>{notice.createdAt}</strong></div>
          <div className="detail-item"><small>Срок обработки</small><strong>{notice.processingDeadline}</strong></div>
          <div className="detail-item"><small>Причина</small><strong>{notice.reason}</strong></div>
          <div className="detail-item"><small>Комментарий</small><strong>{notice.comment || '—'}</strong></div>
        </div>
      </section>
      {session.role !== 'Observer' && <section className="panel"><h2>История статусов</h2>{notice.history.length ? <ol className="timeline">{notice.history.map(h => <li key={h.id}><strong>{h.status}</strong> · {dateTime(h.changedAt)} · {h.changedByUserId}<div>{h.comment || ''}</div></li>)}</ol> : <p>История пуста</p>}</section>}
      {session.role === 'Manager' && <section className="panel"><h2>Журнал аудита</h2>{notice.audit?.length ? <ol className="timeline">{notice.audit.map(a => <li key={a.id}><strong>{a.action}</strong> · {dateTime(a.occurredAt)} · {a.userId}<div>{a.details || ''}</div></li>)}</ol> : <p>Записей аудита нет</p>}</section>}
      <div className="split"><section className="panel"><h2>Уведомления</h2>{notice.notifications.map(n => <p key={n.id}><strong>{n.channel}</strong> → {n.recipientAddress}<br /><small>Демо, {dateTime(n.createdAt)}; {n.attempts[0]?.status ?? '—'}</small></p>)}{!notice.notifications.length && <p>Нет уведомлений</p>}</section>
        <section className="panel"><h2>Обращения</h2>{notice.appeals.map(a => <p key={a.id}><strong>{a.type}</strong> · {a.status}<br />{a.text}</p>)}{!notice.appeals.length && <p>Нет обращений</p>}</section></div>
      <NoticeActions session={session} notice={notice} onChanged={load} />
      <section className="panel"><h2>Документы (метаданные)</h2>{notice.documents.map(d => <p key={d.id}><strong>{d.name}</strong> · {d.mimeType} · <a href={d.storageUri} target="_blank" rel="noopener noreferrer">Открыть ссылку ↗</a></p>)}{!notice.documents.length && <p>Документы отсутствуют</p>}</section>
    </>}
  </>;
}
