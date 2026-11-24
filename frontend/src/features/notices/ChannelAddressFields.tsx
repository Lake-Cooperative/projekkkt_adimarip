import { useEffect } from 'react';
import type { Channel } from '../../api/types';
import { channelLabels, channels, sampleAddress } from '../../shared/labels';

type Props = {
  channel: Channel;
  address: string;
  onChannelChange: (channel: Channel) => void;
  onAddressChange: (address: string) => void;
};

/** Канал и тестовый адрес. При смене канала подставляется адрес подходящего формата. */
export function ChannelAddressFields({ channel, address, onChannelChange, onAddressChange }: Props) {
  useEffect(() => {
    onAddressChange(sampleAddress[channel]);
  }, [channel, onAddressChange]);

  return (
    <>
      <label>
        Канал
        <select value={channel} onChange={e => onChannelChange(e.target.value as Channel)}>
          {channels.map(c => (
            <option key={c} value={c}>
              {channelLabels[c]}
            </option>
          ))}
        </select>
      </label>
      <label>
        Тестовый адрес
        <input required value={address} onChange={e => onAddressChange(e.target.value)} />
      </label>
    </>
  );
}
