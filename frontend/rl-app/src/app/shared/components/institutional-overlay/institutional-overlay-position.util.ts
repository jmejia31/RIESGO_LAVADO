export interface InstitutionalOverlayPlacement {
  direction: 'up' | 'down';
  left: number;
  top: number;
  width: number;
  maxHeight: number;
}

export function calculateInstitutionalOverlayPlacement(
  anchor: Pick<DOMRect, 'left' | 'right' | 'top' | 'bottom' | 'width'>,
  viewport: { width: number; height: number },
  constrainTo?: Pick<DOMRect, 'left' | 'right' | 'top' | 'bottom'>,
  preferredHeight = 320,
  preferredWidth = anchor.width,
  gap = 4,
  margin = 12
): InstitutionalOverlayPlacement {
  const leftBound = Math.max(margin, constrainTo?.left ?? margin);
  const rightBound = Math.min(viewport.width - margin, constrainTo?.right ?? viewport.width - margin);
  const topBound = Math.max(margin, constrainTo?.top ?? margin);
  const bottomBound = Math.min(viewport.height - margin, constrainTo?.bottom ?? viewport.height - margin);
  const below = Math.max(0, bottomBound - anchor.bottom - gap);
  const above = Math.max(0, anchor.top - topBound - gap);
  const direction: InstitutionalOverlayPlacement['direction'] = below >= Math.min(preferredHeight, 160) || below >= above ? 'down' : 'up';
  const maxHeight = Math.max(0, Math.min(preferredHeight, direction === 'down' ? below : above));
  const width = Math.min(Math.max(0, preferredWidth), Math.max(0, rightBound - leftBound));
  const left = Math.min(Math.max(anchor.left, leftBound), Math.max(leftBound, rightBound - width));
  const top = direction === 'down' ? anchor.bottom + gap : anchor.top - gap - maxHeight;

  return { direction, left, top, width, maxHeight };
}
