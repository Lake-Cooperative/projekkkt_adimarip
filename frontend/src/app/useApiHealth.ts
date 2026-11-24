import { useEffect, useState } from 'react';
import { apiBase } from '../api/client';

export type Health = 'loading' | 'ok' | 'error';

export function useApiHealth(): Health {
  const [health, setHealth] = useState<Health>('loading');
  useEffect(() => {
    fetch(`${apiBase}/api/health`)
      .then(response => setHealth(response.ok ? 'ok' : 'error'))
      .catch(() => setHealth('error'));
  }, []);
  return health;
}
