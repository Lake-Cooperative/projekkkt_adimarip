import { useState, type FormEvent } from 'react';
import { broadcastsApi } from '../../api/endpoints';
import type { BroadcastInput, Channel, Session, Severity } from '../../api/types';
import { useAction } from '../../hooks/useAction';

const initialAudience: BroadcastInput['audience'] = { kind: 'All', officeIds: [], recipientIds: [] };

/** Состояние и отправка формы массового оповещения. */
export function useBroadcastForm(session: Session, onStarted: (id: number) => Promise<void>) {
  const [title, setTitle] = useState('');
  const [message, setMessage] = useState('');
  const [severity, setSeverity] = useState<Severity>('Warning');
  const [channels, setChannels] = useState<Channel[]>(['Email', 'Sms']);
  const [officeId, setOfficeId] = useState('');
  const [audience, setAudience] = useState(initialAudience);
  const action = useAction();

  async function submit(e: FormEvent) {
    e.preventDefault();
    const input = { title, message, severity, channels, officeId: Number(officeId), audience };
    let createdId = 0;
    const ok = await action.run(
      async () => {
        const result = await broadcastsApi.create(session, input);
        createdId = result.id;
        return result;
      },
      r => `Рассылка ${r.code} запущена: ${r.targetCount} работников, ${r.plannedNotifications} уведомлений в очереди`,
    );
    if (!ok) return;
    setTitle('');
    setMessage('');
    await onStarted(createdId);
  }

  return {
    values: { title, message, severity, channels, officeId, audience },
    setters: { setTitle, setMessage, setSeverity, setChannels, setOfficeId, setAudience },
    action,
    submit,
  };
}
