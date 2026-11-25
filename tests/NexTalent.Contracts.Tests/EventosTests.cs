using System.Text.Json;

namespace NexTalent.Contracts.Tests;

public class EventosTests
{
    private static readonly DateTimeOffset Fecha = new(2026, 11, 25, 16, 0, 0, TimeSpan.Zero);

    public static TheoryData<object> Eventos() => new()
    {
        new EmpresaVerificada(Id(), Fecha, Id(), "Empresa", true, 1),
        new PerfilHabilidadesActualizado(Id(), Fecha, Id(), [new(Id(), Niveles.Basico)], true, false, 2),
        new VacantePublicada(Id(), Fecha, Id(), Id(), "Empresa", "Puesto", null, 10000.50m, 12000m,
            TiposSalario.Bruto, Fecha, Fecha.AddDays(30), [new(Id(), Niveles.Avanzado, true)], 3),
        new VacantePublicada(Id(), Fecha, Id(), Id(), "Empresa", "Puesto", Id(), 1m, 2m,
            TiposSalario.Neto, Fecha, Fecha.AddDays(30), [], 4),
        new VacanteCerrada(Id(), Fecha, Id(), Id(), "Puesto", CausasCierre.Caducidad, Fecha, 5),
        new PostulacionCreada(Id(), Fecha, Id(), "F-1", Id(), Id(), Id(), "Diseño", "Empresa", Fecha),
        new EstatusCambiado(Id(), Fecha, Id(), "F-1", Id(), Id(), Id(), "Diseño", "Empresa",
            Estados.Enviada, Estados.Vista, CausasCambio.PerfilAbierto, Fecha, 2)
    };

    [Theory]
    [MemberData(nameof(Eventos), DisableDiscoveryEnumeration = true)]
    public void Evento_SeSerializaYDeserializaSinPerderDatos(object evento)
    {
        var json = JsonSerializer.Serialize(evento, evento.GetType(), JsonSerializerOptions.Web);
        var copia = JsonSerializer.Deserialize(json, evento.GetType(), JsonSerializerOptions.Web);

        // Los record comparan sus listas por referencia: se compara propiedad por propiedad.
        foreach (var propiedad in evento.GetType().GetProperties())
        {
            Assert.Equal(propiedad.GetValue(evento), propiedad.GetValue(copia));
        }
    }

    private static Guid Id() => Guid.NewGuid();
}
