namespace NexTalent.Identidad.Domain.Tests;

public class RolesSistemaTests
{
    [Theory]
    [InlineData("candidato")]
    [InlineData("reclutador")]
    [InlineData("admin")]
    [InlineData("CANDIDATO")]
    public void EsValido_RolConocido_DevuelveTrue(string rol)
    {
        Assert.True(RolesSistema.EsValido(rol));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("superusuario")]
    public void EsValido_RolDesconocidoOVacio_DevuelveFalse(string? rol)
    {
        Assert.False(RolesSistema.EsValido(rol));
    }

    [Theory]
    [InlineData("candidato", true)]
    [InlineData("reclutador", true)]
    [InlineData("admin", false)]
    [InlineData("superusuario", false)]
    [InlineData(null, false)]
    public void PermiteRegistroPublico_SoloCandidatoYReclutador(string? rol, bool esperado)
    {
        Assert.Equal(esperado, RolesSistema.PermiteRegistroPublico(rol));
    }
}