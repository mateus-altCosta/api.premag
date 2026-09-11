using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Premag.API.Json;
using Premag.API.Middleware;
using Premag.Application.Interfaces.Services;
using Premag.Application.Services;
using Premag.Core;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;
using Premag.Infrastructure.Data.Seed;
using Premag.Infrastructure.Tenancy;
using Premag.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

var porta = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(porta))
    builder.WebHost.UseUrls($"http://0.0.0.0:{porta}");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
        options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableTimeOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PREMAG API",
        Version = "v1",
        Description = "API de apropriação de mão de obra"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT. Exemplo: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

var origens = (builder.Configuration["Cors:Origins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("pwa", policy =>
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                if (origens.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
                try
                {
                    var host = new Uri(origin).Host;
                    return host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            })
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
        .UseSnakeCaseNamingConvention());

builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();
builder.Services.AddSingleton<ITenantContext, TenantContext>();
builder.Services.AddSingleton<IRelogio, RelogioSistema>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IEquipeService, EquipeService>();
builder.Services.AddScoped<IObraService, ObraService>();
builder.Services.AddScoped<IApontamentoService, ApontamentoService>();
builder.Services.AddScoped<IProducaoService, ProducaoService>();
builder.Services.AddScoped<ISincronizacaoService, SincronizacaoService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IFotoService, FotoService>();
builder.Services.AddScoped<IDiarioService, DiarioService>();
builder.Services.AddScoped<IOcorrenciaService, OcorrenciaService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddScoped<IImportacaoService, ImportacaoService>();
builder.Services.AddScoped<IFechamentoService, FechamentoService>();
builder.Services.AddScoped<IPushService, PushService>();
builder.Services.AddHostedService<Premag.API.Jobs.AlertaHostedService>();

var storageProvider = builder.Configuration["Storage:Provider"];
if (string.Equals(storageProvider, "Supabase", StringComparison.OrdinalIgnoreCase)
    && !string.IsNullOrWhiteSpace(builder.Configuration["Storage:Supabase:Url"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Storage:Supabase:Key"]))
    builder.Services.AddSingleton<IArquivoStorage, Premag.Infrastructure.Storage.SupabaseArquivoStorage>();
else
    builder.Services.AddSingleton<IArquivoStorage, Premag.Infrastructure.Storage.LocalArquivoStorage>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 5_000_000;
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key precisa ter pelo menos 32 caracteres.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Gerente", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Permissoes.Gerente)
            || ctx.User.IsInRole(Permissoes.Diretoria)
            || ctx.User.IsInRole(Permissoes.Admin)));
    options.AddPolicy("Diretoria", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(Permissoes.Diretoria)
            || ctx.User.IsInRole(Permissoes.Admin)));
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
    await db.Database.MigrateAsync();

    var senha = app.Configuration["Seed:AdminPassword"];
    if (string.IsNullOrWhiteSpace(senha))
        senha = SeedData.SenhaPadraoDesenvolvimento;
    var demo = app.Configuration.GetValue("Seed:IncluirDemoFabrica", app.Environment.IsDevelopment());
    await SeedData.AplicarAsync(db, tenant, senha, incluirDemoFabrica: demo);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("pwa");
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
