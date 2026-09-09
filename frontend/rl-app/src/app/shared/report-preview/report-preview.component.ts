import { ChangeDetectionStrategy, Component, computed, effect, inject, OnDestroy, signal } from '@angular/core';
import { ActionIconComponent } from '../components/action-icon/action-icon.component';
import { ReportPreviewService } from './report-preview.service';

@Component({
  selector: 'app-report-preview',
  standalone: true,
  imports: [ActionIconComponent],
  templateUrl: './report-preview.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportPreviewComponent implements OnDestroy {
  readonly Math = Math;
  readonly preview = inject(ReportPreviewService);
  readonly selectedSheet = signal(0);
  readonly page = signal(0);
  readonly pageSize = 50;
  readonly currentSheet = computed(() => this.preview.state()?.sheets?.[this.selectedSheet()] ?? null);
  readonly pageCount = computed(() => {
    const total = this.currentSheet()?.totalRows ?? 0;
    return Math.max(1, Math.ceil(total / this.pageSize));
  });
  readonly headerRow = computed(() => this.currentSheet()?.headerRow ?? null);
  readonly contextRows = computed(() => this.currentSheet()?.contextRows ?? []);
  readonly currentRows = computed(() => {
    const rows = this.currentSheet()?.dataRows ?? [];
    const start = this.page() * this.pageSize;
    return rows.slice(start, start + this.pageSize);
  });
  readonly currentRowRoles = computed(() => {
    const roles = this.currentSheet()?.dataRowRoles ?? [];
    const start = this.page() * this.pageSize;
    return roles.slice(start, start + this.pageSize);
  });

  constructor() {
    effect(() => {
      this.preview.state();
      this.selectedSheet.set(0);
      this.page.set(0);
    });
  }

  close(): void {
    this.preview.close();
  }

  download(): void {
    this.preview.download();
  }

  selectSheet(index: number): void {
    this.selectedSheet.set(index);
    this.page.set(0);
  }

  previousPage(): void {
    this.page.update(value => Math.max(0, value - 1));
  }

  nextPage(): void {
    this.page.update(value => Math.min(this.pageCount() - 1, value + 1));
  }

  ngOnDestroy(): void {
    this.preview.destroy();
  }
}
