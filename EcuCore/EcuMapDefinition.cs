// EcuCore/EcuMapDefinition.cs

/// <summary>
/// Representa a definição de um mapa (tabela) lido de um arquivo XDF.
/// </summary>
public class EcuMapDefinition
{
    /// <summary>
    /// O nome do mapa, como "Tabela de Ignição Carga Parcial".
    /// </summary>
    public string Title { get; set; } = string.Empty; // Valor padrão

    /// <summary>
    /// O endereço de memória (em hexadecimal) onde os dados deste mapa começam no arquivo .BIN.
    /// </summary>
    public long Address { get; set; }

    /// <summary>
    /// Descrição do eixo X (ex: "RPM").
    /// </summary>
    public MapAxis? XAxis { get; set; } // O '?' permite que seja nulo (para mapas 1D)

    /// <summary>
    /// Descrição do eixo Y (ex: "Carga do Motor").
    /// </summary>
    public MapAxis? YAxis { get; set; } // O '?' permite que seja nulo (para mapas 1D/2D)

    // ... (propriedades existentes)

    /// <summary>
    /// O formato dos dados Z (da tabela), ex: "8bit", "16bit_hi_lo".
    /// </summary>
    public string ValueFormat { get; set; } = string.Empty;

    /// <summary>
    /// A fórmula de conversão para os dados Z (da tabela).
    /// </summary>
    public string ValueConversionFormula { get; set; } = "X"; // Padrão

    // ...
    /// <summary>
    /// A fórmula INVERSA para os dados Z (REAL -> RAW).
    /// </summary>
    public string ValueConversionInverseFormula { get; set; } = "X";
}
