import { ChangeDetectionStrategy, Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ListasService } from '../../data-access/listas.service';
import { CoincidenciaJuridica, CoincidenciaNatural, CoincidenciaEmpleado, DetalleCoincidenciaNatural, DetalleCoincidenciaEmpleado, TipoDocumento, TipoListaCautela, RegistrarPositivoDto, Seguimiento, Evidencia, EvidenciaPolitica } from '../../models/listas.models';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import * as XLSX from '../../../../../core/utils/excel-export.util';
import { of, forkJoin } from 'rxjs';

type FiltroTipo = 'juridica' | 'natural' | 'empleado';
type FiltroEstado = 'todos' | 'pendiente' | 'con_motivo' | 'cerrado_pasivo';
type RangoSeguimientoReporte = { desde?: string; hasta?: string; texto: string };

@Component({
  selector: 'app-monitoreo-listas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './monitoreo-listas.component.html',
})
export class MonitoreoListasComponent implements OnInit {
  private sanitizer = inject(DomSanitizer);
  private configService = inject(ConfiguracionService);
  private filtroSeguimientoTimer: ReturnType<typeof setTimeout> | null = null;
  readonly maxTextoTextarea = 1000;

  tipoActivo = signal<FiltroTipo>('juridica');
  cargando = signal(false);
  busqueda = signal('');
  filtroEstado = signal<FiltroEstado>('todos');
  filtroFechaDesde = signal<string>('');
  filtroFechaHasta = signal<string>('');

  // Modal Detalle
  modalDetalleAbierto = signal(false);
  detalleCargando = signal(false);
  detallesNatural = signal<DetalleCoincidenciaNatural[]>([]);
  detallesEmpleado = signal<DetalleCoincidenciaEmpleado[]>([]);
  personaSeleccionada = signal<CoincidenciaNatural | null>(null);
  personaSeleccionadaEmpleado = signal<CoincidenciaEmpleado | null>(null);

  // Visor PDF
  pdfUrl = signal<SafeResourceUrl | null>(null);
  pdfModalAbierto = signal(false);

  // Modal Registrar Motivo
  modalMotivoAbierto = signal(false);
  guardandoMotivo = signal(false);
  listaTiposDocumento = signal<TipoDocumento[]>([]);
  entidadSeleccionada = signal<any | null>(null);
  formTipoDocId = signal<number | null>(null);
  formMotivo = signal<string>('');
  listaTiposListasCautela = signal<TipoListaCautela[]>([]);
  origenesRegistro = [
    { valor: 'DNP_LISTAS', etiqueta: 'Coincidencia DNP / Listas' },
    { valor: 'MANUAL_CUMPLIMIENTO', etiqueta: 'Registro manual Sección de Cumplimiento' },
    { valor: 'NOTICIA_PRENSA', etiqueta: 'Noticia / Prensa / Medio externo' },
    { valor: 'OTRO', etiqueta: 'Otro' }
  ];
  formOrigenRegistro = signal<string>('DNP_LISTAS');
  politicaEvidencias = signal<EvidenciaPolitica>({
    maximoMb: 10,
    maximoBytes: 10 * 1024 * 1024,
    extensionesPermitidas: ['.pdf', '.png', '.jpg', '.jpeg', '.doc', '.docx', '.xls', '.xlsx'],
    tiposPermitidosTexto: 'PDF, imágenes, Word, Excel'
  });
  formTipoListaCautelaId = signal<number | null>(null);
  esRegistroManual = signal<boolean>(false);
  formManualNombre = signal<string>('');
  formManualNoDocumento = signal<string>('');
  formManualTipoPositivoId = signal<number | null>(null);
  formSeguimientoComentario = signal<string>('');
  archivosSeguimiento = signal<File[]>([]);

  // Modal Seguimiento
  modalSeguimientoAbierto = signal(false);
  cargandoSeguimiento = signal(false);
  listaSeguimientos = signal<Seguimiento[]>([]);
  formComentarioSeguimiento = signal<string>('');
  filtroSeguimientoDesde = signal<string>('');
  filtroSeguimientoHasta = signal<string>('');
  reporteSeguimientoDocumento = signal<string>('');
  reporteSeguimientoDesde = signal<string>('');
  reporteSeguimientoHasta = signal<string>('');
  archivosSeleccionados = signal<File[]>([]);
  guardandoSeguimiento = signal(false);

  // Variables de Edición de Seguimiento
  modoEdicion = signal(false);
  seguimientoEditandoId = signal<number | null>(null);
  evidenciasExistentes = signal<Evidencia[]>([]);

  totalCoincidencias = computed(() => {
    return this.tipoActivo() === 'empleado' ? this.detallesEmpleado().length : this.detallesNatural().length;
  });
  
  coincidenciasPep = computed(() => {
    if (this.tipoActivo() === 'empleado') return 0;
    return this.detallesNatural().filter(det => det.esPep === 'SI' || det.esPep === 'S').length;
  });

  empresasUnicas = computed(() => {
    const list = this.tipoActivo() === 'empleado' 
      ? this.detallesEmpleado().map(det => det.nombreEmpresa?.trim() || '')
      : this.detallesNatural().map(det => det.nombreEmpresa?.trim() || '');
    return new Set(list.filter(x => x !== '')).size;
  });

  // Datos crudos de la API
  juridicasRaw = signal<CoincidenciaJuridica[]>([]);
  naturalesRaw = signal<CoincidenciaNatural[]>([]);
  empleadosRaw = signal<CoincidenciaEmpleado[]>([]);

  datosActivos = computed<Array<CoincidenciaJuridica | CoincidenciaNatural | CoincidenciaEmpleado>>(() => {
    if (this.tipoActivo() === 'juridica') return this.juridicasRaw();
    if (this.tipoActivo() === 'natural') return this.naturalesRaw();
    return this.empleadosRaw();
  });

  etiquetaTipoActivo = computed(() => {
    if (this.tipoActivo() === 'juridica') return 'Personas juridicas';
    if (this.tipoActivo() === 'natural') return 'Personas naturales';
    return 'Empleados';
  });

  totalActual = computed(() => this.datosActivos().filter(item => !this.esCerradoPasivo(item)).length);
  pendientesActual = computed(() => this.datosActivos().filter(item => !this.esCerradoPasivo(item) && (!item.tieneMotivo || !!item.esManual)).length);
  conMotivoActual = computed(() => this.datosActivos().filter(item => !this.esCerradoPasivo(item) && !!item.tieneMotivo && !item.esManual).length);
  cerradosPasivosActual = computed(() => this.datosActivos().filter(item => this.esCerradoPasivo(item)).length);

  // Paginación
  paginaActual = signal(1);
  limite = signal(10);

  constructor(private listasService: ListasService) {}

  ngOnInit() {
    this.cargarDatos();
    this.listasService.getTiposDocumento().subscribe({
      next: (res) => this.listaTiposDocumento.set(res),
      error: (err) => console.error('Error al cargar tipos de documento:', err)
    });
    this.listasService.getTiposListasCautela().subscribe({
      next: (res) => this.listaTiposListasCautela.set(res),
      error: (err) => console.error('Error al cargar tipos de listas de cautela:', err)
    });
    this.listasService.getPoliticaEvidencias().subscribe({
      next: (res) => this.politicaEvidencias.set({
        ...res,
        extensionesPermitidas: res.extensionesPermitidas?.length ? res.extensionesPermitidas : this.politicaEvidencias().extensionesPermitidas
      }),
      error: (err) => console.warn('No se pudo cargar la política de evidencias; se usará la política por defecto.', err)
    });
  }

