import type { SessionListDto } from '../types/sessions';

/**
 * Format a time string (HH:mm:ss) to 12-hour format (H:MM AM/PM)
 */
export function formatTime(time: string): string {
  const [hours, minutes] = time.split(':').map(Number);
  const ampm = hours >= 12 ? 'PM' : 'AM';
  const displayHours = hours % 12 || 12;
  return `${displayHours}:${minutes.toString().padStart(2, '0')} ${ampm}`;
}

/**
 * Format session details for the Recent Sessions table.
 * Builds a context-aware summary based on what data exists.
 */
export function formatSessionDetails(session: SessionListDto): string {
  const parts: string[] = [];

  // Chrono: "10rds @ 2880fps (SD: 13.3)"
  if (session.hasChronoData && session.averageVelocity) {
    const rounds = session.velocityReadingCount || session.chronoCount;
    let detail = `${rounds}rds @ ${Math.round(session.averageVelocity)}fps`;
    if (session.standardDeviation) {
      detail += ` (SD: ${session.standardDeviation.toFixed(1)})`;
    }
    parts.push(detail);
  }

  // DOPE: "DOPE: 100-500yds" or "DOPE @ 100yds"
  if (session.dopeEntryCount > 0) {
    if (session.minDopeDistance != null && session.maxDopeDistance != null) {
      if (session.minDopeDistance === session.maxDopeDistance) {
        parts.push(`DOPE @ ${session.minDopeDistance}yds`);
      } else {
        parts.push(`DOPE: ${session.minDopeDistance}-${session.maxDopeDistance}yds`);
      }
    } else {
      parts.push(`${session.dopeEntryCount} DOPE entries`);
    }
  }

  // Groups: "Best: 0.8 MOA @ 100yds"
  if (session.groupEntryCount > 0) {
    if (session.bestGroupMoa != null) {
      let detail = `Best: ${session.bestGroupMoa.toFixed(2)} MOA`;
      if (session.bestGroupDistance != null) {
        detail += ` @ ${session.bestGroupDistance}yds`;
      }
      parts.push(detail);
    } else {
      parts.push(`${session.groupEntryCount} groups`);
    }
  }

  return parts.join(' • ') || '-';
}
