// Контракт API. Имена полей совпадают с JSON, который отдаёт backend.

export type Permission =
  | 'NoticesReadAll'
  | 'NoticesReadOwn'
  | 'NoticesCreate'
  | 'NoticesChangeStatus'
  | 'NoticesAcknowledgeOwn'
  | 'NotificationsSend'
  | 'BroadcastsSend'
  | 'BroadcastsView'
  | 'AppealsCreate'
  | 'DocumentsAttach'
  | 'RecipientsRead'
  | 'RecipientsCreate'
  | 'RecipientsArchive'
  | 'HistoryView'
  | 'AuditView'
  | 'UsersManage'
  | 'RolesManage';

export type Session = {
  token: string;
  expiresAt: string;
  username: string;
  displayName: string;
  role: string;
  roleDisplayName?: string;
  surname?: string;
  recipientId?: number | null;
  permissions: Permission[];
};

export type Me = Pick<Session, 'username' | 'displayName' | 'role' | 'roleDisplayName' | 'recipientId' | 'permissions'>;

export type Office = { id: number; name: string };
export type Recipient = {
  id: number;
  externalIdentifier: string;
  fullName: string;
  dateOfBirth: string;
  email: string | null;
  phone: string | null;
  officeId: number | null;
  officeName: string | null;
};

export type Severity = 'Info' | 'Warning' | 'Critical';
export type Channel = 'Email' | 'Sms' | 'Portal' | 'Max';

export type Notice = {
  id: number;
  number: string;
  recipientId: number;
  recipientName: string;
  officeId: number;
  officeName: string;
  createdAt: string;
  processingDeadline: string;
  reason: string;
  comment: string | null;
  currentStatus: string;
  broadcastId: number | null;
  severity: Severity | null;
};
export type StatusEvent = {
  id: number;
  status: string;
  comment: string | null;
  changedAt: string;
  changedByUserId: string;
};
export type AuditEvent = { id: number; action: string; userId: string; occurredAt: string; details: string | null };
export type NotificationItem = {
  id: number;
  channel: Channel;
  recipientAddress: string;
  createdAt: string;
  status: string;
  lastError: string | null;
  attempts: { status: string; attemptedAt: string }[];
};
export type Appeal = { id: number; type: string; text: string; status: string; submittedAt: string };
export type DocumentMeta = { id: number; name: string; mimeType: string; storageUri: string; addedAt: string };
export type NoticeDetail = Notice & {
  allowedTransitions: string[];
  canAcknowledge: boolean;
  history: StatusEvent[];
  audit: AuditEvent[] | null;
  notifications: NotificationItem[];
  appeals: Appeal[];
  documents: DocumentMeta[];
};
export type Page<T> = { items: T[]; total: number; page: number; pageSize: number; totalPages: number };

export type UserItem = {
  username: string;
  displayName: string;
  role: string;
  recipientId: number | null;
  createdAt: string;
};
export type RoleItem = {
  name: string;
  displayName: string;
  isSystem: boolean;
  scope: 'All' | 'Own' | 'None';
  requiresRecipientBinding: boolean;
  permissions: Permission[];
  usersCount: number;
};
export type PermissionItem = { code: Permission; group: string; description: string };

export type AudienceKind = 'All' | 'Offices' | 'Recipients';
export type BroadcastInput = {
  title: string;
  message: string;
  severity: Severity;
  channels: Channel[];
  officeId: number;
  audience: { kind: AudienceKind; officeIds: number[]; recipientIds: number[] };
};
export type BroadcastSummary = {
  id: number;
  code: string;
  title: string;
  severity: Severity;
  status: string;
  channels: Channel[];
  audienceDescription: string;
  createdByUserId: string;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
  targetCount: number;
  deliveredCount: number;
  failedCount: number;
};
export type BroadcastDetails = {
  broadcast: BroadcastSummary;
  progress: {
    total: number;
    delivered: number;
    failed: number;
    pending: number;
    acknowledged: number;
    channels: { channel: Channel; delivered: number; failed: number; pending: number }[];
  };
  failures: { noticeId: number; recipientName: string; channel: Channel; address: string; error: string | null }[];
};
