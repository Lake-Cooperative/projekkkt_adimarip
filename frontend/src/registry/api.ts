export const apiBase = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

export type Role = 'Manager' | 'Operator' | 'Observer' | 'AutomationEngineer';
export type Session = {
  token: string;
  expiresAt: string;
  username: string;
  displayName: string;
  role: Role;
  surname?: string;
  recipientId?: number | null;
};
export type RecipientOption = { id: number; externalIdentifier: string; fullName: string };
export type Recipient = { id: number; externalIdentifier: string; fullName: string; dateOfBirth: string; email: string | null; phone: string | null };
export type Office = { id: number; name: string };
export type Notice = { id: number; number: string; recipientId: number; recipientName: string; officeId: number; officeName: string; createdAt: string; processingDeadline: string; reason: string; comment: string | null; currentStatus: string };
export type StatusEvent = { id: number; status: string; comment: string | null; changedAt: string; changedByUserId: string };
export type AuditEvent = { id: number; action: string; userId: string; occurredAt: string; details: string | null };
export type Notification = { id: number; channel: string; recipientAddress: string; createdAt: string; attempts: { status: string; attemptedAt: string }[] };
export type Appeal = { id: number; type: string; text: string; status: string; submittedAt: string };
export type DocumentMeta = { id: number; name: string; mimeType: string; storageUri: string; addedAt: string };
export type NoticeDetail = Notice & { history: StatusEvent[]; audit: AuditEvent[] | null; notifications: Notification[]; appeals: Appeal[]; documents: DocumentMeta[] };
export type Page<T> = { items: T[]; total: number; page: number; pageSize: number; totalPages: number };

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) { super(message); }
}

export function loadSession(): Session | null {
  try {
    const text = sessionStorage.getItem('adimarip.session');
    if (!text) return null;
    const session = JSON.parse(text) as Session;
    if (!session.token || !session.expiresAt || Date.parse(session.expiresAt) <= Date.now()) {
      sessionStorage.removeItem('adimarip.session');
      return null;
    }
    return session;
  } catch { return null; }
}
export function saveSession(session: Session | null) {
  if (session) sessionStorage.setItem('adimarip.session', JSON.stringify(session));
  else sessionStorage.removeItem('adimarip.session');
}

export async function api<T>(path: string, session: Session | null, init: RequestInit = {}): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, {
    ...init,
    headers: {
      ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      ...(session?.token ? { Authorization: `Bearer ${session.token}` } : {}),
      ...init.headers
    }
  });
  if (!response.ok) {
    // A rejected authenticated request should expire the local session consistently.
    if (response.status === 401 && session !== null) {
      saveSession(null);
      window.dispatchEvent(new Event('adimarip:session-expired'));
    }
    const result = await response.json().catch(() => null) as { error?: string; detail?: string; title?: string } | null;
    const fallback = response.status === 401 ? 'Войдите в систему повторно' : response.status === 403 ? 'Недостаточно прав' : `Ошибка HTTP ${response.status}`;
    throw new ApiError(response.status, result?.error ?? result?.detail ?? result?.title ?? fallback);
  }
  if (response.status === 204) return undefined as T;
  try {
    return await response.json() as T;
  } catch {
    throw new ApiError(response.status, 'Сервер вернул некорректный JSON. Проверьте API и прокси.');
  }
}
export const asJson = (value: unknown): RequestInit => ({ method: 'POST', body: JSON.stringify(value) });
