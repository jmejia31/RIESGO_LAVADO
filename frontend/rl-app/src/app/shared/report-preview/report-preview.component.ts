import { AfterViewInit, ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, OnDestroy, signal, viewChild } from '@angular/core';
import { ActionIconComponent } from '../components/action-icon/action-icon.component';
import { DataPaginationComponent } from '../components/data-pagination/data-pagination.component';
import { PageSizeSelectorComponent } from '../components/page-size-selector/page-size-selector.component';
import { InstitutionalTooltipDirective } from '../components/institutional-tooltip/institutional-tooltip.directive';
import { ReportPreviewService } from './report-preview.service';
import { getInstitutionalReportBranding } from '../../core/reporting/institutional-report-standard';
import { ConfiguracionService } from '../../core/configuration/configuracion.service';

@Component({
  selector: 'app-report-preview',
  standalone: true,
  imports: [ActionIconComponent, DataPaginationComponent, PageSizeSelectorComponent, InstitutionalTooltipDirective],
  templateUrl: './report-preview.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportPreviewComponent implements AfterViewInit, OnDestroy {
  readonly Math = Math;
  readonly configService = inject(ConfiguracionService);
  readonly institutionalLogoUrl = computed(() => {
    this.configService.configSistema();
    return getInstitutionalReportBranding().logoUrl;
  });
  readonly preview = inject(ReportPreviewService);
  readonly selectedSheet = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(50);
  readonly currentSheet = computed(() => this.preview.state()?.sheets?.[this.selectedSheet()] ?? null);
  readonly pageCount = computed(() => {
    const total = this.currentSheet()?.totalRows ?? 0;
    return Math.max(1, Math.ceil(total / this.pageSize()));
  });
  readonly headerRow = computed(() => this.currentSheet()?.headerRow ?? null);
  readonly contextRows = computed(() => this.currentSheet()?.contextRows ?? []);
  readonly contextRowRoles = computed(() => this.currentSheet()?.contextRowRoles ?? []);
  readonly currentRows = computed(() => {
    const rows = this.currentSheet()?.dataRows ?? [];
    const start = this.page() * this.pageSize();
    return rows.slice(start, start + this.pageSize());
  });
  readonly currentRowRoles = computed(() => {
    const roles = this.currentSheet()?.dataRowRoles ?? [];
    const start = this.page() * this.pageSize();
    return roles.slice(start, start + this.pageSize());
  });
  readonly horizontalScrollContentWidth = signal(0);
  private readonly excelTableScroll = viewChild<ElementRef<HTMLDivElement>>('excelTableScroll');
  private readonly excelHorizontalScroll = viewChild<ElementRef<HTMLDivElement>>('excelHorizontalScroll');
  private horizontalSyncTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      this.preview.state();
      this.selectedSheet.set(0);
      this.page.set(0);
      this.scheduleHorizontalScrollSync();
    });
    effect(() => {
      this.headerRow();
      this.currentRows();
      this.scheduleHorizontalScrollSync();
    });
  }

  ngAfterViewInit(): void {
    this.scheduleHorizontalScrollSync();
  }

  close(): void {
    this.preview.close();
  }

  download(): void {
    void this.preview.download();
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

  changePage(page: number): void {
    this.page.set(Math.max(0, page - 1));
  }

  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(0);
  }

  onTableScroll(event: Event): void {
    const source = event.currentTarget as HTMLDivElement | null;
    const target = this.excelHorizontalScroll()?.nativeElement;
    if (source && target && target.scrollLeft !== source.scrollLeft) target.scrollLeft = source.scrollLeft;
  }

  onHorizontalScroll(event: Event): void {
    const source = event.currentTarget as HTMLDivElement | null;
    const target = this.excelTableScroll()?.nativeElement;
    if (source && target && target.scrollLeft !== source.scrollLeft) target.scrollLeft = source.scrollLeft;
  }

  private scheduleHorizontalScrollSync(): void {
    if (this.horizontalSyncTimer !== null) clearTimeout(this.horizontalSyncTimer);
    this.horizontalSyncTimer = setTimeout(() => {
      this.horizontalSyncTimer = null;
      const viewport = this.excelTableScroll()?.nativeElement;
      const table = viewport?.querySelector('table');
      const width = table?.scrollWidth ?? 0;
      this.horizontalScrollContentWidth.set(width);
      const horizontalScroll = this.excelHorizontalScroll()?.nativeElement;
      if (viewport && horizontalScroll) horizontalScroll.scrollLeft = viewport.scrollLeft;
    }, 0);
  }

  ngOnDestroy(): void {
    if (this.horizontalSyncTimer !== null) clearTimeout(this.horizontalSyncTimer);
    this.preview.destroy();
  }
}
