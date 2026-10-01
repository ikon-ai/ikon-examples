import type { CSSProperties } from 'react';

// The panels are React, but they sit inside Parallax cards, so they draw with the theme's own
// variables: the same borders, radii, type and colours as the C#-rendered rows around them, in light
// and dark alike.
const button: CSSProperties = {
  border: '1px solid var(--border-primary)',
  borderRadius: 'var(--radius-md)',
  padding: '6px 12px',
  cursor: 'pointer',
  background: 'transparent',
  color: 'var(--text-primary)',
  font: 'inherit',
  fontSize: 14,
  fontWeight: 500,
};

export const panelStyles = {
  container: { display: 'flex', flexDirection: 'column', gap: 10, width: '100%', color: 'var(--text-primary)' },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: 12,
    flexWrap: 'wrap',
    borderBottom: '1px solid var(--border-secondary)',
    padding: '8px 0',
  },
  actions: { display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' },
  caption: { color: 'var(--text-tertiary)', fontSize: 13 },
  value: { fontSize: 16, overflowWrap: 'anywhere' },
  heading: { fontWeight: 600, marginTop: 8 },
  button,
  ghostButton: { ...button, border: '1px solid transparent' },
  dangerButton: { ...button, border: 'none', background: 'var(--bg-error-solid)', color: 'var(--text-error-button)' },
  input: { ...button, cursor: 'text', minWidth: 220, fontWeight: 400, background: 'var(--bg-primary)' },
  video: { width: 320, maxWidth: '100%', aspectRatio: '16 / 9', background: 'var(--bg-secondary)', borderRadius: 'var(--radius-md)' },
} satisfies Record<string, CSSProperties>;

export function resultStyle(text: string): CSSProperties {
  const color = text.startsWith('PASS') ? 'var(--text-success-primary)' : text.startsWith('SKIP') ? 'var(--text-warning-primary)' : 'var(--text-error-primary)';
  return { ...panelStyles.value, color };
}

export function describeError(error: unknown): string {
  return error instanceof Error && error.message ? error.message : String(error);
}
