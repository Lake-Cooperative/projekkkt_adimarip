import type { NoticeFilter } from '../../api/endpoints';
import type { Office } from '../../api/types';
import { statusLabels, statuses } from '../../shared/labels';

type Props = {
  filter: NoticeFilter;
  offices: Office[];
  onChange: (patch: Partial<NoticeFilter>) => void;
  /** Не передаётся, если у пользователя нет права создавать оповещения. */
  onCreate?: () => void;
};

export function NoticeFilters({ filter, offices, onChange, onCreate }: Props) {
  return (
    <div className="toolbar">
      <input
        aria-label="Поиск оповещений"
        placeholder="Номер, ФИО, причина…"
        value={filter.search}
        onChange={e => onChange({ search: e.target.value })}
      />
      <select aria-label="Статус" value={filter.status} onChange={e => onChange({ status: e.target.value })}>
        <option value="">Все статусы</option>
        {statuses.map(s => (
          <option key={s} value={s}>
            {statusLabels[s]}
          </option>
        ))}
      </select>
      <select aria-label="Подразделение" value={filter.officeId} onChange={e => onChange({ officeId: e.target.value })}>
        <option value="">Все подразделения</option>
        {offices.map(o => (
          <option key={o.id} value={o.id}>
            {o.name}
          </option>
        ))}
      </select>
      {onCreate && (
        <button className="button primary" onClick={onCreate}>
          + Создать оповещение
        </button>
      )}
    </div>
  );
}
