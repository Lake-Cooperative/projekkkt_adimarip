import { useCallback, useState } from 'react';
import { broadcastsApi } from '../../api/endpoints';
import { can } from '../../api/session';
import type { BroadcastSummary, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { useAsync } from '../../hooks/useAsync';
import { BroadcastComposer } from './BroadcastComposer';
import { BroadcastJournal } from './BroadcastJournal';
import { BroadcastReport } from './BroadcastReport';

const noBroadcasts: BroadcastSummary[] = [];

/** Раздел «Массовое оповещение»: запуск рассылки, журнал рассылок и отчёт о доставке. */
export function BroadcastsPanel({ session }: { session: Session }) {
  const canView = can(session, 'BroadcastsView');
  const load = useCallback(
    () => (canView ? broadcastsApi.list(session) : Promise.resolve(noBroadcasts)),
    [session, canView],
  );
  const list = useAsync(load, noBroadcasts);
  const [selected, setSelected] = useState<number | null>(null);

  const onStarted = async (id: number) => {
    await list.reload();
    if (canView) setSelected(id);
  };

  return (
    <>
      {can(session, 'BroadcastsSend') && <BroadcastComposer session={session} onStarted={onStarted} />}
      {canView && (
        <>
          {selected !== null && <BroadcastReport session={session} id={selected} onFinished={list.reload} />}
          <Feedback error={list.error} />
          <BroadcastJournal broadcasts={list.data} loading={list.loading} onOpen={setSelected} />
        </>
      )}
    </>
  );
}
