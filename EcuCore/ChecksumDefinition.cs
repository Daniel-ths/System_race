// EcuCore/ChecksumDefinition.cs

/// <summary>
/// Define as propriedades de um bloco de checksum lido do XDF.
/// </summary>
public class ChecksumDefinition
{
    public string Title { get; set; } = "Checksum";
    
    /// <summary>
    /// O endereço onde o valor do checksum é *escrito* no .BIN.
    /// </summary>
    public long StorageAddress { get; set; }

    /// <summary>
    /// O tipo de algoritmo (ex: "SUM8", "SUM16").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// O endereço de memória onde o cálculo do checksum começa.
    /// </summary>
    public long StartAddress { get; set; }

    /// <summary>
    /// O endereço de memória onde o cálculo do checksum termina.
    /// </summary>
    public long EndAddress { get; set; }
}