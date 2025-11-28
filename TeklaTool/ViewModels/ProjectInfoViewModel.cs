using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using TeklaTool_2017.Services;

namespace TeklaTool_2017.ViewModels
{
    public class ProjectInfoViewModel : INotifyPropertyChanged
    {
        private readonly ProjectInfoService _projectInfoService;

        private string _projectName;
        private string _designer;
        private string _factoryAddress;
        private string _deliveryLocation;
        private DateTime? _deliveryScheduleDate;
        private string _logoPath;
        private BitmapImage _logoPreview;

        // Sheet selection
        private bool _exportAllSheets = true;
        private bool _exportPlate = true;
        private bool _exportShape = true;
        private bool _exportBolt = true;
        private bool _exportSagRod = true;
        private bool _exportPurlin = true;

        public string ProjectName
        {
            get => _projectName;
            set { _projectName = value; OnPropertyChanged(); }
        }

        public string Designer
        {
            get => _designer;
            set { _designer = value; OnPropertyChanged(); }
        }
        public string FactoryAddress
        {
            get => _factoryAddress;
            set { _factoryAddress = value; OnPropertyChanged(); }
        }

        public string DeliveryLocation
        {
            get => _deliveryLocation;
            set { _deliveryLocation = value; OnPropertyChanged(); }
        }

        public DateTime? DeliveryScheduleDate
        {
            get => _deliveryScheduleDate;
            set { _deliveryScheduleDate = value; OnPropertyChanged(); }
        }

        public string LogoPath
        {
            get => _logoPath;
            set
            {
                _logoPath = value;
                OnPropertyChanged();
                LoadLogoPreview(); // ✅ Auto load preview khi path thay đổi
            }
        }

        // ✅ Property để binding vào Image.Source
        public BitmapImage LogoPreview
        {
            get => _logoPreview;
            set
            {
                _logoPreview = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasLogo));
            }
        }

        // ✅ Property để show/hide preview
        public bool HasLogo => LogoPreview != null;

        // ✅ NEW: Sheet selection properties
        public bool ExportAllSheets
        {
            get => _exportAllSheets;
            set
            {
                _exportAllSheets = value;
                OnPropertyChanged();
                if (value)
                {
                    ExportPlate = ExportShape = ExportBolt = ExportSagRod = ExportPurlin = true;
                }
            }
        }

        public bool ExportPlate
        {
            get => _exportPlate;
            set { _exportPlate = value; OnPropertyChanged(); }
        }

        public bool ExportShape
        {
            get => _exportShape;
            set { _exportShape = value; OnPropertyChanged(); }
        }

        public bool ExportBolt
        {
            get => _exportBolt;
            set { _exportBolt = value; OnPropertyChanged(); }
        }

        public bool ExportSagRod
        {
            get => _exportSagRod;
            set { _exportSagRod = value; OnPropertyChanged(); }
        }

        public bool ExportPurlin
        {
            get => _exportPurlin;
            set { _exportPurlin = value; OnPropertyChanged(); }
        }
        public ProjectInfoViewModel()
        {
            _projectInfoService = new ProjectInfoService();
            LoadSavedInfo();
        }

        private void LoadSavedInfo()
        {
            var savedInfo = _projectInfoService.LoadProjectInfo();

            ProjectName = savedInfo.ProjectName;
            Designer = savedInfo.Designer;
            FactoryAddress = savedInfo.FactoryAddress;
            DeliveryLocation = savedInfo.DeliveryLocation;
            if (DateTime.TryParse(savedInfo.DeliverySchedule, out DateTime date))
            {
                DeliveryScheduleDate = date;
            }
            LogoPath = savedInfo.LogoPath; // ✅ Tự động trigger LoadLogoPreview()

            // Load sheet selection
            ExportAllSheets = savedInfo.ExportAllSheets;
            ExportPlate = savedInfo.ExportPlate;
            ExportShape = savedInfo.ExportShape;
            ExportBolt = savedInfo.ExportBolt;
            ExportSagRod = savedInfo.ExportSagRod;
            ExportPurlin = savedInfo.ExportPurlin;
        }

        /// <summary>
        /// Load logo preview từ path
        /// </summary>
        private void LoadLogoPreview()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(LogoPath) && System.IO.File.Exists(LogoPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new System.Uri(LogoPath, System.UriKind.Absolute);
                    bitmap.EndInit();

                    LogoPreview = bitmap;
                }
                else
                {
                    LogoPreview = null;
                }
            }
            catch
            {
                LogoPreview = null;
            }
        }

        public void SaveCurrentInfo()
        {
            var info = new ProjectInfoService.ProjectInfo
            {
                ProjectName = ProjectName,
                Designer = Designer,
                FactoryAddress = FactoryAddress,
                DeliveryLocation = DeliveryLocation,
                DeliverySchedule = DeliveryScheduleDate?.ToString("dd/MM/yyyy") ?? "",
                LogoPath = LogoPath,
                ExportAllSheets = ExportAllSheets,
                ExportPlate = ExportPlate,
                ExportShape = ExportShape,
                ExportBolt = ExportBolt,
                ExportSagRod = ExportSagRod,
                ExportPurlin = ExportPurlin
            };

            _projectInfoService.SaveProjectInfo(info);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}