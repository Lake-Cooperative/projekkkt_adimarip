import { useState } from 'react';
import { noticesApi } from '../../api/endpoints';
import type { Channel, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { useAction } from '../../hooks/useAction';
import { ChannelAddressFields } from './ChannelAddressFields';

type Props = { session: Session; ids: number[]; onDone: (summary: string) => void };

/** Пакетная имитация по выбранным записям на один тестовый адрес. Работников оповещает раздел «Массовое оповещение». */
export function BulkSendBar({ session, ids, onDone }: Props) {
  const [channel, setChannel] = useState<Channel>('Email');
  const [address, setAddress] = useState('');
  const action = useAction();

  async function send() {
    let summary = '';
    const ok = await action.run(
      () => noticesApi.notifyBulk(session, ids, channel, address),
      result => {
        summary = `Смоделирована доставка: ${result.count} оповещений (доставлено ${result.delivered}, ошибок ${result.failed})`;
        return summary;
      },
    );
    if (ok) onDone(summary);
  }

  return (
    <section className="panel toolbar">
      <strong>Выбрано: {ids.length}</strong>
      <ChannelAddressFields
        channel={channel}
        address={address}
        onChannelChange={setChannel}
        onAddressChange={setAddress}
      />
      <button className="button primary" disabled={action.busy} onClick={() => void send()}>
        Симулировать пакетную отправку
      </button>
      <Feedback error={action.error} />
    </section>
  );
}
