import { useState } from 'react';
import { errorText } from '../api/client';

type SuccessMessage<T> = string | ((result: T) => string);

/** Изменяющее действие: блокировка повторного нажатия, текст ошибки и текст успеха. */
export function useAction() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  /** Возвращает true, если действие выполнено. Текст успеха может зависеть от ответа сервера. */
  async function run<T>(action: () => Promise<T>, message: SuccessMessage<T> = 'Действие выполнено'): Promise<boolean> {
    if (busy) return false;
    setBusy(true);
    setError('');
    setSuccess('');
    try {
      const result = await action();
      setSuccess(typeof message === 'function' ? message(result) : message);
      return true;
    } catch (e) {
      setError(errorText(e, 'Не удалось выполнить действие'));
      return false;
    } finally {
      setBusy(false);
    }
  }

  return { busy, error, success, run };
}
