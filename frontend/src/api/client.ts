import { saveSession, sessionExpiredEvent } from './session';
import type { Session } from './types';

export const apiBase = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

type Method = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
type Problem = { error?: string; detail?: string; title?: string };

const statusMessages: Record<number, string> = {
  401: 'Войдите в систему повторно',
  403: 'Недостаточно прав',
};

function headersFor(session: Session | null, hasBody: boolean): HeadersInit {
  const headers: Record<string, string> = {};
  if (hasBody) headers['Content-Type'] = 'application/json';
  if (session?.token) headers.Authorization = `Bearer ${session.token}`;
  return headers;
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem = (await response.json().catch(() => null)) as Problem | null;
  const message =
    problem?.error ??
    problem?.detail ??
    problem?.title ??
    statusMessages[response.status] ??
    `Ошибка HTTP ${response.status}`;
  return new ApiError(response.status, message);
}

/** Сессия истекла или отозвана: забываем её и сообщаем приложению. */
function expireSession() {
  saveSession(null);
  window.dispatchEvent(new Event(sessionExpiredEvent));
}

/** Единая точка обращения к API: заголовки, разбор ошибок, авто-выход при 401. */
export async function request<T>(method: Method, path: string, session: Session | null, body?: unknown): Promise<T> {
  const hasBody = body !== undefined;
  const response = await fetch(`${apiBase}${path}`, {
    method,
    headers: headersFor(session, hasBody),
    body: hasBody ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    if (response.status === 401 && session) expireSession();
    throw await toApiError(response);
  }
  if (response.status === 204) return undefined as T;
  try {
    return (await response.json()) as T;
  } catch {
    throw new ApiError(response.status, 'Сервер вернул некорректный JSON. Проверьте API и прокси.');
  }
}

export const errorText = (error: unknown, fallback: string) => (error instanceof Error ? error.message : fallback);
