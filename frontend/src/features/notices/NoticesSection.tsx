import { useState } from 'react';
import type { Session } from '../../api/types';
import { CreateNoticePanel } from './CreateNoticePanel';
import { NoticeDetailPanel } from './NoticeDetailPanel';
import { NoticesPanel } from './NoticesPanel';

type View = { kind: 'list' } | { kind: 'create' } | { kind: 'detail'; id: number };

/** Навигация внутри раздела «Оповещения»: список → карточка / форма создания. */
export function NoticesSection({ session }: { session: Session }) {
  const [view, setView] = useState<View>({ kind: 'list' });
  const toList = () => setView({ kind: 'list' });
  const open = (id: number) => setView({ kind: 'detail', id });

  switch (view.kind) {
    case 'create':
      return <CreateNoticePanel session={session} onBack={toList} onSaved={open} />;
    case 'detail':
      return <NoticeDetailPanel session={session} id={view.id} onBack={toList} />;
    default:
      return <NoticesPanel session={session} onSelect={open} onCreate={() => setView({ kind: 'create' })} />;
  }
}
