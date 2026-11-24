import { useCallback } from 'react';
import { noticesApi } from '../../api/endpoints';
import { can } from '../../api/session';
import type { NoticeDetail, Session } from '../../api/types';
import { SeverityChip, StatusChip } from '../../components/Chips';
import { Feedback } from '../../components/Feedback';
import { useAction } from '../../hooks/useAction';
import { useAsync } from '../../hooks/useAsync';
import { channelLabels, formatDateTime, statusLabels } from '../../shared/labels';
import { NoticeActions } from './NoticeActions';

type Props = { session: Session; id: number; onBack: () => void };

export function NoticeDetailPanel({ session, id, onBack }: Props) {
  const load = useCallback(() => noticesApi.get(session, id), [session, id]);
  const notice = useAsync<NoticeDetail | null>(load, null);
  const n = notice.data;

  return (
    <>
      <button className="button ghost" onClick={onBack}>
        ← К реестру
      </button>
      <Feedback error={notice.error} />
      {n ? (
        <>
          <NoticeSummary notice={n} />
          {n.canAcknowledge && <AcknowledgePanel session={session} notice={n} onChanged={notice.reload} />}
          {can(session, 'HistoryView') && <HistoryPanel notice={n} />}
          {n.audit && <AuditPanel audit={n.audit} />}
          <div className="split">
            <NotificationsPanel notice={n} />
            <AppealsPanel notice={n} />
          </div>
          <NoticeActions session={session} notice={n} onChanged={notice.reload} />
          <DocumentsPanel notice={n} />
        </>
      ) : (
        <section className="panel">
          <p>Загрузка карточки…</p>
        </section>
      )}
    </>
  );
}

function NoticeSummary({ notice: n }: { notice: NoticeDetail }) {
  const fields: [string, string][] = [
    ['Адресат', n.recipientName],
    ['Подразделение', n.officeName],
    ['Создано', n.createdAt],
    ['Срок обработки', n.processingDeadline],
    ['Причина', n.reason],
    ['Комментарий', n.comment || '—'],
  ];
  return (
    <section className="panel">
      <div className="toolbar">
        <h2 className="grow">
          Оповещение № {n.number}
          <SeverityChip severity={n.severity} />
        </h2>
        <StatusChip status={n.currentStatus} />
      </div>
      <div className="detail-grid">
        {fields.map(([label, value]) => (
          <div className="detail-item" key={label}>
            <small>{label}</small>
            <strong>{value}</strong>
          </div>
        ))}
      </div>
    </section>
  );
}

function AcknowledgePanel({
  session,
  notice,
  onChanged,
}: {
  session: Session;
  notice: NoticeDetail;
  onChanged: () => Promise<void>;
}) {
  const action = useAction();
  async function acknowledge() {
    if (await action.run(() => noticesApi.acknowledge(session, notice.id), 'Получение подтверждено')) await onChanged();
  }
  return (
    <section className="panel">
      <h2>Подтверждение получения</h2>
      <p>Подтвердите, что вы ознакомились с оповещением. Отправитель увидит это в отчёте.</p>
      <Feedback error={action.error} success={action.success} />
      <button className="button primary" disabled={action.busy} onClick={() => void acknowledge()}>
        Ознакомлен(а)
      </button>
    </section>
  );
}

function HistoryPanel({ notice }: { notice: NoticeDetail }) {
  return (
    <section className="panel">
      <h2>История статусов</h2>
      {notice.history.length === 0 && <p>История пуста</p>}
      <ol className="timeline">
        {notice.history.map(h => (
          <li key={h.id}>
            <strong>{statusLabels[h.status] ?? h.status}</strong> · {formatDateTime(h.changedAt)} · {h.changedByUserId}
            <div>{h.comment}</div>
          </li>
        ))}
      </ol>
    </section>
  );
}

function AuditPanel({ audit }: { audit: NonNullable<NoticeDetail['audit']> }) {
  return (
    <section className="panel">
      <h2>Журнал аудита</h2>
      {audit.length === 0 && <p>Записей аудита нет</p>}
      <ol className="timeline">
        {audit.map(a => (
          <li key={a.id}>
            <strong>{a.action}</strong> · {formatDateTime(a.occurredAt)} · {a.userId}
            <div>{a.details}</div>
          </li>
        ))}
      </ol>
    </section>
  );
}

function NotificationsPanel({ notice }: { notice: NoticeDetail }) {
  return (
    <section className="panel">
      <h2>Уведомления</h2>
      {notice.notifications.length === 0 && <p>Нет уведомлений</p>}
      {notice.notifications.map(x => (
        <p key={x.id}>
          <strong>{channelLabels[x.channel] ?? x.channel}</strong> → {x.recipientAddress} ·{' '}
          <StatusChip status={x.status === 'Pending' ? 'Created' : x.status} />
          <br />
          <small>
            Симуляция, {formatDateTime(x.createdAt)}; попыток: {x.attempts.length}
            {x.lastError && `; ошибка: ${x.lastError}`} (не подтверждение фактического вручения)
          </small>
        </p>
      ))}
    </section>
  );
}

function AppealsPanel({ notice }: { notice: NoticeDetail }) {
  return (
    <section className="panel">
      <h2>Обращения</h2>
      {notice.appeals.length === 0 && <p>Нет обращений</p>}
      {notice.appeals.map(a => (
        <p key={a.id}>
          <strong>{a.type}</strong> · {a.status}
          <br />
          {a.text}
        </p>
      ))}
    </section>
  );
}

function DocumentsPanel({ notice }: { notice: NoticeDetail }) {
  return (
    <section className="panel">
      <h2>Документы (метаданные)</h2>
      {notice.documents.length === 0 && <p>Документы отсутствуют</p>}
      {notice.documents.map(d => (
        <p key={d.id}>
          <strong>{d.name}</strong> · {d.mimeType} ·{' '}
          <a href={d.storageUri} target="_blank" rel="noopener noreferrer">
            Открыть ссылку ↗
          </a>
        </p>
      ))}
    </section>
  );
}
