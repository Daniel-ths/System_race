// EcuTester/Program.cs

using System;
using System.IO;
using System.Linq;

Console.WriteLine("Iniciando o Testador Completo (Parser + Loader v3 - Gerador de BIN)");

// --- 1. GERAR UM ARQUIVO .BIN FALSO ---
byte[] fakeBinFile = new byte[24600]; // Tamanho ligeiramente maior
Console.WriteLine($"Arquivo .BIN falso gerado com {fakeBinFile.Length} bytes.");

long addrXAxis = 0x3F00;
int dimX = 8;
ushort[] rpmValues = { 50, 100, 150, 200, 250, 300, 350, 400 };
for (int i = 0; i < dimX; i++)
{
    fakeBinFile[addrXAxis + (i * 2)]     = (byte)(rpmValues[i] >> 8);   
    fakeBinFile[addrXAxis + (i * 2) + 1] = (byte)(rpmValues[i] & 0xFF); 
}
Console.WriteLine($"  -> {dimX*2} bytes de dados do Eixo X escritos em 0x{addrXAxis:X}");

long addrZAxis = 0x4000;
int dimY = 10;
byte valor = 0;
for (int y = 0; y < dimY; y++)
{
    for (int x = 0; x < dimX; x++)
    {
        fakeBinFile[addrZAxis] = valor;
        valor++;
        addrZAxis++;
    }
}
Console.WriteLine($"  -> {dimX * dimY} bytes de dados da Tabela Z escritos em 0x4000");

// --- CORREÇÃO IMPORTANTE DO CHECKSUM ---
// Nosso XDF de teste espera que o checksum esteja em 0x5FFF.
// Vamos colocar um valor 'falso' lá para o EcuApp ler.
// (O valor real não importa, pois o EcuTester não está testando a gravação)
fakeBinFile[0x5FFF] = 0xAA; // Escreve um byte de teste no local do checksum
Console.WriteLine("  -> Byte de checksum falso escrito em 0x5FFF");


// ----- SALVA O ARQUIVO NO DISCO -----
File.WriteAllBytes("teste.bin", fakeBinFile); 
Console.WriteLine($"  -> ARQUIVO 'teste.bin' SALVO NO DISCO!");


// --- 2. CARREGAR E ANALISAR O ARQUIVO XDF (TESTE DE LEITURA) ---
string xdfFilePath = "teste.xdf";
if (!File.Exists(xdfFilePath))
{
    Console.WriteLine($"Erro: Arquivo XDF não encontrado: {xdfFilePath}");
    return;
}

var parser = new XdfParser();

// ---- MUDANÇA DA CORREÇÃO AQUI ----
// Antes: var mapDefinitions = parser.Parse(xdfFilePath);
XdfFile xdfFile = parser.Parse(xdfFilePath); // Agora retorna um XdfFile
var mapDefinitions = xdfFile.MapDefinitions; // Pegamos a lista de dentro dele
// ---- FIM DA CORREÇÃO ----

var ignMapDef = mapDefinitions.FirstOrDefault(m => m.Title.Contains("Ignicao")); // Esta linha agora funciona

if (ignMapDef == null)
{
    Console.WriteLine("Erro: O 'Tabela de Ignicao Principal' não foi encontrado no XDF.");
    return;
}

Console.WriteLine($"\nParser XDF carregou: '{ignMapDef.Title}'");
Console.WriteLine($"Parser XDF encontrou {xdfFile.ChecksumDefinitions.Count} checksum(s).");


// --- 3. USAR O BINLOADER PARA LER OS DADOS ---
var loader = new BinDataLoader();

try
{
    Console.WriteLine("Invocando o BinDataLoader (com matemática)...");
    EcuMap ignMap = loader.LoadMapData(ignMapDef, fakeBinFile);
    Console.WriteLine("Dados do mapa carregados e CONVERTIDOS com sucesso!");

    // ... (o resto do código de verificação) ...
    Console.WriteLine("\n--- VERIFICANDO DADOS CONVERTIDOS ---");
    Console.WriteLine("Valores do Eixo X (RPM):");
    foreach (var val in ignMap.XAxisValues) { Console.Write($" {val} |"); }
    Console.WriteLine();
    Console.WriteLine("\nValores da Tabela Z (primeira linha):");
    for (int x = 0; x < dimX; x++) { Console.Write($" {ignMap.ZValues[0, x]} |"); }
    Console.WriteLine();
    Console.WriteLine("\nValores da Tabela Z (segunda linha):");
    for (int x = 0; x < dimX; x++) { Console.Write($" {ignMap.ZValues[1, x]} |"); }
    Console.WriteLine("\n\nTeste concluído.");
}
catch (Exception ex)
{
    Console.WriteLine($"\nOcorreu um erro CRÍTICO durante o carregamento de dados: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}