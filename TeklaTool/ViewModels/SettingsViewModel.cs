using System.Windows;
using System.Windows.Input;
using TeklaTool_2017.Helpers;
using TeklaTool_2017.Models;
using TeklaTool_2017.Services;

namespace TeklaTool_2017.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _settingsService;
        private readonly TeklaDataService _dataService;

        #region Properties
        private int _quantityDecimalPlaces;
        public int QuantityDecimalPlaces
        {
            get => _quantityDecimalPlaces;
            set
            {
                if (value < 0) value = 0;
                if (value > 3) value = 3;
                _quantityDecimalPlaces = value;
                OnPropertyChanged();
            }
        }

        private int _weightDecimalPlaces;
        public int WeightDecimalPlaces
        {
            get => _weightDecimalPlaces;
            set
            {
                if (value < 0) value = 0;
                if (value > 3) value = 3;
                _weightDecimalPlaces = value;
                OnPropertyChanged();
            }
        }

        // Plate
        private string _defaultPlateWidth = "1500";
        public string DefaultPlateWidth
        {
            get => _defaultPlateWidth;
            set { _defaultPlateWidth = value; OnPropertyChanged(); }
        }

        private string _defaultPlateLength = "6000";
        public string DefaultPlateLength
        {
            get => _defaultPlateLength;
            set { _defaultPlateLength = value; OnPropertyChanged(); }
        }

        private string _defaultPlateMaterial = "SS400";
        public string DefaultPlateMaterial
        {
            get => _defaultPlateMaterial;
            set { _defaultPlateMaterial = value; OnPropertyChanged(); }
        }

        // Shape
        private string _defaultShapeLength = "12000";
        public string DefaultShapeLength
        {
            get => _defaultShapeLength;
            set { _defaultShapeLength = value; OnPropertyChanged(); }
        }

        private string _defaultShapeMaterial = "SS400";
        public string DefaultShapeMaterial
        {
            get => _defaultShapeMaterial;
            set { _defaultShapeMaterial = value; OnPropertyChanged(); }
        }

        // Bolt
        private string _defaultBoltGrade = "8.8";
        public string DefaultBoltGrade
        {
            get => _defaultBoltGrade;
            set { _defaultBoltGrade = value; OnPropertyChanged(); }
        }

        // ✅ THÊM: SagRod
        private string _defaultSagRodMaterial = "Mạ kẽm";
        public string DefaultSagRodMaterial
        {
            get => _defaultSagRodMaterial;
            set { _defaultSagRodMaterial = value; OnPropertyChanged(); }
        }

        // ✅ THÊM: Purlin
        private string _defaultPurlinGrade = "G450";
        public string DefaultPurlinGrade
        {
            get => _defaultPurlinGrade;
            set { _defaultPurlinGrade = value; OnPropertyChanged(); }
        }

        private string _defaultPurlinMaterial = "Mạ kẽm";
        public string DefaultPurlinMaterial
        {
            get => _defaultPurlinMaterial;
            set { _defaultPurlinMaterial = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand ResetToDefaultsCommand { get; }
        #endregion

        public SettingsViewModel(SettingsService settingsService, TeklaDataService dataService)
        {
            _settingsService = settingsService;
            _dataService = dataService;

            // Load current settings
            var settings = _settingsService.LoadUserSettings();
            QuantityDecimalPlaces = settings.QuantityDecimalPlaces;
            WeightDecimalPlaces = settings.WeightDecimalPlaces;
            DefaultPlateWidth = settings.DefaultPlateWidth;
            DefaultPlateLength = settings.DefaultPlateLength;
            DefaultPlateMaterial = settings.DefaultPlateMaterial;
            DefaultShapeLength = settings.DefaultShapeLength;
            DefaultShapeMaterial = settings.DefaultShapeMaterial;
            DefaultBoltGrade = settings.DefaultBoltGrade;
            // ✅ THÊM
            DefaultSagRodMaterial = settings.DefaultSagRodMaterial;
            DefaultPurlinGrade = settings.DefaultPurlinGrade;
            DefaultPurlinMaterial = settings.DefaultPurlinMaterial;

            ResetToDefaultsCommand = new RelayCommand(_ => ResetToDefaults());
        }

        private void ResetToDefaults()
        {
            var result = MessageBox.Show(
                "Bạn có chắc muốn đặt lại tất cả cài đặt về mặc định?\n\n" +
                "• Số thập phân: 2 (số lượng), 2 (khối lượng)\n" +
                "• Thép tấm: 1500 x 6000 mm, SS400\n" +
                "• Thép hình: 12000 mm, SS400\n" +
                "• Bulong: CB 8.8\n" +
                "• Ty xà gồ: Mạ kẽm\n" +
                "• Xà gồ: G450-Z80, Mạ kẽm",
                "Xác nhận đặt lại",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                QuantityDecimalPlaces = 2;
                WeightDecimalPlaces = 2;
                DefaultPlateWidth = "1500";
                DefaultPlateLength = "6000";
                DefaultPlateMaterial = "SS400";
                DefaultShapeLength = "12000";
                DefaultShapeMaterial = "SS400";
                DefaultBoltGrade = "CB 8.8";
                // ✅ THÊM
                DefaultSagRodMaterial = "Mạ kẽm";
                DefaultPurlinGrade = "G450-Z80";
                DefaultPurlinMaterial = "Mạ kẽm";

                MessageBox.Show(
                    "✓ Đã đặt lại tất cả cài đặt về mặc định!",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        public UserSettings GetSettings()
        {
            return new UserSettings
            {
                QuantityDecimalPlaces = QuantityDecimalPlaces,
                WeightDecimalPlaces = WeightDecimalPlaces,
                DefaultPlateWidth = DefaultPlateWidth,
                DefaultPlateLength = DefaultPlateLength,
                DefaultPlateMaterial = DefaultPlateMaterial,
                DefaultShapeLength = DefaultShapeLength,
                DefaultShapeMaterial = DefaultShapeMaterial,
                DefaultBoltGrade = DefaultBoltGrade,
                // ✅ THÊM
                DefaultSagRodMaterial = DefaultSagRodMaterial,
                DefaultPurlinGrade = DefaultPurlinGrade,
                DefaultPurlinMaterial = DefaultPurlinMaterial
            };
        }
    }
}