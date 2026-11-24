import { noticesApi } from '../../api/endpoints';
import type { Notice, Session } from '../../api/types';
import { useAction } from '../../hooks/useAction';
import { Feedback } from '../../components/Feedback';

const awaitingAck = ['Created', 'Sent', 'Delivered'];

/** Срочные оповещения работнику, которые он ещё не подтвердил. */
export function UrgentBanner({
  session,
  notices,
  onChanged,
}: {
  session: Session;
  notices: Notice[];
  onChanged: () => Promise<void>;
}) {
  const action = useAction();
  const urgent = notices.filter(n => n.severity === 'Critical' && awaitingAck.includes(n.currentStatus));
  if (!urgent.length) return <Feedback error={action.error} success={action.success} />;

  async function acknowledge(id: number) {
    if (await action.run(() => noticesApi.acknowledge(session, id), 'Получение подтверждено')) await onChanged();
  }

  return (
    <>
      <Feedback error={action.error} />
      {urgent.map(n => (
        <section key={n.id} className="urgent-banner" role="alert">
          <div>
            <strong>⚠ {n.reason}</strong>
            <p>{n.comment}</p>
          </div>
          <button className="button danger" disabled={action.busy} onClick={() => void acknowledge(n.id)}>
            Ознакомлен(а)
          </button>
        </section>
      ))}
    </>
  );
}
