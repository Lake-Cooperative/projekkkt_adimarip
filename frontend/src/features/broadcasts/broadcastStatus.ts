/** Рассылка ещё обрабатывается: отчёт нужно обновлять. */
export const isRunning = (status: string) => status === 'Queued' || status === 'Dispatching';
