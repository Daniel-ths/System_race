// EcuCore/XdfParser.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class XdfParser
{
    // Nota: Esta classe está ficando grande. Em um projeto real,
    // poderíamos dividi-la, mas por enquanto está OK.

    /// <summary>
    /// Lê um arquivo .XDF e retorna um objeto XdfFile contendo mapas e checksums.
    /// </summary>
    public XdfFile Parse(string filePath)
    {
        var xdfFile = new XdfFile(); 

        XDocument xdoc = XDocument.Load(filePath);
        XNamespace ns = xdoc.Root?.GetDefaultNamespace() ?? string.Empty;

        // 1. Encontrar todas as tabelas (MAPAS)
        List<XElement> tables = xdoc.Descendants(ns + "XDFTABLE").ToList();
        foreach (var tableElement in tables)
        {
            var mapDef = new EcuMapDefinition();

            mapDef.Title = tableElement.Element(ns + "title")?.Value ?? "Mapa sem Título";

            var addressElement = tableElement.Descendants(ns + "address").FirstOrDefault();
            if (addressElement != null)
            {
                mapDef.Address = ParseHex(addressElement.Value);
            }

            var zAxisElement = tableElement.Element(ns + "zaxis");
            if (zAxisElement != null)
            {
                mapDef.ValueFormat = zAxisElement.Descendants(ns + "value").FirstOrDefault()
                                        ?.Attribute("type")?.Value ?? "8bit";
                mapDef.ValueConversionFormula = zAxisElement.Descendants(ns + "equation").FirstOrDefault()
                                        ?.Value ?? "X";
                mapDef.ValueConversionInverseFormula = zAxisElement.Descendants(ns + "inverse").FirstOrDefault()
                                        ?.Value ?? "X";
            }
            
            mapDef.XAxis = ParseAxis(tableElement.Element(ns + "XAXIS"), ns);
            mapDef.YAxis = ParseAxis(tableElement.Element(ns + "YAXIS"), ns);

            xdfFile.MapDefinitions.Add(mapDef);
        }

        // 2. Encontrar todos os CHECKSUMS
        List<XElement> checksums = xdoc.Descendants(ns + "XDFCHECKSUM").ToList();
        foreach (var checksumElement in checksums)
        {
            xdfFile.ChecksumDefinitions.Add(ParseChecksum(checksumElement, ns));
        }
        
        return xdfFile;
    }

    // ---- Este método deve estar DENTRO da classe XdfParser ----
    private ChecksumDefinition ParseChecksum(XElement checksumElement, XNamespace ns)
    {
        var def = new ChecksumDefinition();
        
        def.Title = checksumElement.Element(ns + "title")?.Value ?? "Checksum";
        def.Type = checksumElement.Element(ns + "type")?.Value ?? string.Empty;
        
        def.StorageAddress = ParseHex(checksumElement.Element(ns + "address")?.Value);
        def.StartAddress = ParseHex(checksumElement.Element(ns + "startaddress")?.Value);
        def.EndAddress = ParseHex(checksumElement.Element(ns + "endaddress")?.Value);

        return def;
    }

    // ---- Este método deve estar DENTRO da classe XdfParser ----
    private MapAxis? ParseAxis(XElement? axisElement, XNamespace ns)
    {
        if (axisElement == null)
            return null;
        var mapAxis = new MapAxis();
        string dim = axisElement.Attribute("dim")?.Value ?? "1";
        mapAxis.Dimension = int.Parse(dim);
        mapAxis.Name = axisElement.Descendants(ns + "name").FirstOrDefault()?.Value ?? "Eixo";
        mapAxis.ConversionFormula = axisElement.Descendants(ns + "equation").FirstOrDefault()
                                        ?.Value ?? "X";
        mapAxis.ConversionInverseFormula = axisElement.Descendants(ns + "inverse").FirstOrDefault()
                                        ?.Value ?? "X";
        var embeddedSource = axisElement.Descendants(ns + "embeddedsource").FirstOrDefault();
        if (embeddedSource != null)
        {
            mapAxis.ValueFormat = embeddedSource.Descendants(ns + "value").FirstOrDefault()
                                        ?.Attribute("type")?.Value ?? "8bit";
            var addressElement = embeddedSource.Descendants(ns + "address").FirstOrDefault();
            if (addressElement != null)
            {
                mapAxis.SourceAddress = ParseHex(addressElement.Value);
            }
        }
        return mapAxis;
    }

    // ---- Este método deve estar DENTRO da classe XdfParser ----
    private long ParseHex(string? hexString)
    {
        if (string.IsNullOrWhiteSpace(hexString))
        {
            return 0;
        }

        if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hexString = hexString.Substring(2);
        }
        
        if (string.IsNullOrWhiteSpace(hexString))
        {
            return 0;
        }

        return long.Parse(hexString, NumberStyles.HexNumber);
    }
    
} // <-- ESTA CHAVE '}' DEVE SER A ÚLTIMA COISA NO ARQUIVO