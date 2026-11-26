using NexTalent.Postulaciones.Api;
using NexTalent.Postulaciones.Api.Middleware;
using NexTalent.Postulaciones.Infrastructure;
using NexTalent.Postulaciones.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(opciones =>
{
    opciones.IncludeScopes = true;
    opciones.SingleLine = true;
    opciones.TimestampFormat = "HH:mm:ss ";
});

// Estados como texto ("EnRevision"), igual que en la base y en los eventos.
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejoErrores>();
builder.Services.AgregarJwt(builder.Configuration);
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new() { Title = "NexTalent - Postulaciones", Version = "v1" });
});

// Sin cadena de conexion el servicio arranca igual y los endpoints responden 503.
var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(conexion))
{
    builder.Services.AgregarPersistencia(conexion);

    // Sin RabbitMQ configurado, los eventos solo se registran en el log y no se consumen.
    if (builder.Configuration["RabbitMq:Host"] is { Length: > 0 } rabbit)
    {
        builder.Services.AgregarMensajeria(
            rabbit, builder.Configuration["RabbitMq:Usuario"] ?? "", builder.Configuration["RabbitMq:Contrasena"] ?? "");
    }
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opciones =>
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "NexTalent - Postulaciones v1"));

    if (conexion is not null && app.Configuration.GetValue<bool>("DatabaseSeed:RunInitializers"))
    {
        using var alcance = app.Services.CreateScope();
        await PostulacionesDbInitializer.EjecutarAsync(
            alcance.ServiceProvider.GetRequiredService<PostulacionesDbContext>(), TimeProvider.System, default);
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
