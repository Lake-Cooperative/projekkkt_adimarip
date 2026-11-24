import { useState } from 'react';
import type { Session } from '../api/types';
import { RolesPanel } from '../features/admin/RolesPanel';
import { UsersPanel } from '../features/admin/UsersPanel';
import { BroadcastsPanel } from '../features/broadcasts/BroadcastsPanel';
import { NoticesSection } from '../features/notices/NoticesSection';
import { RecipientsPanel } from '../features/recipients/RecipientsPanel';
import { Header } from './Header';
import { LoginPage } from './LoginPage';
import { availableSections, type Section } from './navigation';
import { useApiHealth } from './useApiHealth';
import { useSession } from './useSession';

/** Содержимое разделов: добавить раздел = добавить строку здесь и в navigation.ts. */
const sectionViews: Record<Section, (session: Session) => JSX.Element> = {
  notices: s => <NoticesSection session={s} />,
  broadcasts: s => <BroadcastsPanel session={s} />,
  recipients: s => <RecipientsPanel session={s} />,
  users: s => <UsersPanel session={s} />,
  roles: s => <RolesPanel session={s} />,
};

export function RegistryApp() {
  const { session, login, logout } = useSession();
  const health = useApiHealth();

  return (
    <div className="registry-root">
      <Header session={session} health={health} onLogout={logout} />
      {session ? <Workspace session={session} /> : <LoginPage onLogin={login} />}
    </div>
  );
}

function Workspace({ session }: { session: Session }) {
  const sections = availableSections(session);
  const [chosen, setChosen] = useState<Section | null>(null);
  // Если права изменились и выбранный раздел стал недоступен — открываем первый доступный.
  const current = sections.find(s => s.id === chosen) ?? sections[0];

  return (
    <div className="registry-layout">
      <aside className="registry-sidebar">
        <p className="side-caption">РАБОЧЕЕ МЕСТО</p>
        {sections.map(s => (
          <button key={s.id} className={current?.id === s.id ? 'nav active' : 'nav'} onClick={() => setChosen(s.id)}>
            {s.icon} {s.title(session)}
          </button>
        ))}
        <p className="side-help">
          Учебная версия. Демонстрационные каналы не подтверждают реальную доставку. Для промышленного использования
          требуется внешний сервис.
        </p>
      </aside>
      <main className="registry-main">
        <div className="page-heading">
          <p className="eyebrow">КООПЕРАТИВ «ОЗЕРО» / РЕЕСТР</p>
          <h1>{current?.title(session) ?? 'Нет доступных разделов'}</h1>
        </div>
        {current ? (
          sectionViews[current.id](session)
        ) : (
          <p className="alert error">У вашей роли нет прав ни на один раздел. Обратитесь к инженеру автоматизации.</p>
        )}
      </main>
    </div>
  );
}
