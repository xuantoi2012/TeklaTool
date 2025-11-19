using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TeklaTool.Services;
using TeklaTool.Models;
using TeklaTool.Helpers;
using Tekla.Structures.Model.UI;

namespace TeklaTool.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Fields
        private readonly TeklaDataService _dataService;
        private readonly ExcelExportService _excelService;

        private string _plateWidth = "1500";
        private string _plateLength = "6000";
        private string _shapeLength = "12000";
        private string _boltGrade = "8.8";

        private string _statusMessage = "Sẵn sàng. Vui lòng chọn Parts hoặc Model để bắt đầu...";
        private int _progressValue;
        private int _progressMaximum = 100;

        private ObservableCollection<PlateDataRow> _plateData;
        private ObservableCollection<ShapeDataRow> _shapeData;
        private ObservableCollection<BoltDataRow> _boltData;
        private ObservableCollection<PurlinDataRow> _purlinData;
        #endregion

        #region Properties
        public string PlateWidth
        {
            get => _plateWidth;
            set
            {
                _plateWidth = value;
                OnPropertyChanged();
                RecalculatePlateData();
            }
        }

        public string PlateLength
        {
            get => _plateLength;
            set
            {
                _plateLength = value;
                OnPropertyChanged();
                RecalculatePlateData();
            }
        }

        public string ShapeLength
        {
            get => _shapeLength;
            set
            {
                _shapeLength = value;
                OnPropertyChanged();
                RecalculateShapeData();
            }
        }

        public string BoltGrade
        {
            get => _boltGrade;
            set
            {
                _boltGrade = value;
                OnPropertyChanged();
                UpdateBoltGrade();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }

        public int ProgressMaximum
        {
            get => _progressMaximum;
            set
            {
                _progressMaximum = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PlateDataRow> PlateData
        {
            get => _plateData;
            set
            {
                _plateData = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ShapeDataRow> ShapeData
        {
            get => _shapeData;
            set
            {
                _shapeData = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<BoltDataRow> BoltData
        {
            get => _boltData;
            set
            {
                _boltData = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PurlinDataRow> PurlinData
        {
            get => _purlinData;
            set
            {
                _purlinData = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand SelectPartsCommand { get; }
        public ICommand SelectAllModelCommand { get; }
        public ICommand ExportExcelCommand { get; }
        #endregion

        #region Constructor
        public MainViewModel()
        {
            _dataService = new TeklaDataService();
            _excelService = new ExcelExportService();

            PlateData = new ObservableCollection<PlateDataRow>();
            ShapeData = new ObservableCollection<ShapeDataRow>();
            BoltData = new ObservableCollection<BoltDataRow>();
            PurlinData = new ObservableCollection<PurlinDataRow>();

            // Use AsyncRelayCommand for async methods
            SelectPartsCommand = new AsyncRelayCommand(SelectParts, () => true);
            SelectAllModelCommand = new AsyncRelayCommand(SelectAllModel, () => true);
            ExportExcelCommand = new AsyncRelayCommand(ExportToExcel, () => true);
        }
        #endregion

        #region Methods
        private async Task SelectParts()
        {
            try
            {
                StatusMessage = "Đang chờ chọn Parts từ Tekla... (Nhấn Backspace để hoàn tất)";

                Application.Current.MainWindow.WindowState = WindowState.Minimized;

                var picker = new Picker();
                var selectedObjects = picker.PickObjects(Picker.PickObjectsEnum.PICK_N_PARTS);

                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Activate();

                if (selectedObjects == null)
                {
                    StatusMessage = "Không có Parts nào được chọn.";
                    return;
                }

                StatusMessage = "Đang phân tích Parts đã chọn...";

                await _dataService.ProcessSelectedParts(selectedObjects,
                    new Progress<(int current, int total, string message)>(progress =>
                    {
                        ProgressValue = progress.current;
                        ProgressMaximum = progress.total;
                        StatusMessage = progress.message;
                    }));

                StatusMessage = $"✓ Đã chọn {_dataService.TotalSelectedParts} Parts thành công!";
                await ProcessData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chọn Parts: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                StatusMessage = "Lỗi khi chọn Parts.";
            }
        }

        private async Task SelectAllModel()
        {
            try
            {
                StatusMessage = "Đang lấy toàn bộ Parts trong Model...";

                await _dataService.ProcessAllModelParts(
                    new Progress<(int current, int total, string message)>(progress =>
                    {
                        ProgressValue = progress.current;
                        ProgressMaximum = progress.total;
                        StatusMessage = progress.message;
                    }));

                StatusMessage = $"✓ Đã lấy {_dataService.TotalSelectedParts} Parts từ Model!";
                await ProcessData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy Model: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Lỗi khi lấy Model.";
            }
        }

        private async Task ProcessData()
        {
            try
            {
                StatusMessage = "Đang xử lý dữ liệu...";
                ProgressValue = 0;

                // Validate inputs
                if (!double.TryParse(PlateWidth, out double pWidth) ||
                    !double.TryParse(PlateLength, out double pLength))
                {
                    MessageBox.Show("Vui lòng nhập kích thước thép tấm hợp lệ!",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(ShapeLength, out double sLength))
                {
                    MessageBox.Show("Vui lòng nhập chiều dài thép hình hợp lệ!",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Process Plate Data
                var plateTable = await _dataService.ProcessPlateData(pWidth, pLength,
                    new Progress<(int current, int total, string message)>(progress =>
                    {
                        ProgressValue = progress.current;
                        ProgressMaximum = progress.total;
                        StatusMessage = $"Đang xử lý thép tấm... {progress.message}";
                    }));

                PlateData.Clear();
                foreach (System.Data.DataRow row in plateTable.Rows)
                {
                    if (row["STT"] != DBNull.Value)
                    {
                        PlateData.Add(new PlateDataRow
                        {
                            STT = Convert.ToInt32(row["STT"]),
                            ChieuDay = Convert.ToDouble(row["Chiều Dày (mm)"]),
                            ChieuRong = pWidth,
                            ChieuDai = pLength,
                            SoLuong = Convert.ToDouble(row["Số Lượng"]),
                            KhoiLuong = Convert.ToDouble(row["Khối Lượng (kg)"]),
                            VatLieu = row["Vật liệu"].ToString()
                        });
                    }
                }

                // Process Shape Data
                var shapeTable = await _dataService.ProcessShapeData(sLength, true,
                    new Progress<(int current, int total, string message)>(progress =>
                    {
                        ProgressValue = progress.current;
                        ProgressMaximum = progress.total;
                        StatusMessage = $"Đang xử lý thép hình... {progress.message}";
                    }));

                ShapeData.Clear();
                foreach (System.Data.DataRow row in shapeTable.Rows)
                {
                    ShapeData.Add(new ShapeDataRow
                    {
                        STT = Convert.ToInt32(row["STT"]),
                        QuyCach = row["Quy cách"].ToString(),
                        ChieuDai = sLength,
                        SoLuong = Convert.ToDouble(row["Số Lượng"]),
                        KhoiLuongDonVi = Convert.ToDouble(row["Khối lượng đơn vị (kg/m)"]),
                        KhoiLuong = Convert.ToDouble(row["Khối Lượng (kg)"]),
                        VatLieu = row["Vật liệu"].ToString()
                    });
                }

                // Process Bolt Data
                var boltTable = await _dataService.ProcessBoltData(
                    new Progress<(int current, int total, string message)>(progress =>
                    {
                        ProgressValue = progress.current;
                        ProgressMaximum = progress.total;
                        StatusMessage = $"Đang xử lý bulong... {progress.message}";
                    }));

                BoltData.Clear();
                foreach (System.Data.DataRow row in boltTable.Rows)
                {
                    BoltData.Add(new BoltDataRow
                    {
                        STT = Convert.ToInt32(row["STT"]),
                        KichThuoc = row["Kích Thước"].ToString(),
                        CuongDo = BoltGrade,
                        SoLuong = Convert.ToInt32(row["Số Lượng"])
                    });
                }

                // Process Purlin Data
                var purlinTable = await _dataService.ProcessPurlinData(
                    new Progress<(int current, int total, string message)>(progress =>
                    {
                        ProgressValue = progress.current;
                        ProgressMaximum = progress.total;
                        StatusMessage = $"Đang xử lý xà gồ... {progress.message}";
                    }));

                PurlinData.Clear();
                foreach (System.Data.DataRow row in purlinTable.Rows)
                {
                    PurlinData.Add(new PurlinDataRow
                    {
                        STT = Convert.ToInt32(row["STT"]),
                        Assembly = row["Assembly Name"].ToString(),
                        QuyCach = row["Quy cách"].ToString(),
                        SoLuong = Convert.ToDouble(row["Số Lượng"]),
                        ChieuDai = Convert.ToDouble(row["Chiều dài"]),
                        KhoiLuongMotCauKien = Convert.ToDouble(row["Khối lượng 1 cấu kiện (kg)"]),
                        KhoiLuong = Convert.ToDouble(row["Khối Lượng (kg)"]),
                        VatLieu = row["Vật liệu"].ToString()
                    });
                }

                StatusMessage = "✓ Xử lý hoàn tất!";
                ProgressValue = ProgressMaximum;
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Lỗi khi xử lý dữ liệu.";
            }
        }

        private void RecalculatePlateData()
        {
            if (PlateData == null || PlateData.Count == 0) return;

            if (!double.TryParse(PlateWidth, out double width) ||
                !double.TryParse(PlateLength, out double length))
                return;

            foreach (var row in PlateData)
            {
                row.ChieuRong = width;
                row.ChieuDai = length;

                // Recalculate quantity based on new dimensions
                double plateVolume = width * length * row.ChieuDay * 7850 / 1e9;
                if (plateVolume > 0)
                {
                    row.SoLuong = Math.Round(row.KhoiLuong / plateVolume, 2);
                }
            }
        }

        private void RecalculateShapeData()
        {
            if (ShapeData == null || ShapeData.Count == 0) return;

            if (!double.TryParse(ShapeLength, out double length))
                return;

            foreach (var row in ShapeData)
            {
                row.ChieuDai = length;

                // Recalculate quantity based on new length
                if (row.KhoiLuongDonVi > 0)
                {
                    row.SoLuong = Math.Round(row.KhoiLuong / (row.KhoiLuongDonVi * length / 1000), 2);
                }
            }
        }

        private void UpdateBoltGrade()
        {
            if (BoltData == null || BoltData.Count == 0) return;

            foreach (var row in BoltData)
            {
                row.CuongDo = BoltGrade;
            }
        }

        public void OnCellEdited(object row, string propertyName)
        {
            if (row is PlateDataRow plateRow)
            {
                // Recalculate plate quantity
                double plateVolume = plateRow.ChieuRong * plateRow.ChieuDai * plateRow.ChieuDay * 7850 / 1e9;
                if (plateVolume > 0)
                {
                    plateRow.SoLuong = Math.Round(plateRow.KhoiLuong / plateVolume, 2);
                }
            }
            else if (row is ShapeDataRow shapeRow)
            {
                // Recalculate shape quantity
                if (shapeRow.KhoiLuongDonVi > 0)
                {
                    shapeRow.SoLuong = Math.Round(shapeRow.KhoiLuong / (shapeRow.KhoiLuongDonVi * shapeRow.ChieuDai / 1000), 2);
                }
            }
        }

        private async Task ExportToExcel()
        {
            try
            {
                // Show ProjectInfo dialog first
                var projectVm = new ProjectInfoViewModel();
                var projectWin = new TeklaTool.Views.ProjectInfoWindow
                {
                    DataContext = projectVm,
                    Owner = Application.Current.MainWindow
                };

                bool? result = projectWin.ShowDialog();

                if (result != true)
                {
                    // User cancelled
                    StatusMessage = "Đã hủy xuất Excel.";
                    return;
                }

                // Get logo path from window (if any)
                string logoPath = (projectWin as TeklaTool.Views.ProjectInfoWindow)?.LogoPath;

                StatusMessage = "Đang xuất dữ liệu sang Excel...";

                await _excelService.ExportToExcel(
                    PlateData.ToList(),
                    ShapeData.ToList(),
                    BoltData.ToList(),
                    PurlinData.ToList(),
                    projectVm.ProjectName,
                    projectVm.Designer,
                    projectVm.DeliveryLocation,
                    logoPath,
                    new Progress<string>(message => StatusMessage = message));

                StatusMessage = "✓ Xuất Excel thành công!";
                MessageBox.Show("Đã xuất dữ liệu sang Excel thành công!",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Lỗi khi xuất Excel.";
            }
        }

        private bool HasData()
        {
            return (PlateData != null && PlateData.Count > 0) ||
                   (ShapeData != null && ShapeData.Count > 0) ||
                   (BoltData != null && BoltData.Count > 0) ||
                   (PurlinData != null && PurlinData.Count > 0);
        }
        #endregion
    }
}