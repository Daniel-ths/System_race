// EcuCore/MathEvaluator.cs

using System;
using System.Data; // Importante: Esta é a biblioteca que faz a mágica

/// <summary>
/// Avalia expressões matemáticas simples (lidas do XDF).
/// </summary>
public static class MathEvaluator
{
    // Objeto estático para evitar recriá-lo o tempo todo
    private static readonly DataTable _table = new DataTable();

    /// <summary>
    /// Avalia uma expressão, substituindo 'X' pelo valor fornecido.
    /// </summary>
    /// <param name="expression">A fórmula (ex: "X * 0.5 - 10")</param>
    /// <param name="xValue">O valor para substituir 'X'</param>
    /// <returns>O resultado do cálculo</returns>
    public static double Evaluate(string expression, double xValue)
    {
        try
        {
            // 1. Substitui 'X' (maiúsculo) pelo valor.
            //    Usamos .ToString("G17") para garantir a precisão decimal.
            string formattedExpression = expression
                .Replace("X", xValue.ToString("G17", System.Globalization.CultureInfo.InvariantCulture));

            // 2. A "mágica": O método Compute do DataTable avalia a string.
            //    Ex: "50 * 10" vira 500.
            //    Ex: "8 * 0.5 - 10" vira -6.
            var result = _table.Compute(formattedExpression, string.Empty);

            return Convert.ToDouble(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao avaliar expressão: '{expression}' com valor {xValue}. Detalhe: {ex.Message}");
            // Se a fórmula falhar (ex: "X / 0"), retorna o valor original.
            return xValue;
        }
    }
}