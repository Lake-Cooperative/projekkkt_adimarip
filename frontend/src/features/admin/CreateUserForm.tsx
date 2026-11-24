import { useState, type FormEvent } from 'react';
import { adminApi } from '../../api/endpoints';
import type { Office, RoleItem, Session } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { useAction } from '../../hooks/useAction';

const emptyForm = {
  username: '',
  displayName: '',
  password: '',
  role: 'Operator',
  dateOfBirth: '',
  email: '',
  phone: '',
  city: '',
  officeId: '',
};
type Form = typeof emptyForm;

type Props = { session: Session; roles: RoleItem[]; offices: Office[]; onCreated: () => Promise<void> };

/**
 * Создание учётной записи. Если роль видит только свои оповещения (как «Служащий»),
 * вместе с учётной записью создаётся адресат, и его ID становится ID работника.
 */
export function CreateUserForm({ session, roles, offices, onCreated }: Props) {
  const [form, setForm] = useState(emptyForm);
  const action = useAction();
  const needsRecipient = roles.find(r => r.name === form.role)?.requiresRecipientBinding ?? false;
  const set = (field: keyof Form) => (e: { target: { value: string } }) =>
    setForm(f => ({ ...f, [field]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    const ok = await action.run(
      () => adminApi.createUser(session, toRequest(form, needsRecipient)),
      result =>
        needsRecipient
          ? `Учётная запись ${form.username} создана. Работник добавлен в адресаты, его ID: ${result.recipientId ?? '—'}`
          : `Пользователь ${form.username} создан`,
    );
    if (!ok) return;
    setForm(f => ({ ...emptyForm, role: f.role }));
    await onCreated();
  }

  return (
    <section className="panel">
      <h2>Создание учётной записи</h2>
      <p>
        Логин уникален; самостоятельная регистрация отсутствует. Если роль видит только свои оповещения, вместе с
        учётной записью создаётся адресат, и его ID становится ID работника.
      </p>
      <Feedback error={action.error} success={action.success} />
      <form className="form-grid" onSubmit={submit}>
        <label>
          Логин
          <input required pattern="[a-zA-Z0-9_.\-]{3,40}" value={form.username} onChange={set('username')} />
        </label>
        <label>
          {needsRecipient ? 'ФИО (Фамилия Имя Отчество)' : 'Отображаемое имя'}
          <input required minLength={2} value={form.displayName} onChange={set('displayName')} />
        </label>
        <label>
          Роль
          <select value={form.role} onChange={set('role')}>
            {roles.map(r => (
              <option key={r.name} value={r.name}>
                {r.displayName}
                {r.isSystem ? '' : ' (кастомная)'}
              </option>
            ))}
          </select>
        </label>
        {needsRecipient && <EmployeeFields form={form} offices={offices} set={set} />}
        <label>
          Первоначальный пароль
          <input
            type="password"
            required
            minLength={10}
            value={form.password}
            onChange={set('password')}
            autoComplete="new-password"
          />
        </label>
        <div className="form-actions">
          <button className="button primary" disabled={action.busy}>
            {action.busy ? 'Создание…' : 'Создать пользователя'}
          </button>
        </div>
      </form>
    </section>
  );
}

type FieldsProps = {
  form: Form;
  offices: Office[];
  set: (field: keyof Form) => (e: { target: { value: string } }) => void;
};

/** Сведения о работнике для карточки адресата. */
function EmployeeFields({ form, offices, set }: FieldsProps) {
  return (
    <>
      <label>
        Дата рождения
        <input required type="date" value={form.dateOfBirth} onChange={set('dateOfBirth')} />
      </label>
      <label>
        Подразделение
        <select value={form.officeId} onChange={set('officeId')}>
          <option value="">Не указано</option>
          {offices.map(o => (
            <option key={o.id} value={o.id}>
              {o.name}
            </option>
          ))}
        </select>
      </label>
      <label>
        E-mail
        <input type="email" value={form.email} onChange={set('email')} />
      </label>
      <label>
        Телефон
        <input type="tel" value={form.phone} onChange={set('phone')} />
      </label>
      <label>
        Город
        <input value={form.city} onChange={set('city')} />
      </label>
    </>
  );
}

function toRequest(form: Form, withEmployee: boolean) {
  const account = { username: form.username, displayName: form.displayName, password: form.password, role: form.role };
  if (!withEmployee) return account;
  return {
    ...account,
    dateOfBirth: form.dateOfBirth,
    email: form.email || null,
    phone: form.phone || null,
    city: form.city || null,
    officeId: form.officeId ? Number(form.officeId) : null,
  };
}
