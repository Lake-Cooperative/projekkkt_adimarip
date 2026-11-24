import { useState, type FormEvent } from 'react';
import { adminApi } from '../../api/endpoints';
import type { Permission, PermissionItem, RoleItem, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { useAction } from '../../hooks/useAction';
import { toggle } from '../../shared/collections';

export type Editing = { mode: 'create' } | { mode: 'edit'; role: RoleItem } | { mode: 'copy'; role: RoleItem };

type Props = {
  session: Session;
  editing: Editing;
  catalog: PermissionItem[];
  onClose: () => void;
  onSaved: () => Promise<void>;
};

/** Начальные значения формы для каждого режима. */
function initialValues(editing: Editing) {
  switch (editing.mode) {
    case 'create':
      return { name: '', displayName: '', permissions: [] as Permission[] };
    case 'copy':
      return { name: '', displayName: `${editing.role.displayName} (копия)`, permissions: editing.role.permissions };
    case 'edit':
      return { name: editing.role.name, displayName: editing.role.displayName, permissions: editing.role.permissions };
  }
}

export function RoleEditor({ session, editing, catalog, onClose, onSaved }: Props) {
  const initial = initialValues(editing);
  const [name, setName] = useState(initial.name);
  const [displayName, setDisplayName] = useState(initial.displayName);
  const [permissions, setPermissions] = useState(initial.permissions);
  const action = useAction();
  const isEdit = editing.mode === 'edit';

  async function submit(e: FormEvent) {
    e.preventDefault();
    const save = isEdit
      ? () => adminApi.updateRole(session, name, displayName, permissions)
      : () => adminApi.createRole(session, name, displayName, permissions);
    if (await action.run(save)) await onSaved();
  }

  return (
    <section className="panel">
      <h2>{isEdit ? `Изменение роли «${initial.displayName}»` : 'Новая роль'}</h2>
      <p>
        Роль, которая видит только свои оповещения, требует привязки учётной записи к адресату — она создаётся
        автоматически при заведении пользователя.
      </p>
      <Feedback error={action.error} />
      <form className="form-grid" onSubmit={submit}>
        <label>
          Код роли (латиница)
          <input
            required
            pattern="[A-Za-z][A-Za-z0-9_]{2,39}"
            disabled={isEdit}
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="DutyOfficer"
          />
        </label>
        <label>
          Название
          <input
            required
            minLength={2}
            maxLength={100}
            value={displayName}
            onChange={e => setDisplayName(e.target.value)}
            placeholder="Дежурный по безопасности"
          />
        </label>
        <PermissionChecklist
          catalog={catalog}
          selected={permissions}
          onToggle={code => setPermissions(toggle(permissions, code))}
        />
        <div className="form-actions">
          <button className="button primary" disabled={action.busy || permissions.length === 0}>
            Сохранить роль
          </button>
          <button className="button" type="button" onClick={onClose}>
            Отмена
          </button>
        </div>
      </form>
    </section>
  );
}

type ChecklistProps = { catalog: PermissionItem[]; selected: Permission[]; onToggle: (code: Permission) => void };

/** Права, сгруппированные по разделам системы. */
function PermissionChecklist({ catalog, selected, onToggle }: ChecklistProps) {
  const groups = [...new Set(catalog.map(p => p.group))];
  return (
    <div className="wide checkbox-grid">
      {groups.map(group => (
        <div key={group} className="contents">
          <p className="permission-group">{group}</p>
          {catalog
            .filter(p => p.group === group)
            .map(p => (
              <label key={p.code}>
                <input type="checkbox" checked={selected.includes(p.code)} onChange={() => onToggle(p.code)} />
                <span>
                  <strong>{p.code}</strong>
                  <br />
                  {p.description}
                </span>
              </label>
            ))}
        </div>
      ))}
    </div>
  );
}
