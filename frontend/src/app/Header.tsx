import { seesOnlyOwn } from '../api/session';
import type { Session } from '../api/types';
import type { Health } from './useApiHealth';

const healthText: Record<Health, string> = { ok: 'подключён', loading: 'проверка', error: 'не доступен' };

type Props = { session: Session | null; health: Health; onLogout: () => void };

export function Header({ session, health, onLogout }: Props) {
  return (
    <header className="registry-header">
      <div className="registry-brand">
        <span className="registry-monogram">А</span>
        <div>
          <strong>АДИМАРИП</strong>
          <small>Единый реестр оповещений · учебная версия</small>
        </div>
      </div>
      <div className="header-end">
        <span className={`service-badge ${health}`}>● API {healthText[health]}</span>
        {session && (
          <>
            <span className="account-name">{accountTitle(session)}</span>
            <button className="button ghost" onClick={onLogout}>
              Выйти
            </button>
          </>
        )}
      </div>
    </header>
  );
}

/** Работнику «со своими данными» показываем фамилию и ID адресата, остальным — имя и роль. */
function accountTitle(session: Session) {
  const role = session.roleDisplayName ?? session.role;
  if (!seesOnlyOwn(session)) return `${session.displayName} · ${role}`;
  const id = session.recipientId ? ` · ID ${session.recipientId}` : '';
  return `${session.surname || session.displayName} · ${role}${id}`;
}
