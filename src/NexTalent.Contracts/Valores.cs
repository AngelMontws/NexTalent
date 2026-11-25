namespace NexTalent.Contracts;

// Textos estables que viajan en los eventos: no cambiarlos sin acuerdo de ambos backends.

public static class Estados
{
    public const string Enviada = "Enviada";
    public const string Vista = "Vista";
    public const string EnRevision = "EnRevision";
    public const string Entrevista = "Entrevista";
    public const string OfertaContratado = "OfertaContratado";
    public const string NoSeleccionado = "NoSeleccionado";
}

public static class CausasCambio
{
    public const string PerfilAbierto = "PerfilAbierto";
    public const string Avance = "Avance";
    public const string Descarte = "Descarte";
    public const string VacanteCerrada = "VacanteCerrada";
    public const string Reactivacion = "Reactivacion";
}

public static class CausasCierre
{
    public const string Manual = "Manual";
    public const string Caducidad = "Caducidad";
    public const string Moderacion = "Moderacion";
}

public static class Niveles
{
    public const string Basico = "Basico";
    public const string Intermedio = "Intermedio";
    public const string Avanzado = "Avanzado";
}

public static class TiposSalario
{
    public const string Bruto = "Bruto";
    public const string Neto = "Neto";
}
