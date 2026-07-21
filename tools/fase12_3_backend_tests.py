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

p=root/'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosApplicationTests.cs'; s=read(p)
newtest='''
    [Fact]
    public async Task Dashboard_NormalizaFiltrosYDelegaAlRepositorio()
    {
        var service = CrearServicio(out var repo, out _);
        MatrizRiesgoReporteFiltroDto? recibido = null;
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerDashboardAsync), args =>
        {
            recibido = Assert.IsType<MatrizRiesgoReporteFiltroDto>(args[0]);
            return Task.FromResult(new MatricesRiesgoDashboardDto { TotalMatrices = 4 });
        });

        var result = await service.ObtenerDashboardAsync(new MatrizRiesgoReporteFiltroDto
        {
            Estado = " aprobada ",
            SujetoTipo = " proveedor ",
            NivelInherente = " Alto ",
            NivelResidual = " Medio "
        });

        Assert.True(result.Success);
        Assert.Equal(4, result.Data?.TotalMatrices);
        Assert.NotNull(recibido);
        Assert.Equal("APROBADA", recibido!.Estado);
        Assert.Equal("PROVEEDOR", recibido.SujetoTipo);
        Assert.Equal("Alto", recibido.NivelInherente);
        Assert.Equal("Medio", recibido.NivelResidual);
    }

'''
marker='    private static MatricesRiesgosAppService CrearServicio'
if marker not in s: raise RuntimeError('test marker')
s=s.replace(marker,newtest+marker,1)
write(p,s)
