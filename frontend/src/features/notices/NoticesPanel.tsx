import { useCallback, useState } from 'react';
import { noticesApi, type NoticeFilter } from '../../api/endpoints';
import { can, seesOnlyOwn } from '../../api/session';
import type { Notice, Page, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { useOffices } from '../../hooks/queries';
import { useAsync } from '../../hooks/useAsync';
import { BulkSendBar } from './BulkSendBar';
import { NoticeFilters } from './NoticeFilters';
import { NoticesTable } from './NoticesTable';
import { UrgentBanner } from './UrgentBanner';

const emptyPage: Page<Notice> = { items: [], total: 0, page: 1, pageSize: 15, totalPages: 0 };
const firstPage: NoticeFilter = { page: 1, search: '', status: '', officeId: '' };

const isUnfiltered = (f: NoticeFilter) => f.page === 1 && !f.search && !f.status && !f.officeId;

type Props = { session: Session; onSelect: (id: number) => void; onCreate: () => void };

/** Список оповещений: фильтры, таблица, пагинация, пакетная отправка и срочные оповещения работника. */
export function NoticesPanel({ session, onSelect, onCreate }: Props) {
  const [filter, setFilter] = useState(firstPage);
  const [selected, setSelected] = useState<number[]>([]);
  const [bulkResult, setBulkResult] = useState('');
  const offices = useOffices(session);
  const load = useCallback(() => noticesApi.list(session, filter), [session, filter]);
  const notices = useAsync(load, emptyPage);
  const ownOnly = seesOnlyOwn(session);
  const canBulk = can(session, 'NotificationsSend');

  const changeFilter = (patch: Partial<NoticeFilter>) => {
    setSelected([]);
    setFilter(current => ({ ...current, page: 1, ...patch }));
  };
  const goToPage = (page: number) => {
    setSelected([]);
    setFilter(current => ({ ...current, page }));
  };
  const finishBulk = (summary: string) => {
    setBulkResult(summary);
    setSelected([]);
  };

  return (
    <>
      {ownOnly && <UrgentBanner session={session} notices={notices.data.items} onChanged={notices.reload} />}
      <NoticeFilters
        filter={filter}
        offices={offices.data}
        onChange={changeFilter}
        onCreate={can(session, 'NoticesCreate') ? onCreate : undefined}
      />
      <Feedback error={notices.error || offices.error} success={bulkResult} />
      {canBulk && selected.length > 0 && <BulkSendBar session={session} ids={selected} onDone={finishBulk} />}
      {ownOnly && (
        <p className="muted" role="note">
          Здесь отображаются только оповещения, адресованные вам. Чужие записи недоступны.
        </p>
      )}
      <section className="panel">
        <h2>
          {ownOnly ? 'Мои оповещения' : 'Реестр'} <span className="muted">· {notices.data.total} записей</span>
        </h2>
        <NoticesTable
          page={notices.data}
          loading={notices.loading}
          markLatest={isUnfiltered(filter)}
          selected={canBulk ? selected : undefined}
          onSelectionChange={setSelected}
          onOpen={onSelect}
        />
        <Pagination page={filter.page} totalPages={notices.data.totalPages} onChange={goToPage} />
      </section>
    </>
  );
}

function Pagination({
  page,
  totalPages,
  onChange,
}: {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}) {
  return (
    <div className="pagination">
      <span>
        Страница {page} из {Math.max(1, totalPages)}
      </span>
      <button className="button" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        ← Назад
      </button>
      <button className="button" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
        Далее →
      </button>
    </div>
  );
}
