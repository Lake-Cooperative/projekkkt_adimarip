import type { Notice, Page } from '../../api/types';
import { SeverityChip, StatusChip } from '../../components/Chips';

type Props = {
  page: Page<Notice>;
  loading: boolean;
  /** Пометить первую запись как последнюю созданную (только без фильтров). */
  markLatest: boolean;
  /** Выбранные строки; undefined — колонка выбора скрыта (нет права на отправку). */
  selected?: number[];
  onSelectionChange: (ids: number[]) => void;
  onOpen: (id: number) => void;
};

export function NoticesTable({ page, loading, markLatest, selected, onSelectionChange, onOpen }: Props) {
  const selectable = selected !== undefined;
  const toggle = (id: number) =>
    selected && onSelectionChange(selected.includes(id) ? selected.filter(x => x !== id) : [...selected, id]);

  return (
    <>
      <div className="table-wrap">
        <table className="registry-table">
          <thead>
            <tr>
              {selectable && <th>Выбор</th>}
              <th>Номер</th>
              <th>Адресат</th>
              <th>Подразделение</th>
              <th>Создано</th>
              <th>Срок</th>
              <th>Статус</th>
            </tr>
          </thead>
          <tbody>
            {page.items.map((n, index) => (
              <tr
                className="clickable"
                tabIndex={0}
                key={n.id}
                onClick={() => onOpen(n.id)}
                onKeyDown={e => e.key === 'Enter' && onOpen(n.id)}
              >
                {selectable && (
                  <td>
                    <input
                      type="checkbox"
                      aria-label={`Выбрать ${n.number}`}
                      checked={selected.includes(n.id)}
                      onClick={e => e.stopPropagation()}
                      onChange={() => toggle(n.id)}
                    />
                  </td>
                )}
                <td>
                  <strong>{n.number}</strong>
                  <SeverityChip severity={n.severity} />
                  {markLatest && index === 0 && <span className="latest-tag">Последнее</span>}
                </td>
                <td>{n.recipientName}</td>
                <td>{n.officeName}</td>
                <td>{n.createdAt}</td>
                <td>{n.processingDeadline}</td>
                <td>
                  <StatusChip status={n.currentStatus} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {loading && <p className="empty">Загрузка данных…</p>}
      {!loading && page.items.length === 0 && <p className="empty">По выбранным фильтрам ничего нет</p>}
    </>
  );
}
