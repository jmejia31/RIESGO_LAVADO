using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using RL.API.Infrastructure.Database;
using RL.API.Middleware;
using RL.API.Features.Auditoria.Application;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Catalogos.Application;
using RL.API.Features.Catalogos.Persistence;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Persistence;
using RL.API.Features.Identidad.Application;
using RL.API.Features.Identidad.Integrations.ActiveDirectory;
using RL.API.Features.Identidad.Integrations.Email;
using RL.API.Features.Identidad.Persistence;
using RL.API.Features.Listas.Application;
using RL.API.Features.Listas.Persistence;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

var builder = WebApplication.CreateBuilder(args);

// Proceso de arranque: configura la bitácora técnica del API antes de registrar servicios.
// Los eventos funcionales y sensibles se registran por RL_AUDITORIA desde los servicios/repositorios.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/rl-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Proceso HTTP: habilita controladores y serialización JSON para exponer contratos REST limpios.
builder.Services.AddControllers()
    .AddNewtonsoftJson(opts =>
    {
        opts.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        opts.SerializerSettings.NullValueHandling     = Newtonsoft.Json.NullValueHandling.Ignore;
    });

builder.Services.AddEndpointsApiExplorer();

// Proceso de documentación técnica: Swagger queda preparado para probar endpoints con JWT.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "RIESGO_LAVADO API",
        Version     = "v1",
        Description = "API para el Sistema de Gestión de Riesgo de Lavado de Activos del IHSS"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Ingrese: Bearer {token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Proceso de seguridad: valida emisor, audiencia, vigencia y firma del token JWT en cada solicitud protegida.
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey   = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidAudience            = jwtSettings["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(secretKey),
            ClockSkew                = TimeSpan.Zero
        };
    });

// Proceso de integración frontend-backend: limita los orígenes permitidos según configuración.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("RLPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Proceso de infraestructura: registra la conexión Oracle usada por repositorios y servicios.
builder.Services.AddSingleton<OracleDbContext>(sp =>
    new OracleDbContext(builder.Configuration.GetConnectionString("OracleDB")!));

// Proceso de persistencia: registra repositorios responsables de acceso a tablas y consultas Oracle.
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IListasRepository, ListasRepository>();
builder.Services.AddScoped<IMatricesRiesgosRepository, MatricesRiesgosRepository>();

// Proceso de negocio: registra servicios que concentran validaciones, auditoría y reglas funcionales.
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IActivoDirectorioService, ActiveDirectorioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IListasService, ListasService>();
builder.Services.AddScoped<IEvidenciasService, EvidenciasService>();
builder.Services.AddScoped<ICoincidenciasService, CoincidenciasService>();
builder.Services.AddScoped<IMatricesRiesgoService, MatricesRiesgoService>();
builder.Services.AddScoped<IFormularioValidador, FormularioValidador>();
builder.Services.AddScoped<IMatricesRiesgosAppService, MatricesRiesgosAppService>();

// Proceso de contexto: permite obtener IP, usuario y datos de solicitud para auditoría.
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Proceso de ejecución HTTP: aplica manejo de errores, CORS, archivos estáticos, autenticación y autorización.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RIESGO_LAVADO API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("RLPolicy");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
