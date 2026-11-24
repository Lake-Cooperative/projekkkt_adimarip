import type { Severity } from '../api/types';
import { severityLabels, statusLabels } from '../shared/labels';

export const StatusChip = ({ status }: { status: string }) => (
  <span className={`status-chip ${status}`} title={status}>
    {statusLabels[status] ?? status}
  </span>
);

export const SeverityChip = ({ severity }: { severity: Severity | null }) =>
  severity ? <span className={`severity-chip ${severity}`}>{severityLabels[severity]}</span> : null;
