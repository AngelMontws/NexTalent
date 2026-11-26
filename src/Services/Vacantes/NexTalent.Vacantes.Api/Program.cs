using NexTalent.Vacantes.Api.Middleware;
using NexTalent.Vacantes.Infrastructure;
using NexTalent.Vacantes.Infrastructure.Persistence;

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
    opciones.SwaggerDoc("v1", new() { Title = "NexTalent - Vacantes", Version = "v1" });
});

var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(conexion))
{
    builder.Services.AddVacantesInfrastructure(conexion);

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
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "NexTalent - Vacantes v1"));

    if (!string.IsNullOrWhiteSpace(conexion) && app.Configuration.GetValue<bool>("DatabaseSeed:RunInitializers"))
    {
        using var alcance = app.Services.CreateScope();
        var seed50k = app.Configuration.GetValue<bool>("DatabaseSeed:SeedBenchmark50k");
        await VacantesDbInitializer.EjecutarAsync(
            alcance.ServiceProvider.GetRequiredService<VacantesDbContext>(),
            seed50k,
            default);
    }
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
