import { adminApi } from '../../api/endpoints';
import type { RoleItem, Session, UserItem } from '../../api/types';
import { useAction } from '../../hooks/useAction';
import { formatDateTime } from '../../shared/labels';

type Props = { session: Session; users: UserItem[]; roles: RoleItem[]; onChanged: () => Promise<void> };

export function UsersTable({ session, users, roles, onChanged }: Props) {
  return (
    <section className="panel">
      <h2>
        Учётные записи <span className="muted">· {users.length}</span>
      </h2>
      <div className="table-wrap">
        <table className="registry-table">
          <thead>
            <tr>
              <th>Логин</th>
              <th>Имя</th>
              <th>Роль</th>
              <th>ID адресата</th>
              <th>Создана</th>
            </tr>
          </thead>
          <tbody>
            {users.map(u => (
              <tr key={u.username}>
                <td>
                  <strong>{u.username}</strong>
                </td>
                <td>{u.displayName}</td>
                <td>
                  <RoleSelect session={session} user={u} roles={roles} onChanged={onChanged} />
                </td>
                <td>{u.recipientId ?? '—'}</td>
                <td>{formatDateTime(u.createdAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

type RoleSelectProps = { session: Session; user: UserItem; roles: RoleItem[]; onChanged: () => Promise<void> };

/** Смена роли прямо в таблице; ошибки (например, самоблокировка) показываются под списком. */
function RoleSelect({ session, user, roles, onChanged }: RoleSelectProps) {
  const action = useAction();
  async function change(role: string) {
    if (await action.run(() => adminApi.changeRole(session, user.username, role))) await onChanged();
  }
  return (
    <>
      <select
        aria-label={`Роль ${user.username}`}
        value={user.role}
        disabled={action.busy}
        onChange={e => void change(e.target.value)}
      >
        {roles.map(r => (
          <option key={r.name} value={r.name}>
            {r.displayName}
          </option>
        ))}
      </select>
      {action.error && <div className="alert error">{action.error}</div>}
    </>
  );
}
