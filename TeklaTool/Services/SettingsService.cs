using Newtonsoft.Json;
using System;
using System.IO;
using TeklaTool_2017.Models;

namespace TeklaTool_2017.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private UserSettings _currentSettings;

        public SettingsService()
        {
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TeklaTool_2017"
            );

            if (!Directory.Exists(appDataFolder))
                Directory.CreateDirectory(appDataFolder);

            _settingsFilePath = Path.Combine(appDataFolder, "settings.json");
            _currentSettings = LoadUserSettings();
        }

        // ✅ THAY ĐỔI THÀNH PUBLIC
        public UserSettings LoadUserSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<UserSettings>(json);

                    if (settings != null)
                    {
                        // Validate decimal places
                        if (settings.QuantityDecimalPlaces < 0 || settings.QuantityDecimalPlaces > 3)
                            settings.QuantityDecimalPlaces = 2;
                        if (settings.WeightDecimalPlaces < 0 || settings.WeightDecimalPlaces > 3)
                            settings.WeightDecimalPlaces = 2;

                        // Validate Plate defaults
                        if (string.IsNullOrWhiteSpace(settings.DefaultPlateWidth))
                            settings.DefaultPlateWidth = "1500";
                        if (string.IsNullOrWhiteSpace(settings.DefaultPlateLength))
                            settings.DefaultPlateLength = "6000";
                        if (string.IsNullOrWhiteSpace(settings.DefaultPlateMaterial))
                            settings.DefaultPlateMaterial = "SS400";

                        // Validate Shape defaults
                        if (string.IsNullOrWhiteSpace(settings.DefaultShapeLength))
                            settings.DefaultShapeLength = "12000";
                        if (string.IsNullOrWhiteSpace(settings.DefaultShapeMaterial))
                            settings.DefaultShapeMaterial = "SS400";

                        // Validate Bolt defaults
                        if (string.IsNullOrWhiteSpace(settings.DefaultBoltGrade))
                            settings.DefaultBoltGrade = "CB 8.8";

                        // ✅ THÊM: Validate SagRod defaults
                        if (string.IsNullOrWhiteSpace(settings.DefaultSagRodMaterial))
                            settings.DefaultSagRodMaterial = "Mạ kẽm";

                        // ✅ THÊM: Validate Purlin defaults
                        if (string.IsNullOrWhiteSpace(settings.DefaultPurlinGrade))
                            settings.DefaultPurlinGrade = "G450-Z80";
                        if (string.IsNullOrWhiteSpace(settings.DefaultPurlinMaterial))
                            settings.DefaultPurlinMaterial = "Mạ kẽm";

                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }

            return new UserSettings();
        }

        public void SaveUserSettings(UserSettings settings)
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
                _currentSettings = settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
                throw;
            }
        }

        // ✅ GET CURRENT SETTINGS
        public UserSettings GetCurrentSettings()
        {
            return _currentSettings ?? new UserSettings();
        }

        // Existing rounding methods
        public int GetQuantityDecimalPlaces() => _currentSettings?.QuantityDecimalPlaces ?? 2;
        public int GetWeightDecimalPlaces() => _currentSettings?.WeightDecimalPlaces ?? 2;

        public double RoundQuantity(double value, string type)
        {
            int decimalPlaces = GetQuantityDecimalPlaces();

            switch (type?.ToLower())
            {
                case "plate":
                    return Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);

                case "shape":
                case "sagrod":
                case "purlin":
                    return Math.Ceiling(value * Math.Pow(10, decimalPlaces)) / Math.Pow(10, decimalPlaces);

                default:
                    return Math.Round(value, decimalPlaces);
            }
        }

        public double RoundWeight(double value)
        {
            int decimalPlaces = GetWeightDecimalPlaces();
            return Math.Round(value, decimalPlaces);
        }

        public double RoundLength(double value)
        {
            return Math.Round(value, 0);
        }
    }
}