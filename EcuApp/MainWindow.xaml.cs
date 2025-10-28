// EcuApp/MainWindow.xaml.cs
// Código COMPLETO (Corrigido CS1501, CS0103 e todas as funcionalidades)

using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Data;
using System;
using System.Globalization;
using ScottPlot;
using System.Windows.Input; // Para KeyEventArgs

namespace EcuApp
{
    public partial class MainWindow : Window
    {
        // --- Nossas ferramentas do EcuCore ---
        private XdfParser _parser = new XdfParser();
        private BinDataLoader _loader = new BinDataLoader();
        private ChecksumCalculator _checksumCalc = new ChecksumCalculator();

        // --- Estado da Aplicação ---
        private List<EcuMap> _loadedMaps = new List<EcuMap>();
        private XdfFile? _currentXdfFile;
        private byte[]? _currentBinFile;
        private string _currentBinFilePath = "";

        // --- Construtor ---
        public MainWindow()
        {
            InitializeComponent();
        }

        // --- Lógica de Arquivo (Abrir/Salvar) ---
        private void OpenFilesButton_Click(object sender, RoutedEventArgs e)
        {
            string xdfFilePath = "";
            OpenFileDialog xdfDialog = new OpenFileDialog { Title = "Selecione o Arquivo de Definição (XDF)", Filter = "Arquivos XDF (*.xdf)|*.xdf|Todos os Arquivos (*.*)|*.*" };
            if (xdfDialog.ShowDialog() != true) return;
            xdfFilePath = xdfDialog.FileName;
            OpenFileDialog binDialog = new OpenFileDialog { Title = "Selecione o Arquivo Binário (BIN)", Filter = "Arquivos BIN (*.bin)|*.bin|Todos os Arquivos (*.*)|*.*" };
            if (binDialog.ShowDialog() != true) return;
            _currentBinFilePath = binDialog.FileName;

            try {
                _loadedMaps.Clear();
                _currentBinFile = File.ReadAllBytes(_currentBinFilePath);
                _currentXdfFile = _parser.Parse(xdfFilePath);
                List<EcuMapDefinition> definitions = _currentXdfFile.MapDefinitions;
                foreach (var def in definitions) { _loadedMaps.Add(_loader.LoadMapData(def, _currentBinFile)); }

                MapListView.ItemsSource = _loadedMaps;
                MapListView.Items.Refresh();
                MapDataGrid.ItemsSource = null;
                WpfPlot.Reset();
                ClearMapDetails(); // Limpa detalhes
                this.Title = $"System Race - {Path.GetFileName(_currentBinFilePath)}";
                SaveFileButton.IsEnabled = true;
                MenuSalvar.IsEnabled = true;

                MessageBox.Show($"Carregamento Concluído! {definitions.Count} mapas encontrados.");
            } catch (Exception ex) { MessageBox.Show($"Ocorreu um erro ao carregar os arquivos:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
             if (_currentBinFile == null || _currentXdfFile == null || string.IsNullOrEmpty(_currentBinFilePath)) { MessageBox.Show("Nenhum arquivo .BIN carregado.", "Erro"); return; }
            try {
                string backupPath = _currentBinFilePath + ".bak";
                File.Copy(_currentBinFilePath, backupPath, true);

                // Recalcula e escreve checksums
                foreach (var checksumDef in _currentXdfFile.ChecksumDefinitions) {
                    byte[] newChecksumBytes = _checksumCalc.Calculate(_currentBinFile, checksumDef);
                    for (int i = 0; i < newChecksumBytes.Length; i++) {
                        long storageAddr = checksumDef.StorageAddress + i;
                        if (storageAddr >= 0 && storageAddr < _currentBinFile.Length) {
                            _currentBinFile[storageAddr] = newChecksumBytes[i];
                        } else {
                             throw new IndexOutOfRangeException($"Endereço ({storageAddr:X}) fora dos limites para checksum '{checksumDef.Title}'.");
                        }
                    }
                    Console.WriteLine($"Checksum '{checksumDef.Title}' escrito em {checksumDef.StorageAddress:X} ({newChecksumBytes.Length} bytes).");
                }

                File.WriteAllBytes(_currentBinFilePath, _currentBinFile);
                this.Title = $"System Race - {Path.GetFileName(_currentBinFilePath)}"; // Remove *MODIFICADO*
                MessageBox.Show($"Arquivo salvo e backup criado em:\n{backupPath}", "Sucesso");

            } catch (NotSupportedException nse) { MessageBox.Show($"Erro: {nse.Message}", "Erro Checksum");
            } catch (ArgumentOutOfRangeException aoore) { MessageBox.Show($"Erro: Endereços inválidos no XDF.\n{aoore.Message}", "Erro Checksum");
            } catch (IndexOutOfRangeException ioore) { MessageBox.Show($"Erro: Endereço inválido.\n{ioore.Message}", "Erro Checksum");
            } catch (Exception ex) { MessageBox.Show($"Erro inesperado ao salvar:\n{ex.Message}", "Erro"); }
        }

        // --- Métodos do Menu ---
        private void MenuAbrir_Click(object sender, RoutedEventArgs e) { OpenFilesButton_Click(sender, e); }
        private void MenuSalvar_Click(object sender, RoutedEventArgs e) { SaveFileButton_Click(sender, e); }
        private void MenuSair_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        // --- Lógica de UI (Seleção e Visualização) ---
        private void MapListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             EcuMap? selectedMap = MapListView.SelectedItem as EcuMap;
            MapDataGrid.ItemsSource = null; WpfPlot.Reset(); ClearMapDetails();
            if (selectedMap == null) return;
            FillMapDetails(selectedMap);
            DataTable table = ConvertMapToDataTable(selectedMap);
            MapDataGrid.ItemsSource = table.DefaultView;
            UpdateScottPlot(selectedMap);
        }

        private void ClearMapDetails()
        {
             MapAddressText.Text = "N/A"; MapSizeText.Text = "N/A";
             MapFormatText.Text = "N/A"; MapEquationText.Text = "N/A";
        }

        private void FillMapDetails(EcuMap map)
        {
             try {
                 var def = map.Definition; MapAddressText.Text = $"0x{def.Address:X}";
                 int dimY = def.YAxis?.Dimension ?? 1; int dimX = def.XAxis?.Dimension ?? 1;
                 MapSizeText.Text = $"{dimY} x {dimX}"; MapFormatText.Text = def.ValueFormat;
                 MapEquationText.Text = def.ValueConversionFormula;
             } catch (Exception ex) {
                 MapAddressText.Text = "Erro"; MapSizeText.Text = "Erro";
                 MapFormatText.Text = "Erro"; MapEquationText.Text = $"Erro: {ex.Message}";
             }
        }

        private void UpdateScottPlot(EcuMap map)
        {
             bool hasX = map.Definition.XAxis != null && map.XAxisValues != null && map.XAxisValues.Count > 0;
             bool hasY = map.Definition.YAxis != null;
             if (!hasX || !hasY) { WpfPlot.Reset(); return; }
             try {
                 WpfPlot.Plot.Clear();
                 double xMin=0, xMax=0, yMin=0, yMax=0;
                 int nC=map.ZValues.GetLength(1); int nR=map.ZValues.GetLength(0);
                 xMin=map.XAxisValues!.First(); xMax=map.XAxisValues!.Last();
                 double xS=(nC>1)?(xMax-xMin)/(nC-1):1;
                 xMin-=xS/2.0; xMax+=xS/2.0;
                 yMin=-0.5; yMax=nR-0.5;
                 var hm=WpfPlot.Plot.Add.Heatmap(map.ZValues);
                 hm.Extent = new ScottPlot.CoordinateRect(xMin, xMax, yMin, yMax);
                 WpfPlot.Plot.Add.ColorBar(hm);
                 WpfPlot.Plot.XLabel(map.Definition.XAxis?.Name??"X");
                 WpfPlot.Plot.YLabel(map.Definition.YAxis?.Name??"Y");
                 WpfPlot.Plot.Title(map.Definition.Title);
                 WpfPlot.Plot.Axes.SetLimits(left:xMin, right:xMax, bottom:yMax, top:yMin); // Inverte Y
                 WpfPlot.Refresh();
             } catch (Exception ex) { MessageBox.Show($"Erro gráfico: {ex.Message}"); }
        }

        // --- Lógica de Edição ---
        private void MapDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
             EcuMap? map = MapListView.SelectedItem as EcuMap;
             if (map == null || _currentBinFile == null || map.Definition.XAxis == null) return;
             var tb = e.EditingElement as TextBox; if (tb == null) return;
             string valStr = tb.Text; int r = e.Row.GetIndex();
             int cIdx = e.Column.DisplayIndex; if (cIdx == 0) return; int c = cIdx - 1;
             try { bool ok = UpdateMapValue(map, r, c, valStr); if (!ok) e.Cancel = true; }
             catch (Exception ex) { MessageBox.Show($"Valor inválido: {ex.Message}"); e.Cancel = true; }
        }

