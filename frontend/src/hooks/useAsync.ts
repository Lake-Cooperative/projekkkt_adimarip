import { useCallback, useEffect, useRef, useState } from 'react';
import { errorText } from '../api/client';

export type AsyncState<T> = {
  data: T;
  loading: boolean;
  error: string;
  reload: () => Promise<void>;
};

/**
 * Загрузка данных с состояниями «загрузка / ошибка / данные».
 * `load` должен быть мемоизирован (useCallback): при его смене данные загружаются заново.
 * Ответ устаревшего запроса отбрасывается, поэтому быстрый ввод в поиске не «перепрыгивает».
 */
export function useAsync<T>(load: () => Promise<T>, initial: T): AsyncState<T> {
  const [data, setData] = useState<T>(initial);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const latestRequest = useRef(0);

  const reload = useCallback(async () => {
    const request = ++latestRequest.current;
    setLoading(true);
    try {
      const result = await load();
      if (request !== latestRequest.current) return;
      setData(result);
      setError('');
    } catch (e) {
      if (request === latestRequest.current) setError(errorText(e, 'Не удалось загрузить данные'));
    } finally {
      if (request === latestRequest.current) setLoading(false);
    }
  }, [load]);

  useEffect(() => {
    void reload();
  }, [reload]);

  return { data, loading, error, reload };
}
