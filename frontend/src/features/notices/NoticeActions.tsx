import { useState, type FormEvent, type ReactNode } from 'react';
import { noticesApi } from '../../api/endpoints';
import { can } from '../../api/session';
import type { Channel, NoticeDetail, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { useAction } from '../../hooks/useAction';
import { statusLabels } from '../../shared/labels';
import { ChannelAddressFields } from './ChannelAddressFields';

type Props = { session: Session; notice: NoticeDetail; onChanged: () => Promise<void> };

/** Выполняет действие формы; true — успешно (форма может очиститься). */
type Submit = (job: () => Promise<unknown>) => Promise<boolean>;

/** Формы действий с карточкой. Каждая показывается только при наличии соответствующего права. */
export function NoticeActions({ session, notice, onChanged }: Props) {
  const action = useAction();
  const submit: Submit = async job => {
    const ok = await action.run(job);
    if (ok) await onChanged();
    return ok;
  };
  const id = notice.id;

  const forms: ReactNode[] = [];
  if (can(session, 'NoticesChangeStatus'))
    forms.push(
      <StatusForm
        key="status"
        transitions={notice.allowedTransitions}
        busy={action.busy}
        onSubmit={(status, comment) => submit(() => noticesApi.changeStatus(session, id, status, comment))}
      />,
    );
  if (can(session, 'NotificationsSend'))
    forms.push(
      <NotifyForm
        key="notify"
        busy={action.busy}
        onSubmit={(channel, address) => submit(() => noticesApi.notify(session, id, channel, address))}
      />,
    );
  if (can(session, 'AppealsCreate'))
    forms.push(
      <AppealForm
        key="appeal"
        busy={action.busy}
        onSubmit={(type, text) => submit(() => noticesApi.appeal(session, id, type, text))}
      />,
    );
  if (can(session, 'DocumentsAttach'))
    forms.push(
      <DocumentForm
        key="document"
        busy={action.busy}
        onSubmit={(name, mime, uri) => submit(() => noticesApi.document(session, id, name, mime, uri))}
      />,
    );
  if (forms.length === 0) return null;

  return (
    <section className="panel">
      <h2>Действия с оповещением</h2>
      <Feedback error={action.error} success={action.success} />
      <div className="split">{forms}</div>
    </section>
  );
}

/** Обёртка формы: отменяет перезагрузку страницы и вызывает обработчик. */
function ActionForm({ title, onSubmit, children }: { title: string; onSubmit: () => void; children: ReactNode }) {
  const handle = (e: FormEvent) => {
    e.preventDefault();
    onSubmit();
  };
  return (
    <form className="form-grid" onSubmit={handle}>
      <h3 className="wide">{title}</h3>
      {children}
    </form>
  );
}

type FormProps<A extends unknown[]> = { busy: boolean; onSubmit: (...args: A) => Promise<boolean> };

function StatusForm({ transitions, busy, onSubmit }: FormProps<[string, string]> & { transitions: string[] }) {
  const [target, setTarget] = useState('');
  const [comment, setComment] = useState('');
  // Переходы приходят с сервера — интерфейс не дублирует конечный автомат статусов.
  return (
    <ActionForm title="Сменить статус" onSubmit={() => void onSubmit(target, comment)}>
      <label className="wide">
        Следующий статус
        <select required value={target} onChange={e => setTarget(e.target.value)}>
          <option value="">Выберите переход</option>
          {transitions.map(s => (
            <option key={s} value={s}>
              {statusLabels[s] ?? s}
            </option>
          ))}
        </select>
      </label>
      <label className="wide">
        Комментарий
        <input value={comment} onChange={e => setComment(e.target.value)} maxLength={4000} />
      </label>
      <div className="form-actions">
        <button disabled={busy || transitions.length === 0} className="button primary">
          Сменить статус
        </button>
      </div>
    </ActionForm>
  );
}

function NotifyForm({ busy, onSubmit }: FormProps<[Channel, string]>) {
  const [channel, setChannel] = useState<Channel>('Email');
  const [address, setAddress] = useState('');
  return (
    <ActionForm title="Демонстрационная отправка" onSubmit={() => void onSubmit(channel, address)}>
      <p className="wide muted">Сохраняется запись о симуляции. Реальная SMS/e-mail отправка не происходит.</p>
      <ChannelAddressFields
        channel={channel}
        address={address}
        onChannelChange={setChannel}
        onAddressChange={setAddress}
      />
      <div className="form-actions">
        <button disabled={busy} className="button primary">
          Симулировать доставку
        </button>
      </div>
    </ActionForm>
  );
}

function AppealForm({ busy, onSubmit }: FormProps<[string, string]>) {
  const [type, setType] = useState('Clarification');
  const [text, setText] = useState('');
  const send = async () => {
    if (await onSubmit(type, text)) setText('');
  };
  return (
    <ActionForm title="Зарегистрировать обращение" onSubmit={() => void send()}>
      <label>
        Тип обращения
        <input required value={type} onChange={e => setType(e.target.value)} />
      </label>
      <label className="wide">
        Текст
        <textarea required value={text} maxLength={4000} onChange={e => setText(e.target.value)} />
      </label>
      <div className="form-actions">
        <button disabled={busy} className="button primary">
          Зарегистрировать
        </button>
      </div>
    </ActionForm>
  );
}

function DocumentForm({ busy, onSubmit }: FormProps<[string, string, string]>) {
  const [name, setName] = useState('');
  const [mime, setMime] = useState('application/pdf');
  const [uri, setUri] = useState('https://example.org/document.pdf');
  return (
    <ActionForm title="Добавить ссылку на документ" onSubmit={() => void onSubmit(name, mime, uri)}>
      <p className="wide muted">Сохраняются только метаданные, сам файл не загружается.</p>
      <label>
        Название
        <input required value={name} onChange={e => setName(e.target.value)} />
      </label>
      <label>
        MIME
        <input required value={mime} onChange={e => setMime(e.target.value)} />
      </label>
      <label className="wide">
        HTTPS-ссылка
        <input type="url" required value={uri} onChange={e => setUri(e.target.value)} />
      </label>
      <div className="form-actions">
        <button disabled={busy} className="button primary">
          Добавить документ
        </button>
      </div>
    </ActionForm>
  );
}
