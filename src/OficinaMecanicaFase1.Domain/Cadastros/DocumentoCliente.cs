namespace OficinaMecanicaFase1.Domain.Cadastros;

public static class DocumentoCliente
{
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var digitos = new string(valor.Where(c => c is >= '0' and <= '9').ToArray());
        if (valor.Any(c => c is not (>= '0' and <= '9') and not ('.' or '/' or '-' or ' '))) return null;
        if (digitos.Length is not (11 or 14) || digitos.Distinct().Count() == 1) return null;
        return digitos.Length == 11 ? (CpfValido(digitos) ? digitos : null) : (CnpjValido(digitos) ? digitos : null);
    }

    private static bool CpfValido(string cpf)
    {
        var primeiro = Digito(cpf, 9, 10);
        var segundo = Digito(cpf, 10, 11);
        return primeiro == cpf[9] - '0' && segundo == cpf[10] - '0';
    }

    private static int Digito(string cpf, int tamanho, int pesoInicial)
    {
        var soma = 0;
        for (var i = 0; i < tamanho; i++) soma += (cpf[i] - '0') * (pesoInicial - i);
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static bool CnpjValido(string cnpj)
    {
        int[] pesosPrimeiro = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesosSegundo = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        return DigitoCnpj(cnpj, pesosPrimeiro) == cnpj[12] - '0' &&
               DigitoCnpj(cnpj, pesosSegundo) == cnpj[13] - '0';
    }

    private static int DigitoCnpj(string cnpj, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < pesos.Length; i++) soma += (cnpj[i] - '0') * pesos[i];
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
