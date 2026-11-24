import { can, seesOnlyOwn } from '../api/session';
import type { Permission, Session } from '../api/types';

export type Section = 'notices' | 'broadcasts' | 'recipients' | 'users' | 'roles';

type SectionInfo = { id: Section; icon: string; title: (s: Session) => string; requires: Permission[] };

/** Меню строится из конфигурации по правам — без цепочек if по названиям ролей. */
export const sections: SectionInfo[] = [
  {
    id: 'notices',
    icon: '▤',
    title: s => (seesOnlyOwn(s) ? 'Мои оповещения' : 'Реестр оповещений'),
    requires: ['NoticesReadAll', 'NoticesReadOwn'],
  },
  { id: 'broadcasts', icon: '📣', title: () => 'Массовое оповещение', requires: ['BroadcastsSend', 'BroadcastsView'] },
  { id: 'recipients', icon: '♙', title: () => 'Адресаты', requires: ['RecipientsRead'] },
  { id: 'users', icon: '⚙', title: () => 'Учётные записи', requires: ['UsersManage'] },
  { id: 'roles', icon: '🛡', title: () => 'Роли и права', requires: ['RolesManage'] },
];

export const availableSections = (session: Session) => sections.filter(s => can(session, ...s.requires));
