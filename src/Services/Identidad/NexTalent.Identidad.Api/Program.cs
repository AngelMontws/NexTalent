using NexTalent.Identidad.Api.Middleware;

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

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opciones =>
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "NexTalent - Identidad v1"));
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();