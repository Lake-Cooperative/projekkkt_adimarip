import type { BroadcastSummary } from '../../api/types';
import { SeverityChip } from '../../components/Chips';
import { broadcastStatusLabels, channelLabels, formatDateTime } from '../../shared/labels';
import { isRunning } from './broadcastStatus';

type Props = { broadcasts: BroadcastSummary[]; loading: boolean; onOpen: (id: number) => void };

export function BroadcastJournal({ broadcasts, loading, onOpen }: Props) {
  return (
    <section className="panel">
      <h2>
        Журнал рассылок <span className="muted">· последние {broadcasts.length}</span>
      </h2>
      <div className="table-wrap">
        <table className="registry-table">
          <thead>
            <tr>
              <th>Код</th>
              <th>Заголовок</th>
              <th>Аудитория</th>
              <th>Каналы</th>
              <th>Создана</th>
              <th>Статус</th>
              <th>Итог</th>
            </tr>
          </thead>
          <tbody>
            {broadcasts.map(b => (
              <tr
                key={b.id}
                className="clickable"
                tabIndex={0}
                onClick={() => onOpen(b.id)}
                onKeyDown={e => e.key === 'Enter' && onOpen(b.id)}
              >
                <td>
                  <strong>{b.code}</strong>
                </td>
                <td>
                  {b.title}
                  <SeverityChip severity={b.severity} />
                </td>
                <td>
                  {b.audienceDescription} ({b.targetCount})
                </td>
                <td>{b.channels.map(c => channelLabels[c]).join(', ')}</td>
                <td>
                  {formatDateTime(b.createdAt)}
                  <br />
                  <small className="muted">{b.createdByUserId}</small>
                </td>
                <td>{broadcastStatusLabels[b.status] ?? b.status}</td>
                <td>{outcome(b)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {!loading && broadcasts.length === 0 && <p className="empty">Рассылок пока не было</p>}
    </section>
  );
}

function outcome(b: BroadcastSummary) {
  if (isRunning(b.status)) return '…';
  const errors = b.failedCount ? `, ошибок: ${b.failedCount}` : '';
  return `${b.deliveredCount} / ${b.targetCount}${errors}`;
}
