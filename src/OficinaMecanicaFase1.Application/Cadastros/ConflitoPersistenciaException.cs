namespace OficinaMecanicaFase1.Application.Cadastros;

public sealed class ConflitoPersistenciaException : Exception
{
    public ConflitoPersistenciaException(Exception innerException)
        : base("O cadastro conflita com outro registro ou está em uso.", innerException) { }
}
