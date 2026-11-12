import { Injectable } from '@angular/core';

export interface InstitutionalOverlayHandle {
  close: () => void;
}

@Injectable({ providedIn: 'root' })
export class InstitutionalOverlayRegistry {
  private readonly overlays = new Set<InstitutionalOverlayHandle>();

  register(handle: InstitutionalOverlayHandle): void {
    this.overlays.add(handle);
  }

  unregister(handle: InstitutionalOverlayHandle): void {
    this.overlays.delete(handle);
  }

  activate(handle: InstitutionalOverlayHandle): void {
    for (const overlay of this.overlays) {
      if (overlay !== handle) overlay.close();
    }
  }
}
