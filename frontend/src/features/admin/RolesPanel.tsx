import { useState } from 'react';
import { adminApi } from '../../api/endpoints';
import type { Permission, RoleItem, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { usePermissionCatalog, useRoles } from '../../hooks/queries';
import { useAction } from '../../hooks/useAction';
import { RoleEditor, type Editing } from './RoleEditor';

const scopeLabels: Record<RoleItem['scope'], string> = {
  All: 'Весь реестр',
  Own: 'Только свои оповещения',
  None: 'Без доступа к реестру',
};

/** Кастомные роли: инженер собирает роль из атомарных прав. Системные роли можно только скопировать. */
export function RolesPanel({ session }: { session: Session }) {
  const roles = useRoles(session);
  const catalog = usePermissionCatalog(session);
  const [editing, setEditing] = useState<Editing | null>(null);
  const action = useAction();

  async function remove(role: RoleItem) {
    const ok = await action.run(() => adminApi.deleteRole(session, role.name), `Роль «${role.displayName}» удалена`);
    if (ok) await roles.reload();
  }
  async function saved() {
    setEditing(null);
    await roles.reload();
  }
  const describe = (code: Permission) => catalog.data.find(p => p.code === code)?.description ?? code;

  return (
    <>
      <div className="toolbar">
        <button className="button primary" onClick={() => setEditing({ mode: 'create' })}>
          + Новая роль
        </button>
      </div>
      <Feedback error={roles.error || catalog.error || action.error} success={action.success} />
      {editing && (
        <RoleEditor
          session={session}
          editing={editing}
          catalog={catalog.data}
          onClose={() => setEditing(null)}
          onSaved={saved}
        />
      )}
      <section className="panel">
        <h2>
          Роли <span className="muted">· {roles.data.length}</span>
        </h2>
        <div className="table-wrap">
          <table className="registry-table">
            <thead>
              <tr>
                <th>Роль</th>
                <th>Область данных</th>
                <th>Права</th>
                <th>Пользователей</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              {roles.data.map(role => (
                <tr key={role.name}>
                  <td>
                    <strong>{role.displayName}</strong>
                    <br />
                    <small className="muted">{role.name}</small>
                    {role.isSystem && <span className="tag system-tag">системная</span>}
                  </td>
                  <td>{scopeLabels[role.scope]}</td>
                  <td>
                    <div className="tag-list">
                      {role.permissions.map(p => (
                        <span key={p} className="tag" title={describe(p)}>
                          {p}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td>{role.usersCount}</td>
                  <td>
                    <RoleButtons role={role} busy={action.busy} onEdit={setEditing} onDelete={remove} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </>
  );
}

type ButtonsProps = {
  role: RoleItem;
  busy: boolean;
  onEdit: (editing: Editing) => void;
  onDelete: (role: RoleItem) => Promise<void>;
};

function RoleButtons({ role, busy, onEdit, onDelete }: ButtonsProps) {
  if (role.isSystem)
    return (
      <button className="button" onClick={() => onEdit({ mode: 'copy', role })}>
        Копировать
      </button>
    );
  return (
    <div className="tag-list">
      <button className="button" onClick={() => onEdit({ mode: 'edit', role })}>
        Изменить
      </button>
      <button
        className="button danger"
        disabled={busy || role.usersCount > 0}
        title={role.usersCount ? 'Роль назначена пользователям' : ''}
        onClick={() => void onDelete(role)}
      >
        Удалить
      </button>
    </div>
  );
}
