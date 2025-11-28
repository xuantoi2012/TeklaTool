using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tekla.Structures.Model.UI;
using TeklaTool_2017.Helpers;
using TeklaTool_2017.Models;
using TeklaTool_2017.Services;
using TeklaTool_2017.Views;

namespace TeklaTool_2017.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Fields
        private readonly TeklaDataService _dataService;
        private readonly ExcelExportService _excelService;
        private readonly TeklaConnectionService _teklaConnectionService;
        private readonly SettingsService _settingsService;
        private readonly LicenseService _licenseService;

        private string _plateWidth;
        private string _plateLength;
        private string _plateMaterial;
        private string _plateNote;

        private string _shapeLength;
        private string _shapeMaterial;
        private string _shapeNote;

        private string _boltGrade;

        // ✅ Assembly filters (affects calculation)
        private string _sagRodFilter = "";
        private string _purlinFilter = "";

        // ✅ Profile filters (grid display only)
        private string _sagRodProfileFilter = "";
        private string _purlinProfileFilter = "";

        private string _sagRodMaterial;
        private string _sagRodNote;

        private string _purlinGrade;
        private string _purlinMaterial;
        private string _purlinNote;

        private string _statusMessage = "Sẵn sàng.  Vui lòng kiểm tra kết nối Tekla... ";
        private int _progressValue;
        private int _progressMaximum = 100;

        private ObservableCollection<PlateDataRow> _plateData;
        private ObservableCollection<ShapeDataRow> _shapeData;
        private ObservableCollection<BoltDataRow> _boltData;
        private ObservableCollection<SagRodDataRow> _sagRodData;
        private ObservableCollection<PurlinDataRow> _purlinData;

        // ✅ Store original data (before profile filtering)
        private ObservableCollection<SagRodDataRow> _sagRodDataOriginal;
        private ObservableCollection<PurlinDataRow> _purlinDataOriginal;

        // ✅ Flag: đã load cache chưa
        private bool _isDataLoaded = false;
        #endregion

        #region Properties
        public string PlateWidth
        {
            get => _plateWidth;
            set { _plateWidth = value; OnPropertyChanged(); RecalculatePlateData(); }
        }

        public string PlateLength
        {
            get => _plateLength;
            set { _plateLength = value; OnPropertyChanged(); RecalculatePlateData(); }
        }

        public string PlateMaterial
        {
            get => _plateMaterial;
            set
            {
                _plateMaterial = value;
                OnPropertyChanged();
                FillPlateMaterial();
            }
        }

        public string PlateNote
        {
            get => _plateNote;
            set
            {
                _plateNote = value;
                OnPropertyChanged();
                FillPlateNote();
            }
        }

        public string ShapeLength
        {
            get => _shapeLength;
            set { _shapeLength = value; OnPropertyChanged(); RecalculateShapeData(); }
        }

        public string ShapeMaterial
        {
            get => _shapeMaterial;
            set
            {
                _shapeMaterial = value;
                OnPropertyChanged();
                FillShapeMaterial();
            }
        }

        public string ShapeNote
        {
            get => _shapeNote;
            set
            {
                _shapeNote = value;
                OnPropertyChanged();
                FillShapeNote();
            }
        }

        public string BoltGrade
        {
            get => _boltGrade;
            set { _boltGrade = value; OnPropertyChanged(); UpdateBoltGrade(); }
        }

        // ✅ Assembly Filter (affects calculation)
        public string SagRodFilter
        {
            get => _sagRodFilter;
            set
            {
                _sagRodFilter = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
                (RecalculateSagRodCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

                // ✅ Clear profile filter khi assembly filter thay đổi
                SagRodProfileFilter = "";
            }
        }

        // ✅ Profile Filter (grid display only)
        public string SagRodProfileFilter
        {
            get => _sagRodProfileFilter;
            set
            {
                _sagRodProfileFilter = value;
                OnPropertyChanged();
                FilterSagRodDataGrid();
            }
        }

        public string SagRodMaterial
        {
            get => _sagRodMaterial;
            set
            {
                _sagRodMaterial = value;
                OnPropertyChanged();
                FillSagRodMaterial();
            }
        }

        public string SagRodNote
        {
            get => _sagRodNote;
            set
            {
                _sagRodNote = value;
                OnPropertyChanged();
                FillSagRodNote();
            }
        }

        // ✅ Assembly Filter (affects calculation)
        public string PurlinFilter
        {
            get => _purlinFilter;
            set
            {
                _purlinFilter = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
                (RecalculatePurlinCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

                // ✅ Clear profile filter
                PurlinProfileFilter = "";
            }
        }

        // ✅ Profile Filter (grid display only)
        public string PurlinProfileFilter
        {
            get => _purlinProfileFilter;
            set
            {
                _purlinProfileFilter = value;
                OnPropertyChanged();
                FilterPurlinDataGrid();
            }
        }

        public string PurlinGrade
        {
            get => _purlinGrade;
            set
            {
                _purlinGrade = value;
                OnPropertyChanged();
                UpdatePurlinGrade();
            }
        }

        public string PurlinMaterial
        {
            get => _purlinMaterial;
            set
            {
                _purlinMaterial = value;
                OnPropertyChanged();
                FillPurlinMaterial();
            }
        }

        public string PurlinNote
        {
            get => _purlinNote;
            set
            {
                _purlinNote = value;
                OnPropertyChanged();
                FillPurlinNote();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public int ProgressMaximum
        {
            get => _progressMaximum;
            set { _progressMaximum = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PlateDataRow> PlateData
        {
            get => _plateData;
            set { _plateData = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ShapeDataRow> ShapeData
        {
            get => _shapeData;
            set { _shapeData = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BoltDataRow> BoltData
        {
            get => _boltData;
            set { _boltData = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SagRodDataRow> SagRodData
        {
            get => _sagRodData;
            set { _sagRodData = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PurlinDataRow> PurlinData
        {
            get => _purlinData;
            set { _purlinData = value; OnPropertyChanged(); }
        }

        // ✅ Enable/disable profile filter textbox
        public bool CanFilterSagRodProfile => _sagRodDataOriginal != null && _sagRodDataOriginal.Count > 0;
        public bool CanFilterPurlinProfile => _purlinDataOriginal != null && _purlinDataOriginal.Count > 0;

        public bool CanRecalculateSagRodProperty => CanRecalculateSagRod();
        public bool CanRecalculatePurlinProperty => CanRecalculatePurlin();
        #endregion

        #region Commands
        private readonly AsyncRelayCommand _selectPartsCommand;
        private readonly AsyncRelayCommand _selectAllModelCommand;
        private readonly AsyncRelayCommand _processDataCommand;

        public ICommand SelectPartsCommand => _selectPartsCommand;
        public ICommand SelectAllModelCommand => _selectAllModelCommand;
        public ICommand ProcessDataCommand => _processDataCommand;
        public ICommand ExportExcelCommand { get; }
        public ICommand OpenLicenseCommand { get; }
        public ICommand RecalculateSagRodCommand { get; }
        public ICommand RecalculatePurlinCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand CheckTeklaConnectionCommand { get; }
        #endregion

        #region Constructor
        public MainViewModel()
        {
            _teklaConnectionService = new TeklaConnectionService();
            _dataService = new TeklaDataService();
            _excelService = new ExcelExportService();
            _licenseService = new LicenseService();
            _settingsService = new SettingsService();

            // ✅ Load default values from settings
            var settings = _settingsService.LoadUserSettings();
            PlateWidth = settings.DefaultPlateWidth;
            PlateLength = settings.DefaultPlateLength;
            PlateMaterial = settings.DefaultPlateMaterial;
            ShapeLength = settings.DefaultShapeLength;
            ShapeMaterial = settings.DefaultShapeMaterial;
            BoltGrade = settings.DefaultBoltGrade;
            SagRodMaterial = settings.DefaultSagRodMaterial;
            PurlinGrade = settings.DefaultPurlinGrade;
            PurlinMaterial = settings.DefaultPurlinMaterial;

            PlateData = new ObservableCollection<PlateDataRow>();
            ShapeData = new ObservableCollection<ShapeDataRow>();
            BoltData = new ObservableCollection<BoltDataRow>();
            SagRodData = new ObservableCollection<SagRodDataRow>();
            PurlinData = new ObservableCollection<PurlinDataRow>();

            _selectPartsCommand = new AsyncRelayCommand(SelectParts, CanSelectParts);
            _selectAllModelCommand = new AsyncRelayCommand(SelectAllModel, CanSelectModel);

            ExportExcelCommand = new AsyncRelayCommand(ExportExcelWithProjectInfo, HasData);
            OpenLicenseCommand = new RelayCommand(_ => OpenLicenseWindow());
            RecalculateSagRodCommand = new AsyncRelayCommand(RecalculateSagRod, CanRecalculateSagRod);
            RecalculatePurlinCommand = new AsyncRelayCommand(RecalculatePurlin, CanRecalculatePurlin);
            OpenSettingsCommand = new RelayCommand(_ => OpenSettingsWindow());

            CheckTeklaConnectionCommand = new RelayCommand(_ => CheckTeklaConnection());

            _elapsedTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _elapsedTimer.Tick += (s, e) =>
            {
                if (_globalStopwatch != null && _globalStopwatch.IsRunning)
                {
                    var elapsed = _globalStopwatch.Elapsed;
                    ElapsedTime = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}. {elapsed.Milliseconds / 100}";
                }
            };

            CheckTeklaConnection();
            CheckLicenseStatus();
        }
        #endregion

        #region Timer (existing code - keep as is)
        private System.Diagnostics.Stopwatch _globalStopwatch;
        private System.Windows.Threading.DispatcherTimer _elapsedTimer;
        private string _elapsedTime = "00:00.0";
        private bool _isProcessing = false;
        private bool _isCompleted = false;

        public string ElapsedTime
        {
            get => _elapsedTime;
            set { _elapsedTime = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(); }
        }

        private void StartTimer()
        {
            try
            {
                _globalStopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (_elapsedTimer != null)
                {
                    _elapsedTimer.Start();
                }
                ElapsedTime = "00:00.0";
                IsProcessing = true;
                IsCompleted = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting timer: {ex.Message}");
            }
        }

        private void StopTimer()
        {
            try
            {
                _globalStopwatch?.Stop();
                if (_elapsedTimer != null && _elapsedTimer.IsEnabled)
                {
                    _elapsedTimer.Stop();
                }
                if (_globalStopwatch != null)
                {
                    var elapsed = _globalStopwatch.Elapsed;
                    ElapsedTime = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds / 100}";
                }
                IsProcessing = false;
                IsCompleted = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping timer: {ex.Message}");
            }
        }

        private void ResetTimer()
        {
            try
            {
                _globalStopwatch?.Reset();
                if (_elapsedTimer != null && _elapsedTimer.IsEnabled)
                {
                    _elapsedTimer.Stop();
                }
                ElapsedTime = "00:00.0";
                IsProcessing = false;
                IsCompleted = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting timer: {ex.Message}");
            }
        }
        #endregion

        #region CanExecute
        private bool CanSelectParts()
        {
            return _teklaConnectionService.IsTeklaConnected();
        }

        private bool CanSelectModel()
        {
            return _teklaConnectionService.IsTeklaConnected();
        }

        private bool CanRecalculateSagRod()
        {
            return _isDataLoaded && !string.IsNullOrWhiteSpace(SagRodFilter);
        }

        private bool CanRecalculatePurlin()
        {
            return _isDataLoaded && !string.IsNullOrWhiteSpace(PurlinFilter);
        }
        #endregion

        #region 🚀 PHASE 1: Select Parts/Model (Load to cache)
        private async Task SelectParts()
        {
            try
            {
                if (!_teklaConnectionService.IsTeklaConnected())
                {
                    MessageBox.Show(
                        "Tekla Structures chưa được mở hoặc chưa load model!\n\n" +
                        "Vui lòng mở Tekla và load model trước khi chọn Parts.",
                        "Lỗi kết nối Tekla",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    StatusMessage = "✗ Không thể kết nối Tekla";
                    return;
                }

                if (!ValidateInputs())
                    return;

                StatusMessage = "Đang chờ chọn Parts từ Tekla...  (Nhấn Backspace khi xong)";
                Application.Current.MainWindow.WindowState = WindowState.Minimized;

                // ✅ Wrap blocking call in Task. Run
                var selectedObjects = await Task.Run(() =>
                {
                    var picker = new Picker();
                    return picker.PickObjects(Picker.PickObjectsEnum.PICK_N_PARTS);
                });

                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Activate();

                if (selectedObjects == null || selectedObjects.GetSize() == 0)
                {
                    StatusMessage = "Không có Parts nào được chọn.";
                    return;
                }

                // ✅ START: Process selected parts
                StartTimer();

                double pWidth = double.Parse(PlateWidth);
                double pLength = double.Parse(PlateLength);
                double sLength = double.Parse(ShapeLength);

                var progress = new Progress<(int current, int total, string message)>(p =>
                {
                    ProgressValue = p.current;
                    ProgressMaximum = p.total > 0 ? p.total : 100;
                    StatusMessage = p.message;
                });
                StatusMessage = "⚡ Đang đọc selected parts... ";

                // Placeholder - cần implement
                MessageBox.Show(
                    "Tính năng Select Parts đang được hoàn thiện.\n\n" +
                    "Hiện tại vui lòng dùng 'Select All Model'.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                StopTimer();
                await _dataService.LoadSelectedPartsToCache(selectedObjects, progress);

                _isDataLoaded = true;

                StatusMessage = "⚙️ Đang xử lý dữ liệu...";
                await _dataService.ProcessAllFromCache(
                    pWidth, pLength, PlateMaterial,
                    sLength, ShapeMaterial,
                    SagRodFilter, SagRodMaterial,
                    PurlinFilter, PurlinMaterial,
                    progress);

                await BuildAllDataTables();

                StopTimer();

                StatusMessage = $"✓ Hoàn tất trong {ElapsedTime}!  " +
                               $"Thép tấm: {PlateData.Count} | Thép hình: {ShapeData.Count} | " +
                               $"Bulong: {BoltData.Count} | Ty xà gồ: {SagRodData.Count} | Xà gồ: {PurlinData.Count}";

                (_processDataCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ExportExcelCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RecalculateSagRodCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RecalculatePurlinCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                StopTimer();
                MessageBox.Show($"Lỗi khi chọn Parts: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                StatusMessage = "✗ Lỗi khi chọn Parts";
            }
        }

        private async Task SelectAllModel()
        {
            try
            {
                if (!_teklaConnectionService.IsTeklaConnected())
                {
                    MessageBox.Show(
                        "Tekla Structures chưa được mở hoặc chưa load model! ",
                        "Lỗi kết nối Tekla",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!ValidateInputs())
                    return;

                var result = MessageBox.Show(
                    "Bạn có chắc muốn xử lý toàn bộ Model?\n\n" +
                    "Quá trình này có thể mất vài phút với model lớn.",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    StatusMessage = "Đã hủy xử lý Model. ";
                    return;
                }

                StartTimer();

                double pWidth = double.Parse(PlateWidth);
                double pLength = double.Parse(PlateLength);
                double sLength = double.Parse(ShapeLength);

                var progress = new Progress<(int current, int total, string message)>(p =>
                {
                    ProgressValue = p.current;
                    ProgressMaximum = p.total > 0 ? p.total : 100;
                    StatusMessage = p.message;
                });

                // ✅ PHASE 1: Đọc tất cả parts vào cache (chỉ 1 lần)
                StatusMessage = "⚡ Đang đọc model... ";
                await _dataService.LoadAllPartsToCache(progress);

                _isDataLoaded = true;

                // ✅ PHASE 2: Xử lý với filters
                StatusMessage = "⚙️ Đang xử lý dữ liệu...";
                await _dataService.ProcessAllFromCache(
                    pWidth, pLength, PlateMaterial,
                    sLength, ShapeMaterial,
                    SagRodFilter, SagRodMaterial,
                    PurlinFilter, PurlinMaterial,
                    progress);

                // ✅ Build DataTables
                await BuildAllDataTables();

                StopTimer();

                StatusMessage = $"✓ Hoàn tất trong {ElapsedTime}!  " +
                               $"Thép tấm: {PlateData.Count} | Thép hình: {ShapeData.Count} | " +
                               $"Bulong: {BoltData.Count} | Ty xà gồ: {SagRodData.Count} | Xà gồ: {PurlinData.Count}";

                (_processDataCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ExportExcelCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RecalculateSagRodCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RecalculatePurlinCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                StopTimer();
                MessageBox.Show($"Lỗi: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "✗ Lỗi khi xử lý Model";
            }
        }

        private bool ValidateInputs()
        {
            if (!double.TryParse(PlateWidth, out double pWidth) ||
                !double.TryParse(PlateLength, out double pLength))
            {
                MessageBox.Show("Vui lòng nhập kích thước thép tấm hợp lệ! ",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!double.TryParse(ShapeLength, out double sLength))
            {
                MessageBox.Show("Vui lòng nhập chiều dài thép hình hợp lệ!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
        #endregion

        #region 🚀 PHASE 2: Build DataTables from cache
        private async Task BuildAllDataTables()
        {
            double pWidth = double.Parse(PlateWidth);
            double pLength = double.Parse(PlateLength);
            double sLength = double.Parse(ShapeLength);

            // ✅ Build Plate
            var plateTable = await _dataService.BuildPlateDataTable(pWidth, pLength, PlateMaterial);
            PlateData.Clear();
            foreach (System.Data.DataRow row in plateTable.Rows)
            {
                if (row["STT"] != DBNull.Value)
                {
                    PlateData.Add(new PlateDataRow(_settingsService)
                    {
                        STT = Convert.ToInt32(row["STT"]),
                        ChieuDay = Convert.ToDouble(row["Chiều Dày (mm)"]),
                        ChieuRong = pWidth,
                        ChieuDai = pLength,
                        RawSoLuong = Convert.ToDouble(row["Số Lượng"]),
                        RawKhoiLuong = Convert.ToDouble(row["Khối Lượng (kg)"]),
                        VatLieu = row["Vật liệu"].ToString(),
                        GhiChu = PlateNote
                    });
                }
            }

            // ✅ Build Shape
            var shapeTable = await _dataService.BuildShapeDataTable(sLength, ShapeMaterial);
            ShapeData.Clear();
            foreach (System.Data.DataRow row in shapeTable.Rows)
            {
                ShapeData.Add(new ShapeDataRow(_settingsService)
                {
                    STT = Convert.ToInt32(row["STT"]),
                    QuyCach = row["Quy cách"].ToString(),
                    ChieuDai = Convert.ToDouble(row["ChieuDai"]),
                    RawTongChieuDai = Convert.ToDouble(row["TongChieuDai"]),
                    RawKhoiLuongDonVi = Convert.ToDouble(row["Khối lượng đơn vị (kg/m)"]),
                    RawKhoiLuong = Convert.ToDouble(row["Khối Lượng (kg)"]),
                    VatLieu = row["Vật liệu"].ToString(),
                    GhiChu = ShapeNote
                });
            }

            // ✅ Build Bolt
            BoltData.Clear();
            if (_dataService.BoltDataTable != null && _dataService.BoltDataTable.Rows.Count > 0)
            {
                foreach (System.Data.DataRow row in _dataService.BoltDataTable.Rows)
                {
                    string boltSize = row["Kích Thước"].ToString();
                    BoltData.Add(new BoltDataRow(_settingsService)
                    {
                        STT = Convert.ToInt32(row["STT"]),
                        KichThuoc = boltSize,
                        ChieuDai = BoltLengthHelper.GetDefaultLength(boltSize),
                        CuongDo = BoltGrade,
                        SoLuong = Convert.ToInt32(row["Số Lượng"])
                    });
                }
            }

            // ✅ Build SagRod
            await BuildSagRodDataTable();

            // ✅ Build Purlin
            await BuildPurlinDataTable();
        }

        private async Task BuildSagRodDataTable()
        {
            var sagRodTable = await _dataService.BuildSagRodDataTable(SagRodMaterial);
            SagRodData.Clear();

            foreach (System.Data.DataRow row in sagRodTable.Rows)
            {
                SagRodData.Add(new SagRodDataRow(_settingsService)
                {
                    STT = SagRodData.Count + 1,
                    Assembly = row["Assembly Name"].ToString(),
                    QuyCach = row["Quy cách"].ToString(),
                    RawSoLuong = Convert.ToDouble(row["Số Lượng"]),
                    RawChieuDai = Convert.ToDouble(row["Chiều dài"]),
                    RawKhoiLuongMotCauKien = Convert.ToDouble(row["Khối lượng 1 cấu kiện (kg)"]),
                    RawKhoiLuong = Convert.ToDouble(row["Khối Lượng (kg)"]),
                    VatLieu = row["Vật liệu"].ToString(),
                    GhiChu = SagRodNote
                });
            }

            // ✅ Store original (before profile filter)
            _sagRodDataOriginal = new ObservableCollection<SagRodDataRow>(SagRodData);
            OnPropertyChanged(nameof(CanFilterSagRodProfile));
        }

        private async Task BuildPurlinDataTable()
        {
            var purlinTable = await _dataService.BuildPurlinDataTable(PurlinMaterial);
            PurlinData.Clear();

            foreach (System.Data.DataRow row in purlinTable.Rows)
            {
                PurlinData.Add(new PurlinDataRow(_settingsService)
                {
                    STT = PurlinData.Count + 1,
                    Assembly = row["Assembly Name"].ToString(),
                    QuyCach = row["Quy cách"].ToString(),
                    RawSoLuong = Convert.ToDouble(row["Số Lượng"]),
                    RawChieuDai = Convert.ToDouble(row["Chiều dài"]),
                    RawKhoiLuongDonVi = Convert.ToDouble(row["Khối lượng đơn vị (kg/m)"]),
                    RawKhoiLuongMotCauKien = Convert.ToDouble(row["Khối lượng 1 cấu kiện (kg)"]),
                    RawKhoiLuong = Convert.ToDouble(row["Khối Lượng (kg)"]),
                    CuongDo = PurlinGrade,
                    VatLieu = row["Vật liệu"].ToString(),
                    GhiChu = PurlinNote
                });
            }

            // ✅ Store original
            _purlinDataOriginal = new ObservableCollection<PurlinDataRow>(PurlinData);
            OnPropertyChanged(nameof(CanFilterPurlinProfile));
        }
        #endregion

        #region 🚀 Recalculate (< 0.5s each)
        private async Task RecalculateSagRod()
        {
            try
            {
                if (!_isDataLoaded)
                {
                    MessageBox.Show("Vui lòng load Model trước! ",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = "Đang tính lại Ty xà gồ...";

                var progress = new Progress<(int current, int total, string message)>(p =>
                {
                    StatusMessage = p.message;
                });

                // ✅ Recalculate (siêu nhanh < 0.5s)
                await _dataService.RecalculateSagRod(SagRodFilter, SagRodMaterial, progress);

                // ✅ Rebuild DataTable
                await BuildSagRodDataTable();

                // ✅ Clear profile filter
                SagRodProfileFilter = "";

                StatusMessage = $"✓ Tính lại Ty xà gồ: {SagRodData.Count} items";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "✗ Lỗi khi tính lại";
            }
        }

        private async Task RecalculatePurlin()
        {
            try
            {
                if (!_isDataLoaded)
                {
                    MessageBox.Show("Vui lòng load Model trước!",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = "Đang tính lại Xà gồ...";

                var progress = new Progress<(int current, int total, string message)>(p =>
                {
                    StatusMessage = p.message;
                });

                await _dataService.RecalculatePurlin(PurlinFilter, PurlinMaterial, progress);

                await BuildPurlinDataTable();

                PurlinProfileFilter = "";

                StatusMessage = $"✓ Tính lại Xà gồ: {PurlinData.Count} items";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "✗ Lỗi khi tính lại";
            }
        }
        #endregion

        #region Profile Grid Filtering (display only)
        private void FilterSagRodDataGrid()
        {
            if (_sagRodDataOriginal == null || _sagRodDataOriginal.Count == 0)
                return;

            if (string.IsNullOrWhiteSpace(SagRodProfileFilter))
            {
                SagRodData = new ObservableCollection<SagRodDataRow>(_sagRodDataOriginal);
                StatusMessage = $"Ty xà gồ: {SagRodData.Count} items";
                return;
            }

            var profileFilters = SagRodProfileFilter
                .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim().ToUpper())
                .ToArray();

            var filtered = _sagRodDataOriginal.Where(row =>
            {
                string profile = row.QuyCach?.ToUpper() ?? "";
                return profileFilters.Any(filter =>
                    profile.Contains(filter) || profile.StartsWith(filter));
            }).ToList();

            int stt = 1;
            foreach (var row in filtered)
            {
                row.STT = stt++;
            }

            SagRodData = new ObservableCollection<SagRodDataRow>(filtered);
            StatusMessage = $"✓ Lọc Ty xà gồ: {filtered.Count}/{_sagRodDataOriginal.Count} items";
        }

        private void FilterPurlinDataGrid()
        {
            if (_purlinDataOriginal == null || _purlinDataOriginal.Count == 0)
                return;

            if (string.IsNullOrWhiteSpace(PurlinProfileFilter))
            {
                PurlinData = new ObservableCollection<PurlinDataRow>(_purlinDataOriginal);
                StatusMessage = $"Xà gồ: {PurlinData.Count} items";
                return;
            }

            var profileFilters = PurlinProfileFilter
                .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim().ToUpper())
                .ToArray();

            var filtered = _purlinDataOriginal.Where(row =>
            {
                string profile = row.QuyCach?.ToUpper() ?? "";
                return profileFilters.Any(filter =>
                    profile.Contains(filter) || profile.StartsWith(filter));
            }).ToList();

            int stt = 1;
            foreach (var row in filtered)
            {
                row.STT = stt++;
            }

            PurlinData = new ObservableCollection<PurlinDataRow>(filtered);
            StatusMessage = $"✓ Lọc Xà gồ: {filtered.Count}/{_purlinDataOriginal.Count} items";
        }
        #endregion

        #region Helper Methods (existing - keep as is)

        private void RecalculatePlateData()
        {
            if (PlateData == null || PlateData.Count == 0) return;
            if (!double.TryParse(PlateWidth, out double width) ||
                !double.TryParse(PlateLength, out double length)) return;

            foreach (var row in PlateData)
            {
                row.ChieuRong = width;
                row.ChieuDai = length;

                double plateVolume = width * length * row.ChieuDay * 7850 / 1e9;
                if (plateVolume > 0)
                {
                    row.RawSoLuong = row.RawKhoiLuong / plateVolume;
                }
            }
        }

        private void RecalculateShapeData()
        {
            if (ShapeData == null || ShapeData.Count == 0) return;
            if (!double.TryParse(ShapeLength, out double length)) return;

            foreach (var row in ShapeData)
            {
                row.ChieuDai = length;
            }
        }

        private void UpdateBoltGrade()
        {
            if (BoltData == null || BoltData.Count == 0) return;
            foreach (var row in BoltData)
                row.CuongDo = BoltGrade;
        }

        private void FillPlateMaterial()
        {
            if (PlateData == null || PlateData.Count == 0 || string.IsNullOrEmpty(_plateMaterial)) return;
            foreach (var row in PlateData)
                row.VatLieu = _plateMaterial;
        }

        private void FillPlateNote()
        {
            if (PlateData == null || PlateData.Count == 0) return;
            foreach (var row in PlateData)
                row.GhiChu = _plateNote;
        }

        private void FillShapeMaterial()
        {
            if (ShapeData == null || ShapeData.Count == 0 || string.IsNullOrEmpty(_shapeMaterial)) return;
            foreach (var row in ShapeData)
                row.VatLieu = _shapeMaterial;
        }

        private void FillShapeNote()
        {
            if (ShapeData == null || ShapeData.Count == 0) return;
            foreach (var row in ShapeData)
                row.GhiChu = _shapeNote;
        }

        private void FillSagRodMaterial()
        {
            if (_sagRodDataOriginal == null || _sagRodDataOriginal.Count == 0 || string.IsNullOrEmpty(_sagRodMaterial)) return;
            foreach (var row in _sagRodDataOriginal)
                row.VatLieu = _sagRodMaterial;
            SagRodData = new ObservableCollection<SagRodDataRow>(_sagRodDataOriginal);
        }

        private void FillSagRodNote()
        {
            if (_sagRodDataOriginal == null || _sagRodDataOriginal.Count == 0) return;
            foreach (var row in _sagRodDataOriginal)
                row.GhiChu = _sagRodNote;
            SagRodData = new ObservableCollection<SagRodDataRow>(_sagRodDataOriginal);
        }

        private void UpdatePurlinGrade()
        {
            if (_purlinDataOriginal == null || _purlinDataOriginal.Count == 0) return;
            foreach (var row in _purlinDataOriginal)
                row.CuongDo = PurlinGrade;
            PurlinData = new ObservableCollection<PurlinDataRow>(_purlinDataOriginal);
        }

        private void FillPurlinMaterial()
        {
            if (_purlinDataOriginal == null || _purlinDataOriginal.Count == 0 || string.IsNullOrEmpty(_purlinMaterial)) return;
            foreach (var row in _purlinDataOriginal)
                row.VatLieu = _purlinMaterial;
            PurlinData = new ObservableCollection<PurlinDataRow>(_purlinDataOriginal);
        }

        private void FillPurlinNote()
        {
            if (_purlinDataOriginal == null || _purlinDataOriginal.Count == 0) return;
            foreach (var row in _purlinDataOriginal)
                row.GhiChu = _purlinNote;
            PurlinData = new ObservableCollection<PurlinDataRow>(_purlinDataOriginal);
        }

        private bool HasData()
        {
            return (PlateData != null && PlateData.Count > 0)
                || (ShapeData != null && ShapeData.Count > 0)
                || (BoltData != null && BoltData.Count > 0)
                || (SagRodData != null && SagRodData.Count > 0)
                || (PurlinData != null && PurlinData.Count > 0);
        }

        public void OnCellEdited(object row, string propertyName)
        {
            if (row is PlateDataRow plateRow)
            {
                double plateVolume = plateRow.ChieuRong * plateRow.ChieuDai * plateRow.ChieuDay * 7850 / 1e9;
                if (plateVolume > 0)
                {
                    plateRow.RawSoLuong = plateRow.RawKhoiLuong / plateVolume;
                }
            }
            else if (row is ShapeDataRow shapeRow)
            {
                if (propertyName == nameof(ShapeDataRow.ChieuDai))
                {
                    shapeRow.RefreshDisplay();
                }
            }
        }
        #endregion

        #region License, Settings, Tekla Connection (existing - keep as is)
        private string _licenseStatusShort = "Chưa kích hoạt";
        public string LicenseStatusShort
        {
            get => _licenseStatusShort;
            set { _licenseStatusShort = value; OnPropertyChanged(); }
        }

        private void CheckLicenseStatus()
        {
            try
            {
                var (isValid, daysRemaining) = _licenseService.TryLoadLicenseFromFile();

                if (!isValid)
                {
                    LicenseStatusShort = "Chưa kích hoạt";
                }
                else if (daysRemaining == int.MaxValue)
                {
                    LicenseStatusShort = "Vĩnh viễn";
                }
                else if (daysRemaining > 30)
                {
                    LicenseStatusShort = $"Còn {daysRemaining} ngày";
                }
                else if (daysRemaining > 0)
                {
                    LicenseStatusShort = $"⚠ {daysRemaining} ngày";
                }
                else if (daysRemaining == 0)
                {
                    LicenseStatusShort = "⚠ Hết hạn hôm nay";
                }
                else
                {
                    LicenseStatusShort = $"✗ Đã hết hạn";
                }
            }
            catch
            {
                LicenseStatusShort = "Chưa kích hoạt";
            }
        }

        private void OpenLicenseWindow()
        {
            var licenseVm = new LicenseViewModel(_licenseService);
            var win = new LicenseWindow
            {
                DataContext = licenseVm,
                Owner = Application.Current.MainWindow
            };

            bool? result = win.ShowDialog();

            if (result == true)
            {
                CheckLicenseStatus();
            }
        }

        private void OpenSettingsWindow()
        {
            var settingsVm = new SettingsViewModel(_settingsService, _dataService);
            var win = new SettingsWindow
            {
                DataContext = settingsVm,
                Owner = Application.Current.MainWindow
            };

            bool? result = win.ShowDialog();

            if (result == true)
            {
                var newSettings = settingsVm.GetSettings();
                _settingsService.SaveUserSettings(newSettings);

                PlateWidth = newSettings.DefaultPlateWidth;
                PlateLength = newSettings.DefaultPlateLength;
                PlateMaterial = newSettings.DefaultPlateMaterial;
                ShapeLength = newSettings.DefaultShapeLength;
                ShapeMaterial = newSettings.DefaultShapeMaterial;
                BoltGrade = newSettings.DefaultBoltGrade;
                SagRodMaterial = newSettings.DefaultSagRodMaterial;
                PurlinGrade = newSettings.DefaultPurlinGrade;
                PurlinMaterial = newSettings.DefaultPurlinMaterial;

                RefreshAllDataDisplay();

                MessageBox.Show(
                    "✓ Đã lưu cài đặt và áp dụng giá trị mặc định! ",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void RefreshAllDataDisplay()
        {
            if (PlateData != null)
            {
                foreach (var row in PlateData)
                    row.RefreshDisplay();
            }

            if (ShapeData != null)
            {
                foreach (var row in ShapeData)
                    row.RefreshDisplay();
            }

            if (_sagRodDataOriginal != null)
            {
                foreach (var row in _sagRodDataOriginal)
                    row.RefreshDisplay();
            }

            if (_purlinDataOriginal != null)
            {
                foreach (var row in _purlinDataOriginal)
                    row.RefreshDisplay();
            }

            StatusMessage = "✓ Đã cập nhật hiển thị theo cài đặt mới! ";
        }

        private bool _isTeklaConnected;
        private string _teklaModelName = "Model: Chưa kết nối";

        public bool IsTeklaConnected
        {
            get => _isTeklaConnected;
            set
            {
                _isTeklaConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TeklaConnectionButtonText));
            }
        }

        public string TeklaModelName
        {
            get => _teklaModelName;
            set { _teklaModelName = value; OnPropertyChanged(); }
        }

        public string TeklaConnectionButtonText => IsTeklaConnected ? "✓ Connected" : "✗ Disconnected";

        private void CheckTeklaConnection()
        {
            try
            {
                _teklaConnectionService.RefreshConnection();
                bool isConnected = _teklaConnectionService.IsTeklaConnected();
                IsTeklaConnected = isConnected;

                if (isConnected)
                {
                    var modelInfo = _teklaConnectionService.GetModelInfo();
                    string modelName = string.IsNullOrEmpty(modelInfo?.ModelName)
                        ? "Chưa đặt tên"
                        : modelInfo.ModelName;

                    TeklaModelName = $"Model: {modelName}";
                    StatusMessage = $"✓ Đã kết nối Tekla - {modelName}";
                }
                else
                {
                    TeklaModelName = "Model: Chưa kết nối";
                    StatusMessage = "✗ Tekla chưa được mở hoặc chưa load model";
                }

                CommandManager.InvalidateRequerySuggested();
                _selectPartsCommand?.RaiseCanExecuteChanged();
                _selectAllModelCommand?.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                IsTeklaConnected = false;
                TeklaModelName = "Model: Lỗi kết nối";
                StatusMessage = "✗ Lỗi khi kiểm tra kết nối Tekla";
                System.Diagnostics.Debug.WriteLine($"Error checking Tekla: {ex.Message}");
            }
        }

        private async Task ExportExcelWithProjectInfo()
        {
            var projectVm = new ProjectInfoViewModel();
            var win = new ProjectInfoWindow
            {
                DataContext = projectVm,
                Owner = Application.Current.MainWindow
            };

            bool? result = win.ShowDialog();
            if (result != true) return;

            StatusMessage = "Đang xuất dữ liệu sang Excel...";

            try
            {
                var plateToExport = (projectVm.ExportAllSheets || projectVm.ExportPlate) && PlateData != null && PlateData.Count > 0
                    ? PlateData.ToList()
                    : new List<PlateDataRow>();

                var shapeToExport = (projectVm.ExportAllSheets || projectVm.ExportShape) && ShapeData != null && ShapeData.Count > 0
                    ? ShapeData.ToList()
                    : new List<ShapeDataRow>();

                var boltToExport = (projectVm.ExportAllSheets || projectVm.ExportBolt) && BoltData != null && BoltData.Count > 0
                    ? BoltData.ToList()
                    : new List<BoltDataRow>();

                var sagRodToExport = (projectVm.ExportAllSheets || projectVm.ExportSagRod) && _sagRodDataOriginal != null && _sagRodDataOriginal.Count > 0
                    ? _sagRodDataOriginal.ToList()
                    : new List<SagRodDataRow>();

                var purlinToExport = (projectVm.ExportAllSheets || projectVm.ExportPurlin) && _purlinDataOriginal != null && _purlinDataOriginal.Count > 0
                    ? _purlinDataOriginal.ToList()
                    : new List<PurlinDataRow>();

                string exportedFilePath = await _excelService.ExportToExcel(
                    plateToExport,
                    shapeToExport,
                    boltToExport,
                    purlinToExport,
                    sagRodToExport,
                    projectVm.ProjectName,
                    projectVm.Designer,
                    projectVm.FactoryAddress,
                    projectVm.DeliveryLocation,
                    projectVm.DeliveryScheduleDate.ToString(),
                    projectVm.LogoPath,
                    new Progress<string>(message => StatusMessage = message)
                );

                StatusMessage = "✓ Xuất Excel thành công!";

                if (!string.IsNullOrEmpty(exportedFilePath) && System.IO.File.Exists(exportedFilePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exportedFilePath,
                            UseShellExecute = true
                        });

                        MessageBox.Show(
                            $"✓ Xuất Excel thành công!\n\nFile đã được mở tự động: {System.IO.Path.GetFileName(exportedFilePath)}",
                            "Thành công",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch
                    {
                        MessageBox.Show(
                            $"✓ Xuất Excel thành công!\n\nFile: {exportedFilePath}",
                            "Thành công",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi xuất Excel: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusMessage = "✗ Lỗi khi xuất Excel";
            }
        }
        #endregion
    }
}