  cambiarTipo(tipo: FiltroTipo) {
    this.tipoActivo.set(tipo);
    this.busqueda.set('');
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  limpiarFiltrosPrincipales() {
    this.busqueda.set('');
    this.filtroEstado.set('todos');
    this.filtroFechaDesde.set('');
    this.filtroFechaHasta.set('');
    this.paginaActual.set(1);
  }

  private limitarTexto(valor: string | null | undefined): string {
    return (valor || '').slice(0, this.maxTextoTextarea);
  }

  actualizarMotivoIngreso(valor: string) {
    this.formMotivo.set(this.limitarTexto(valor));
  }

  actualizarComentarioSeguimientoInicial(valor: string) {
    this.formSeguimientoComentario.set(this.limitarTexto(valor));
  }

  actualizarComentarioSeguimiento(valor: string) {
    this.formComentarioSeguimiento.set(this.limitarTexto(valor));
  }

  cargarDatos() {
    this.cargando.set(true);
    const tipo = this.tipoActivo();

    if (tipo === 'juridica') {
      this.listasService.getJuridicas().subscribe({
        next: (res) => { this.juridicasRaw.set(res); this.cargando.set(false); },
        error: () => { this.juridicasRaw.set([]); this.cargando.set(false); }
      });
    } else if (tipo === 'natural') {
      this.listasService.getNaturales().subscribe({
        next: (res) => { this.naturalesRaw.set(res); this.cargando.set(false); },
        error: () => { this.naturalesRaw.set([]); this.cargando.set(false); }
      });
    } else if (tipo === 'empleado') {
      this.listasService.getEmpleados().subscribe({
        next: (res) => { this.empleadosRaw.set(res); this.cargando.set(false); },
        error: () => { this.empleadosRaw.set([]); this.cargando.set(false); }
      });
    }
  }

  // Filtrado reactivo en memoria
  datosFiltrados = computed(() => {
    const query = this.busqueda().trim().toLowerCase();
    const tipo = this.tipoActivo();
    const estado = this.filtroEstado();
    const desde = this.filtroFechaDesde();
    const hasta = this.filtroFechaHasta();

    if (tipo === 'juridica') {
      const data = this.juridicasRaw();
      return data.filter(item =>
        this.coincideTexto(item, query, [item.nombre, item.rtn, item.numeroPatrono, item.listaCoincidencia]) &&
        this.coincideEstado(item, estado) &&
        this.coincideFecha(item, desde, hasta)
      );
    } else if (tipo === 'natural') {
      const data = this.naturalesRaw();
      return data.filter(item =>
        this.coincideTexto(item, query, [item.nombre, item.numeroIdentificacion, item.listaCoincidencia]) &&
        this.coincideEstado(item, estado) &&
        this.coincideFecha(item, desde, hasta)
      );
    } else {
      const data = this.empleadosRaw();
      return data.filter(item =>
        this.coincideTexto(item, query, [item.nombre, item.identidad, item.listaCoincidencia]) &&
        this.coincideEstado(item, estado) &&
        this.coincideFecha(item, desde, hasta)
      );
    }
  });

  private coincideTexto(item: unknown, query: string, valores: Array<string | null | undefined>): boolean {
    if (!query) return true;
    return valores.some(valor => (valor || '').toLowerCase().includes(query));
  }

  private coincideEstado(item: { tieneMotivo?: boolean; esManual?: boolean }, estado: FiltroEstado): boolean {
    const cerradoPasivo = this.esCerradoPasivo(item);
    if (estado === 'todos') return !cerradoPasivo;
    if (estado === 'cerrado_pasivo') return cerradoPasivo;
    if (cerradoPasivo) return false;
    if (estado === 'con_motivo') return !!item.tieneMotivo && !item.esManual;
    if (estado === 'pendiente') return !item.tieneMotivo || !!item.esManual;
    return true;
  }

  private esCerradoPasivo(item: unknown): boolean {
    const estado = String(
      (item as { estadoRegistro?: unknown; estado?: unknown; estatus?: unknown })?.estadoRegistro ??
      (item as { estado?: unknown })?.estado ??
      (item as { estatus?: unknown })?.estatus ??
      ''
    ).trim().toUpperCase();

    const activo = (item as { activo?: unknown; esActivo?: unknown })?.activo ?? (item as { esActivo?: unknown })?.esActivo;
    return estado === 'CERRADO' || estado === 'CERRADA' || estado === 'PASIVO' || estado === 'PASIVA' || estado === 'SUSPENDIDO' || estado === 'SUSPENDIDA' || activo === false || activo === 0;
  }

  private coincideFecha(item: { fechaEncontro?: string | null; fechaRegistroInterno?: string | null }, desde: string, hasta: string): boolean {
    if (!desde && !hasta) return true;

    const fechaBase = item.fechaEncontro || item.fechaRegistroInterno;
    if (!fechaBase) return false;

    const fecha = new Date(fechaBase);
    if (Number.isNaN(fecha.getTime())) return false;

    if (desde) {
      const min = new Date(`${desde}T00:00:00`);
      if (fecha < min) return false;
    }

    if (hasta) {
      const max = new Date(`${hasta}T23:59:59`);
      if (fecha > max) return false;
    }

    return true;
  }

  obtenerEstadoMonitoreo(item: { tieneMotivo?: boolean; esManual?: boolean }): string {
    if (this.esCerradoPasivo(item)) return 'Cerrado / Pasivo';
    if (item.esManual) return 'Pendiente';
    return item.tieneMotivo ? 'Con motivo' : 'Pendiente';
  }

  obtenerEstadoMonitoreoReporte(item: { tieneMotivo?: boolean; esManual?: boolean }): string {
    const estado = this.obtenerEstadoMonitoreo(item);
    if (estado === 'Con motivo') return 'CON MOTIVO REGISTRADO';
    if (estado === 'Cerrado / Pasivo') return 'CERRADO / PASIVO';
    return 'PENDIENTE DE REGISTRO';
  }

  obtenerClaseEstado(item: { tieneMotivo?: boolean; esManual?: boolean }): string {
    if (this.esCerradoPasivo(item))
      return 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-slate-100 text-slate-700 ring-1 ring-slate-300';

    return item.tieneMotivo && !item.esManual
      ? 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/10'
      : 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-amber-50 text-amber-700 ring-1 ring-amber-600/10';
  }

  puedeDarSeguimiento(item: { tieneMotivo?: boolean; esManual?: boolean }): boolean {
    return this.obtenerEstadoMonitoreo(item) === 'Con motivo';
  }

  // Datos paginados reactivos por tipo
  juridicasPaginadas = computed(() => {
    if (this.tipoActivo() !== 'juridica') return [];
    const filtered = this.datosFiltrados() as CoincidenciaJuridica[];
    const startIndex = (this.paginaActual() - 1) * this.limite();
    return filtered.slice(startIndex, startIndex + this.limite());
  });

  naturalesPaginadas = computed(() => {
    if (this.tipoActivo() !== 'natural') return [];
    const filtered = this.datosFiltrados() as CoincidenciaNatural[];
    const startIndex = (this.paginaActual() - 1) * this.limite();
    return filtered.slice(startIndex, startIndex + this.limite());
  });

  empleadosPaginadas = computed(() => {
    if (this.tipoActivo() !== 'empleado') return [];
    const filtered = this.datosFiltrados() as CoincidenciaEmpleado[];
    const startIndex = (this.paginaActual() - 1) * this.limite();
    return filtered.slice(startIndex, startIndex + this.limite());
  });

  paginasTotales = computed(() => {
    return Math.ceil(this.datosFiltrados().length / this.limite()) || 1;
  });

  paginasArray = computed(() => {
    const total = this.paginasTotales();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  abrirDetalle(row: CoincidenciaNatural) {
    this.personaSeleccionada.set(row);
    this.modalDetalleAbierto.set(true);
    this.detalleCargando.set(true);
    this.listasService.getDetalleNatural(row.numeroIdentificacion).subscribe({
      next: (res) => {
        this.detallesNatural.set(res);
        this.detalleCargando.set(false);
      },
      error: () => {
        this.detallesNatural.set([]);
        this.detalleCargando.set(false);
      }
    });
  }

  abrirDetalleEmpleado(row: CoincidenciaEmpleado) {
    this.personaSeleccionadaEmpleado.set(row);
    this.modalDetalleAbierto.set(true);
    this.detalleCargando.set(true);
    this.listasService.getDetalleEmpleado(row.identidad).subscribe({
      next: (res) => {
        this.detallesEmpleado.set(res);
        this.detalleCargando.set(false);
      },
      error: () => {
        this.detallesEmpleado.set([]);
        this.detalleCargando.set(false);
      }
    });
  }

  cerrarModal() {
    this.modalDetalleAbierto.set(false);
    this.personaSeleccionada.set(null);
    this.personaSeleccionadaEmpleado.set(null);
    this.detallesNatural.set([]);
    this.detallesEmpleado.set([]);
  }

  verPdf() {
    const isEmpleado = this.tipoActivo() === 'empleado';
    const personaNatural = this.personaSeleccionada();
    const personaEmpleado = this.personaSeleccionadaEmpleado();
    
    if (isEmpleado ? !personaEmpleado : !personaNatural) return;

    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    // Encabezado
    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.setTextColor(31, 41, 55); // Gray 800
    doc.text(institucion, 14, 15);

    doc.setFontSize(16);
    doc.text('Monitoreo de Listas de Riesgo', 14, 22);
    
    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(107, 114, 128); // Gray 500
    doc.text(isEmpleado ? 'Reporte Detallado de Coincidencias - Empleado IHSS' : 'Reporte Detallado de Coincidencias - Persona Natural', 14, 28);
    
    // Línea separadora
    doc.setDrawColor(229, 231, 235); // Gray 200
    doc.line(14, 32, 196, 32);

    // Información de la persona
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    doc.setTextColor(55, 65, 81); // Gray 700
    doc.text('Nombre:', 14, 40);
    doc.setFont('helvetica', 'normal');
    doc.text(isEmpleado ? personaEmpleado!.nombre : personaNatural!.nombre, 32, 40);

    doc.setFont('helvetica', 'bold');
    doc.text(isEmpleado ? 'Identidad:' : 'DNI:', 14, 46);
    doc.setFont('helvetica', 'normal');
    doc.text(isEmpleado ? personaEmpleado!.identidad : personaNatural!.numeroIdentificacion, 32, 46);

    // Tabla de coincidencias
    let tableHead: string[][];
    let tableBody: string[][];
    let colStyles: any;

    if (isEmpleado) {
      tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']];
      tableBody = this.detallesEmpleado().map(det => [
        det.tipoCondicionActuaDesc,
        det.numeroPatrono,
        det.nombreEmpresa,
        det.razoSoci,
        det.listaCoincidencia,
        det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
        det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
      ]);
      colStyles = {
        2: { cellWidth: 35 },
        3: { cellWidth: 35 }
      };
    } else {
      tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']];
      tableBody = this.detallesNatural().map(det => [
        det.tipoCondicionActuaDesc,
        det.numeroPatronal,
        det.nombreEmpresa,
        (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO',
        det.listaCoincidencia,
        det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
        det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
      ]);
      colStyles = {
        2: { cellWidth: 50 }
      };
    }

    autoTable(doc, {
      startY: 50,
      head: tableHead,
      body: tableBody,
      headStyles: {
        fillColor: [15, 23, 42],
        textColor: [255, 255, 255],
        fontSize: 8,
        fontStyle: 'bold'
      },
      bodyStyles: {
        fontSize: 8
      },
      columnStyles: colStyles,
      theme: 'striped',
      margin: { top: 50 },
      didParseCell: (data) => {
        if (data.row.section === 'body') {
          if (!isEmpleado) {
            // Columna Es PEP
            if (data.column.index === 3) {
              const rawVal = (data.row.raw as any)[3];
              if (rawVal === 'SÍ') {
                data.cell.styles.fillColor = [254, 226, 226]; // bg-red-100
                data.cell.styles.textColor = [153, 27, 27]; // text-red-800
                data.cell.styles.fontStyle = 'bold';
              } else {
                data.cell.styles.fillColor = [243, 244, 246]; // bg-gray-100
                data.cell.styles.textColor = [75, 85, 99]; // text-gray-600
              }
            }
            // Columna Lista
            if (data.column.index === 4) {
              data.cell.styles.fillColor = [254, 242, 242]; // bg-red-50
              data.cell.styles.textColor = [185, 28, 28]; // text-red-700
              data.cell.styles.fontStyle = 'bold';
            }
          } else {
            // Columna Lista
            if (data.column.index === 4) {
              data.cell.styles.fillColor = [254, 242, 242]; // bg-red-50
              data.cell.styles.textColor = [185, 28, 28]; // text-red-700
              data.cell.styles.fontStyle = 'bold';
            }
          }
        }
      }
    });

    // Resumen
    const finalY = (doc as any).lastAutoTable.finalY + 10;
    doc.setDrawColor(229, 231, 235);
    doc.line(14, finalY, 196, finalY);

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    doc.text('Resumen del Reporte:', 14, finalY + 8);
    
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(55, 65, 81);
    doc.text(`Total de Coincidencias: ${this.totalCoincidencias()}`, 14, finalY + 14);
    
    if (!isEmpleado) {
      if (this.coincidenciasPep() > 0) {
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(185, 28, 28); // red-700
        doc.text(`Coincidencias PEP: ${this.coincidenciasPep()}`, 14, finalY + 20);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(55, 65, 81);
      } else {
        doc.text(`Coincidencias PEP: ${this.coincidenciasPep()}`, 14, finalY + 20);
      }
      doc.text(`Empresas Relacionadas: ${this.empresasUnicas()}`, 14, finalY + 26);
    } else {
      doc.text(`Empresas Relacionadas: ${this.empresasUnicas()}`, 14, finalY + 20);
    }

    // Generar Blob y abrir modal de visualización
    const blob = doc.output('blob');
    const url = URL.createObjectURL(blob);
    this.pdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
    this.pdfModalAbierto.set(true);
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  }

  private formatDateOrNd(dateStr?: string | null): string {
    return dateStr ? this.formatDate(dateStr) : 'N/D';
  }

  private obtenerEtiquetaOrigenRegistro(origen?: string | null): string {
    return this.origenesRegistro.find(item => item.valor === origen)?.etiqueta || 'N/D';
  }

  // Fecha propia del alta en Riesgo Lavado; no reemplaza la fecha de coincidencia de DNP.
  private obtenerFechaRegistroInterno(rowFecha?: string | null, positivo?: { fechaRegistroInterno?: string | null } | null): string | null {
    return positivo?.fechaRegistroInterno || rowFecha || null;
  }

  cerrarPdfModal() {
    this.pdfModalAbierto.set(false);
    this.pdfUrl.set(null);
  }

  private obtenerRangoSeguimientoReporte(noDocumento?: string): RangoSeguimientoReporte {
    const usaRangoAplicado = !noDocumento || this.reporteSeguimientoDocumento() === noDocumento;
    const desde = usaRangoAplicado ? this.reporteSeguimientoDesde() : '';
    const hasta = usaRangoAplicado ? this.reporteSeguimientoHasta() : '';

    if (desde && hasta) {
      return { desde, hasta, texto: `${this.formatDate(desde)} al ${this.formatDate(hasta)}` };
    }
    if (desde) {
      return { desde, texto: `Desde ${this.formatDate(desde)}` };
    }
    if (hasta) {
      return { hasta, texto: `Hasta ${this.formatDate(hasta)}` };
    }
    return { texto: 'Todos los seguimientos registrados' };
  }

  private agregarEncabezadoPdf(doc: jsPDF, titulo: string) {
    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';
    const sistema = this.configService.configSistema()?.nombreSistema || 'Sistema de Monitoreo RIESGO IHSS';

    doc.setFillColor(15, 23, 42); // Slate 900
    doc.rect(0, 0, 210, 38, 'F');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.setTextColor(255, 255, 255);
    doc.text(institucion.toUpperCase(), 14, 15);

    doc.setFontSize(18);
    doc.text(titulo, 14, 23);

    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(203, 213, 225); // Slate 300
    doc.text(`${sistema}  |  Fecha de Generación: ${new Date().toLocaleString()}`, 14, 30);
  }

  private agregarDatosMemo(doc: jsPDF, y: number, titulo: string, generalData: string[][]): number {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text(titulo, 14, y);
    y += 4;
    doc.setDrawColor(226, 232, 240);
    doc.line(14, y, 196, y);
    y += 6;

    autoTable(doc, {
      startY: y,
      body: generalData,
      theme: 'plain',
      styles: {
        fontSize: 9,
        cellPadding: 2,
        textColor: [51, 65, 85]
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 40, textColor: [30, 41, 59] },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 45, textColor: [30, 41, 59] },
        3: { cellWidth: 50 }
      },
      margin: { left: 14, right: 14 }
    });

    return (doc as any).lastAutoTable.finalY + 10;
  }

  private agregarMotivoPdf(doc: jsPDF, y: number, titulo: string, motivoTexto: string): number {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text(titulo, 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(71, 85, 105);

    const splitMotivo = doc.splitTextToSize(motivoTexto, 180);
    doc.text(splitMotivo, 14, y);
    return y + (splitMotivo.length * 5) + 10;
  }

  private agregarSeguimientosPdf(doc: jsPDF, y: number, titulo: string, seguimientos: Seguimiento[], mensajeVacio: string, rango: RangoSeguimientoReporte): number {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text(titulo, 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.setTextColor(71, 85, 105);
    doc.text(`Rango de seguimientos: ${rango.texto}`, 14, y);
    y += 6;

    if (seguimientos && seguimientos.length > 0) {
      const seguimientosRows = seguimientos.map(seg => {
        const evidenciasTexto = seg.evidencias && seg.evidencias.length > 0
          ? seg.evidencias.map(e => e.nombreArchivo).join('\n')
          : 'Sin evidencias';
        return [
          this.formatDate(seg.fechaCreacion),
          seg.usrEmail || 'Sistema',
          seg.motivoIngreso || '',
          evidenciasTexto
        ];
      });

      autoTable(doc, {
        startY: y,
        head: [['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias']],
        body: seguimientosRows,
        headStyles: {
          fillColor: [15, 23, 42],
          textColor: [255, 255, 255],
          fontSize: 8.5,
          fontStyle: 'bold'
        },
        bodyStyles: {
          fontSize: 8,
          textColor: [71, 85, 105]
        },
        columnStyles: {
          0: { cellWidth: 25 },
          1: { cellWidth: 35 },
          2: { cellWidth: 80 },
          3: { cellWidth: 42 }
        },
        theme: 'striped',
        margin: { left: 14, right: 14 }
      });
      return (doc as any).lastAutoTable.finalY + 10;
    }

    doc.setFont('helvetica', 'italic');
    doc.setFontSize(9.5);
    doc.setTextColor(100, 116, 139);
    doc.text(mensajeVacio, 14, y);
    return y + 8;
  }

  private abrirPdf(doc: jsPDF) {
    const blob = doc.output('blob');
    const url = URL.createObjectURL(blob);
    this.pdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
    this.pdfModalAbierto.set(true);
  }

  imprimirReportePatrono(row: CoincidenciaJuridica) {
    this.cargando.set(true);
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(row.numeroPatrono);

    const obsPositivo = row.tieneMotivo 
      ? this.listasService.getPositivoPorDocumento(row.numeroPatrono) 
      : of(null);
      
    const obsSeguimientos = row.tieneMotivo 
      ? this.listasService.getSeguimientos(row.numeroPatrono, rangoSeguimiento.desde, rangoSeguimiento.hasta)
      : of([]);

    forkJoin([obsPositivo, obsSeguimientos]).subscribe({
      next: ([positivo, seguimientos]) => {
        const auditoriaData = {
          rtn: row.rtn,
          nombre: row.nombre,
          numeroPatrono: row.numeroPatrono,
          listaCoincidencia: row.listaCoincidencia,
          tieneMotivo: row.tieneMotivo,
          tipoReporte: 'PDF_PERFIL_FICHA',
          rangoSeguimientoDesde: rangoSeguimiento.desde || null,
          rangoSeguimientoHasta: rangoSeguimiento.hasta || null,
          rangoSeguimientoTexto: rangoSeguimiento.texto,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.numeroPatrono, auditoriaData).subscribe({
          next: () => {
            this.generarPdfPatrono(row, positivo, seguimientos, rangoSeguimiento);
            this.cargando.set(false);
          },
          error: (err) => {
            this.manejarErrorAuditoriaObligatoria(err, 'generación de reporte PDF');
          }
        });
      },
      error: (err) => {
        console.error('Error al cargar datos para el reporte:', err);
        this.cargando.set(false);
      }
    });
  }

  imprimirReporteNatural(row: CoincidenciaNatural) {
    this.cargando.set(true);
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(row.numeroIdentificacion);

    const obsDetalles = this.listasService.getDetalleNatural(row.numeroIdentificacion);
    const obsPositivo = row.tieneMotivo 
      ? this.listasService.getPositivoPorDocumento(row.numeroIdentificacion) 
      : of(null);
    const obsSeguimientos = row.tieneMotivo 
      ? this.listasService.getSeguimientos(row.numeroIdentificacion, rangoSeguimiento.desde, rangoSeguimiento.hasta)
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const auditoriaData = {
          numeroIdentificacion: row.numeroIdentificacion,
          nombre: row.nombre,
          listaCoincidencia: row.listaCoincidencia,
          totalRepetidos: row.totalRepetidos,
          tipoReporte: 'PDF_PERFIL_FICHA',
          rangoSeguimientoDesde: rangoSeguimiento.desde || null,
          rangoSeguimientoHasta: rangoSeguimiento.hasta || null,
          rangoSeguimientoTexto: rangoSeguimiento.texto,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.numeroIdentificacion, auditoriaData).subscribe({
          next: () => {
            this.generarPdfNatural(row, detalles, positivo, seguimientos, rangoSeguimiento);
            this.cargando.set(false);
          },
          error: (err) => {
            this.manejarErrorAuditoriaObligatoria(err, 'generación de reporte PDF');
          }
        });
      },
      error: (err) => {
        console.error('Error al cargar datos para el reporte:', err);
        this.cargando.set(false);
      }
    });
  }

  generarPdfNatural(row: CoincidenciaNatural, detalles: DetalleCoincidenciaNatural[], positivo: any, seguimientos: Seguimiento[], rangoSeguimiento: RangoSeguimientoReporte) {
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';
    const sistema = this.configService.configSistema()?.nombreSistema || 'Sistema de Monitoreo RIESGO IHSS';

    // Banner de encabezado
    doc.setFillColor(15, 23, 42); // Slate 900
    doc.rect(0, 0, 210, 38, 'F');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.setTextColor(255, 255, 255);
    doc.text(institucion.toUpperCase(), 14, 15);

    doc.setFontSize(18);
    doc.text('REPORTE INTEGRAL DE PERSONA NATURAL', 14, 23);

    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(203, 213, 225); // Slate 300
    doc.text(`${sistema}  |  Fecha de Generación: ${new Date().toLocaleString()}`, 14, 30);

    // Información General
    let y = 48;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('1. INFORMACIÓN GENERAL DE LA PERSONA', 14, y);
    y += 4;
    doc.setDrawColor(226, 232, 240);
    doc.line(14, y, 196, y);
    y += 6;

    const generalData = [
      ['DNI / Identificación:', row.numeroIdentificacion || 'N/D', 'Nombre Completo:', row.nombre || 'N/D'],
      ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Total de Coincidencias:', String(row.totalRepetidos || 0)],
      ['Estado Monitoreo:', this.obtenerEstadoMonitoreoReporte(row), 'Registro Interno:', this.formatDateOrNd(positivo?.fechaRegistroInterno)],
      ['Origen del Registro:', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', '']
    ];

    autoTable(doc, {
      startY: y,
      body: generalData,
      theme: 'plain',
      styles: {
        fontSize: 9,
        cellPadding: 2,
        textColor: [51, 65, 85]
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 40, textColor: [30, 41, 59] },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 45, textColor: [30, 41, 59] },
        3: { cellWidth: 50 }
      },
      margin: { left: 14, right: 14 }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    // Sección 2: Motivo de ingreso a la lista
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('2. MOTIVO DE INGRESO A LISTA DE MONITOREO', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const motivoTexto = positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para esta persona.';
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(71, 85, 105);
    
    const splitMotivo = doc.splitTextToSize(motivoTexto, 180);
    doc.text(splitMotivo, 14, y);
    y += (splitMotivo.length * 5) + 10;

    // Sección 3: Detalle de Coincidencias
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('3. DETALLE DE COINCIDENCIAS ENCONTRADAS', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']];
    const tableBody = detalles.map(det => [
      det.tipoCondicionActuaDesc,
      det.numeroPatronal,
      det.nombreEmpresa,
      (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO',
      det.listaCoincidencia,
      det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
      det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
    ]);

    autoTable(doc, {
      startY: y,
      head: tableHead,
      body: tableBody,
      headStyles: {
        fillColor: [15, 23, 42],
        textColor: [255, 255, 255],
        fontSize: 8,
        fontStyle: 'bold'
      },
      bodyStyles: {
        fontSize: 8
      },
      columnStyles: {
        2: { cellWidth: 50 }
      },
      theme: 'striped',
      margin: { left: 14, right: 14 },
      didParseCell: (data) => {
        if (data.row.section === 'body') {
          if (data.column.index === 3) {
            const rawVal = (data.row.raw as any)[3];
            if (rawVal === 'SÍ') {
              data.cell.styles.fillColor = [254, 226, 226];
              data.cell.styles.textColor = [153, 27, 27];
              data.cell.styles.fontStyle = 'bold';
            } else {
              data.cell.styles.fillColor = [243, 244, 246];
              data.cell.styles.textColor = [75, 85, 99];
            }
          }
          if (data.column.index === 4) {
            data.cell.styles.fillColor = [254, 242, 242];
            data.cell.styles.textColor = [185, 28, 28];
            data.cell.styles.fontStyle = 'bold';
          }
        }
      }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    this.agregarSeguimientosPdf(
      doc,
      y,
      '4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
      seguimientos,
      'No se registran acciones de seguimiento ni evidencias adicionales para esta persona.',
      rangoSeguimiento
    );

    this.abrirPdf(doc);
  }

  imprimirReporteEmpleado(row: CoincidenciaEmpleado) {
    this.cargando.set(true);
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(row.identidad);

    const obsDetalles = this.listasService.getDetalleEmpleado(row.identidad);
    const obsPositivo = row.tieneMotivo 
      ? this.listasService.getPositivoPorDocumento(row.identidad) 
      : of(null);
    const obsSeguimientos = row.tieneMotivo 
      ? this.listasService.getSeguimientos(row.identidad, rangoSeguimiento.desde, rangoSeguimiento.hasta)
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const auditoriaData = {
          identidad: row.identidad,
          nombre: row.nombre,
          listaCoincidencia: row.listaCoincidencia,
          totalRepetidos: row.totalRepetidos,
          tipoReporte: 'PDF_PERFIL_FICHA',
          rangoSeguimientoDesde: rangoSeguimiento.desde || null,
          rangoSeguimientoHasta: rangoSeguimiento.hasta || null,
          rangoSeguimientoTexto: rangoSeguimiento.texto,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.identidad, auditoriaData).subscribe({
          next: () => {
            this.generarPdfEmpleado(row, detalles, positivo, seguimientos, rangoSeguimiento);
            this.cargando.set(false);
          },
          error: (err) => {
            this.manejarErrorAuditoriaObligatoria(err, 'generación de reporte PDF');
          }
        });
      },
      error: (err) => {
        console.error('Error al cargar datos para el reporte:', err);
        this.cargando.set(false);
      }
    });
  }

  generarPdfEmpleado(row: CoincidenciaEmpleado, detalles: DetalleCoincidenciaEmpleado[], positivo: any, seguimientos: Seguimiento[], rangoSeguimiento: RangoSeguimientoReporte) {
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';
    const sistema = this.configService.configSistema()?.nombreSistema || 'Sistema de Monitoreo RIESGO IHSS';

    // Banner de encabezado
    doc.setFillColor(15, 23, 42); // Slate 900
    doc.rect(0, 0, 210, 38, 'F');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.setTextColor(255, 255, 255);
    doc.text(institucion.toUpperCase(), 14, 15);

    doc.setFontSize(18);
    doc.text('REPORTE INTEGRAL DE EMPLEADO IHSS', 14, 23);

    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(203, 213, 225); // Slate 300
    doc.text(`${sistema}  |  Fecha de Generación: ${new Date().toLocaleString()}`, 14, 30);

    // Información General
    let y = 48;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('1. INFORMACIÓN GENERAL DEL EMPLEADO', 14, y);
    y += 4;
    doc.setDrawColor(226, 232, 240);
    doc.line(14, y, 196, y);
    y += 6;

    const generalData = [
      ['DNI / Identidad:', row.identidad || 'N/D', 'Nombre Completo:', row.nombre || 'N/D'],
      ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Total de Coincidencias:', String(row.totalRepetidos || 0)],
      ['Estado Monitoreo:', this.obtenerEstadoMonitoreoReporte(row), 'Registro Interno:', this.formatDateOrNd(positivo?.fechaRegistroInterno)],
      ['Origen del Registro:', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', '']
    ];

    autoTable(doc, {
      startY: y,
      body: generalData,
      theme: 'plain',
      styles: {
        fontSize: 9,
        cellPadding: 2,
        textColor: [51, 65, 85]
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 40, textColor: [30, 41, 59] },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 45, textColor: [30, 41, 59] },
        3: { cellWidth: 50 }
      },
      margin: { left: 14, right: 14 }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    // Sección 2: Motivo de ingreso a la lista
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('2. MOTIVO DE INGRESO A LISTA DE MONITOREO', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const motivoTexto = positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para este empleado.';
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(71, 85, 105);
    
    const splitMotivo = doc.splitTextToSize(motivoTexto, 180);
    doc.text(splitMotivo, 14, y);
    y += (splitMotivo.length * 5) + 10;

    // Sección 3: Detalle de Coincidencias
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('3. DETALLE DE COINCIDENCIAS ENCONTRADAS', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']];
    const tableBody = detalles.map(det => [
      det.tipoCondicionActuaDesc,
      det.numeroPatrono,
      det.nombreEmpresa,
      det.razoSoci,
      det.listaCoincidencia,
      det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
      det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
    ]);

    autoTable(doc, {
      startY: y,
      head: tableHead,
      body: tableBody,
      headStyles: {
        fillColor: [15, 23, 42],
        textColor: [255, 255, 255],
        fontSize: 8,
        fontStyle: 'bold'
      },
      bodyStyles: {
        fontSize: 8
      },
      columnStyles: {
        2: { cellWidth: 35 },
        3: { cellWidth: 35 }
      },
      theme: 'striped',
      margin: { left: 14, right: 14 },
      didParseCell: (data) => {
        if (data.row.section === 'body') {
          if (data.column.index === 4) {
            data.cell.styles.fillColor = [254, 242, 242];
            data.cell.styles.textColor = [185, 28, 28];
            data.cell.styles.fontStyle = 'bold';
          }
        }
      }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    this.agregarSeguimientosPdf(
      doc,
      y,
      '4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
      seguimientos,
      'No se registran acciones de seguimiento ni evidencias adicionales para este empleado.',
      rangoSeguimiento
    );

    this.abrirPdf(doc);
  }

  generarPdfPatrono(row: CoincidenciaJuridica, positivo: any, seguimientos: Seguimiento[], rangoSeguimiento: RangoSeguimientoReporte) {
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';
    const sistema = this.configService.configSistema()?.nombreSistema || 'Sistema de Monitoreo RIESGO IHSS';

    // Banner de encabezado
    doc.setFillColor(15, 23, 42); // Slate 900
    doc.rect(0, 0, 210, 38, 'F');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.setTextColor(255, 255, 255);
    doc.text(institucion.toUpperCase(), 14, 15);

    doc.setFontSize(18);
    doc.text('REPORTE INTEGRAL DE PATRONO', 14, 23);

    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(203, 213, 225); // Slate 300
    doc.text(`${sistema}  |  Fecha de Generación: ${new Date().toLocaleString()}`, 14, 30);

    // Grid de Información General del Patrono
    let y = 48;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59); // Slate 800
    doc.text('1. INFORMACIÓN GENERAL DEL PATRONO', 14, y);
    y += 4;
    doc.setDrawColor(226, 232, 240); // Slate 200
    doc.line(14, y, 196, y);
    y += 6;

    const generalData = [
      ['Número Patronal:', row.numeroPatrono || 'N/D', 'RTN:', row.rtn || 'N/D'],
      ['Nombre / Razón Social:', row.nombre || 'N/D', 'Proveedor IHSS:', row.esProveedorIhss || 'No'],
      ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Estado Monitoreo:', this.obtenerEstadoMonitoreoReporte(row)],
      ['Fecha Coincidencia:', row.fechaEncontro ? this.formatDate(row.fechaEncontro) : 'N/D', 'Fecha Calificación:', row.fechaCalifico ? this.formatDate(row.fechaCalifico) : 'N/D'],
      ['Registro Interno:', this.formatDateOrNd(this.obtenerFechaRegistroInterno(row.fechaRegistroInterno, positivo)), 'Origen del Registro:', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro)]
    ];

    autoTable(doc, {
      startY: y,
      body: generalData,
      theme: 'plain',
      styles: {
        fontSize: 9,
        cellPadding: 2,
        textColor: [51, 65, 85]
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 40, textColor: [30, 41, 59] },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 35, textColor: [30, 41, 59] },
        3: { cellWidth: 60 }
      },
      margin: { left: 14, right: 14 }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    // Sección 2: Clasificación y Motivo Inicial de Monitoreo
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('2. MOTIVO DE INGRESO A LISTA DE MONITOREO', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const motivoTexto = positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para este patrono.';
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(71, 85, 105);
    
    const splitMotivo = doc.splitTextToSize(motivoTexto, 180);
    doc.text(splitMotivo, 14, y);
    y += (splitMotivo.length * 5) + 10;

    this.agregarSeguimientosPdf(
      doc,
      y,
      '3. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
      seguimientos,
      'No se registran acciones de seguimiento ni evidencias adicionales para este patrono.',
      rangoSeguimiento
    );

    this.abrirPdf(doc);
  }

  private ajustarColumnasExcel(ws: XLSX.WorkSheet, data: any[][]) {
    const maxLens = data.reduce((acc, row) => {
      row.forEach((val, colIdx) => {
        const len = val ? val.toString().length : 0;
        if (!acc[colIdx] || len > acc[colIdx]) {
          acc[colIdx] = len;
        }
      });
      return acc;
    }, [] as number[]);

    ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 45) }));
  }

  private agregarSeguimientosExcel(data: any[][], seguimientos: Seguimiento[], rangoSeguimiento: RangoSeguimientoReporte) {
    data.push([]);
    data.push(['Historial de Seguimientos y Evidencias']);
    data.push(['Rango de seguimientos:', rangoSeguimiento.texto]);
    data.push(['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias']);

    if (!seguimientos || seguimientos.length === 0) {
      data.push(['', '', 'No se registran acciones de seguimiento ni evidencias para el rango seleccionado.', '']);
      return;
    }

    seguimientos.forEach(seg => {
      data.push([
        seg.fechaCreacion ? this.formatDate(seg.fechaCreacion) : '',
        seg.usrEmail || 'Sistema',
        seg.motivoIngreso || '',
        seg.evidencias && seg.evidencias.length > 0
          ? seg.evidencias.map(e => e.nombreArchivo).join(', ')
          : 'Sin evidencias'
      ]);
    });
  }

  private escribirFichaExcel(data: any[][], sheetName: string, fileName: string) {
    const ws = XLSX.utils.aoa_to_sheet(data);
    this.ajustarColumnasExcel(ws, data);

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, sheetName);
    XLSX.writeFile(wb, fileName);
  }

  private manejarErrorAuditoriaObligatoria(err: unknown, operacion: string) {
    console.error(`No se pudo registrar auditoría de ${operacion}:`, err);
    this.cargando.set(false);
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        allowOutsideClick: false,
        title: 'Auditoría requerida',
        text: `No se pudo registrar la auditoría de ${operacion}. La operación fue cancelada.`,
        icon: 'error',
        confirmButtonColor: '#1e3a8a'
      });
    });
  }

  private registrarAuditoriaFichaExcel(noDocumento: string, tipo: string, nombre: string, fileName: string, cantidadSeguimientos: number, rangoSeguimiento: RangoSeguimientoReporte) {
    return this.listasService.registrarAuditoriaExportacion(
      'RL_LISTA_POSITIVOS',
      noDocumento,
      'ExportacionFichaPerfil',
      {
        accion: 'EXPORTACION_EXCEL',
        tipoReporte: 'EXCEL_PERFIL_FICHA',
        tipo,
        nombre,
        cantidadSeguimientos,
        rangoSeguimientoDesde: rangoSeguimiento.desde || null,
        rangoSeguimientoHasta: rangoSeguimiento.hasta || null,
        rangoSeguimientoTexto: rangoSeguimiento.texto,
        archivo: fileName
      }
    );
  }

  exportarFichaExcelPatrono(row: CoincidenciaJuridica) {
    this.cargando.set(true);
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(row.numeroPatrono);

    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(row.numeroPatrono)
      : of(null);
    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(row.numeroPatrono, rangoSeguimiento.desde, rangoSeguimiento.hasta)
      : of([]);

    forkJoin([obsPositivo, obsSeguimientos]).subscribe({
      next: ([positivo, seguimientos]) => {
        const data: any[][] = [
          ['Ficha de Perfil / Memorando'],
          [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
          [`Fecha de Generación: ${new Date().toLocaleString()}`],
          [],
          ['Información General del Patrono'],
          ['Número Patronal', row.numeroPatrono || 'N/D', 'RTN', row.rtn || 'N/D'],
          ['Nombre / Razón Social', row.nombre || 'N/D', 'Proveedor IHSS', row.esProveedorIhss || 'No'],
          ['Lista Coincidencia', row.listaCoincidencia || 'N/D', 'Estado Monitoreo', this.obtenerEstadoMonitoreoReporte(row)],
          ['Fecha Coincidencia', row.fechaEncontro ? this.formatDate(row.fechaEncontro) : 'N/D', 'Fecha Calificación', row.fechaCalifico ? this.formatDate(row.fechaCalifico) : 'N/D'],
          ['Registro Interno', this.formatDateOrNd(this.obtenerFechaRegistroInterno(row.fechaRegistroInterno, positivo)), 'Origen del Registro', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro)],
          [],
          ['Motivo de Ingreso a Lista de Monitoreo'],
          [positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para este patrono.']
        ];

        this.agregarSeguimientosExcel(data, seguimientos, rangoSeguimiento);

        const fileName = `Ficha_Patrono_${row.numeroPatrono}.xlsx`;
        this.registrarAuditoriaFichaExcel(row.numeroPatrono, 'juridica', row.nombre, fileName, seguimientos.length, rangoSeguimiento).subscribe({
          next: () => {
            this.escribirFichaExcel(data, 'Ficha Patrono', fileName);
            this.cargando.set(false);
          },
          error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: (err) => {
        console.error('Error al exportar ficha Excel de patrono:', err);
        this.cargando.set(false);
      }
    });
  }

  exportarFichaExcelNatural(row: CoincidenciaNatural) {
    this.cargando.set(true);
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(row.numeroIdentificacion);

    const obsDetalles = this.listasService.getDetalleNatural(row.numeroIdentificacion);
    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(row.numeroIdentificacion)
      : of(null);
    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(row.numeroIdentificacion, rangoSeguimiento.desde, rangoSeguimiento.hasta)
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const data: any[][] = [
          ['Ficha de Perfil / Memorando'],
          [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
          [`Fecha de Generación: ${new Date().toLocaleString()}`],
          [],
          ['Información General de la Persona'],
          ['DNI / Identificación', row.numeroIdentificacion || 'N/D', 'Nombre Completo', row.nombre || 'N/D'],
          ['Lista Coincidencia', row.listaCoincidencia || 'N/D', 'Total de Coincidencias', String(row.totalRepetidos || 0)],
          ['Estado Monitoreo', this.obtenerEstadoMonitoreoReporte(row), 'Registro Interno', this.formatDateOrNd(positivo?.fechaRegistroInterno)],
          ['Origen del Registro', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', ''],
          [],
          ['Motivo de Ingreso a Lista de Monitoreo'],
          [positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para esta persona.'],
          [],
          ['Detalle de Coincidencias Encontradas'],
          ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
        ];

        detalles.forEach(det => {
          data.push([
            det.tipoCondicionActuaDesc,
            det.numeroPatronal,
            det.nombreEmpresa,
            (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO',
            det.listaCoincidencia,
            det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
            det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
          ]);
        });

        this.agregarSeguimientosExcel(data, seguimientos, rangoSeguimiento);

        const fileName = `Ficha_Natural_${row.numeroIdentificacion}.xlsx`;
        this.registrarAuditoriaFichaExcel(row.numeroIdentificacion, 'natural', row.nombre, fileName, seguimientos.length, rangoSeguimiento).subscribe({
          next: () => {
            this.escribirFichaExcel(data, 'Ficha Natural', fileName);
            this.cargando.set(false);
          },
          error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: (err) => {
        console.error('Error al exportar ficha Excel de persona natural:', err);
        this.cargando.set(false);
      }
    });
  }

  exportarFichaExcelEmpleado(row: CoincidenciaEmpleado) {
    this.cargando.set(true);
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(row.identidad);

    const obsDetalles = this.listasService.getDetalleEmpleado(row.identidad);
    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(row.identidad)
      : of(null);
    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(row.identidad, rangoSeguimiento.desde, rangoSeguimiento.hasta)
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const data: any[][] = [
          ['Ficha de Perfil / Memorando'],
          [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
          [`Fecha de Generación: ${new Date().toLocaleString()}`],
          [],
          ['Información General del Empleado'],
          ['DNI / Identidad', row.identidad || 'N/D', 'Nombre Completo', row.nombre || 'N/D'],
          ['Lista Coincidencia', row.listaCoincidencia || 'N/D', 'Total de Coincidencias', String(row.totalRepetidos || 0)],
          ['Estado Monitoreo', this.obtenerEstadoMonitoreoReporte(row), 'Registro Interno', this.formatDateOrNd(positivo?.fechaRegistroInterno)],
          ['Origen del Registro', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', ''],
          [],
          ['Motivo de Ingreso a Lista de Monitoreo'],
          [positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para este empleado.'],
          [],
          ['Detalle de Coincidencias Encontradas'],
          ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
        ];

        detalles.forEach(det => {
          data.push([
            det.tipoCondicionActuaDesc,
            det.numeroPatrono,
            det.nombreEmpresa,
            det.razoSoci,
            det.listaCoincidencia,
            det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
            det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
          ]);
        });

        this.agregarSeguimientosExcel(data, seguimientos, rangoSeguimiento);

        const fileName = `Ficha_Empleado_${row.identidad}.xlsx`;
        this.registrarAuditoriaFichaExcel(row.identidad, 'empleado', row.nombre, fileName, seguimientos.length, rangoSeguimiento).subscribe({
          next: () => {
            this.escribirFichaExcel(data, 'Ficha Empleado', fileName);
            this.cargando.set(false);
          },
          error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: (err) => {
        console.error('Error al exportar ficha Excel de empleado:', err);
        this.cargando.set(false);
      }
    });
  }

  exportarExcel() {
    if (this.tipoActivo() === 'empleado') {
      const persona = this.personaSeleccionadaEmpleado();
      if (!persona) return;

      const data = [
        ['Monitoreo de Listas de Riesgo'],
        [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
        ['Reporte Detallado de Coincidencias - Empleado IHSS'],
        [],
        ['Nombre:', persona.nombre],
        ['Identidad:', persona.identidad],
        [],
        ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
      ];

      this.detallesEmpleado().forEach(det => {
        data.push([
          det.tipoCondicionActuaDesc,
          det.numeroPatrono,
          det.nombreEmpresa,
          det.razoSoci,
          det.listaCoincidencia,
          det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
          det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
        ]);
      });

      data.push([]);
      data.push(['Resumen del Reporte']);
      data.push(['Total de Coincidencias', this.totalCoincidencias().toString()]);
      data.push(['Empresas Relacionadas', this.empresasUnicas().toString()]);

      const ws = XLSX.utils.aoa_to_sheet(data);
      
      // Auto-ajustar ancho de columnas básico
      const maxLens = data.reduce((acc, row) => {
        row.forEach((val, colIdx) => {
          const len = val ? val.toString().length : 0;
          if (!acc[colIdx] || len > acc[colIdx]) {
            acc[colIdx] = len;
          }
        });
        return acc;
      }, [] as number[]);
      ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 40) }));

      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, 'Detalle Coincidencias');

      const fileName = `Reporte_Coincidencias_Empleado_${persona.identidad}.xlsx`;
      this.listasService.registrarAuditoriaExportacion(
        'RL_LISTA_POSITIVOS',
        persona.identidad,
        'ExportacionMonitoreoListas',
        {
          accion: 'EXPORTACION_EXCEL',
          tipo: 'empleado',
          nombre: persona.nombre,
          cantidadRegistros: this.detallesEmpleado().length,
          archivo: fileName
        }
      ).subscribe({
        next: () => XLSX.writeFile(wb, fileName),
        error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
      });
    } else {
      const persona = this.personaSeleccionada();
      if (!persona) return;

      const data = [
        ['Monitoreo de Listas de Riesgo'],
        [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
        ['Reporte Detallado de Coincidencias - Persona Natural'],
        [],
        ['Nombre:', persona.nombre],
        ['DNI:', persona.numeroIdentificacion],
        [],
        ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
      ];

      this.detallesNatural().forEach(det => {
        data.push([
          det.tipoCondicionActuaDesc,
          det.numeroPatronal,
          det.nombreEmpresa,
          (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO',
          det.listaCoincidencia,
          det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
          det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
        ]);
      });

      data.push([]);
      data.push(['Resumen del Reporte']);
      data.push(['Total de Coincidencias', this.totalCoincidencias().toString()]);
      data.push(['Coincidencias PEP', this.coincidenciasPep().toString()]);
      data.push(['Empresas Relacionadas', this.empresasUnicas().toString()]);

      const ws = XLSX.utils.aoa_to_sheet(data);
      
      // Auto-ajustar ancho de columnas básico
      const maxLens = data.reduce((acc, row) => {
        row.forEach((val, colIdx) => {
          const len = val ? val.toString().length : 0;
          if (!acc[colIdx] || len > acc[colIdx]) {
            acc[colIdx] = len;
          }
        });
        return acc;
      }, [] as number[]);
      ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 40) }));

      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, 'Detalle Coincidencias');

      const fileName = `Reporte_Coincidencias_${persona.numeroIdentificacion}.xlsx`;
      this.listasService.registrarAuditoriaExportacion(
        'RL_LISTA_POSITIVOS',
        persona.numeroIdentificacion,
        'ExportacionMonitoreoListas',
        {
          accion: 'EXPORTACION_EXCEL',
          tipo: 'natural',
          nombre: persona.nombre,
          cantidadRegistros: this.detallesNatural().length,
          archivo: fileName
        }
      ).subscribe({
        next: () => XLSX.writeFile(wb, fileName),
        error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
      });
    }
  }

  exportarListaPrincipal() {
    const tipo = this.tipoActivo();
    const dataFiltrada = this.datosFiltrados();
    if (dataFiltrada.length === 0) return;

    let headers: string[] = [];
    let title = '';
    let rows: any[][] = [];

    if (tipo === 'juridica') {
      title = 'Reporte de Coincidencias Jurídicas';
      headers = ['Número Patronal', 'RTN', 'Nombre Empresa', 'Lista Coincidencia', 'Proveedor IHSS', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'];
      rows = (dataFiltrada as CoincidenciaJuridica[]).map(item => [
        item.numeroPatrono,
        item.rtn,
        item.nombre,
        item.listaCoincidencia,
        item.esProveedorIhss || 'No',
        item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
        item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
        item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
      ]);
    } else if (tipo === 'natural') {
      title = 'Reporte de Coincidencias Naturales';
      headers = ['Número Identificación', 'Nombre Completo', 'Lista Coincidencia', 'Estado', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'];
      rows = (dataFiltrada as CoincidenciaNatural[]).map(item => [
        item.numeroIdentificacion,
        item.nombre,
        item.listaCoincidencia,
        this.obtenerEstadoMonitoreo(item),
        item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
        item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
        item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
      ]);
    } else {
      title = 'Reporte de Coincidencias Empleados';
      headers = ['Identidad', 'Nombre Empleado', 'Lista Coincidencia', 'Estado', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'];
      rows = (dataFiltrada as CoincidenciaEmpleado[]).map(item => [
        item.identidad,
        item.nombre,
        item.listaCoincidencia,
        this.obtenerEstadoMonitoreo(item),
        item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
        item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
        item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
      ]);
    }

    const dataExcel = [
      [title],
      [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
      [`Fecha de Generación: ${this.formatDate(new Date().toISOString())}`],
      [],
      headers,
      ...rows
    ];

    const ws = XLSX.utils.aoa_to_sheet(dataExcel);
    
    // Auto-ajustar ancho de columnas
    const maxLens = dataExcel.reduce((acc, row) => {
      row.forEach((val, colIdx) => {
        const len = val ? val.toString().length : 0;
        if (!acc[colIdx] || len > acc[colIdx]) {
          acc[colIdx] = len;
        }
      });
      return acc;
    }, [] as number[]);
    ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 40) }));

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Coincidencias');

    const fileName = `Reporte_${tipo.charAt(0).toUpperCase() + tipo.slice(1)}s_${new Date().toISOString().split('T')[0]}.xlsx`;
    this.listasService.registrarAuditoriaExportacion(
      'RL_LISTA_POSITIVOS',
      tipo,
      'ExportacionMonitoreoListas',
      {
        accion: 'EXPORTACION_EXCEL',
        tipo,
        titulo: title,
        cantidadRegistros: rows.length,
        archivo: fileName
      }
    ).subscribe({
      next: () => XLSX.writeFile(wb, fileName),
      error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
    });
  }

  mathMin(a: number, b: number): number {
    return Math.min(a, b);
  }

  registrarMotivo(row: any) {
    this.esRegistroManual.set(false);
    this.formManualNombre.set('');
    this.formManualNoDocumento.set('');
    this.formManualTipoPositivoId.set(null);
    this.formSeguimientoComentario.set('');
    this.archivosSeguimiento.set([]);
    this.formTipoListaCautelaId.set(null);
    this.formOrigenRegistro.set('DNP_LISTAS');

    let tipoPosId = 1; // 1 = JURÍDICO, 2 = NATURAL, 3 = EMPLEADO
    const tipo = this.tipoActivo();
    let docNum = '';

    if (tipo === 'juridica') {
      tipoPosId = 1;
      docNum = row.numeroPatrono || row.rtn;
    } else if (tipo === 'natural') {
      tipoPosId = 2;
      docNum = row.numeroIdentificacion;
    } else if (tipo === 'empleado') {
      tipoPosId = 3;
      docNum = row.identidad;
    }

    this.entidadSeleccionada.set({
      nombreCompleto: row.nombre,
      noDocumento: docNum,
      tipoPositivoId: tipoPosId,
      tipoListaText: tipo === 'juridica' ? 'Jurídica' : tipo === 'natural' ? 'Natural' : 'Empleado'
    });

    this.listasService.getPositivoPorDocumento(docNum).subscribe({
      next: (existing) => {
        if (existing) {
          this.formTipoDocId.set(existing.tipoDocumentoId);
          this.formMotivo.set(existing.motivoIngreso);
          this.formTipoListaCautelaId.set(existing.tipoListaCautelaId || null);
          this.formOrigenRegistro.set(existing.origenRegistro || 'DNP_LISTAS');
        } else {
          this.formTipoDocId.set(null);
          this.formMotivo.set('');
          this.formTipoListaCautelaId.set(null);
          this.formOrigenRegistro.set('DNP_LISTAS');
        }
        this.modalMotivoAbierto.set(true);
      },
      error: (err) => {
        console.error('Error al obtener datos existentes de la lista de positivos:', err);
        this.formTipoDocId.set(null);
        this.formMotivo.set('');
        this.formTipoListaCautelaId.set(null);
        this.formOrigenRegistro.set('DNP_LISTAS');
        this.modalMotivoAbierto.set(true);
      }
    });
  }

  agregarPositivoManual() {
    this.esRegistroManual.set(true);
    this.formManualNombre.set('');
    this.formManualNoDocumento.set('');
    this.formManualTipoPositivoId.set(null);
    this.formTipoDocId.set(null);
    this.formMotivo.set('');
    this.formTipoListaCautelaId.set(null);
    this.formOrigenRegistro.set('MANUAL_CUMPLIMIENTO');
    this.formSeguimientoComentario.set('');
    this.archivosSeguimiento.set([]);
    this.entidadSeleccionada.set(null);
    this.modalMotivoAbierto.set(true);
  }

  cerrarModalMotivo() {
    this.modalMotivoAbierto.set(false);
    this.entidadSeleccionada.set(null);
    this.formTipoDocId.set(null);
    this.formMotivo.set('');
    this.formTipoListaCautelaId.set(null);
    this.formOrigenRegistro.set('DNP_LISTAS');
    this.esRegistroManual.set(false);
    this.formManualNombre.set('');
    this.formManualNoDocumento.set('');
    this.formManualTipoPositivoId.set(null);
    this.formSeguimientoComentario.set('');
    this.archivosSeguimiento.set([]);
  }

  private obtenerExtensionesPermitidasEvidencia(): string[] {
    return this.politicaEvidencias().extensionesPermitidas
      .map(ext => ext.replace(/^\./, '').toLowerCase())
      .filter(ext => !!ext);
  }

  private validarArchivoEvidencia(file: File): string | null {
    const politica = this.politicaEvidencias();
    const maximoBytes = politica.maximoBytes || (politica.maximoMb * 1024 * 1024);

    if (file.size > maximoBytes) {
      return `El archivo ${file.name} supera el límite de ${politica.maximoMb}MB.`;
    }

    const ext = file.name.split('.').pop()?.toLowerCase();
    if (!ext || !this.obtenerExtensionesPermitidasEvidencia().includes(ext)) {
      return `El archivo ${file.name} no tiene una extensión permitida (${politica.tiposPermitidosTexto}).`;
    }

    return null;
  }

  private mostrarAdvertenciaArchivo(text: string) {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        allowOutsideClick: false,
        title: 'Archivo no permitido',
        text,
        icon: 'warning',
        confirmButtonColor: '#1d4ed8'
      });
    });
  }

  onManualSeguimientoFileSelected(event: any) {
    const files: FileList = event.target.files;
    if (files && files.length > 0) {
      const currentList = [...this.archivosSeguimiento()];
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        
        const errorArchivo = this.validarArchivoEvidencia(file);
        if (errorArchivo) {
          this.mostrarAdvertenciaArchivo(errorArchivo);
          continue;
        }

        currentList.push(file);
      }
      this.archivosSeguimiento.set(currentList);
    }
  }

  eliminarArchivoSeguimientoManual(index: number) {
    const currentList = [...this.archivosSeguimiento()];
    currentList.splice(index, 1);
    this.archivosSeguimiento.set(currentList);
  }

  guardarMotivo() {
    const manual = this.esRegistroManual();
    const docId = this.formTipoDocId();
    const motivo = this.formMotivo().trim();
    const cautelaId = this.formTipoListaCautelaId();
    const origenRegistro = this.formOrigenRegistro();
    const comentarioSeg = this.formSeguimientoComentario().trim();
    const archivosSeg = this.archivosSeguimiento();

    let noDocumento = '';
    let nombreCompleto = '';
    let tipoPositivoId = 1;

    if (manual) {
      noDocumento = this.formManualNoDocumento().trim();
      nombreCompleto = this.formManualNombre().trim();
      tipoPositivoId = this.formManualTipoPositivoId() || 1;
      if (!noDocumento || !nombreCompleto || !tipoPositivoId) {
        import('sweetalert2').then(Swal => {
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Campos requeridos',
            text: 'Por favor complete todos los campos obligatorios del registro manual.',
            icon: 'warning',
            confirmButtonColor: '#1d4ed8'
          });
        });
        return;
      }
    } else {
      const entidad = this.entidadSeleccionada();
      if (!entidad) return;
      noDocumento = entidad.noDocumento;
      nombreCompleto = entidad.nombreCompleto;
      tipoPositivoId = entidad.tipoPositivoId;
    }

    if (!docId || !cautelaId || !origenRegistro || !motivo) {
      import('sweetalert2').then(Swal => {
        Swal.default.fire({
          allowOutsideClick: false,
          title: 'Campos requeridos',
          text: 'Por favor seleccione el tipo de documento, el tipo de lista de cautela, el origen del registro e ingrese el motivo.',
          icon: 'warning',
          confirmButtonColor: '#1d4ed8'
        });
      });
      return;
    }

    if (archivosSeg.length > 0 && !comentarioSeg) {
      import('sweetalert2').then(Swal => {
        Swal.default.fire({
          allowOutsideClick: false,
          title: 'Seguimiento requerido',
          text: 'Para adjuntar evidencias debe escribir una nota de seguimiento.',
          icon: 'warning',
          confirmButtonColor: '#1d4ed8'
        });
      });
      return;
    }

    this.guardandoMotivo.set(true);

    const dto: RegistrarPositivoDto = {
      tipoDocumentoId: Number(docId),
      tipoPositivoId: tipoPositivoId,
      noDocumento: noDocumento,
      nombreCompleto: nombreCompleto,
      motivoIngreso: motivo,
      tipoListaCautelaId: Number(cautelaId),
      origenRegistro: origenRegistro
    };

    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: 'Procesando...',
        text: 'Registrando motivo en lista de positivos.',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.default.showLoading();
        }
      });

      this.listasService.registrarPositivo(dto).subscribe({
        next: (resp) => {
          if (comentarioSeg) {
            this.listasService.registrarSeguimiento(noDocumento, comentarioSeg, archivosSeg).subscribe({
              next: () => {
                this.guardandoMotivo.set(false);
                this.cerrarModalMotivo();
                Swal.default.fire({
                  allowOutsideClick: false,
                  title: 'Registro Completo',
                  text: 'Se ha registrado el motivo y el primer seguimiento correctamente.',
                  icon: 'success',
                  confirmButtonColor: '#1d4ed8'
                });
                this.cargarDatos();
              },
              error: (errSeg) => {
                this.guardandoMotivo.set(false);
                this.cerrarModalMotivo();
                Swal.default.fire({
                  allowOutsideClick: false,
                  title: 'Registro Parcial',
                  text: 'El motivo se registró con éxito, pero hubo un error al registrar el seguimiento: ' + (errSeg.error?.mensaje || 'Error desconocido'),
                  icon: 'warning',
                  confirmButtonColor: '#1d4ed8'
                });
                this.cargarDatos();
              }
            });
          } else {
            this.guardandoMotivo.set(false);
            this.cerrarModalMotivo();
            Swal.default.fire({
              allowOutsideClick: false,
              title: 'Registro Exitoso',
              text: resp.mensaje || 'Se ha registrado el motivo correctamente.',
              icon: 'success',
              confirmButtonColor: '#1d4ed8'
            });
            this.cargarDatos();
          }
        },
        error: (err) => {
          this.guardandoMotivo.set(false);
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Error',
            text: err.error?.mensaje || 'No se pudo guardar el registro.',
            icon: 'error',
            confirmButtonColor: '#1d4ed8'
          });
        }
      });
    });
  }

  darSeguimiento(row: any) {
    let tipoPosId = 1;
    const tipo = this.tipoActivo();
    let docNum = '';

    if (tipo === 'juridica') {
      tipoPosId = 1;
      docNum = row.numeroPatrono || row.rtn;
    } else if (tipo === 'natural') {
      tipoPosId = 2;
      docNum = row.numeroIdentificacion;
    } else if (tipo === 'empleado') {
      tipoPosId = 3;
      docNum = row.identidad;
    }

    this.entidadSeleccionada.set({
      nombreCompleto: row.nombre,
      noDocumento: docNum,
      tipoPositivoId: tipoPosId,
      tipoListaText: tipo === 'juridica' ? 'Jurídica' : tipo === 'natural' ? 'Natural' : 'Empleado'
    });

    this.modoEdicion.set(false);
    this.seguimientoEditandoId.set(null);
    this.evidenciasExistentes.set([]);
    this.formComentarioSeguimiento.set('');
    if (this.reporteSeguimientoDocumento() === docNum) {
      this.filtroSeguimientoDesde.set(this.reporteSeguimientoDesde());
      this.filtroSeguimientoHasta.set(this.reporteSeguimientoHasta());
    } else {
      this.filtroSeguimientoDesde.set('');
      this.filtroSeguimientoHasta.set('');
    }
    this.archivosSeleccionados.set([]);
    this.modalSeguimientoAbierto.set(true);

    this.cargarSeguimientos(docNum);
  }

  cargarSeguimientos(noDocumento: string) {
    this.cargandoSeguimiento.set(true);
    this.listasService.getSeguimientos(noDocumento, this.filtroSeguimientoDesde(), this.filtroSeguimientoHasta()).subscribe({
      next: (res) => {
        this.listaSeguimientos.set(res);
        this.cargandoSeguimiento.set(false);
      },
      error: (err) => {
        console.error('Error al obtener historial de seguimientos:', err);
        this.listaSeguimientos.set([]);
        this.cargandoSeguimiento.set(false);
      }
    });
  }

  aplicarFiltroSeguimientos() {
    const entidad = this.entidadSeleccionada();
    if (!entidad) return;

    const desde = this.filtroSeguimientoDesde();
    const hasta = this.filtroSeguimientoHasta();
    if (desde && hasta && desde > hasta) {
      import('sweetalert2').then(Swal => {
        Swal.default.fire({
          allowOutsideClick: false,
          title: 'Rango no valido',
          text: 'La fecha desde no puede ser mayor que la fecha hasta.',
          icon: 'warning',
          confirmButtonColor: '#1d4ed8'
        });
      });
      return;
    }

    this.reporteSeguimientoDocumento.set(entidad.noDocumento);
    this.reporteSeguimientoDesde.set(desde);
    this.reporteSeguimientoHasta.set(hasta);
    this.cargarSeguimientos(entidad.noDocumento);
  }

  programarFiltroSeguimientos() {
    if (this.filtroSeguimientoTimer) clearTimeout(this.filtroSeguimientoTimer);
    this.filtroSeguimientoTimer = setTimeout(() => this.aplicarFiltroSeguimientos(), 350);
  }

  limpiarFiltroSeguimientos() {
    const entidad = this.entidadSeleccionada();
    this.filtroSeguimientoDesde.set('');
    this.filtroSeguimientoHasta.set('');
    if (entidad && this.reporteSeguimientoDocumento() === entidad.noDocumento) {
      this.reporteSeguimientoDocumento.set('');
      this.reporteSeguimientoDesde.set('');
      this.reporteSeguimientoHasta.set('');
    }
    if (entidad) {
      this.cargarSeguimientos(entidad.noDocumento);
    }
  }

  cerrarModalSeguimiento() {
    this.modalSeguimientoAbierto.set(false);
    this.entidadSeleccionada.set(null);
    this.listaSeguimientos.set([]);
    this.formComentarioSeguimiento.set('');
    this.filtroSeguimientoDesde.set('');
    this.filtroSeguimientoHasta.set('');
    this.archivosSeleccionados.set([]);
    this.modoEdicion.set(false);
    this.seguimientoEditandoId.set(null);
    this.evidenciasExistentes.set([]);
  }

  onFileSelected(event: any) {
    const files: FileList = event.target.files;
    if (files && files.length > 0) {
      const currentList = [...this.archivosSeleccionados()];
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        
        const errorArchivo = this.validarArchivoEvidencia(file);
        if (errorArchivo) {
          this.mostrarAdvertenciaArchivo(errorArchivo);
          continue;
        }

        currentList.push(file);
      }
      this.archivosSeleccionados.set(currentList);
    }
  }

  eliminarArchivoSeleccionado(index: number) {
    const currentList = [...this.archivosSeleccionados()];
    currentList.splice(index, 1);
    this.archivosSeleccionados.set(currentList);
  }

  iniciarEdicionSeguimiento(seg: Seguimiento) {
    this.modoEdicion.set(true);
    this.seguimientoEditandoId.set(seg.detalleListaId);
    this.formComentarioSeguimiento.set(seg.motivoIngreso);
    this.evidenciasExistentes.set(seg.evidencias || []);
    this.archivosSeleccionados.set([]);
  }

  cancelarEdicion() {
    this.modoEdicion.set(false);
    this.seguimientoEditandoId.set(null);
    this.formComentarioSeguimiento.set('');
    this.evidenciasExistentes.set([]);
    this.archivosSeleccionados.set([]);
  }

  // Centraliza el motivo obligatorio para eliminaciones lógicas de monitoreo.
  private async solicitarMotivoEliminacion(Swal: any, title: string, text: string): Promise<string | null> {
    const result = await Swal.default.fire({
      allowOutsideClick: false,
      title,
      text,
      icon: 'warning',
      input: 'textarea',
      inputLabel: 'Motivo de eliminación',
      inputPlaceholder: 'Escriba el motivo de la eliminación...',
      inputAttributes: {
        maxlength: String(this.maxTextoTextarea),
        'aria-label': 'Motivo de eliminación'
      },
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      inputValidator: (value: string) => {
        const motivo = value?.trim();
        if (!motivo) return 'Debe ingresar un motivo de eliminación.';
        if (motivo.length > 1000) return 'El motivo no debe superar los 1000 caracteres.';
        return null;
      }
    });

    return result.isConfirmed ? String(result.value).trim() : null;
  }

  eliminarEvidenciaExistente(evi: Evidencia) {
    import('sweetalert2').then(async Swal => {
      const motivoEliminacion = await this.solicitarMotivoEliminacion(
        Swal,
        '¿Eliminar evidencia?',
        `Se inactivará lógicamente el archivo ${evi.nombreArchivo}.`
      );

      if (motivoEliminacion) {
          Swal.default.fire({
            title: 'Eliminando...',
            allowOutsideClick: false,
            didOpen: () => {
              Swal.default.showLoading();
            }
          });

          this.listasService.eliminarEvidencia(evi.evidenciaId, motivoEliminacion).subscribe({
            next: (resp) => {
              this.evidenciasExistentes.set(
                this.evidenciasExistentes().filter(e => e.evidenciaId !== evi.evidenciaId)
              );

              this.listaSeguimientos.set(
                this.listaSeguimientos().map(s => {
                  if (s.evidencias) {
                    s.evidencias = s.evidencias.filter(e => e.evidenciaId !== evi.evidenciaId);
                  }
                  return s;
                })
              );

              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Eliminado',
                text: resp.mensaje || 'Evidencia eliminada correctamente.',
                icon: 'success',
                confirmButtonColor: '#1d4ed8'
              });
            },
            error: (err) => {
              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Error',
                text: err.error?.mensaje || 'No se pudo eliminar la evidencia.',
                icon: 'error',
                confirmButtonColor: '#1d4ed8'
              });
            }
          });
      }
    });
  }

  guardarSeguimiento() {
    const entidad = this.entidadSeleccionada();
    const motivo = this.formComentarioSeguimiento().trim();
    const archivos = this.archivosSeleccionados();

    if (!entidad || !motivo) return;

    this.guardandoSeguimiento.set(true);

    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: 'Guardando...',
        text: this.modoEdicion() ? 'Actualizando nota de seguimiento...' : 'Registrando nota de seguimiento y evidencia...',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.default.showLoading();
        }
      });

      const request$ = this.modoEdicion()
        ? this.listasService.actualizarSeguimiento(this.seguimientoEditandoId()!, motivo, archivos)
        : this.listasService.registrarSeguimiento(entidad.noDocumento, motivo, archivos);

      request$.subscribe({
        next: (resp) => {
          this.guardandoSeguimiento.set(false);
          const editMode = this.modoEdicion();
          
          this.cancelarEdicion();
          
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Éxito',
            text: resp.mensaje || (editMode ? 'Seguimiento actualizado exitosamente.' : 'Seguimiento registrado exitosamente.'),
            icon: 'success',
            confirmButtonColor: '#1d4ed8'
          });

          this.cargarSeguimientos(entidad.noDocumento);
        },
        error: (err) => {
          this.guardandoSeguimiento.set(false);
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Error',
            text: err.error?.mensaje || 'No se pudo guardar el seguimiento.',
            icon: 'error',
            confirmButtonColor: '#1d4ed8'
          });
        }
      });
    });
  }

  descargarEvidencia(evi: Evidencia) {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: 'Cargando archivo...',
        text: 'Por favor, espere.',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.default.showLoading();
        }
      });

      this.listasService.descargarEvidenciaBlob(evi.evidenciaId).subscribe({
        next: (blob) => {
          Swal.default.close();
          const mimeType = blob.type || evi.tipoMime;
          const blobUrl = URL.createObjectURL(blob);

          const esVisualizable = mimeType.includes('pdf') || mimeType.includes('image');
          if (esVisualizable) {
            window.open(blobUrl, '_blank');
          } else {
            const a = document.createElement('a');
            a.href = blobUrl;
            a.download = evi.nombreArchivo;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
          }

          setTimeout(() => URL.revokeObjectURL(blobUrl), 15000);
        },
        error: (err) => {
          Swal.default.close();
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Error',
            text: 'No se pudo cargar el archivo de evidencia.',
            icon: 'error',
            confirmButtonColor: '#1d4ed8'
          });
        }
      });
    });
  }

  eliminarSeguimiento(seg: Seguimiento) {
    import('sweetalert2').then(async Swal => {
      const motivoEliminacion = await this.solicitarMotivoEliminacion(
        Swal,
        '¿Eliminar seguimiento?',
        'Esta acción realizará una eliminación lógica de la nota de seguimiento.'
      );

      if (motivoEliminacion) {
          Swal.default.fire({
            title: 'Eliminando...',
            allowOutsideClick: false,
            didOpen: () => {
              Swal.default.showLoading();
            }
          });

          this.listasService.eliminarSeguimiento(seg.detalleListaId, motivoEliminacion).subscribe({
            next: (resp) => {
              if (this.modoEdicion() && this.seguimientoEditandoId() === seg.detalleListaId) {
                this.cancelarEdicion();
              }

              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Eliminado',
                text: resp.mensaje || 'El seguimiento ha sido eliminado correctamente.',
                icon: 'success',
                confirmButtonColor: '#1d4ed8'
              });

              const entidad = this.entidadSeleccionada();
              if (entidad) {
                this.cargarSeguimientos(entidad.noDocumento);
              }
            },
            error: (err) => {
              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Error',
                text: err.error?.mensaje || 'No se pudo eliminar el seguimiento.',
                icon: 'error',
                confirmButtonColor: '#1d4ed8'
              });
            }
          });
      }
    });
  }

  obtenerIconoArchivo(mime: string): string {
    const m = mime.toLowerCase();
    if (m.includes('pdf')) return 'application/pdf';
    if (m.includes('image') || m.includes('png') || m.includes('jpeg') || m.includes('gif')) return 'image';
    if (m.includes('word') || m.includes('officedocument.word') || m.includes('msword')) return 'word';
    if (m.includes('excel') || m.includes('officedocument.spreadsheet') || m.includes('csv')) return 'excel';
    return 'default';
  }
}
