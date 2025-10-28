// EcuCore/EcuMap.cs

using System.Collections.Generic;

/// <summary>
/// Representa um mapa carregado na memória, contendo os dados reais
/// lidos do arquivo .BIN e convertidos.
/// </summary>
public class EcuMap
{
    /// <summary>
    /// A definição (o "como fazer") deste mapa.
    /// </summary>
    public EcuMapDefinition Definition { get; }

    /// <summary>
    /// Os valores do cabeçalho do Eixo X (ex: 500.0, 1000.0, 1500.0).
    /// </summary>
    public List<double> XAxisValues { get; set; }

    /// <summary>
    /// Os valores do cabeçalho do Eixo Y (ex: 80.0, 90.0, 100.0).
    /// </summary>
    public List<double> YAxisValues { get; set; }

    /// <summary>
    /// Os valores principais da tabela (Z-Axis), já convertidos.
    /// Este é um array 2D [linha, coluna].
    /// </summary>
    public double[,] ZValues { get; set; }

    /// <summary>
    /// Construtor: Cria um EcuMap com base em sua definição.
    /// </summary>
    public EcuMap(EcuMapDefinition definition)
    {
        Definition = definition;
        
        // Pega as dimensões da definição
        int dimX = definition.XAxis?.Dimension ?? 1;
        int dimY = definition.YAxis?.Dimension ?? 1;

        // Inicializa as listas e o array com o tamanho correto
        XAxisValues = new List<double>(dimX);
        YAxisValues = new List<double>(dimY);
        ZValues = new double[dimY, dimX]; // Importante: a ordem é [Linhas, Colunas]
    }
}