export type FormBuilderWorkspaceView = 'editor' | 'preview' | 'catalogs' | 'json';

export type FormBuilderInspectorTab = 'general' | 'reglas' | 'datos' | 'presentacion';

export interface FormBuilderWorkspaceUiState {
  view: FormBuilderWorkspaceView;
  inspectorTab: FormBuilderInspectorTab;
  paletteCollapsed: boolean;
  inspectorCollapsed: boolean;
  dirty: boolean;
  saving: boolean;
}

export const FORM_BUILDER_UI_V2_CONTRACT = 'UI-FORM-V2' as const;

export function crearEstadoInicialWorkspace(): FormBuilderWorkspaceUiState {
  return {
    view: 'editor',
    inspectorTab: 'general',
    paletteCollapsed: false,
    inspectorCollapsed: false,
    dirty: false,
    saving: false
  };
}
