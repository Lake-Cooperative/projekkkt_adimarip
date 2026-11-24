import type { Permission, Session } from './types';

const storageKey = 'adimarip.session';

export function loadSession(): Session | null {
  try {
    const text = sessionStorage.getItem(storageKey);
    if (!text) return null;
    const session = JSON.parse(text) as Session;
    const expired = !session.token || !session.expiresAt || Date.parse(session.expiresAt) <= Date.now();
    if (expired || !Array.isArray(session.permissions)) {
      sessionStorage.removeItem(storageKey);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

export function saveSession(session: Session | null) {
  if (session) sessionStorage.setItem(storageKey, JSON.stringify(session));
  else sessionStorage.removeItem(storageKey);
}

/** Есть ли у пользователя хотя бы одно из прав. Только для UX: сервер проверяет права сам. */
export function can(session: Session | null, ...permissions: Permission[]): boolean {
  return !!session && permissions.some(p => session.permissions.includes(p));
}

/** Пользователь видит только свои оповещения (как служащий). */
export const seesOnlyOwn = (session: Session) => !can(session, 'NoticesReadAll') && can(session, 'NoticesReadOwn');

export const sessionExpiredEvent = 'adimarip:session-expired';
