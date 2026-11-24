import { useEffect, useState } from 'react';
import { broadcastsApi } from '../../api/endpoints';
import type { BroadcastInput, Session } from '../../api/types';

type Audience = BroadcastInput['audience'];

const isEmpty = (a: Audience) =>
  (a.kind === 'Offices' && a.officeIds.length === 0) || (a.kind === 'Recipients' && a.recipientIds.length === 0);

/** Сколько работников получит оповещение — пересчитывается сервером при изменении аудитории. */
export function useAudiencePreview(session: Session, audience: Audience): string {
  const [text, setText] = useState('');
  const { kind, officeIds, recipientIds } = audience;

  useEffect(() => {
    const current = { kind, officeIds, recipientIds };
    if (isEmpty(current)) {
      setText('выберите получателей');
      return;
    }
    let cancelled = false;
    broadcastsApi
      .preview(session, current)
      .then(r => {
        if (cancelled) return;
        setText(
          r.recipients > r.maxAllowed ? `${r.recipients} — больше лимита ${r.maxAllowed}` : `${r.recipients} чел.`,
        );
      })
      .catch(() => !cancelled && setText('не удалось посчитать'));
    return () => {
      cancelled = true;
    };
  }, [session, kind, officeIds, recipientIds]);

  return text;
}
