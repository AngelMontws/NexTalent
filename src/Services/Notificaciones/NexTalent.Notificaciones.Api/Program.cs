using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NexTalent.Notificaciones.Api.Middleware;
using NexTalent.Notificaciones.Infrastructure;
using NexTalent.Notificaciones.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(opciones =>
{
    opciones.IncludeScopes = true;
    opciones.SingleLine = true;
    opciones.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new() { Title = "NexTalent - Notificaciones", Version = "v1" });
});

if (builder.Configuration["Jwt:Key"] is { Length: >= 32 } jwtKey)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.MapInboundClaims = false;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NexTalent.Identidad",
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NexTalent.Api",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = "role"
            };
        });
}
else
{
    builder.Services.AddAuthentication();
}
builder.Services.AddAuthorization();

var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
var usarHangfire = !string.IsNullOrWhiteSpace(conexion)
    && builder.Configuration.GetValue("Hangfire:Enabled", true);

if (!string.IsNullOrWhiteSpace(conexion))
{
    builder.Services.AgregarPersistencia(conexion, builder.Configuration);

    if (usarHangfire)
    {
        builder.Services.AgregarHangfire(conexion);
    }

    if (builder.Configuration["RabbitMq:Host"] is { Length: > 0 } rabbit)
    {
        builder.Services.AgregarMensajeria(
            rabbit,
            builder.Configuration["RabbitMq:Username"] ?? "",
            builder.Configuration["RabbitMq:Password"] ?? "");
    }
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opciones =>
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "NexTalent - Notificaciones v1"));

    if (!string.IsNullOrWhiteSpace(conexion) && app.Configuration.GetValue<bool>("DatabaseSeed:RunInitializers"))
    {
        using var alcance = app.Services.CreateScope();
        await NotificacionesDbInitializer.EjecutarAsync(
            alcance.ServiceProvider.GetRequiredService<NotificacionesDbContext>(),
            TimeProvider.System,
            default);
    }
}

if (usarHangfire)
{
    using var alcance = app.Services.CreateScope();
    DependencyInjection.ProgramarTareasRecurrentes(alcance.ServiceProvider.GetRequiredService<IRecurringJobManager>());
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
