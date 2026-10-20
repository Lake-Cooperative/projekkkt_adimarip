import { useEffect, useState, type FormEvent } from 'react';
import { api, asJson, type Office, type Recipient, type Session } from './api';

const isoDay = (offset = 0) => new Date(Date.now() + offset * 86400000).toISOString().slice(0, 10);

export function CreateNoticePanel({ session, onBack, onSaved }: { session: Session; onBack: () => void; onSaved: (id: number) => void }) {
  const [recipients, setRecipients] = useState<Recipient[]>([]);
  const [offices, setOffices] = useState<Office[]>([]);
  const [number, setNumber] = useState('');
  const [recipientId, setRecipientId] = useState('');
  const [officeId, setOfficeId] = useState('');
  const [createdAt, setCreatedAt] = useState(isoDay());
  const [processingDeadline, setProcessingDeadline] = useState('');
  const [reason, setReason] = useState('');
  const [comment, setComment] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    void Promise.all([api<Recipient[]>('/api/recipients', session), api<Office[]>('/api/offices', session)])
      .then(([r, o]) => { setRecipients(r); setOffices(o); })
      .catch(() => setError('Не удалось загрузить справочники'));
  }, [session]);
  async function submit(e: FormEvent) {
    e.preventDefault(); setError(''); setBusy(true);
    try {
      const created = await api<{ id: number }>('/api/notices', session, asJson({ number, recipientId: Number(recipientId), officeId: Number(officeId), createdAt, processingDeadline, reason, comment }));
      onSaved(created.id);
    } catch (err) { setError(err instanceof Error ? err.message : 'Ошибка сохранения'); }
    finally { setBusy(false); }
  }
  return <>
    <button className="button ghost" onClick={onBack}>← К реестру</button>
    <section className="panel"><h2>Создать оповещение</h2><p>Поля проверяются на сервере. После сохранения будут созданы история статуса и запись аудита.</p>
      {error && <p className="alert error" role="alert">{error}</p>}
      <form className="form-grid" onSubmit={submit}>
        <label>Номер<input value={number} onChange={e => setNumber(e.target.value)} placeholder="AD-2026-001" required maxLength={100} /></label>
        <label>Адресат<select value={recipientId} required onChange={e => setRecipientId(e.target.value)}><option value="">Выберите адресата</option>{recipients.map(r => <option value={r.id} key={r.id}>{r.fullName} ({r.externalIdentifier})</option>)}</select></label>
        <label>Подразделение<select value={officeId} required onChange={e => setOfficeId(e.target.value)}><option value="">Выберите подразделение</option>{offices.map(o => <option value={o.id} key={o.id}>{o.name}</option>)}</select></label>
        <label>Дата создания<input type="date" required min={isoDay(-3)} value={createdAt} onChange={e => setCreatedAt(e.target.value)} /></label>
        <label>Срок обработки<input type="date" required min={createdAt} value={processingDeadline} onChange={e => setProcessingDeadline(e.target.value)} /></label>
        <label className="wide">Причина<textarea value={reason} maxLength={1000} required onChange={e => setReason(e.target.value)} /></label>
        <label className="wide">Комментарий (необязательно)<textarea value={comment} maxLength={4000} onChange={e => setComment(e.target.value)} /></label>
        <div className="form-actions"><button disabled={busy} className="button primary" type="submit">{busy ? 'Сохранение…' : 'Создать оповещение'}</button><button className="button" type="button" onClick={onBack}>Отмена</button></div>
      </form>
    </section>
  </>;
}
