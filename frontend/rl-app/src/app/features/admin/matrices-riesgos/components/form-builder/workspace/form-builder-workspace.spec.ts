import type { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormBuilderCanvasV2Component } from './form-builder-canvas.component';
import { FormBuilderInspectorV2Component } from './form-builder-inspector.component';
import { FormBuilderPaletteV2Component } from './form-builder-palette.component';
import { FormBuilderStatusbarV2Component } from './form-builder-statusbar.component';
import { FormBuilderToolbarV2Component } from './form-builder-toolbar.component';
import { FormBuilderWorkspaceV2Component } from './form-builder-workspace.component';
import { crearEstadoInicialWorkspace, FORM_BUILDER_UI_V2_CONTRACT } from './form-builder-workspace.types';

describe('Form Builder UI v2 - scaffold de migración', () => {
  const componentes: Type<unknown>[] = [
    FormBuilderWorkspaceV2Component,
    FormBuilderToolbarV2Component,
    FormBuilderPaletteV2Component,
    FormBuilderCanvasV2Component,
    FormBuilderInspectorV2Component,
    FormBuilderStatusbarV2Component
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: componentes }).compileComponents();
  });

  it('mantiene el contrato UI separado del contrato JSON persistido', () => {
    const estado = crearEstadoInicialWorkspace();

    expect(FORM_BUILDER_UI_V2_CONTRACT).toBe('UI-FORM-V2');
    expect(estado).toEqual({
      view: 'editor',
      inspectorTab: 'general',
      paletteCollapsed: false,
      inspectorCollapsed: false,
      dirty: false,
      saving: false
    });
  });

  it('compila todos los shells presentacionales sin dependencias de HTTP ni backend', () => {
    for (const tipo of componentes) {
      const fixture = TestBed.createComponent(tipo);
      fixture.detectChanges();
      expect(fixture.componentInstance).toBeTruthy();
    }
  });
});
