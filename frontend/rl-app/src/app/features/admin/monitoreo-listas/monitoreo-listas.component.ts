import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ListasService, CoincidenciaJuridica, CoincidenciaNatural, CoincidenciaEmpleado, DetalleCoincidenciaNatural, DetalleCoincidenciaEmpleado, TipoDocumento, TipoListaCautela, RegistrarPositivoDto, Seguimiento, Evidencia } from '../../../core/services/listas.service';
import { ConfiguracionService } from '../../../core/services/configuracion.service';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import * as XLSX from 'xlsx';
import { of, forkJoin } from 'rxjs';

type FiltroTipo = 'juridica' | 'natural' | 'empleado';

@Component({
  selector: 'app-monitoreo-listas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="w-full space-y-6">
      
      <!-- Encabezado -->
      <div class="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h2 class="text-2xl font-bold text-gray-800">Monitoreo de Listas</h2>
          <p class="text-sm text-gray-500">Coincidencias en listas de riesgo clasificadas por categoría.</p>
        </div>
        <button (click)="agregarPositivoManual()"
          class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-xl transition-all flex items-center gap-1.5 shadow-sm">
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          <span>Agregar Positivo Manual</span>
        </button>
      </div>

      <!-- Filtros en forma de Botones Grandes (Categorías) -->
      <div class="grid grid-cols-3 gap-4 bg-white p-3 rounded-2xl shadow-sm border border-gray-100">
        
        <!-- Botón Jurídicas -->
        <button (click)="cambiarTipo('juridica')"
          [class]="tipoActivo() === 'juridica' 
            ? 'bg-ihss-900 text-white ring-2 ring-ihss-600/20' 
            : 'bg-gray-50 text-gray-600 hover:bg-gray-100'"
          class="flex flex-col items-center justify-center p-4 rounded-xl font-semibold transition-all duration-200 gap-2">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5"/>
          </svg>
          <span class="text-sm sm:text-base">Jurídicas</span>
        </button>

        <!-- Botón Naturales -->
        <button (click)="cambiarTipo('natural')"
          [class]="tipoActivo() === 'natural' 
            ? 'bg-ihss-900 text-white ring-2 ring-ihss-600/20' 
            : 'bg-gray-50 text-gray-600 hover:bg-gray-100'"
          class="flex flex-col items-center justify-center p-4 rounded-xl font-semibold transition-all duration-200 gap-2">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
          </svg>
          <span class="text-sm sm:text-base">Naturales</span>
        </button>

        <!-- Botón Empleados -->
        <button (click)="cambiarTipo('empleado')"
          [class]="tipoActivo() === 'empleado' 
            ? 'bg-ihss-900 text-white ring-2 ring-ihss-600/20' 
            : 'bg-gray-50 text-gray-600 hover:bg-gray-100'"
          class="flex flex-col items-center justify-center p-4 rounded-xl font-semibold transition-all duration-200 gap-2">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          <span class="text-sm sm:text-base">Empleados</span>
        </button>

      </div>

      <!-- Buscador y Tabla de Contenido -->
      <div class="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 space-y-4">
        
        <div class="flex flex-col sm:flex-row justify-between items-stretch sm:items-center gap-4">
          <!-- Búsqueda -->
          <div class="relative flex-1 max-w-md">
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-gray-400">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </span>
            <input type="text" [ngModel]="busqueda()" (ngModelChange)="busqueda.set($event); paginaActual.set(1)"
              placeholder="Buscar por coincidencia, nombre o número de identificación..."
              class="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors text-sm" />
          </div>

          <!-- Acciones de exportación y límite -->
          <div class="flex flex-wrap items-center gap-3 text-sm text-gray-500">
            <button (click)="exportarListaPrincipal()" [disabled]="datosFiltrados().length === 0"
              class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl transition-all flex items-center gap-1.5 shadow-sm disabled:opacity-50 disabled:cursor-not-allowed">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <span>Exportar Excel</span>
            </button>

            <span class="text-gray-300">|</span>

            <span>Mostrar</span>
            <select [ngModel]="limite()" (ngModelChange)="limite.set(+$event); paginaActual.set(1)"
              class="border border-gray-200 rounded-xl pl-3 pr-8 py-1.5 focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors bg-white">
              <option [value]="10">10</option>
              <option [value]="25">25</option>
              <option [value]="50">50</option>
            </select>
            <span>registros</span>
          </div>
        </div>

        <!-- Tabla -->
        <div class="overflow-x-auto rounded-xl border border-gray-200">
          
          @if (cargando()) {
            <!-- Spinner -->
            <div class="py-20 flex flex-col items-center justify-center gap-3">
              <svg class="animate-spin h-8 w-8 text-ihss-900" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <p class="text-sm font-medium text-gray-500">Cargando información...</p>
            </div>
          } @else {
            
            @if (datosFiltrados().length === 0) {
              <!-- Vacío -->
              <div class="py-20 flex flex-col items-center justify-center gap-2">
                <svg class="w-12 h-12 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p class="text-sm font-medium text-gray-500">No se encontraron registros de coincidencias.</p>
              </div>
            } @else {
              
              <!-- Contenido según Tipo -->
              <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50 text-[10px] font-bold text-gray-500 uppercase tracking-wider">
                  
                  @if (tipoActivo() === 'juridica') {
                    <tr>
                      <th class="px-6 py-3 text-left">Patrono / RTN</th>
                      <th class="px-6 py-3 text-left">Nombre de la Empresa</th>
                      <th class="px-6 py-3 text-left">Lista Coincidencia</th>
                      <th class="px-6 py-3 text-left">Proveedor IHSS</th>
                      <th class="px-6 py-3 text-left">Fecha Encontrado</th>
                      <th class="px-6 py-3 text-left">Fecha Calificado</th>
                      <th class="px-6 py-3 text-center">Acciones</th>
                    </tr>
                  }

                  @if (tipoActivo() === 'natural') {
                    <tr>
                      <th class="px-6 py-3 text-left">Identificación</th>
                      <th class="px-6 py-3 text-left">Nombre Completo</th>
                      <th class="px-6 py-3 text-center">Acciones</th>
                    </tr>
                  }

                  @if (tipoActivo() === 'empleado') {
                    <tr>
                      <th class="px-6 py-3 text-left">Identidad</th>
                      <th class="px-6 py-3 text-left">Nombre Empleado</th>
                      <th class="px-6 py-3 text-center">Acciones</th>
                    </tr>
                  }

                </thead>
                <tbody class="bg-white divide-y divide-gray-200 text-sm text-gray-700">
                  
                  @if (tipoActivo() === 'juridica') {
                    @for (row of juridicasPaginadas(); track row.rtn + row.numeroPatrono) {
                      <tr class="hover:bg-gray-50/50 transition-colors">
                        <td class="px-6 py-4">
                          <span class="font-medium block text-gray-900">{{ row.numeroPatrono }}</span>
                          <span class="text-xs text-gray-400">RTN: {{ row.rtn }}</span>
                        </td>
                        <td class="px-6 py-4 font-semibold text-gray-900">
                          <div class="flex items-center gap-2">
                            <span>{{ row.nombre }}</span>
                            @if (row.esManual) {
                              <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-blue-50 text-blue-700 ring-1 ring-blue-500/20">
                                Manual
                              </span>
                            }
                          </div>
                        </td>
                        <td class="px-6 py-4">
                          <span [class]="row.esManual
                            ? 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-blue-50 text-blue-700 ring-1 ring-blue-500/20'
                            : 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-red-50 text-red-700 ring-1 ring-red-600/10'">
                            {{ row.listaCoincidencia }}
                          </span>
                        </td>
                        <td class="px-6 py-4">
                          <span [class]="row.esProveedorIhss === 'Si'
                            ? 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-orange-50 text-orange-700 ring-1 ring-orange-500/20'
                            : 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-gray-50 text-gray-500 ring-1 ring-gray-200'">
                            {{ row.esProveedorIhss || 'No' }}
                          </span>
                        </td>
                        <td class="px-6 py-4 text-xs text-gray-500">{{ row.fechaEncontro | date:'dd/MM/yyyy HH:mm' }}</td>
                        <td class="px-6 py-4 text-xs text-gray-500">{{ row.fechaCalifico | date:'dd/MM/yyyy HH:mm' }}</td>
                        <td class="px-6 py-4 text-center">
                          <div class="flex items-center justify-center gap-2">
                            <!-- Registrar Motivo -->
                            <div class="relative group inline-block">
                              <button (click)="registrarMotivo(row)" 
                                class="inline-flex items-center justify-center p-1.5 text-blue-600 bg-blue-50 hover:bg-blue-100 rounded-lg transition-colors border border-blue-200">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                Registrar Motivo
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>

                            <!-- Dar Seguimiento -->
                            <div class="relative group inline-block">
                              <button (click)="darSeguimiento(row)" [disabled]="!row.tieneMotivo"
                                [class]="row.tieneMotivo 
                                  ? 'inline-flex items-center justify-center p-1.5 text-amber-600 bg-amber-50 hover:bg-amber-100 rounded-lg transition-colors border border-amber-200 cursor-pointer' 
                                  : 'inline-flex items-center justify-center p-1.5 text-gray-400 bg-gray-100 rounded-lg border border-gray-200 cursor-not-allowed opacity-50'">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                {{ row.tieneMotivo ? 'Dar Seguimiento' : 'Debe registrar un motivo primero' }}
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>

                            <!-- Imprimir Reporte -->
                            <div class="relative group inline-block">
                              <button (click)="imprimirReportePatrono(row)" 
                                class="inline-flex items-center justify-center p-1.5 text-emerald-600 bg-emerald-50 hover:bg-emerald-100 rounded-lg transition-colors border border-emerald-200">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                Imprimir Reporte
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>
                          </div>
                        </td>
                      </tr>
                    }
                  }

                  @if (tipoActivo() === 'natural') {
                    @for (row of naturalesPaginadas(); track row.numeroIdentificacion) {
                      <tr class="hover:bg-gray-50/50 transition-colors">
                        <td class="px-6 py-4 font-medium text-gray-900">{{ row.numeroIdentificacion }}</td>
                         <td class="px-6 py-4 font-semibold text-gray-900">
                          <div class="flex items-center gap-2">
                            <span>{{ row.nombre }}</span>
                            @if (row.esManual) {
                              <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-blue-50 text-blue-700 ring-1 ring-blue-500/20">
                                Manual
                              </span>
                            }
                          </div>
                        </td>
                        <td class="px-6 py-4 text-center">
                          <div class="flex items-center justify-center gap-2">
                            <!-- Ver Detalle -->
                            <div class="relative group inline-block">
                              <button (click)="abrirDetalle(row)" [disabled]="row.esManual"
                                [class]="row.esManual
                                  ? 'inline-flex items-center justify-center p-1.5 text-gray-400 bg-gray-100 rounded-lg border border-gray-200 cursor-not-allowed opacity-50'
                                  : 'inline-flex items-center justify-center p-1.5 text-ihss-900 bg-ihss-50 hover:bg-ihss-100 rounded-lg transition-colors border border-ihss-200'">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                {{ row.esManual ? 'No aplica a registro manual' : 'Ver Detalle' }}
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>

                            <!-- Registrar Motivo -->
                            <div class="relative group inline-block">
                              <button (click)="registrarMotivo(row)" 
                                class="inline-flex items-center justify-center p-1.5 text-blue-600 bg-blue-50 hover:bg-blue-100 rounded-lg transition-colors border border-blue-200">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                Registrar Motivo
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>

                            <!-- Dar Seguimiento -->
                            <div class="relative group inline-block">
                              <button (click)="darSeguimiento(row)" [disabled]="!row.tieneMotivo"
                                [class]="row.tieneMotivo 
                                  ? 'inline-flex items-center justify-center p-1.5 text-amber-600 bg-amber-50 hover:bg-amber-100 rounded-lg transition-colors border border-amber-200 cursor-pointer' 
                                  : 'inline-flex items-center justify-center p-1.5 text-gray-400 bg-gray-100 rounded-lg border border-gray-200 cursor-not-allowed opacity-50'">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                {{ row.tieneMotivo ? 'Dar Seguimiento' : 'Debe registrar un motivo primero' }}
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>

                            <!-- Imprimir Reporte -->
                            <div class="relative group inline-block">
                              <button (click)="imprimirReporteNatural(row)" 
                                class="inline-flex items-center justify-center p-1.5 text-emerald-600 bg-emerald-50 hover:bg-emerald-100 rounded-lg transition-colors border border-emerald-200">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                Imprimir Reporte
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>
                          </div>
                        </td>
                      </tr>
                    }
                  }

                  @if (tipoActivo() === 'empleado') {
                    @for (row of empleadosPaginadas(); track row.identidad) {
                      <tr class="hover:bg-gray-50/50 transition-colors">
                        <td class="px-6 py-4 font-medium text-gray-900">{{ row.identidad }}</td>
                        <td class="px-6 py-4 font-semibold text-gray-900">
                          <div class="flex items-center gap-2">
                            <span>{{ row.nombre }}</span>
                            @if (row.esManual) {
                              <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-blue-50 text-blue-700 ring-1 ring-blue-500/20">
                                Manual
                              </span>
                            }
                          </div>
                        </td>
                        <td class="px-6 py-4 text-center">
                          <div class="flex items-center justify-center gap-2">
                            <!-- Ver Detalle -->
                            <div class="relative group inline-block">
                              <button (click)="abrirDetalleEmpleado(row)" [disabled]="row.esManual"
                                [class]="row.esManual
                                  ? 'inline-flex items-center justify-center p-1.5 text-gray-400 bg-gray-100 rounded-lg border border-gray-200 cursor-not-allowed opacity-50'
                                  : 'inline-flex items-center justify-center p-1.5 text-ihss-900 bg-ihss-50 hover:bg-ihss-100 rounded-lg transition-colors border border-ihss-200'">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                {{ row.esManual ? 'No aplica a registro manual' : 'Ver Detalle' }}
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>

                            <!-- Registrar Motivo -->
                            <div class="relative group inline-block">
                              <button (click)="registrarMotivo(row)" 
                                class="inline-flex items-center justify-center p-1.5 text-blue-600 bg-blue-50 hover:bg-blue-100 rounded-lg transition-colors border border-blue-200">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                Registrar Motivo
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>

                            <!-- Dar Seguimiento -->
                            <div class="relative group inline-block">
                              <button (click)="darSeguimiento(row)" [disabled]="!row.tieneMotivo"
                                [class]="row.tieneMotivo 
                                  ? 'inline-flex items-center justify-center p-1.5 text-amber-600 bg-amber-50 hover:bg-amber-100 rounded-lg transition-colors border border-amber-200 cursor-pointer' 
                                  : 'inline-flex items-center justify-center p-1.5 text-gray-400 bg-gray-100 rounded-lg border border-gray-200 cursor-not-allowed opacity-50'">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                {{ row.tieneMotivo ? 'Dar Seguimiento' : 'Debe registrar un motivo primero' }}
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>

                            <!-- Imprimir Reporte -->
                            <div class="relative group inline-block">
                              <button (click)="imprimirReporteEmpleado(row)" 
                                class="inline-flex items-center justify-center p-1.5 text-emerald-600 bg-emerald-50 hover:bg-emerald-100 rounded-lg transition-colors border border-emerald-200">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/>
                                </svg>
                              </button>
                              <span class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 scale-0 group-hover:scale-100 transition-all duration-150 origin-bottom bg-slate-900 text-white text-[10px] font-bold py-1 px-2.5 rounded-lg shadow-xl whitespace-nowrap z-20">
                                Imprimir Reporte
                                <span class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-900"></span>
                              </span>
                            </div>
                          </div>
                        </td>
                      </tr>
                    }
                  }

                </tbody>
              </table>

              <!-- Paginación -->
              <div class="px-6 py-4 flex items-center justify-between border-t border-gray-150 bg-gray-50/30">
                <div class="text-xs text-gray-500">
                  Mostrando {{ (paginaActual() - 1) * limite() + 1 }} a {{ mathMin(paginaActual() * limite(), datosFiltrados().length) }} de {{ datosFiltrados().length }} registros
                </div>
                <div class="flex items-center gap-1">
                  <button (click)="paginaActual.set(paginaActual() - 1)" [disabled]="paginaActual() === 1"
                    class="p-2 border border-gray-200 rounded-lg bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-40 transition-all">
                    <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                    </svg>
                  </button>
                  
                  @for (page of paginasArray(); track page) {
                    <button (click)="paginaActual.set(page)"
                      [class]="paginaActual() === page 
                        ? 'bg-ihss-900 text-white font-bold' 
                        : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50'"
                      class="w-8 h-8 rounded-lg flex items-center justify-center text-xs transition-all">
                      {{ page }}
                    </button>
                  }

                  <button (click)="paginaActual.set(paginaActual() + 1)" [disabled]="paginaActual() === paginasTotales()"
                    class="p-2 border border-gray-200 rounded-lg bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-40 transition-all">
                    <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                    </svg>
                  </button>
                </div>
              </div>

            }
          }

        </div>

      </div>

      <!-- Modal de Detalle -->
      @if (modalDetalleAbierto()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 overflow-y-auto" role="dialog" aria-modal="true">
          
          <div class="fixed inset-0 bg-gray-500/75 transition-opacity" (click)="cerrarModal()"></div>

          <div class="relative bg-white rounded-2xl text-left overflow-hidden shadow-xl transform transition-all max-w-4xl w-full border border-gray-100 flex flex-col max-h-[90vh]">
              
               <div class="bg-gray-50 px-6 py-4 flex justify-between items-center border-b border-gray-200">
                <div>
                  <h3 class="text-lg font-bold text-gray-900">Detalle de Coincidencias</h3>
                  <p class="text-xs text-gray-500">
                    @if (tipoActivo() === 'empleado') {
                      Empleado IHSS: {{ personaSeleccionadaEmpleado()?.nombre }} (Identidad: {{ personaSeleccionadaEmpleado()?.identidad }})
                    } @else {
                      Persona Natural: {{ personaSeleccionada()?.nombre }} (DNI: {{ personaSeleccionada()?.numeroIdentificacion }})
                    }
                  </p>
                </div>
                <button (click)="cerrarModal()" class="text-gray-400 hover:text-gray-600 transition-colors">
                  <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div class="p-6 space-y-4 overflow-y-auto">
                @if (detalleCargando()) {
                  <div class="py-12 flex flex-col items-center justify-center gap-3">
                    <svg class="animate-spin h-8 w-8 text-ihss-900" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    <p class="text-sm font-medium text-gray-500">Cargando detalles...</p>
                  </div>
                } @else if ((tipoActivo() === 'empleado' ? detallesEmpleado().length : detallesNatural().length) === 0) {
                  <div class="py-12 flex flex-col items-center justify-center gap-2">
                    <svg class="w-12 h-12 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <p class="text-sm font-medium text-gray-500">No se encontraron registros detallados para esta coincidencia.</p>
                  </div>
                } @else {
                  <div class="overflow-x-auto rounded-xl border border-gray-200 max-h-96">
                    <table class="min-w-full divide-y divide-gray-200">
                      <thead class="bg-gray-50 text-[10px] font-bold text-gray-500 uppercase tracking-wider sticky top-0">
                        <tr>
                          <th class="px-4 py-3 text-left">Condición Actúa</th>
                          <th class="px-4 py-3 text-left">Nro Patronal</th>
                          <th class="px-4 py-3 text-left">Empresa</th>
                          @if (tipoActivo() === 'empleado') {
                            <th class="px-4 py-3 text-left">Razón Social</th>
                          } @else {
                            <th class="px-4 py-3 text-center">Es PEP</th>
                          }
                          <th class="px-4 py-3 text-left">Lista</th>
                          <th class="px-4 py-3 text-left">Fecha Coincidencia</th>
                          <th class="px-4 py-3 text-left">Fecha Calificación</th>
                        </tr>
                      </thead>
                      <tbody class="bg-white divide-y divide-gray-200 text-xs text-gray-700">
                        @if (tipoActivo() === 'empleado') {
                          @for (det of detallesEmpleado(); track $index) {
                            <tr class="hover:bg-gray-50/50 transition-colors">
                              <td class="px-4 py-3 font-medium text-gray-900">{{ det.tipoCondicionActuaDesc }}</td>
                              <td class="px-4 py-3">{{ det.numeroPatrono }}</td>
                              <td class="px-4 py-3 font-semibold">{{ det.nombreEmpresa }}</td>
                              <td class="px-4 py-3">{{ det.razoSoci }}</td>
                              <td class="px-4 py-3">
                                <span class="inline-flex px-2 py-0.5 rounded-md text-[10px] font-bold bg-red-50 text-red-700 ring-1 ring-red-600/10">
                                  {{ det.listaCoincidencia }}
                                </span>
                              </td>
                              <td class="px-4 py-3 text-gray-500">{{ det.fechaCoincidencia | date:'dd/MM/yyyy' }}</td>
                              <td class="px-4 py-3 text-gray-500">{{ det.fechaCalifico | date:'dd/MM/yyyy' }}</td>
                            </tr>
                          }
                        } @else {
                          @for (det of detallesNatural(); track $index) {
                            <tr class="hover:bg-gray-50/50 transition-colors">
                              <td class="px-4 py-3 font-medium text-gray-900">{{ det.tipoCondicionActuaDesc }}</td>
                              <td class="px-4 py-3">{{ det.numeroPatronal }}</td>
                              <td class="px-4 py-3 font-semibold">{{ det.nombreEmpresa }}</td>
                              <td class="px-4 py-3 text-center">
                                <span [class]="(det.esPep === 'SI' || det.esPep === 'S') 
                                  ? 'inline-flex px-2 py-0.5 rounded-full text-[10px] font-bold bg-red-100 text-red-800' 
                                  : 'inline-flex px-2 py-0.5 rounded-full text-[10px] font-semibold bg-gray-100 text-gray-600'">
                                  {{ (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO' }}
                                </span>
                              </td>
                              <td class="px-4 py-3">
                                <span class="inline-flex px-2 py-0.5 rounded-md text-[10px] font-bold bg-red-50 text-red-700 ring-1 ring-red-600/10">
                                  {{ det.listaCoincidencia }}
                                </span>
                              </td>
                              <td class="px-4 py-3 text-gray-500">{{ det.fechaCoincidencia | date:'dd/MM/yyyy' }}</td>
                              <td class="px-4 py-3 text-gray-500">{{ det.fechaCalifico | date:'dd/MM/yyyy' }}</td>
                            </tr>
                          }
                        }
                      </tbody>
                    </table>
                  </div>

                  <!-- Resumen al pie de la tabla -->
                  <div class="grid grid-cols-3 gap-4 bg-gray-50 p-4 rounded-xl border border-gray-200 text-sm">
                    <div class="flex flex-col">
                      <span class="text-xs font-semibold text-gray-500 uppercase tracking-wider">Total Coincidencias</span>
                      <span class="text-lg font-bold text-gray-900">{{ totalCoincidencias() }}</span>
                    </div>
                    @if (tipoActivo() !== 'empleado') {
                      <div class="flex flex-col">
                        <span class="text-xs font-semibold text-gray-500 uppercase tracking-wider">Coincidencias PEP</span>
                        <span class="text-lg font-bold" [class]="coincidenciasPep() > 0 ? 'text-red-600' : 'text-gray-900'">
                          {{ coincidenciasPep() }}
                        </span>
                      </div>
                    } @else {
                      <div class="flex flex-col">
                        <span class="text-xs font-semibold text-gray-500 uppercase tracking-wider">Tipo Relación</span>
                        <span class="text-lg font-bold text-gray-900">IHSS</span>
                      </div>
                    }
                    <div class="flex flex-col">
                      <span class="text-xs font-semibold text-gray-500 uppercase tracking-wider">Empresas Relacionadas</span>
                      <span class="text-lg font-bold text-gray-900">{{ empresasUnicas() }}</span>
                    </div>
                  </div>
                }
              </div>

              <div class="bg-gray-50 px-6 py-3 flex justify-end gap-2 border-t border-gray-200">
                <button (click)="exportarExcel()" class="px-4 py-2 bg-emerald-600 text-white rounded-xl hover:bg-emerald-700 font-semibold text-xs transition-colors flex items-center gap-1.5 shadow-sm">
                  <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                  Exportar Excel
                </button>
                <button (click)="verPdf()" class="px-4 py-2 bg-ihss-900 text-white rounded-xl hover:bg-ihss-800 font-semibold text-xs transition-colors flex items-center gap-1.5 shadow-sm">
                  <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
                  </svg>
                  Ver Reporte PDF
                </button>
                <button (click)="cerrarModal()" class="px-4 py-2 border border-gray-200 rounded-xl bg-white text-gray-700 hover:bg-gray-50 font-semibold text-xs transition-colors">
                  Cerrar
                </button>
              </div>

            </div>
        </div>
      }

      <!-- Modal de Visor PDF -->
      @if (pdfModalAbierto()) {
        <div class="fixed inset-0 z-[60] flex items-center justify-center p-4 overflow-y-auto" role="dialog" aria-modal="true">
          
          <div class="fixed inset-0 bg-gray-500/75 transition-opacity" (click)="cerrarPdfModal()"></div>

          <div class="relative bg-white rounded-2xl text-left overflow-hidden shadow-xl transform transition-all max-w-5xl w-full border border-gray-100 flex flex-col max-h-[95vh]">
              
              <div class="bg-gray-50 px-6 py-4 flex justify-between items-center border-b border-gray-200">
                <div>
                  <h3 class="text-lg font-bold text-gray-900">Reporte PDF Generado</h3>
                  <p class="text-xs text-gray-500">Visualización del documento de coincidencias</p>
                </div>
                <button (click)="cerrarPdfModal()" class="text-gray-400 hover:text-gray-600 transition-colors">
                  <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div class="p-6 overflow-y-auto">
                @if (pdfUrl()) {
                  <iframe [src]="pdfUrl()!" class="w-full h-[650px] rounded-xl border border-gray-200 shadow-sm" type="application/pdf"></iframe>
                }
              </div>

              <div class="bg-gray-50 px-6 py-3 flex justify-end gap-2 border-t border-gray-200">
                <button (click)="cerrarPdfModal()" class="px-4 py-2 border border-gray-200 rounded-xl bg-white text-gray-700 hover:bg-gray-50 font-semibold text-xs transition-colors">
                  Cerrar
                </button>
              </div>

            </div>
        </div>
      }

      <!-- Modal de Registrar Motivo -->
      @if (modalMotivoAbierto()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 overflow-y-auto" role="dialog" aria-modal="true">
          
          <div class="fixed inset-0 bg-gray-500/75 transition-opacity" (click)="cerrarModalMotivo()"></div>

          <div class="relative bg-white rounded-2xl text-left overflow-hidden shadow-xl transform transition-all max-w-lg w-full border border-gray-100 flex flex-col z-50">
              
              <div class="bg-gray-50 px-6 py-4 flex justify-between items-center border-b border-gray-200">
                <div>
                  <h3 class="text-lg font-bold text-gray-900">Registrar Motivo en Lista de Positivos</h3>
                  <p class="text-xs text-gray-500">Ingrese los detalles para registrar a la entidad en la lista de positivos.</p>
                </div>
                <button (click)="cerrarModalMotivo()" class="text-gray-400 hover:text-gray-600 transition-colors">
                  <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div class="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
                <!-- Nombre Completo -->
                <div>
                  <label class="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Nombre Completo</label>
                  @if (esRegistroManual()) {
                    <input type="text" [ngModel]="formManualNombre()" (ngModelChange)="formManualNombre.set($event)"
                      placeholder="Ingrese el nombre completo..."
                      class="w-full px-3 py-2 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors bg-white" />
                  } @else {
                    <input type="text" [value]="entidadSeleccionada()?.nombreCompleto" disabled
                      class="w-full px-3 py-2 bg-gray-50 border border-gray-200 rounded-xl text-gray-500 text-sm focus:outline-none cursor-not-allowed" />
                  }
                </div>

                <div class="grid grid-cols-2 gap-4">
                  <!-- Documento / Identificación -->
                  <div>
                    <label class="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Documento / Identificación</label>
                    @if (esRegistroManual()) {
                      <input type="text" [ngModel]="formManualNoDocumento()" (ngModelChange)="formManualNoDocumento.set($event)"
                        placeholder="Ingrese el documento..."
                        class="w-full px-3 py-2 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors bg-white" />
                    } @else {
                      <input type="text" [value]="entidadSeleccionada()?.noDocumento" disabled
                        class="w-full px-3 py-2 bg-gray-50 border border-gray-200 rounded-xl text-gray-500 text-sm focus:outline-none cursor-not-allowed" />
                    }
                  </div>
                  <!-- Tipo de Lista -->
                  <div>
                    <label class="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Tipo de Lista</label>
                    @if (esRegistroManual()) {
                      <select [ngModel]="formManualTipoPositivoId()" (ngModelChange)="formManualTipoPositivoId.set(+$event || null)"
                        class="w-full px-3 py-2 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors bg-white">
                        <option [value]="null" disabled selected>Seleccione...</option>
                        <option [value]="1">Jurídica</option>
                        <option [value]="2">Natural</option>
                        <option [value]="3">Empleado</option>
                      </select>
                    } @else {
                      <input type="text" [value]="entidadSeleccionada()?.tipoListaText" disabled
                        class="w-full px-3 py-2 bg-gray-50 border border-gray-200 rounded-xl text-gray-500 text-sm focus:outline-none cursor-not-allowed" />
                    }
                  </div>
                </div>

                <!-- Tipo de Documento Catálogo -->
                <div>
                  <label class="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Tipo de Documento</label>
                  <select [ngModel]="formTipoDocId()" (ngModelChange)="formTipoDocId.set(+$event || null)"
                    class="w-full px-3 py-2 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors bg-white">
                    <option [value]="null" disabled selected>Seleccione un tipo de documento...</option>
                    @for (doc of listaTiposDocumento(); track doc.tipoDocumentoId) {
                      <option [value]="doc.tipoDocumentoId">{{ doc.descripcion }}</option>
                    }
                  </select>
                </div>

                <!-- Tipo de Lista de Cautela -->
                <div>
                  <label class="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Tipo de Lista de Cautela</label>
                  <select [ngModel]="formTipoListaCautelaId()" (ngModelChange)="formTipoListaCautelaId.set(+$event || null)"
                    class="w-full px-3 py-2 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors bg-white">
                    <option [value]="null" disabled selected>Seleccione un tipo de lista de cautela...</option>
                    @for (cautela of listaTiposListasCautela(); track cautela.tipoListaCautelaId) {
                      <option [value]="cautela.tipoListaCautelaId">{{ cautela.descripcion }}</option>
                    }
                  </select>
                </div>

                <!-- Motivo de Ingreso -->
                <div>
                  <label class="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Motivo de Ingreso</label>
                  <textarea [ngModel]="formMotivo()" (ngModelChange)="formMotivo.set($event)" rows="4"
                    placeholder="Escriba detalladamente el motivo de ingreso a la lista de positivos..."
                    class="w-full px-3 py-2 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors resize-none"></textarea>
                </div>

                <!-- Primer Seguimiento (Opcional) -->
                <div class="border-t border-gray-200 pt-4 space-y-3">
                  <h4 class="text-xs font-bold text-gray-700 uppercase tracking-wider">Primer Seguimiento (Opcional)</h4>
                  
                  <div>
                    <label class="block text-xs font-semibold text-gray-500 mb-1">Nota o Comentario de Seguimiento</label>
                    <textarea [ngModel]="formSeguimientoComentario()" (ngModelChange)="formSeguimientoComentario.set($event)" rows="3"
                      placeholder="Escriba un comentario inicial de seguimiento si lo desea..."
                      class="w-full px-3 py-2 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors resize-none"></textarea>
                  </div>

                  <div>
                    <label class="block text-xs font-semibold text-gray-500 mb-1">Archivos de Evidencia del Seguimiento</label>
                    <div class="border-2 border-dashed border-gray-200 hover:border-blue-500 rounded-2xl p-4 text-center cursor-pointer transition-colors relative bg-gray-50">
                      <input type="file" multiple (change)="onManualSeguimientoFileSelected($event)"
                         class="absolute inset-0 opacity-0 cursor-pointer w-full h-full z-10" />
                      
                      <div class="flex flex-col items-center justify-center gap-1.5">
                        <svg class="w-8 h-8 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                        </svg>
                        <span class="text-xs font-bold text-gray-700">Arrastra archivos aquí o haz clic</span>
                        <span class="text-[10px] text-gray-400">PDF, imágenes, Word, Excel (Máx. 10MB)</span>
                      </div>
                    </div>
                  </div>

                  @if (archivosSeguimiento().length > 0) {
                    <div class="space-y-1.5 max-h-[120px] overflow-y-auto">
                      @for (file of archivosSeguimiento(); track $index) {
                        <div class="flex items-center justify-between bg-blue-50/50 border border-blue-100 rounded-xl p-2 text-xs">
                          <span class="text-blue-900 font-medium truncate max-w-[200px]">{{ file.name }}</span>
                          <div class="flex items-center gap-2">
                            <span class="text-[10px] text-gray-400">{{ (file.size / (1024 * 1024)) | number:'1.1-2' }} MB</span>
                            <button (click)="eliminarArchivoSeguimientoManual($index)" class="text-red-500 hover:text-red-700 transition-colors">
                              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                              </svg>
                            </button>
                          </div>
                        </div>
                      }
                    </div>
                  }
                </div>
              </div>

              <div class="bg-gray-50 px-6 py-3 flex justify-end gap-2 border-t border-gray-200">
                <button (click)="cerrarModalMotivo()" [disabled]="guardandoMotivo()"
                  class="px-4 py-2 border border-gray-200 rounded-xl bg-white text-gray-700 hover:bg-gray-50 font-semibold text-xs transition-colors disabled:opacity-50">
                  Cancelar
                </button>
                <button (click)="guardarMotivo()" [disabled]="guardandoMotivo() || !formTipoDocId() || !formMotivo().trim()"
                  class="px-4 py-2 bg-blue-600 text-white rounded-xl hover:bg-blue-700 font-semibold text-xs transition-colors flex items-center gap-1.5 shadow-sm disabled:opacity-50 disabled:cursor-not-allowed">
                  @if (guardandoMotivo()) {
                    <svg class="animate-spin -ml-1 mr-1.5 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    <span>Guardando...</span>
                  } @else {
                    <span>Guardar Motivo</span>
                  }
                </button>
              </div>

          </div>
        </div>
      }

      <!-- Modal de Seguimiento -->
      @if (modalSeguimientoAbierto()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 overflow-y-auto" role="dialog" aria-modal="true">
          <div class="fixed inset-0 bg-gray-500/75 transition-opacity" (click)="cerrarModalSeguimiento()"></div>

          <div class="relative bg-white rounded-2xl text-left overflow-hidden shadow-xl transform transition-all max-w-5xl w-full border border-gray-100 flex flex-col z-50 max-h-[90vh]">
            
            <div class="bg-gray-50 px-6 py-4 flex justify-between items-center border-b border-gray-200">
              <div>
                <h3 class="text-lg font-bold text-gray-900">Seguimiento e Historial de Controles</h3>
                <p class="text-xs text-gray-500">
                  {{ entidadSeleccionada()?.nombreCompleto }} (Documento: {{ entidadSeleccionada()?.noDocumento }})
                </p>
              </div>
              <button (click)="cerrarModalSeguimiento()" class="text-gray-400 hover:text-gray-600 transition-colors">
                <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div class="flex-1 overflow-y-auto p-6 grid grid-cols-1 md:grid-cols-12 gap-6 min-h-[450px]">
              
              <!-- Columna Izquierda: Historial -->
              <div class="md:col-span-7 flex flex-col space-y-4 border-r border-gray-100 pr-0 md:pr-6">
                <h4 class="text-xs font-bold text-gray-400 uppercase tracking-wider">Historial de Seguimientos</h4>
                
                @if (cargandoSeguimiento()) {
                  <div class="flex-1 flex flex-col items-center justify-center py-10 gap-2">
                    <svg class="animate-spin h-6 w-6 text-ihss-900" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    <span class="text-xs text-gray-400 font-medium">Cargando historial...</span>
                  </div>
                } @else if (listaSeguimientos().length === 0) {
                  <div class="flex-1 flex flex-col items-center justify-center py-10 gap-2">
                    <svg class="w-10 h-10 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                    </svg>
                    <span class="text-xs text-gray-400 font-medium text-center">No hay notas de seguimiento registradas.</span>
                  </div>
                } @else {
                  <div class="space-y-6 overflow-y-auto max-h-[480px] pl-4 pr-2">
                    @for (seg of listaSeguimientos(); track seg.detalleListaId) {
                      <div class="relative pl-6 border-l-2 border-gray-200 pb-2">
                        <span class="absolute -left-1.5 top-1.5 bg-blue-600 rounded-full w-3 h-3 ring-4 ring-white"></span>
                        
                        <div class="bg-gray-50 rounded-xl p-4 border border-gray-100 shadow-sm space-y-2">
                          <div class="flex justify-between items-center text-xs text-gray-400">
                            <span class="font-bold text-gray-600">{{ seg.usrEmail }}</span>
                            <div class="flex items-center gap-2">
                              <span>{{ seg.fechaCreacion | date:'dd/MM/yyyy HH:mm' }}</span>
                              <button (click)="iniciarEdicionSeguimiento(seg)" class="text-blue-600 hover:text-blue-800 transition-colors p-0.5" title="Editar seguimiento">
                                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                                </svg>
                              </button>
                              <button (click)="eliminarSeguimiento(seg)" class="text-red-500 hover:text-red-700 transition-colors p-0.5" title="Eliminar seguimiento">
                                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                </svg>
                              </button>
                            </div>
                          </div>
                          
                          <p class="text-sm text-gray-700 whitespace-pre-line leading-relaxed font-normal">
                            {{ seg.motivoIngreso }}
                          </p>

                          @if (seg.evidencias && seg.evidencias.length > 0) {
                            <div class="pt-2 border-t border-gray-200/50 flex flex-wrap gap-2">
                              @for (evi of seg.evidencias; track evi.evidenciaId) {
                                <button (click)="descargarEvidencia(evi)"
                                  class="inline-flex items-center gap-1.5 px-2.5 py-1 bg-white hover:bg-gray-150 border border-gray-200 rounded-lg text-xs font-medium text-gray-600 transition-colors shadow-sm">
                                  
                                  @if (obtenerIconoArchivo(evi.tipoMime) === 'application/pdf') {
                                    <svg class="w-4 h-4 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                                    </svg>
                                  } @else if (obtenerIconoArchivo(evi.tipoMime) === 'image') {
                                    <svg class="w-4 h-4 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                                    </svg>
                                  } @else if (obtenerIconoArchivo(evi.tipoMime) === 'word') {
                                    <svg class="w-4 h-4 text-indigo-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                    </svg>
                                  } @else if (obtenerIconoArchivo(evi.tipoMime) === 'excel') {
                                    <svg class="w-4 h-4 text-emerald-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                    </svg>
                                  } @else {
                                    <svg class="w-4 h-4 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                    </svg>
                                  }
                                  
                                  <span class="truncate max-w-[150px]">{{ evi.nombreArchivo }}</span>
                                </button>
                              }
                            </div>
                          }
                        </div>
                      </div>
                    }
                  </div>
                }
              </div>

              <!-- Columna Derecha: Registro -->
              <div class="md:col-span-5 flex flex-col space-y-4">
                <h4 class="text-xs font-bold text-gray-400 uppercase tracking-wider">
                  {{ modoEdicion() ? 'Editar Seguimiento' : 'Agregar Seguimiento' }}
                </h4>
                
                <div class="flex flex-col">
                  <label class="block text-xs font-semibold text-gray-500 mb-1">Nota o Comentario</label>
                  <textarea [ngModel]="formComentarioSeguimiento()" (ngModelChange)="formComentarioSeguimiento.set($event)" rows="5"
                    placeholder="Escriba los comentarios o acciones de seguimiento tomadas..."
                    class="w-full px-3 py-2 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors resize-none"></textarea>
                </div>

                @if (modoEdicion() && evidenciasExistentes().length > 0) {
                  <div class="flex flex-col">
                    <label class="block text-xs font-semibold text-gray-500 mb-1">Evidencias Actuales</label>
                    <div class="space-y-1.5 max-h-[120px] overflow-y-auto mb-1">
                      @for (evi of evidenciasExistentes(); track evi.evidenciaId) {
                        <div class="flex items-center justify-between bg-gray-100 border border-gray-200 rounded-xl p-2 text-xs">
                          <span class="text-gray-800 font-medium truncate max-w-[200px]">{{ evi.nombreArchivo }}</span>
                          <button (click)="eliminarEvidenciaExistente(evi)" class="text-red-500 hover:text-red-700 transition-colors" title="Eliminar evidencia">
                            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                            </svg>
                          </button>
                        </div>
                      }
                    </div>
                  </div>
                }

                <div class="flex flex-col">
                  <label class="block text-xs font-semibold text-gray-500 mb-1">
                    {{ modoEdicion() ? 'Agregar Nuevas Evidencias' : 'Archivos de Evidencia' }}
                  </label>
                  
                  <div class="border-2 border-dashed border-gray-200 hover:border-blue-500 rounded-2xl p-4 text-center cursor-pointer transition-colors relative bg-gray-50">
                    <input type="file" multiple (change)="onFileSelected($event)"
                       class="absolute inset-0 opacity-0 cursor-pointer w-full h-full z-10" />
                    
                    <div class="flex flex-col items-center justify-center gap-1.5">
                      <svg class="w-8 h-8 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                      </svg>
                      <span class="text-xs font-bold text-gray-700">Arrastra archivos aquí o haz clic</span>
                      <span class="text-[10px] text-gray-400">PDF, imágenes, Word, Excel (Máx. 10MB c/u)</span>
                    </div>
                  </div>
                </div>

                @if (archivosSeleccionados().length > 0) {
                  <div class="space-y-1.5 max-h-[120px] overflow-y-auto">
                    @for (file of archivosSeleccionados(); track $index) {
                      <div class="flex items-center justify-between bg-blue-50/50 border border-blue-100 rounded-xl p-2 text-xs">
                        <span class="text-blue-900 font-medium truncate max-w-[200px]">{{ file.name }}</span>
                        <div class="flex items-center gap-2">
                          <span class="text-[10px] text-gray-400">{{ (file.size / (1024 * 1024)) | number:'1.1-2' }} MB</span>
                          <button (click)="eliminarArchivoSeleccionado($index)" class="text-red-500 hover:text-red-700 transition-colors">
                            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                            </svg>
                          </button>
                        </div>
                      </div>
                    }
                  </div>
                }

                <div class="pt-2 flex flex-col gap-2">
                  <button (click)="guardarSeguimiento()" [disabled]="guardandoSeguimiento() || !formComentarioSeguimiento().trim()"
                    class="w-full px-4 py-2.5 bg-blue-600 text-white rounded-xl hover:bg-blue-700 font-semibold text-xs transition-colors flex items-center justify-center gap-1.5 shadow-sm disabled:opacity-50 disabled:cursor-not-allowed font-bold">
                    @if (guardandoSeguimiento()) {
                      <svg class="animate-spin -ml-1 mr-1.5 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      <span>Guardando...</span>
                    } @else {
                      <span>{{ modoEdicion() ? 'Guardar Cambios' : 'Guardar Seguimiento' }}</span>
                    }
                  </button>

                  @if (modoEdicion()) {
                    <button (click)="cancelarEdicion()" [disabled]="guardandoSeguimiento()"
                      class="w-full px-4 py-2 border border-gray-250 bg-white text-gray-700 rounded-xl hover:bg-gray-50 font-semibold text-xs transition-colors">
                      Cancelar Edición
                    </button>
                  }
                </div>

              </div>

            </div>

            <div class="bg-gray-50 px-6 py-3 flex justify-end border-t border-gray-200">
              <button (click)="cerrarModalSeguimiento()" [disabled]="guardandoSeguimiento()"
                class="px-4 py-2 border border-gray-200 rounded-xl bg-white text-gray-700 hover:bg-gray-50 font-semibold text-xs transition-colors">
                Cerrar
              </button>
            </div>

          </div>
        </div>
      }

    </div>
  `
})
export class MonitoreoListasComponent implements OnInit {
  private sanitizer = inject(DomSanitizer);
  private configService = inject(ConfiguracionService);

  tipoActivo = signal<FiltroTipo>('juridica');
  cargando = signal(false);
  busqueda = signal('');

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
  }

  cambiarTipo(tipo: FiltroTipo) {
    this.tipoActivo.set(tipo);
    this.busqueda.set('');
    this.paginaActual.set(1);
    this.cargarDatos();
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

    if (tipo === 'juridica') {
      const data = this.juridicasRaw();
      if (!query) return data;
      return data.filter(item => 
        item.nombre.toLowerCase().includes(query) ||
        item.rtn.includes(query) ||
        item.numeroPatrono.includes(query) ||
        item.listaCoincidencia.toLowerCase().includes(query)
      );
    } else if (tipo === 'natural') {
      const data = this.naturalesRaw();
      if (!query) return data;
      return data.filter(item => 
        item.nombre.toLowerCase().includes(query) ||
        item.numeroIdentificacion.includes(query) ||
        item.listaCoincidencia.toLowerCase().includes(query)
      );
    } else {
      const data = this.empleadosRaw();
      if (!query) return data;
      return data.filter(item => 
        item.nombre.toLowerCase().includes(query) ||
        item.identidad.includes(query) ||
        item.listaCoincidencia.toLowerCase().includes(query)
      );
    }
  });

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
      tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincid.', 'Fecha Calific.']];
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
      tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincid.', 'Fecha Calific.']];
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

  cerrarPdfModal() {
    this.pdfModalAbierto.set(false);
    this.pdfUrl.set(null);
  }

  imprimirReportePatrono(row: CoincidenciaJuridica) {
    this.cargando.set(true);

    const obsPositivo = row.tieneMotivo 
      ? this.listasService.getPositivoPorDocumento(row.numeroPatrono) 
      : of(null);
      
    const obsSeguimientos = row.tieneMotivo 
      ? this.listasService.getSeguimientos(row.numeroPatrono) 
      : of([]);

    forkJoin([obsPositivo, obsSeguimientos]).subscribe({
      next: ([positivo, seguimientos]) => {
        const auditoriaData = {
          rtn: row.rtn,
          nombre: row.nombre,
          numeroPatrono: row.numeroPatrono,
          listaCoincidencia: row.listaCoincidencia,
          tieneMotivo: row.tieneMotivo,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.numeroPatrono, auditoriaData).subscribe({
          next: () => {
            this.generarPdfPatrono(row, positivo, seguimientos);
            this.cargando.set(false);
          },
          error: (err) => {
            console.error('Error al registrar auditoría de impresión:', err);
            this.generarPdfPatrono(row, positivo, seguimientos);
            this.cargando.set(false);
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

    const obsDetalles = this.listasService.getDetalleNatural(row.numeroIdentificacion);
    const obsPositivo = row.tieneMotivo 
      ? this.listasService.getPositivoPorDocumento(row.numeroIdentificacion) 
      : of(null);
    const obsSeguimientos = row.tieneMotivo 
      ? this.listasService.getSeguimientos(row.numeroIdentificacion) 
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const auditoriaData = {
          numeroIdentificacion: row.numeroIdentificacion,
          nombre: row.nombre,
          listaCoincidencia: row.listaCoincidencia,
          totalRepetidos: row.totalRepetidos,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.numeroIdentificacion, auditoriaData).subscribe({
          next: () => {
            this.generarPdfNatural(row, detalles, positivo, seguimientos);
            this.cargando.set(false);
          },
          error: (err) => {
            console.error('Error al registrar auditoría de impresión:', err);
            this.generarPdfNatural(row, detalles, positivo, seguimientos);
            this.cargando.set(false);
          }
        });
      },
      error: (err) => {
        console.error('Error al cargar datos para el reporte:', err);
        this.cargando.set(false);
      }
    });
  }

  generarPdfNatural(row: CoincidenciaNatural, detalles: DetalleCoincidenciaNatural[], positivo: any, seguimientos: Seguimiento[]) {
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
      ['Estado Monitoreo:', row.tieneMotivo ? 'CON MOTIVO REGISTRADO' : 'PENDIENTE DE REGISTRO', '', '']
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

    const tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincid.', 'Fecha Calific.']];
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

    // Sección 4: Historial de Seguimiento
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
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
    } else {
      doc.setFont('helvetica', 'italic');
      doc.setFontSize(9.5);
      doc.setTextColor(100, 116, 139);
      doc.text('No se registran acciones de seguimiento ni evidencias adicionales para esta persona.', 14, y);
    }

    const blob = doc.output('blob');
    const url = URL.createObjectURL(blob);
    this.pdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
    this.pdfModalAbierto.set(true);
  }

  imprimirReporteEmpleado(row: CoincidenciaEmpleado) {
    this.cargando.set(true);

    const obsDetalles = this.listasService.getDetalleEmpleado(row.identidad);
    const obsPositivo = row.tieneMotivo 
      ? this.listasService.getPositivoPorDocumento(row.identidad) 
      : of(null);
    const obsSeguimientos = row.tieneMotivo 
      ? this.listasService.getSeguimientos(row.identidad) 
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const auditoriaData = {
          identidad: row.identidad,
          nombre: row.nombre,
          listaCoincidencia: row.listaCoincidencia,
          totalRepetidos: row.totalRepetidos,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.identidad, auditoriaData).subscribe({
          next: () => {
            this.generarPdfEmpleado(row, detalles, positivo, seguimientos);
            this.cargando.set(false);
          },
          error: (err) => {
            console.error('Error al registrar auditoría de impresión:', err);
            this.generarPdfEmpleado(row, detalles, positivo, seguimientos);
            this.cargando.set(false);
          }
        });
      },
      error: (err) => {
        console.error('Error al cargar datos para el reporte:', err);
        this.cargando.set(false);
      }
    });
  }

  generarPdfEmpleado(row: CoincidenciaEmpleado, detalles: DetalleCoincidenciaEmpleado[], positivo: any, seguimientos: Seguimiento[]) {
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
      ['Estado Monitoreo:', row.tieneMotivo ? 'CON MOTIVO REGISTRADO' : 'PENDIENTE DE REGISTRO', '', '']
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

    const tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincid.', 'Fecha Calific.']];
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

    // Sección 4: Historial de Seguimiento
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
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
    } else {
      doc.setFont('helvetica', 'italic');
      doc.setFontSize(9.5);
      doc.setTextColor(100, 116, 139);
      doc.text('No se registran acciones de seguimiento ni evidencias adicionales para este empleado.', 14, y);
    }

    const blob = doc.output('blob');
    const url = URL.createObjectURL(blob);
    this.pdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
    this.pdfModalAbierto.set(true);
  }

  generarPdfPatrono(row: CoincidenciaJuridica, positivo: any, seguimientos: Seguimiento[]) {
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
      ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Estado Monitoreo:', row.tieneMotivo ? 'CON MOTIVO REGISTRADO' : 'PENDIENTE DE REGISTRO'],
      ['Fecha Encontrado:', row.fechaEncontro ? this.formatDate(row.fechaEncontro) : 'N/D', 'Fecha Calificado:', row.fechaCalifico ? this.formatDate(row.fechaCalifico) : 'N/D']
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

    // Sección 3: Historial de Seguimientos
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text('3. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
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
    } else {
      doc.setFont('helvetica', 'italic');
      doc.setFontSize(9.5);
      doc.setTextColor(100, 116, 139);
      doc.text('No se registran acciones de seguimiento ni evidencias adicionales para este patrono.', 14, y);
    }

    const blob = doc.output('blob');
    const url = URL.createObjectURL(blob);
    this.pdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
    this.pdfModalAbierto.set(true);
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
      XLSX.writeFile(wb, fileName);
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
      XLSX.writeFile(wb, fileName);
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
      headers = ['Número Patronal', 'RTN', 'Nombre Empresa', 'Lista Coincidencia', 'Proveedor IHSS', 'Fecha Encontrado', 'Fecha Calificado'];
      rows = (dataFiltrada as CoincidenciaJuridica[]).map(item => [
        item.numeroPatrono,
        item.rtn,
        item.nombre,
        item.listaCoincidencia,
        item.esProveedorIhss || 'No',
        item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
        item.fechaCalifico ? this.formatDate(item.fechaCalifico) : ''
      ]);
    } else if (tipo === 'natural') {
      title = 'Reporte de Coincidencias Naturales';
      headers = ['Número Identificación', 'Nombre Completo'];
      rows = (dataFiltrada as CoincidenciaNatural[]).map(item => [
        item.numeroIdentificacion,
        item.nombre
      ]);
    } else {
      title = 'Reporte de Coincidencias Empleados';
      headers = ['Identidad', 'Nombre Empleado'];
      rows = (dataFiltrada as CoincidenciaEmpleado[]).map(item => [
        item.identidad,
        item.nombre
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
    XLSX.writeFile(wb, fileName);
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
        } else {
          this.formTipoDocId.set(null);
          this.formMotivo.set('');
          this.formTipoListaCautelaId.set(null);
        }
        this.modalMotivoAbierto.set(true);
      },
      error: (err) => {
        console.error('Error al obtener datos existentes de la lista de positivos:', err);
        this.formTipoDocId.set(null);
        this.formMotivo.set('');
        this.formTipoListaCautelaId.set(null);
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
    this.esRegistroManual.set(false);
    this.formManualNombre.set('');
    this.formManualNoDocumento.set('');
    this.formManualTipoPositivoId.set(null);
    this.formSeguimientoComentario.set('');
    this.archivosSeguimiento.set([]);
  }

  onManualSeguimientoFileSelected(event: any) {
    const files: FileList = event.target.files;
    if (files && files.length > 0) {
      const currentList = [...this.archivosSeguimiento()];
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        
        const sizeMb = file.size / (1024 * 1024);
        if (sizeMb > 10) {
          import('sweetalert2').then(Swal => {
            Swal.default.fire({
              title: 'Archivo muy grande',
              text: `El archivo ${file.name} supera el límite de 10MB.`,
              icon: 'warning',
              confirmButtonColor: '#1d4ed8'
            });
          });
          continue;
        }

        const ext = file.name.split('.').pop()?.toLowerCase();
        const allowedExts = ['pdf', 'png', 'jpg', 'jpeg', 'doc', 'docx', 'xls', 'xlsx'];
        if (!ext || !allowedExts.includes(ext)) {
          import('sweetalert2').then(Swal => {
            Swal.default.fire({
              title: 'Formato no permitido',
              text: `El archivo ${file.name} no tiene una extensión permitida (PDF, imágenes, Word, Excel).`,
              icon: 'warning',
              confirmButtonColor: '#1d4ed8'
            });
          });
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

    if (!docId || !cautelaId || !motivo) {
      import('sweetalert2').then(Swal => {
        Swal.default.fire({
          title: 'Campos requeridos',
          text: 'Por favor seleccione el tipo de documento, el tipo de lista de cautela e ingrese el motivo.',
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
      tipoListaCautelaId: Number(cautelaId)
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
          const comentarioSeg = this.formSeguimientoComentario().trim();
          const archivosSeg = this.archivosSeguimiento();
          
          if (comentarioSeg) {
            this.listasService.registrarSeguimiento(noDocumento, comentarioSeg, archivosSeg).subscribe({
              next: () => {
                this.guardandoMotivo.set(false);
                this.cerrarModalMotivo();
                Swal.default.fire({
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
    this.archivosSeleccionados.set([]);
    this.modalSeguimientoAbierto.set(true);
    this.cargandoSeguimiento.set(true);

    this.listasService.getSeguimientos(docNum).subscribe({
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

  cerrarModalSeguimiento() {
    this.modalSeguimientoAbierto.set(false);
    this.entidadSeleccionada.set(null);
    this.listaSeguimientos.set([]);
    this.formComentarioSeguimiento.set('');
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
        
        const sizeMb = file.size / (1024 * 1024);
        if (sizeMb > 10) {
          import('sweetalert2').then(Swal => {
            Swal.default.fire({
              title: 'Archivo muy grande',
              text: `El archivo ${file.name} supera el límite de 10MB.`,
              icon: 'warning',
              confirmButtonColor: '#1d4ed8'
            });
          });
          continue;
        }

        const ext = file.name.split('.').pop()?.toLowerCase();
        const allowedExts = ['pdf', 'png', 'jpg', 'jpeg', 'doc', 'docx', 'xls', 'xlsx'];
        if (!ext || !allowedExts.includes(ext)) {
          import('sweetalert2').then(Swal => {
            Swal.default.fire({
              title: 'Formato no permitido',
              text: `El archivo ${file.name} no tiene una extensión permitida (PDF, imágenes, Word, Excel).`,
              icon: 'warning',
              confirmButtonColor: '#1d4ed8'
            });
          });
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

  eliminarEvidenciaExistente(evi: Evidencia) {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: '¿Eliminar evidencia?',
        text: `Se eliminará permanentemente el archivo ${evi.nombreArchivo}`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar'
      }).then((result) => {
        if (result.isConfirmed) {
          Swal.default.fire({
            title: 'Eliminando...',
            allowOutsideClick: false,
            didOpen: () => {
              Swal.default.showLoading();
            }
          });

          this.listasService.eliminarEvidencia(evi.evidenciaId).subscribe({
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
                title: 'Eliminado',
                text: resp.mensaje || 'Evidencia eliminada correctamente.',
                icon: 'success',
                confirmButtonColor: '#1d4ed8'
              });
            },
            error: (err) => {
              Swal.default.fire({
                title: 'Error',
                text: err.error?.mensaje || 'No se pudo eliminar la evidencia.',
                icon: 'error',
                confirmButtonColor: '#1d4ed8'
              });
            }
          });
        }
      });
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
            title: 'Éxito',
            text: resp.mensaje || (editMode ? 'Seguimiento actualizado exitosamente.' : 'Seguimiento registrado exitosamente.'),
            icon: 'success',
            confirmButtonColor: '#1d4ed8'
          });

          this.cargandoSeguimiento.set(true);
          this.listasService.getSeguimientos(entidad.noDocumento).subscribe({
            next: (res) => {
              this.listaSeguimientos.set(res);
              this.cargandoSeguimiento.set(false);
            },
            error: () => {
              this.listaSeguimientos.set([]);
              this.cargandoSeguimiento.set(false);
            }
          });
        },
        error: (err) => {
          this.guardandoSeguimiento.set(false);
          Swal.default.fire({
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
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: '¿Eliminar seguimiento?',
        text: 'Esta acción realizará una eliminación lógica de la nota de seguimiento.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar'
      }).then((result) => {
        if (result.isConfirmed) {
          Swal.default.fire({
            title: 'Eliminando...',
            allowOutsideClick: false,
            didOpen: () => {
              Swal.default.showLoading();
            }
          });

          this.listasService.eliminarSeguimiento(seg.detalleListaId).subscribe({
            next: (resp) => {
              if (this.modoEdicion() && this.seguimientoEditandoId() === seg.detalleListaId) {
                this.cancelarEdicion();
              }

              Swal.default.fire({
                title: 'Eliminado',
                text: resp.mensaje || 'El seguimiento ha sido eliminado correctamente.',
                icon: 'success',
                confirmButtonColor: '#1d4ed8'
              });

              const entidad = this.entidadSeleccionada();
              if (entidad) {
                this.cargandoSeguimiento.set(true);
                this.listasService.getSeguimientos(entidad.noDocumento).subscribe({
                  next: (res) => {
                    this.listaSeguimientos.set(res);
                    this.cargandoSeguimiento.set(false);
                  },
                  error: () => {
                    this.listaSeguimientos.set([]);
                    this.cargandoSeguimiento.set(false);
                  }
                });
              }
            },
            error: (err) => {
              Swal.default.fire({
                title: 'Error',
                text: err.error?.mensaje || 'No se pudo eliminar el seguimiento.',
                icon: 'error',
                confirmButtonColor: '#1d4ed8'
              });
            }
          });
        }
      });
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
