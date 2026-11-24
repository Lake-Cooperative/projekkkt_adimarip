import { useCallback, useEffect, useState } from 'react';
import { authApi } from '../api/endpoints';
import { loadSession, saveSession, sessionExpiredEvent } from '../api/session';
import type { Session } from '../api/types';

/** Сессия пользователя: хранение, авто-выход при 401 и обновление прав после перезагрузки страницы. */
export function useSession() {
  const [session, setSession] = useState<Session | null>(loadSession);

  const replace = useCallback((next: Session | null) => {
    saveSession(next);
    setSession(next);
  }, []);

  useEffect(() => {
    const expire = () => setSession(null);
    window.addEventListener(sessionExpiredEvent, expire);
    return () => window.removeEventListener(sessionExpiredEvent, expire);
  }, []);

  // Роль могли изменить, пока вкладка была закрыта: права перечитываются с сервера один раз при открытии.
  useEffect(() => {
    const stored = loadSession();
    if (!stored) return;
    authApi
      .me(stored)
      .then(me => replace({ ...stored, ...me }))
      .catch(() => undefined);
  }, [replace]);

  return { session, login: replace, logout: () => replace(null) };
}
