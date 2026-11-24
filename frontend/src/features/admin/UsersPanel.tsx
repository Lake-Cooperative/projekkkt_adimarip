import { useCallback } from 'react';
import { adminApi } from '../../api/endpoints';
import type { Session, UserItem } from '../../api/types';
import { Feedback } from '../../components/Feedback';
import { useOffices, useRoles } from '../../hooks/queries';
import { useAsync } from '../../hooks/useAsync';
import { CreateUserForm } from './CreateUserForm';
import { UsersTable } from './UsersTable';

const noUsers: UserItem[] = [];

/** Учётные записи: создание и смена роли. */
export function UsersPanel({ session }: { session: Session }) {
  const roles = useRoles(session);
  const offices = useOffices(session);
  const loadUsers = useCallback(() => adminApi.users(session), [session]);
  const users = useAsync(loadUsers, noUsers);

  return (
    <>
      <Feedback error={roles.error || offices.error || users.error} />
      <CreateUserForm session={session} roles={roles.data} offices={offices.data} onCreated={users.reload} />
      <UsersTable session={session} users={users.data} roles={roles.data} onChanged={users.reload} />
    </>
  );
}
