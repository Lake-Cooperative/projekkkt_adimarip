import { useState } from 'react';
import { directoryApi } from '../../api/endpoints';
import { can } from '../../api/session';
import type { Recipient, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { useRecipients } from '../../hooks/queries';
import { useAction } from '../../hooks/useAction';

export function RecipientsPanel({ session }: { session: Session }) {
  const [search, setSearch] = useState('');
  const recipients = useRecipients(session, search);
  const [archiving, setArchiving] = useState<Recipient | null>(null);
  const canArchive = can(session, 'RecipientsArchive');

  return (
    <>
      <div className="toolbar">
        <input
          aria-label="Поиск адресата"
          placeholder="Поиск по ФИО или идентификатору"
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>
      <Feedback error={recipients.error} />
      {archiving && (
        <ArchiveForm
          session={session}
          recipient={archiving}
          onClose={() => setArchiving(null)}
          onDone={async () => {
            setArchiving(null);
            await recipients.reload();
          }}
        />
      )}
      <section className="panel">
        <h2>Список адресатов</h2>
        <div className="table-wrap">
          <table className="registry-table">
            <thead>
              <tr>
                <th>Идентификатор</th>
                <th>ФИО</th>
                <th>Подразделение</th>
                <th>Дата рождения</th>
                <th>Контакты</th>
                {canArchive && <th>Действия</th>}
              </tr>
            </thead>
            <tbody>
              {recipients.data.map(r => (
                <tr key={r.id}>
                  <td>{r.externalIdentifier}</td>
                  <td>
                    <strong>{r.fullName}</strong>
                  </td>
                  <td>{r.officeName ?? '—'}</td>
                  <td>{r.dateOfBirth}</td>
                  <td>{[r.email, r.phone].filter(Boolean).join(', ') || '—'}</td>
                  {canArchive && (
                    <td>
                      <button className="button danger" onClick={() => setArchiving(r)}>
                        Списать
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {!recipients.loading && !recipients.data.length && <p className="empty">Адресаты не найдены</p>}
      </section>
    </>
  );
}

function ArchiveForm({
  session,
  recipient,
  onClose,
  onDone,
}: {
  session: Session;
  recipient: Recipient;
  onClose: () => void;
  onDone: () => Promise<void>;
}) {
  const [reason, setReason] = useState('');
  const action = useAction();
  async function archive() {
    if (
      await action.run(
        () => directoryApi.archive(session, recipient.id, reason),
        'Адресат списан, исторические записи сохранены',
      )
    )
      await onDone();
  }
  return (
    <section className="panel">
      <h2>Списание адресата: {recipient.fullName}</h2>
      <p>
        Адресат будет скрыт из рабочего справочника и исключён из массовых оповещений, но связанные оповещения и история
        сохранятся.
      </p>
      <Feedback error={action.error} />
      <div className="toolbar">
        <input
          placeholder="Причина списания"
          aria-label="Причина списания"
          value={reason}
          onChange={e => setReason(e.target.value)}
        />
        <button
          className="button danger"
          disabled={action.busy || reason.trim().length < 3}
          onClick={() => void archive()}
        >
          Подтвердить списание
        </button>
        <button className="button" onClick={onClose}>
          Отмена
        </button>
      </div>
    </section>
  );
}
