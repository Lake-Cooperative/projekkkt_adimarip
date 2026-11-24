import { useCallback } from 'react';
import { adminApi, directoryApi } from '../api/endpoints';
import type { Office, PermissionItem, Recipient, RoleItem, Session } from '../api/types';
import { useAsync } from './useAsync';

// Загрузка справочников, которые нужны в нескольких разделах.

const noOffices: Office[] = [];
const noRecipients: Recipient[] = [];
const noRoles: RoleItem[] = [];
const noPermissions: PermissionItem[] = [];

export function useOffices(session: Session) {
  const load = useCallback(() => directoryApi.offices(session), [session]);
  return useAsync(load, noOffices);
}

/** Справочник адресатов; при `enabled = false` (нет права) запрос не выполняется. */
export function useRecipients(session: Session, search = '', enabled = true) {
  const load = useCallback(
    () => (enabled ? directoryApi.recipients(session, search) : Promise.resolve(noRecipients)),
    [session, search, enabled],
  );
  return useAsync(load, noRecipients);
}

export function useRoles(session: Session) {
  const load = useCallback(() => adminApi.roles(session), [session]);
  return useAsync(load, noRoles);
}

export function usePermissionCatalog(session: Session) {
  const load = useCallback(() => adminApi.permissions(session), [session]);
  return useAsync(load, noPermissions);
}