        private void MapDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (MapDataGrid.SelectedCells.Count == 0) return;

            double increment = 0;
            if (e.Key == Key.Add || e.Key == Key.OemPlus) { increment = 1.0; e.Handled = true; }
            else if (e.Key == Key.Subtract || e.Key == Key.OemMinus) { increment = -1.0; e.Handled = true; }

            if (increment != 0) {
                 ApplyBulkOperation(currentValue => currentValue + increment);
            }
        }

        private void ApplyValueButton_Click(object sender, RoutedEventArgs e)
        {
            string valueString = SetValueTextBox.Text;
            if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out double newValue)) {
                 ApplyBulkOperation(currentValue => newValue);
            } else { MessageBox.Show($"Valor '{valueString}' não é um número válido.", "Erro"); }
        }

        private void IncrementValueButton_Click(object sender, RoutedEventArgs e)
        {
            string valueString = IncrementValueTextBox.Text;
             if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out double increment)) {
                 ApplyBulkOperation(currentValue => currentValue + increment);
            } else { MessageBox.Show($"Valor '{valueString}' inválido.", "Erro"); }
        }

        private void DecrementValueButton_Click(object sender, RoutedEventArgs e)
        {
            string valueString = DecrementValueTextBox.Text;
             if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out double decrement)) {
                 ApplyBulkOperation(currentValue => currentValue - decrement);
            } else { MessageBox.Show($"Valor '{valueString}' inválido.", "Erro"); }
        }

        private void IncrementPercentButton_Click(object sender, RoutedEventArgs e)
        {
            string valueString = IncrementPercentTextBox.Text;
             if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out double percent)) {
                 ApplyBulkOperation(currentValue => currentValue * (1.0 + percent / 100.0));
            } else { MessageBox.Show($"Valor '{valueString}' inválido.", "Erro"); }
        }

        private void DecrementPercentButton_Click(object sender, RoutedEventArgs e)
        {
             string valueString = DecrementPercentTextBox.Text;
             if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out double percent)) {
                 ApplyBulkOperation(currentValue => currentValue * (1.0 - percent / 100.0));
            } else { MessageBox.Show($"Valor '{valueString}' inválido.", "Erro"); }
        }

        // --- Método Auxiliar Centralizado para Operações em Lote ---
        private void ApplyBulkOperation(Func<double, double> calculateNewValue)
        {
            if (MapDataGrid.SelectedCells.Count == 0) { MessageBox.Show("Nenhuma célula selecionada.", "Aviso"); return; }
            EcuMap? selectedMap = MapListView.SelectedItem as EcuMap;
            if (selectedMap == null) return;

            bool updateNeeded = false;

            foreach (var cellInfo in MapDataGrid.SelectedCells) {
                var dataRowView = cellInfo.Item as DataRowView;
                if (dataRowView == null) continue;
                int z_y = MapDataGrid.Items.IndexOf(cellInfo.Item);
                int colIndex = cellInfo.Column.DisplayIndex;
                if (colIndex == 0) continue; int z_x = colIndex - 1;
                 if (z_y < 0 || z_y >= selectedMap.ZValues.GetLength(0) || z_x < 0 || z_x >= selectedMap.ZValues.GetLength(1)) continue;

                try {
                    double currentRealValue = selectedMap.ZValues[z_y, z_x];
                    double newRealValue = calculateNewValue(currentRealValue);
                    // Passa o dataRowView para UpdateMapValue para otimizar
                    bool success = UpdateMapValue(selectedMap, z_y, z_x, newRealValue.ToString(CultureInfo.InvariantCulture), dataRowView);
                    if (success) { updateNeeded = true; }
                } catch (Exception ex) { MessageBox.Show($"Erro célula [{z_y},{z_x}]: {ex.Message}"); }
            }

            if (updateNeeded) { UpdateScottPlot(selectedMap); }
        }


        // --- Lógica Central de Atualização de Valor (Assinatura CORRETA) ---
        // CORREÇÃO CS1501: A assinatura agora inclui DataRowView? rowView = null
        private bool UpdateMapValue(EcuMap selectedMap, int z_y, int z_x, string newRealValueString, DataRowView? rowView = null)
        {
            if (_currentBinFile == null || selectedMap.Definition.XAxis == null) return false;

            try {
                double newRealValue = double.Parse(newRealValueString, CultureInfo.InvariantCulture);
                string inverseFormula = selectedMap.Definition.ValueConversionInverseFormula;
                double newRawValue = MathEvaluator.Evaluate(inverseFormula, newRealValue);
                double finalRealValue = newRealValue;

                // CORREÇÃO CS0103: Usa a variável 'format' definida aqui
                string format = selectedMap.Definition.ValueFormat.ToLowerInvariant();
                int dimX = selectedMap.Definition.XAxis.Dimension;
                long baseAddress = selectedMap.Definition.Address;
                int valueSize = GetSizeFromFormat(format);
                // CORREÇÃO CS0103: Usa a variável 'address' definida aqui
                long address = baseAddress + (z_y * dimX * valueSize) + (z_x * valueSize);

                if (address < 0 || (address + valueSize) > _currentBinFile.Length) {
                     MessageBox.Show($"Erro: Endereço ({address:X}) fora dos limites (Tam: {_currentBinFile.Length:X}).", "Erro"); return false;
                }

                // Lógica de escrita por tipo
                if (format == "8bit") {
                    double roundedValue = Math.Round(newRawValue); if (roundedValue > byte.MaxValue) roundedValue = byte.MaxValue; if (roundedValue < byte.MinValue) roundedValue = byte.MinValue; byte nB = (byte)roundedValue;
                    _currentBinFile[address] = nB; finalRealValue = MathEvaluator.Evaluate(selectedMap.Definition.ValueConversionFormula, nB);
                } else if (format == "signed_8bit") {
                    double roundedValue = Math.Round(newRawValue); if (roundedValue > sbyte.MaxValue) roundedValue = sbyte.MaxValue; if (roundedValue < sbyte.MinValue) roundedValue = sbyte.MinValue; sbyte nSB = (sbyte)roundedValue;
                    _currentBinFile[address] = (byte)nSB; finalRealValue = MathEvaluator.Evaluate(selectedMap.Definition.ValueConversionFormula, nSB);
                } else if (format == "16bit_hi_lo" || format == "16bit_lo_hi") {
                    double roundedValue = Math.Round(newRawValue); if (roundedValue > ushort.MaxValue) roundedValue = ushort.MaxValue; if (roundedValue < ushort.MinValue) roundedValue = ushort.MinValue; ushort nUS = (ushort)roundedValue;
                    byte hB = (byte)(nUS >> 8); byte lB = (byte)(nUS & 0xFF);
                    // CORREÇÃO CS0103: Usa 'format' aqui
                    if (format == "16bit_hi_lo") { _currentBinFile[address] = hB; _currentBinFile[address + 1] = lB; }
                    else { _currentBinFile[address] = lB; _currentBinFile[address + 1] = hB; }
                    finalRealValue = MathEvaluator.Evaluate(selectedMap.Definition.ValueConversionFormula, nUS);
                } else if (format == "signed_16bit_hi_lo") {
                    double roundedValue = Math.Round(newRawValue); if (roundedValue > short.MaxValue) roundedValue = short.MaxValue; if (roundedValue < short.MinValue) roundedValue = short.MinValue; short nS = (short)roundedValue;
                    byte hB = (byte)(nS >> 8); byte lB = (byte)(nS & 0xFF);
                    // CORREÇÃO CS0103: Usa 'address' aqui
                    _currentBinFile[address] = hB; _currentBinFile[address + 1] = lB;
                    finalRealValue = MathEvaluator.Evaluate(selectedMap.Definition.ValueConversionFormula, nS);
                }
                 else if (format == "32bit_hi_lo_hi_lo" || format == "32bit_lo_hi_lo_hi") {
                    double roundedValue = Math.Round(newRawValue); if (roundedValue > uint.MaxValue) roundedValue = uint.MaxValue; if (roundedValue < uint.MinValue) roundedValue = uint.MinValue; uint newRawUint = (uint)roundedValue;
                    byte b1 = (byte)(newRawUint >> 24); byte b2 = (byte)(newRawUint >> 16); byte b3 = (byte)(newRawUint >> 8); byte b4 = (byte)(newRawUint & 0xFF);
                     // CORREÇÃO CS0103: Usa 'format' e 'address' aqui
                    if (format == "32bit_hi_lo_hi_lo") { _currentBinFile[address] = b1; _currentBinFile[address + 1] = b2; _currentBinFile[address + 2] = b3; _currentBinFile[address + 3] = b4; }
                    else { _currentBinFile[address] = b4; _currentBinFile[address + 1] = b3; _currentBinFile[address + 2] = b2; _currentBinFile[address + 3] = b1; }
                    finalRealValue = MathEvaluator.Evaluate(selectedMap.Definition.ValueConversionFormula, newRawUint);
                }
                else { MessageBox.Show($"Formato '{format}' não suportado.", "Aviso"); return false; }

                // Atualiza UI e estado
                if (!this.Title.EndsWith(" *MODIFICADO*")) { this.Title += " *MODIFICADO*"; }
                selectedMap.ZValues[z_y, z_x] = finalRealValue;

                // Atualiza DataGrid visualmente (usando o rowView passado ou encontrado)
                DataRowView? theRowView = rowView ?? (MapDataGrid.Items.Count > z_y ? MapDataGrid.Items[z_y] as DataRowView : null);
                if (theRowView != null && theRowView.Row.ItemArray.Length > (z_x + 1)) {
                    theRowView.Row[z_x + 1] = finalRealValue.ToString("F2"); // Ou formato apropriado
                }

                return true; // Sucesso
            }
            catch (FormatException) { MessageBox.Show($"Valor inválido: '{newRealValueString}'"); return false; }
            catch (Exception ex) { MessageBox.Show($"Erro ao atualizar: {ex.Message}"); return false; }
        }


        // --- Métodos Auxiliares ---
        private int GetSizeFromFormat(string format)
        {
             switch(format.ToLowerInvariant()){case"8bit":case"signed_8bit":return 1;case"16bit_hi_lo":case"16bit_lo_hi":case"signed_16bit_hi_lo":case"signed_16bit_lo_hi":return 2;case"32bit_hi_lo_hi_lo":case"32bit_lo_hi_lo_hi":case"float_ieee_be":case"float_ieee_le":return 4;default:return 1;}
        }

        private DataTable ConvertMapToDataTable(EcuMap map)
        {
            var t=new DataTable(map.Definition.Title);
            t.Columns.Add(map.Definition.YAxis?.Name??"Y");
            if(map.XAxisValues!=null&&map.XAxisValues.Count>0){foreach(var xV in map.XAxisValues){t.Columns.Add(xV.ToString("F2"));}}else{t.Columns.Add("Valor");}
            int nR=map.ZValues.GetLength(0);int nC=map.ZValues.GetLength(1);
            for(int y=0;y<nR;y++){
                DataRow nRw=t.NewRow(); string yL=map.Definition.YAxis?.Name??(y).ToString(); nRw[0]=yL+$" [{y}]";
                for(int x=0;x<nC;x++){ if(x+1<t.Columns.Count){ nRw[x+1]=map.ZValues[y,x].ToString("F2");}}
                t.Rows.Add(nRw);
            }
            return t;
        }

    } // Fim da classe MainWindow
} // Fim do namespace EcuApp