// EcuCore/MapAxis.cs

using System.Collections.Generic;

/// <summary>
/// Representa a definição de um eixo (X ou Y) de um mapa.
/// </summary>
public class MapAxis
{
    /// <summary>
    /// Nome do eixo, ex: "RPM".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// O número de colunas (para eixo X) ou linhas (para eixo Y).
    /// </summary>
    public int Dimension { get; set; }

    /// <summary>
    /// Lista de valores do cabeçalho do eixo (ex: 500, 1000, 1500...).
    /// </summary>
    public List<double> Values { get; set; }

    /// <summary>
    /// A fórmula matemática para converter o valor lido do .BIN em um valor real.
    /// </summary>
    public string ConversionFormula { get; set; } = "X"; // Fórmula padrão
    
    public MapAxis()
    {
        Values = new List<double>();
    }

    // ... (propriedades existentes)
    
    /// <summary>
    /// O formato dos dados do eixo, ex: "8bit", "16bit_hi_lo".
    /// </summary>
    public string ValueFormat { get; set; } = string.Empty;

    /// <summary>
    /// O endereço de onde vêm os valores do eixo (se não forem fixos).
    /// </summary>
    public long SourceAddress { get; set; }

// ...
    /// <summary>
    /// A fórmula INVERSA para os dados do eixo (REAL -> RAW).
    /// </summary>
    public string ConversionInverseFormula { get; set; } = "X";


}
