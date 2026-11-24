import { can } from '../../api/session';
import type { Channel, Session, Severity } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { SelectField, TextField } from '../../components/Fields';
import { useOffices, useRecipients } from '../../hooks/queries';
import { toggle } from '../../shared/collections';
import { channelLabels, channels, severities, severityLabels } from '../../shared/labels';
import { AudiencePicker } from './AudiencePicker';
import { useAudiencePreview } from './useAudiencePreview';
import { useBroadcastForm } from './useBroadcastForm';

type Props = { session: Session; onStarted: (id: number) => Promise<void> };

/** Форма запуска массового оповещения с предварительным подсчётом аудитории. */
export function BroadcastComposer({ session, onStarted }: Props) {
  const canPickPeople = can(session, 'RecipientsRead');
  const offices = useOffices(session);
  const people = useRecipients(session, '', canPickPeople);
  const { values, setters, action, submit } = useBroadcastForm(session, onStarted);
  const audienceSize = useAudiencePreview(session, values.audience);
  const urgent = values.severity === 'Critical';

  return (
    <section className="panel">
      <h2>Новое массовое оповещение</h2>
      <p>
        Каждый работник получит отдельное оповещение в реестре и сообщения по выбранным каналам. Если контакта для
        канала нет, сообщение уйдёт в личный кабинет на портале. <strong>Срочные</strong> оповещения обрабатываются вне
        очереди и по всем доступным каналам. Доставка имитируется — реальные SMS и письма не отправляются.
      </p>
      <Feedback error={action.error || offices.error} success={action.success} />
      <form className="form-grid" onSubmit={submit}>
        <TextField
          label="Заголовок"
          className="wide"
          required
          minLength={3}
          maxLength={200}
          value={values.title}
          onChange={setters.setTitle}
        />
        <TextField
          label="Текст сообщения"
          className="wide"
          multiline
          required
          maxLength={4000}
          value={values.message}
          onChange={setters.setMessage}
        />
        <SelectField
          label="Важность"
          value={values.severity}
          onChange={value => setters.setSeverity(value as Severity)}
          options={severities.map(s => ({ value: s, label: severityLabels[s] }))}
        />
        <SelectField
          label="Подразделение-отправитель"
          required
          value={values.officeId}
          onChange={setters.setOfficeId}
          placeholder="Выберите подразделение"
          options={offices.data.map(o => ({ value: o.id, label: o.name }))}
        />
        <ChannelPicker picked={values.channels} onChange={setters.setChannels} />
        <AudiencePicker
          audience={values.audience}
          offices={offices.data}
          people={canPickPeople ? people.data : null}
          size={audienceSize}
          onChange={setters.setAudience}
        />
        <div className="form-actions">
          <button
            className={urgent ? 'button danger' : 'button primary'}
            disabled={action.busy || !values.channels.length}
          >
            {submitLabel(action.busy, urgent)}
          </button>
        </div>
      </form>
    </section>
  );
}

function ChannelPicker({ picked, onChange }: { picked: Channel[]; onChange: (channels: Channel[]) => void }) {
  return (
    <fieldset className="wide checkbox-grid plain-fieldset">
      <p className="permission-group">Каналы доставки</p>
      {channels.map(c => (
        <label key={c}>
          <input type="checkbox" checked={picked.includes(c)} onChange={() => onChange(toggle(picked, c))} />
          {channelLabels[c]}
        </label>
      ))}
    </fieldset>
  );
}

function submitLabel(busy: boolean, urgent: boolean) {
  if (busy) return 'Запуск…';
  return urgent ? '⚠ Оповестить срочно' : 'Запустить рассылку';
}
