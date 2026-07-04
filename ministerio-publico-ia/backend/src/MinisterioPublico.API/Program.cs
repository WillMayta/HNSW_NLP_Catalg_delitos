using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinisterioPublico.Application.Interfaces;
using MinisterioPublico.Application.Services;
using MinisterioPublico.Domain.Entities;
using MinisterioPublico.Domain.Interfaces;
using MinisterioPublico.Infrastructure.ExternalServices;
using MinisterioPublico.Infrastructure.Persistence;
using MinisterioPublico.Infrastructure.Repositories;
using MinisterioPublico.API;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Persistencia ----------------
// Demo: SQLite (no requiere servidor). Producción: PostgreSQL + pgvector.
// Para cambiar a PostgreSQL:
//   1. Agregar paquete Npgsql.EntityFrameworkCore.PostgreSQL + Pgvector.EntityFrameworkCore
//   2. options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql"))
//   3. Ejecutar: CREATE EXTENSION IF NOT EXISTS vector; en la base de datos.
builder.Services.AddDbContext<MinisterioPublicoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")
        ?? "Data Source=ministerio_publico_ia.db"));

// ---------------- Repositorios (Repository Pattern) ----------------
builder.Services.AddScoped(typeof(IRepositorioGenerico<>), typeof(RepositorioGenerico<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDelitoOriginalRepository, DelitoOriginalRepository>();
builder.Services.AddScoped<IDelitoNormalizadoRepository, DelitoNormalizadoRepository>();
builder.Services.AddScoped<IDelitoCatalogoRepository, DelitoCatalogoRepository>();
builder.Services.AddScoped<IPropuestaAgrupamientoRepository, PropuestaAgrupamientoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRegistroAuditoriaRepository, RegistroAuditoriaRepository>();
builder.Services.AddScoped<IDashboardQueryRepository, DashboardQueryRepository>();

// ---------------- Cliente HTTP al microservicio de IA (Python) ----------------
var motorIaBaseUrl = builder.Configuration["MotorInteligente:BaseUrl"] ?? "http://localhost:8001";
builder.Services.AddHttpClient<IMotorInteligenteClient, MotorInteligenteHttpClient>(client =>
{
    client.BaseAddress = new Uri(motorIaBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // cargas masivas de miles de registros pueden tardar
});

// ---------------- Servicios de Aplicación ----------------
builder.Services.AddScoped<INormalizacionService, NormalizacionService>();
builder.Services.AddScoped<IBusquedaInteligenteService, BusquedaInteligenteService>();
builder.Services.AddScoped<IAgrupamientoService, AgrupamientoService>();
builder.Services.AddScoped<IValidacionJuridicaService, ValidacionJuridicaService>();
builder.Services.AddScoped<ICatalogoPenalService, CatalogoPenalService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();

// ---------------- Autenticación JWT ----------------
var claveSecreta = builder.Configuration["Jwt:ClaveSecreta"]
    ?? "CLAVE_DEMO_CAMBIAR_EN_PRODUCCION_MIN_32_CARACTERES_!!";
var emisor = builder.Configuration["Jwt:Emisor"] ?? "MinisterioPublicoIA";
var audiencia = builder.Configuration["Jwt:Audiencia"] ?? "MinisterioPublicoIA.Clientes";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = emisor,
        ValidAudience = audiencia,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta)),
    };
});
builder.Services.AddAuthorization();

// ---------------- CORS (para el frontend React) ----------------
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaFrontend", policy =>
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ---------------- Controladores + Swagger ----------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API - Sistema Inteligente Catálogo de Delitos MP Perú",
        Version = "v1",
        Description = "Normalización, agrupación semántica y consolidación del catálogo de delitos mediante búsqueda vectorial HNSW.",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese 'Bearer' seguido de un espacio y el token JWT.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "MinisterioPublico.API.xml");
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ---------------- Migraciones automáticas + datos semilla (solo demo) ----------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MinisterioPublicoDbContext>();
    db.Database.EnsureCreated(); // en producción real: usar Migrations (dotnet ef database update)
    await SeedData.EjecutarAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Catálogo de Delitos v1"));
}

app.UseHttpsRedirection();
app.UseCors("PoliticaFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
