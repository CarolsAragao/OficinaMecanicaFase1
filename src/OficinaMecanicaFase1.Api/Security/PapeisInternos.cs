namespace OficinaMecanicaFase1.Api.Security;

public static class PapeisInternos
{
    public const string Administrador = "Administrador";
    public const string Atendente = "Atendente";
    public const string Mecanico = "Mecânico";
    public const string Estoquista = "Estoquista";

    public static readonly string[] Todos = [Administrador, Atendente, Mecanico, Estoquista];
}
