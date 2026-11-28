using NexTalent.Identidad.Api.Middleware;
using NexTalent.Identidad.Infrastructure;
using NexTalent.Identidad.Infrastructure.Persistence;

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
    opciones.SwaggerDoc("v1", new() { Title = "NexTalent - Identidad", Version = "v1" });
});

var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(conexion))
{
    builder.Services.AddIdentidadInfrastructure(conexion);

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
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "NexTalent - Identidad v1"));

    if (!string.IsNullOrWhiteSpace(conexion) && app.Configuration.GetValue<bool>("DatabaseSeed:RunInitializers"))
    {
        using var alcance = app.Services.CreateScope();
        await IdentidadDbInitializer.EjecutarAsync(
            alcance.ServiceProvider.GetRequiredService<IdentidadDbContext>(), default);
    }
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();