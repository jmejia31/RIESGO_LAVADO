from pathlib import Path
import re

root=Path(__file__).resolve().parents[1]
B=root/'backend/RL.API/Features/MatricesRiesgos'
F=root/'frontend/rl-app/src/app/features/admin/matrices-riesgos'

def read(p): return p.read_text(encoding='utf-8-sig')
def write(p,s): p.write_text(s,encoding='utf-8')
def rep(s,old,new,label):
    if old not in s:
        if new in s: return s
        raise RuntimeError(label)
    return s.replace(old,new,1)
def reg(s,pat,new,label):
    out,n=re.subn(pat,new,s,count=1,flags=re.S)
    if n!=1: raise RuntimeError(label)
    return out

p=F/'models/matrices-riesgos.models.ts'; s=read(p)
s=rep(s,'export interface MatrizRiesgoReporteFiltro extends MatrizRiesgoFiltro {\n  nivelResidual?: string;','export interface MatrizRiesgoReporteFiltro extends MatrizRiesgoFiltro {\n  nivelInherente?: string;\n  nivelResidual?: string;','model filter')
old='''export interface MatrizRiesgoDashboard {
  totalMatrices: number;
  totalCalculadas: number;
  totalCerradas: number;
  totalConPlanAccion: number;
  porEstado: { nombre: string; total: number }[];
  porNivelResidual: { nombre: string; total: number }[];
}
'''
new='''export interface MatrizRiesgoMapaTransicion {
  nivelInherente: string;
  nivelResidual: string;
  total: number;
  promedioInherente: number;
  promedioResidual: number;
}

export interface MatrizRiesgoDashboard {
  fechaGeneracion: string;
  filtro: MatrizRiesgoReporteFiltro;
  totalMatrices: number;
  totalCalculadas: number;
  totalCerradas: number;
  totalConPlanAccion: number;
  totalAltoCritico: number;
  totalPlanesVencidos: number;
  porEstado: { nombre: string; total: number }[];
  porNivelInherente: { nombre: string; total: number }[];
  porNivelResidual: { nombre: string; total: number }[];
  mapaTransicion: MatrizRiesgoMapaTransicion[];
  matricesCriticas: MatrizRiesgoResumen[];
  matricesFiltradas: MatrizRiesgoResumen[];
  planesAccion: { estado: string; total: number; vencidos: number }[];
}
'''
s=rep(s,old,new,'frontend dashboard model')
write(p,s)

p=F/'data-access/matrices-riesgos.service.ts'; s=read(p)
s=rep(s,'  dashboard(): Observable<MatrizRiesgoDashboard> {\n    return this.http.get<ApiResponse<MatrizRiesgoDashboard>>(`${this.apiUrl}/dashboard`)','  dashboard(filtro: MatrizRiesgoReporteFiltro = {}): Observable<MatrizRiesgoDashboard> {\n    return this.http.get<ApiResponse<MatrizRiesgoDashboard>>(`${this.apiUrl}/dashboard`, { params: this.construirParams(filtro) })','service dashboard')
write(p,s)
p=F/'data-access/matrices-riesgos.service.spec.ts'; s=read(p)
newtest='''
  it('consulta el dashboard ejecutivo con filtros reales', () => {
    const result = vi.fn();
    service.dashboard({ sujetoTipo: 'PROVEEDOR', nivelInherente: 'ALTO', nivelResidual: 'MEDIO' }).subscribe(result);

    const request = http.expectOne(req => req.url === `${apiUrl}/dashboard`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('sujetoTipo')).toBe('PROVEEDOR');
    expect(request.request.params.get('nivelInherente')).toBe('ALTO');
    expect(request.request.params.get('nivelResidual')).toBe('MEDIO');
    request.flush({ success: true, datos: { totalMatrices: 2 } });
    expect(result).toHaveBeenCalledWith({ totalMatrices: 2 });
  });

'''
marker="  it('construye el reporte omitiendo filtros vacios', () => {"
s=s.replace(marker,newtest+marker,1)
write(p,s)
