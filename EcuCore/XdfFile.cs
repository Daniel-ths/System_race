// EcuCore/XdfFile.cs
using System.Collections.Generic;

/// <summary>
/// Representa o conteúdo completo de um arquivo XDF carregado.
/// </summary>
public class XdfFile
{
    public List<EcuMapDefinition> MapDefinitions { get; set; }
    public List<ChecksumDefinition> ChecksumDefinitions { get; set; }

    public XdfFile()
    {
        MapDefinitions = new List<EcuMapDefinition>();
        ChecksumDefinitions = new List<ChecksumDefinition>();
    }
}