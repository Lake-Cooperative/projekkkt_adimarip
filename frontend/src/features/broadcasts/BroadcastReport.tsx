import { useCallback, useEffect } from 'react';
import { broadcastsApi } from '../../api/endpoints';
import type { BroadcastDetails, Session } from '../../api/types';
import { SeverityChip } from '../../components/Chips';
import { Feedback } from '../../components/Feedback';
import { useAsync } from '../../hooks/useAsync';
import { broadcastStatusLabels, channelLabels } from '../../shared/labels';
import { isRunning } from './broadcastStatus';

const pollIntervalMs = 1500;

type Props = { session: Session; id: number; onFinished: () => Promise<void> };

/** Отчёт по рассылке. Пока рассылка идёт, данные обновляются раз в полторы секунды. */
export function BroadcastReport({ session, id, onFinished }: Props) {
  const load = useCallback(() => broadcastsApi.get(session, id), [session, id]);
  const report = useAsync<BroadcastDetails | null>(load, null);
  const status = report.data?.broadcast.status;
  const { reload } = report;

  useEffect(() => {
    if (!status) return;
    if (!isRunning(status)) {
      void onFinished();
      return;
    }
    const timer = window.setTimeout(() => void reload(), pollIntervalMs);
    return () => window.clearTimeout(timer);
  }, [report.data, status, reload, onFinished]);

  if (!report.data)
    return (
      <section className="panel">
        <Feedback error={report.error} />
        <p>Загрузка отчёта…</p>
      </section>
    );

  const { broadcast: b, progress: p, failures } = report.data;
  return (
    <section className="panel">
      <h2>
        {b.code} · {b.title}
        <SeverityChip severity={b.severity} />
      </h2>
      <p>
        {broadcastStatusLabels[b.status] ?? b.status} · время доставки: {duration(b.startedAt, b.completedAt)}
      </p>
      <ProgressBar total={p.total} delivered={p.delivered} failed={p.failed} pending={p.pending} />
      <div className="stats">
        <Stat value={p.total} label="работников" />
        <Stat value={p.delivered} label="доставлено" />
        <Stat value={p.failed} label="не доставлено" />
        <Stat value={p.pending} label="в очереди" />
        <Stat value={p.acknowledged} label="подтвердили получение" />
      </div>
      {p.channels.length > 0 && <ChannelTable channels={p.channels} />}
      {failures.length > 0 && <FailuresTable failures={failures} />}
    </section>
  );
}

function duration(startedAt: string | null, completedAt: string | null) {
  if (!startedAt || !completedAt) return '—';
  return `${((Date.parse(completedAt) - Date.parse(startedAt)) / 1000).toFixed(1)} с`;
}

const Stat = ({ value, label }: { value: number; label: string }) => (
  <div>
    <strong>{value}</strong>
    {label}
  </div>
);

function ProgressBar({
  total,
  delivered,
  failed,
  pending,
}: Record<'total' | 'delivered' | 'failed' | 'pending', number>) {
  const width = (value: number) => ({ width: `${total ? (value / total) * 100 : 0}%` });
  return (
    <div className="progress" aria-label="Ход рассылки">
      <span className="ok" style={width(delivered)} />
      <span className="bad" style={width(failed)} />
      <span className="wait" style={width(pending)} />
    </div>
  );
}

function ChannelTable({ channels }: { channels: BroadcastDetails['progress']['channels'] }) {
  return (
    <table className="registry-table">
      <thead>
        <tr>
          <th>Канал</th>
          <th>Доставлено</th>
          <th>Ошибки</th>
          <th>В очереди</th>
        </tr>
      </thead>
      <tbody>
        {channels.map(c => (
          <tr key={c.channel}>
            <td>{channelLabels[c.channel] ?? c.channel}</td>
            <td>{c.delivered}</td>
            <td>{c.failed}</td>
            <td>{c.pending}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function FailuresTable({ failures }: { failures: BroadcastDetails['failures'] }) {
  return (
    <>
      <h3>Недоставленные уведомления</h3>
      <table className="registry-table">
        <thead>
          <tr>
            <th>Работник</th>
            <th>Канал</th>
            <th>Адрес</th>
            <th>Причина</th>
          </tr>
        </thead>
        <tbody>
          {failures.map(f => (
            <tr key={`${f.noticeId}-${f.channel}`}>
              <td>{f.recipientName}</td>
              <td>{channelLabels[f.channel]}</td>
              <td>{f.address}</td>
              <td>{f.error ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
