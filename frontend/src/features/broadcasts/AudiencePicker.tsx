import type { AudienceKind, BroadcastInput, Office, Recipient } from '../../api/types';
import { toggle } from '../../shared/collections';

type Audience = BroadcastInput['audience'];

type Props = {
  audience: Audience;
  offices: Office[];
  /** null — у пользователя нет права видеть справочник адресатов. */
  people: Recipient[] | null;
  size: string;
  onChange: (audience: Audience) => void;
};

/** Выбор аудитории: все работники, подразделения или конкретные люди. */
export function AudiencePicker({ audience, offices, people, size, onChange }: Props) {
  const set = (patch: Partial<Audience>) => onChange({ ...audience, ...patch });
  return (
    <>
      <label>
        Аудитория
        <select value={audience.kind} onChange={e => set({ kind: e.target.value as AudienceKind })}>
          <option value="All">Все действующие работники</option>
          <option value="Offices">Работники подразделений</option>
          {people && <option value="Recipients">Выбранные работники</option>}
        </select>
      </label>
      <label>
        Получат оповещение
        <input readOnly value={size} />
      </label>
      {audience.kind === 'Offices' && (
        <div className="wide checkbox-grid">
          {offices.map(o => (
            <label key={o.id}>
              <input
                type="checkbox"
                checked={audience.officeIds.includes(o.id)}
                onChange={() => set({ officeIds: toggle(audience.officeIds, o.id) })}
              />
              {o.name}
            </label>
          ))}
        </div>
      )}
      {audience.kind === 'Recipients' && people && (
        <div className="wide checkbox-grid">
          {people.map(r => (
            <label key={r.id}>
              <input
                type="checkbox"
                checked={audience.recipientIds.includes(r.id)}
                onChange={() => set({ recipientIds: toggle(audience.recipientIds, r.id) })}
              />
              <span>
                {r.fullName}
                <br />
                <small className="muted">{r.officeName ?? 'без подразделения'}</small>
              </span>
            </label>
          ))}
        </div>
      )}
    </>
  );
}
