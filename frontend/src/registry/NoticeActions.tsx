import { useState, type FormEvent } from 'react';
import { api, asJson, type NoticeDetail, type Session } from './api';

const transitions: Record<string, string[]> = {
  Created: ['Sent', 'Cancelled'], Sent: ['Delivered', 'Failed', 'Cancelled'],
  Delivered: ['Read', 'Closed'], Read: ['Closed'], Failed: ['Sent', 'Cancelled']
};

export function NoticeActions({ session, notice, onChanged }: { session: Session; notice: NoticeDetail; onChanged: () => Promise<void> }) {
  const writer = session.role === 'Operator' || session.role === 'Manager';
  const [targetStatus, setTargetStatus] = useState('');
  const [comment, setComment] = useState('');
  const [channel, setChannel] = useState('Email');
  const [address, setAddress] = useState('demo@example.invalid');
  const [appealType, setAppealType] = useState('Clarification');
  const [appealText, setAppealText] = useState('');
  const [documentName, setDocumentName] = useState('');
  const [mimeType, setMimeType] = useState('application/pdf');
  const [storageUri, setStorageUri] = useState('https://example.org/document.pdf');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  async function send(path: string, payload: unknown, method: 'POST' | 'PATCH' = 'POST'): Promise<boolean> {
    if (busy) return false;
    setBusy(true); setError(''); setSuccess('');
    try {
      await api(path, session, { ...asJson(payload), method });
      await onChanged(); setSuccess('Действие выполнено');
      return true;
    } catch (e) { setError(e instanceof Error ? e.message : 'Не удалось выполнить действие'); return false; }
    finally { setBusy(false); }
  }
  const submit = (handler: () => Promise<void>) => (e: FormEvent) => { e.preventDefault(); void handler(); };
  return <section className="panel"><h2>Действия с оповещением</h2>{error && <p role="alert" className="alert error">{error}</p>}{success && <p className="alert success">{success}</p>}
    <div className="split">
      {writer && <>
        <form className="form-grid" onSubmit={submit(() => send(`/api/notices/${notice.id}/status`, { status: targetStatus, comment }, 'PATCH'))}>
          <h3 className="wide">Сменить статус</h3>
          <label className="wide">Следующий статус<select required value={targetStatus} onChange={e => setTargetStatus(e.target.value)}><option value="">Выберите переход</option>{(transitions[notice.currentStatus] ?? []).map(x => <option key={x}>{x}</option>)}</select></label>
          <label className="wide">Комментарий<input value={comment} onChange={e => setComment(e.target.value)} maxLength={4000} /></label>
          <div className="form-actions"><button disabled={busy || !(transitions[notice.currentStatus] ?? []).length} className="button primary">Сменить статус</button></div>
        </form>
        <form className="form-grid" onSubmit={submit(() => send(`/api/notices/${notice.id}/notifications`, { channel, recipientAddress: address }))}>
          <h3 className="wide">Демонстрационная отправка</h3><p className="wide muted">Сохраняется запись о симуляции. Реальная SMS/e-mail отправка не происходит.</p>
          <label>Канал<select value={channel} onChange={e => setChannel(e.target.value)}><option value="Email">E-mail</option><option value="Sms">SMS</option><option value="Portal">Портал</option><option value="Max">MAX (макет)</option></select></label>
          <label>Тестовый адрес<input required value={address} onChange={e => setAddress(e.target.value)} /></label>
          <div className="form-actions"><button disabled={busy} className="button primary">Симулировать доставку</button></div>
        </form>
        <form className="form-grid" onSubmit={submit(async () => { if (await send(`/api/notices/${notice.id}/appeals`, { type: appealType, text: appealText })) setAppealText(''); })}>
          <h3 className="wide">Зарегистрировать обращение</h3>
          <label>Тип обращения<input required value={appealType} onChange={e => setAppealType(e.target.value)} /></label>
          <label className="wide">Текст<textarea required value={appealText} maxLength={4000} onChange={e => setAppealText(e.target.value)} /></label>
          <div className="form-actions"><button disabled={busy} className="button primary">Зарегистрировать</button></div>
        </form>
      </>}
      <form className="form-grid" onSubmit={submit(() => send(`/api/notices/${notice.id}/documents`, { name: documentName, mimeType, storageUri }))}>
        <h3 className="wide">Добавить ссылку на документ</h3><p className="wide muted">Сохраняются только метаданные, сам файл не загружается.</p>
        <label>Название<input required value={documentName} onChange={e => setDocumentName(e.target.value)} /></label>
        <label>MIME<input required value={mimeType} onChange={e => setMimeType(e.target.value)} /></label>
        <label className="wide">HTTPS-ссылка<input type="url" required value={storageUri} onChange={e => setStorageUri(e.target.value)} /></label>
        <div className="form-actions"><button disabled={busy} className="button primary">Добавить документ</button></div>
      </form>
    </div>
  </section>;
}
