import type { Channel, Severity } from '../api/types';

export const statusLabels: Record<string, string> = {
  Created: 'Создано',
  Sent: 'Отправлено',
  Delivered: 'Доставлено',
  Read: 'Прочитано',
  Closed: 'Закрыто',
  Cancelled: 'Отменено',
  Failed: 'Ошибка доставки',
};
export const statuses = Object.keys(statusLabels);

export const channelLabels: Record<Channel, string> = {
  Email: 'E-mail',
  Sms: 'SMS',
  Portal: 'Портал',
  Max: 'MAX (макет)',
};
export const channels = Object.keys(channelLabels) as Channel[];

/** Тестовый адрес по умолчанию, подходящий формату канала. */
export const sampleAddress: Record<Channel, string> = {
  Email: 'demo@example.invalid',
  Sms: '+70000000000',
  Portal: 'portal:demo',
  Max: '+70000000000',
};

export const severityLabels: Record<Severity, string> = { Info: 'Информация', Warning: 'Внимание', Critical: 'Срочно' };
export const severities = Object.keys(severityLabels) as Severity[];

export const broadcastStatusLabels: Record<string, string> = {
  Queued: 'В очереди',
  Dispatching: 'Идёт рассылка',
  Completed: 'Завершено',
  CompletedWithErrors: 'Завершено с ошибками',
};

export const formatDateTime = (value: string | null) => (value ? new Date(value).toLocaleString('ru-RU') : '—');
