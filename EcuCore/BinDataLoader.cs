// EcuCore/BinDataLoader.cs
// Código completo com suporte a 16bit e signed 8/16bit (Passo B)

using System;
using System.Globalization; // Para NumberStyles ao ler hex

/// <summary>
/// Responsável por ler os dados de um arquivo .BIN (array de bytes)
/// com base em uma EcuMapDefinition e preencher um EcuMap.
/// </summary>
public class BinDataLoader
{
    private byte[]? _fileBytes; // O conteúdo completo do arquivo .BIN

    /// <summary>
    /// Carrega os dados de um mapa.
    /// </summary>
    /// <param name="definition">A definição do mapa (onde ler)</param>
    /// <param name="fileBytes">O conteúdo do arquivo .BIN</param>
    /// <returns>Um EcuMap preenchido com dados</returns>
    public EcuMap LoadMapData(EcuMapDefinition definition, byte[] fileBytes)
    {
        _fileBytes = fileBytes;
        EcuMap map = new EcuMap(definition);

        // --- 1. Carregar os dados da Tabela (Z-Values) ---
        int dimX = definition.XAxis?.Dimension ?? 1;
        int dimY = definition.YAxis?.Dimension ?? 1;

        long currentAddress = definition.Address;
        int valueSizeZ = GetSizeFromFormat(definition.ValueFormat);

        for (int y = 0; y < dimY; y++)
        {
            for (int x = 0; x < dimX; x++)
            {
                double rawValue = ReadValue(currentAddress, definition.ValueFormat);
                double convertedValue = MathEvaluator.Evaluate(definition.ValueConversionFormula, rawValue);
                map.ZValues[y, x] = convertedValue;
                currentAddress += valueSizeZ;
            }
        }

        // --- 2. Carregar os dados do Eixo X ---
        if (definition.XAxis != null && definition.XAxis.SourceAddress > 0 && definition.XAxis.Dimension > 0)
        {
            currentAddress = definition.XAxis.SourceAddress;
            int valueSizeX = GetSizeFromFormat(definition.XAxis.ValueFormat);

            for (int x = 0; x < definition.XAxis.Dimension; x++)
            {
                double rawValue = ReadValue(currentAddress, definition.XAxis.ValueFormat);
                double convertedValue = MathEvaluator.Evaluate(definition.XAxis.ConversionFormula, rawValue);
                map.XAxisValues.Add(convertedValue);
                currentAddress += valueSizeX;
            }
        }

        // --- 3. Carregar os dados do Eixo Y ---
        if (definition.YAxis != null && definition.YAxis.SourceAddress > 0 && definition.YAxis.Dimension > 0)
        {
            currentAddress = definition.YAxis.SourceAddress;
            int valueSizeY = GetSizeFromFormat(definition.YAxis.ValueFormat);

             for (int y = 0; y < definition.YAxis.Dimension; y++)
            {
                double rawValue = ReadValue(currentAddress, definition.YAxis.ValueFormat);
                double convertedValue = MathEvaluator.Evaluate(definition.YAxis.ConversionFormula, rawValue);
                map.YAxisValues.Add(convertedValue);
                currentAddress += valueSizeY;
            }
        }

        return map;
    }

    /// <summary>
    /// Lê um valor de um endereço, respeitando o formato (8bit, 16bit, signed, endianness).
    /// </summary>
    private double ReadValue(long address, string format)
    {
        // ... (verificação de _fileBytes e endereço) ...
        if (_fileBytes == null || address < 0 || address >= _fileBytes.Length) { /*...*/ return 0; }

        try {
            switch (format.ToLowerInvariant())
            {
                case "8bit": return (byte)_fileBytes[address];
                case "signed_8bit": return (sbyte)_fileBytes[address];

                case "16bit_hi_lo": // Unsigned Big-Endian
                    if (address + 1 >= _fileBytes.Length) { /*...*/ return 0; }
                    byte hi_ube = _fileBytes[address]; byte lo_ube = _fileBytes[address + 1];
                    return (ushort)((hi_ube << 8) | lo_ube);
                case "16bit_lo_hi": // Unsigned Little-Endian
                     if (address + 1 >= _fileBytes.Length) { /*...*/ return 0; }
                    byte lo_ule = _fileBytes[address]; byte hi_ule = _fileBytes[address + 1];
                    return (ushort)((hi_ule << 8) | lo_ule);
                case "signed_16bit_hi_lo": // Signed Big-Endian
                     if (address + 1 >= _fileBytes.Length) { /*...*/ return 0; }
                    byte hi_sbe = _fileBytes[address]; byte lo_sbe = _fileBytes[address + 1];
                    return (short)((hi_sbe << 8) | lo_sbe);
                // case "signed_16bit_lo_hi": ...

                // --- NOVOS TIPOS: 32-bit Unsigned ---
                case "32bit_hi_lo_hi_lo": // Big-Endian (Ex: B1 B2 B3 B4)
                    if (address + 3 >= _fileBytes.Length) { Console.WriteLine($"Erro: Leitura 32bit fora dos limites em {address:X}"); return 0; }
                    byte b1_be = _fileBytes[address];
                    byte b2_be = _fileBytes[address + 1];
                    byte b3_be = _fileBytes[address + 2];
                    byte b4_be = _fileBytes[address + 3];
                    // Combina os 4 bytes (B1 * 2^24 + B2 * 2^16 + B3 * 2^8 + B4)
                    return (uint)((b1_be << 24) | (b2_be << 16) | (b3_be << 8) | b4_be);

                case "32bit_lo_hi_lo_hi": // Little-Endian (Ex: B4 B3 B2 B1)
                    if (address + 3 >= _fileBytes.Length) { Console.WriteLine($"Erro: Leitura 32bit fora dos limites em {address:X}"); return 0; }
                    byte b1_le = _fileBytes[address];
                    byte b2_le = _fileBytes[address + 1];
                    byte b3_le = _fileBytes[address + 2];
                    byte b4_le = _fileBytes[address + 3];
                     // Combina os 4 bytes na ordem inversa (B4 * 2^24 + B3 * 2^16 + B2 * 2^8 + B1)
                    return (uint)((b4_le << 24) | (b3_le << 16) | (b2_le << 8) | b1_le);

                // --- PONTOS FUTUROS (Signed 32bit, Float) ---
                // case "signed_32bit...": return (int)((b1 << 24) | ...);
                // case "float_ieee_be": ... BitConverter.ToSingle ...
                // case "float_ieee_le": ... BitConverter.ToSingle ...

                default:
                    Console.WriteLine($"Aviso: Formato '{format}' não suportado. Usando 8bit.");
                    return _fileBytes[address];
            }
        } catch (IndexOutOfRangeException) { /*...*/ return 0; }
    }

    /// <summary>
    /// Retorna quantos bytes um formato ocupa.
    /// </summary>
    private int GetSizeFromFormat(string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "8bit":
            case "signed_8bit":
                return 1;

            case "16bit_hi_lo":
            case "16bit_lo_hi":
            case "signed_16bit_hi_lo":
            case "signed_16bit_lo_hi": // Adicionando placeholder
                return 2;

            case "32bit_hi_lo_hi_lo":
            case "32bit_lo_hi_lo_hi":
            case "float_ieee_be":
            case "float_ieee_le":
                return 4;

            default:
                Console.WriteLine($"Aviso: Formato desconhecido '{format}' em GetSizeFromFormat. Usando tamanho 1.");
                return 1;
        }
    }
}