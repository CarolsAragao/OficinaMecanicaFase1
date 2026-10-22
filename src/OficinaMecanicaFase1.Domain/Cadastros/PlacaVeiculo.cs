namespace OficinaMecanicaFase1.Domain.Cadastros;

public static class PlacaVeiculo
{
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var placa = valor.Trim().ToUpperInvariant().Replace("-", "");
        if (placa.Length != 7 || placa.Take(3).Any(c => c is < 'A' or > 'Z') ||
            placa[3] is < '0' or > '9' || placa[5] is < '0' or > '9' || placa[6] is < '0' or > '9') return null;
        return placa[4] is >= '0' and <= '9' or >= 'A' and <= 'Z' ? placa : null;
    }
}
