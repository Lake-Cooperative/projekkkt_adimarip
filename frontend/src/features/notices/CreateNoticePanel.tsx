import { useState, type FormEvent } from 'react';
import { noticesApi } from '../../api/endpoints';
import type { Office, Recipient, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { SelectField, TextField } from '../../components/Fields';
import { useOffices, useRecipients } from '../../hooks/queries';
import { useAction } from '../../hooks/useAction';

const isoDay = (offsetDays = 0) => new Date(Date.now() + offsetDays * 86_400_000).toISOString().slice(0, 10);

const emptyForm = {
  number: '',
  recipientId: '',
  officeId: '',
  createdAt: isoDay(),
  processingDeadline: '',
  reason: '',
  comment: '',
};

type Form = typeof emptyForm;
type Setter = (field: keyof Form) => (value: string) => void;

type Props = { session: Session; onBack: () => void; onSaved: (id: number) => void };

/** Регистрация оповещения. Правила (дата не раньше трёх дней назад, срок не раньше даты) проверяет сервер. */
export function CreateNoticePanel({ session, onBack, onSaved }: Props) {
  const recipients = useRecipients(session);
  const offices = useOffices(session);
  const [form, setForm] = useState(emptyForm);
  const action = useAction();
  const set: Setter = field => value => setForm(f => ({ ...f, [field]: value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    let createdId = 0;
    const request = { ...form, recipientId: Number(form.recipientId), officeId: Number(form.officeId) };
    const ok = await action.run(async () => {
      createdId = (await noticesApi.create(session, request)).id;
    });
    if (ok) onSaved(createdId);
  }

  return (
    <>
      <button className="button ghost" onClick={onBack}>
        ← К реестру
      </button>
      <section className="panel">
        <h2>Создать оповещение</h2>
        <p>Поля проверяются на сервере. После сохранения будут созданы история статуса и запись аудита.</p>
        <Feedback error={action.error || recipients.error || offices.error} />
        <form className="form-grid" onSubmit={submit}>
          <NoticeFields form={form} set={set} recipients={recipients.data} offices={offices.data} />
          <div className="form-actions">
            <button disabled={action.busy} className="button primary">
              {action.busy ? 'Сохранение…' : 'Создать оповещение'}
            </button>
            <button className="button" type="button" onClick={onBack}>
              Отмена
            </button>
          </div>
        </form>
      </section>
    </>
  );
}

type FieldsProps = { form: Form; set: Setter; recipients: Recipient[]; offices: Office[] };

function NoticeFields({ form, set, recipients, offices }: FieldsProps) {
  return (
    <>
      <TextField
        label="Номер"
        value={form.number}
        onChange={set('number')}
        placeholder="AD-2026-001"
        required
        maxLength={100}
      />
      <SelectField
        label="Адресат"
        required
        value={form.recipientId}
        onChange={set('recipientId')}
        placeholder="Выберите адресата"
        options={recipients.map(r => ({ value: r.id, label: `${r.fullName} (${r.externalIdentifier})` }))}
      />
      <SelectField
        label="Подразделение"
        required
        value={form.officeId}
        onChange={set('officeId')}
        placeholder="Выберите подразделение"
        options={offices.map(o => ({ value: o.id, label: o.name }))}
      />
      <TextField
        label="Дата создания"
        type="date"
        required
        min={isoDay(-3)}
        value={form.createdAt}
        onChange={set('createdAt')}
      />
      <TextField
        label="Срок обработки"
        type="date"
        required
        min={form.createdAt}
        value={form.processingDeadline}
        onChange={set('processingDeadline')}
      />
      <TextField
        label="Причина"
        className="wide"
        multiline
        required
        maxLength={1000}
        value={form.reason}
        onChange={set('reason')}
      />
      <TextField
        label="Комментарий (необязательно)"
        className="wide"
        multiline
        maxLength={4000}
        value={form.comment}
        onChange={set('comment')}
      />
    </>
  );
}
