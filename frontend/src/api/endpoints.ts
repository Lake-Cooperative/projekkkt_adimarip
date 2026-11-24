import { request } from './client';
import type {
  BroadcastDetails,
  BroadcastInput,
  BroadcastSummary,
  Me,
  Notice,
  NoticeDetail,
  Office,
  Page,
  PermissionItem,
  Recipient,
  RoleItem,
  Session,
  UserItem,
} from './types';

// Типизированные функции по ресурсам API — компоненты не собирают URL вручную.

export type NoticeFilter = { page: number; search: string; status: string; officeId: string };

export const authApi = {
  login: (username: string, password: string) =>
    request<Session>('POST', '/api/auth/login', null, { username, password }),
  me: (s: Session) => request<Me>('GET', '/api/auth/me', s),
};

export const noticesApi = {
  list: (s: Session, f: NoticeFilter) => {
    const params = new URLSearchParams({ page: String(f.page), pageSize: '15' });
    if (f.search.trim()) params.set('search', f.search.trim());
    if (f.status) params.set('status', f.status);
    if (f.officeId) params.set('officeId', f.officeId);
    return request<Page<Notice>>('GET', `/api/notices?${params}`, s);
  },
  get: (s: Session, id: number) => request<NoticeDetail>('GET', `/api/notices/${id}`, s),
  create: (s: Session, body: unknown) => request<{ id: number }>('POST', '/api/notices', s, body),
  changeStatus: (s: Session, id: number, status: string, comment: string) =>
    request('PATCH', `/api/notices/${id}/status`, s, { status, comment }),
  acknowledge: (s: Session, id: number) => request('POST', `/api/notices/${id}/acknowledge`, s),
  notify: (s: Session, id: number, channel: string, recipientAddress: string) =>
    request('POST', `/api/notices/${id}/notifications`, s, { channel, recipientAddress }),
  notifyBulk: (s: Session, noticeIds: number[], channel: string, recipientAddress: string) =>
    request<{ count: number; delivered: number; failed: number }>('POST', '/api/notices/notifications/bulk', s, {
      noticeIds,
      channel,
      recipientAddress,
    }),
  appeal: (s: Session, id: number, type: string, text: string) =>
    request('POST', `/api/notices/${id}/appeals`, s, { type, text }),
  document: (s: Session, id: number, name: string, mimeType: string, storageUri: string) =>
    request('POST', `/api/notices/${id}/documents`, s, { name, mimeType, storageUri }),
};

export const directoryApi = {
  offices: (s: Session) => request<Office[]>('GET', '/api/offices', s),
  recipients: (s: Session, search = '') =>
    request<Recipient[]>('GET', `/api/recipients?search=${encodeURIComponent(search)}`, s),
  archive: (s: Session, id: number, reason: string) => request('POST', `/api/recipients/${id}/archive`, s, { reason }),
};

export const adminApi = {
  users: (s: Session) => request<UserItem[]>('GET', '/api/admin/users', s),
  createUser: (s: Session, body: unknown) =>
    request<{ recipientId?: number | null }>('POST', '/api/admin/users', s, body),
  changeRole: (s: Session, username: string, role: string) =>
    request<UserItem>('PUT', `/api/admin/users/${encodeURIComponent(username)}/role`, s, { role }),
  roles: (s: Session) => request<RoleItem[]>('GET', '/api/roles', s),
  permissions: (s: Session) => request<PermissionItem[]>('GET', '/api/permissions', s),
  createRole: (s: Session, name: string, displayName: string, permissions: string[]) =>
    request<RoleItem>('POST', '/api/roles', s, { name, displayName, permissions }),
  updateRole: (s: Session, name: string, displayName: string, permissions: string[]) =>
    request<RoleItem>('PUT', `/api/roles/${encodeURIComponent(name)}`, s, { displayName, permissions }),
  deleteRole: (s: Session, name: string) => request('DELETE', `/api/roles/${encodeURIComponent(name)}`, s),
};

export const broadcastsApi = {
  list: (s: Session) => request<BroadcastSummary[]>('GET', '/api/broadcasts', s),
  get: (s: Session, id: number) => request<BroadcastDetails>('GET', `/api/broadcasts/${id}`, s),
  preview: (s: Session, audience: BroadcastInput['audience']) =>
    request<{ recipients: number; maxAllowed: number }>('POST', '/api/broadcasts/preview', s, audience),
  create: (s: Session, body: BroadcastInput) =>
    request<{ id: number; code: string; targetCount: number; plannedNotifications: number }>(
      'POST',
      '/api/broadcasts',
      s,
      body,
    ),
};
