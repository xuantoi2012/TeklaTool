using Newtonsoft.Json;
using System;
using System.IO;

namespace TeklaTool_2017.Services
{
    public class ProjectInfoService
    {
        private static readonly string _settingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TeklaTool_2017");

        private static readonly string _settingsFile = Path.Combine(_settingsFolder, "project_info.json");

        public class ProjectInfo
        {
            public string ProjectName { get; set; }
            public string Designer { get; set; }
            public string FactoryAddress { get; set; }
            public string DeliveryLocation { get; set; }
            public string DeliverySchedule { get; set; }
            public string LogoPath { get; set; }

            // Sheet selection
            public bool ExportAllSheets { get; set; } = true;
            public bool ExportPlate { get; set; } = true;
            public bool ExportShape { get; set; } = true;
            public bool ExportBolt { get; set; } = true;
            public bool ExportSagRod { get; set; } = true;
            public bool ExportPurlin { get; set; } = true;
        }

        /// <summary>
        /// Lưu thông tin dự án vào file JSON
        /// </summary>
        public void SaveProjectInfo(ProjectInfo info)
        {
            try
            {
                // Tạo folder nếu chưa có
                if (!Directory.Exists(_settingsFolder))
                {
                    Directory.CreateDirectory(_settingsFolder);
                }

                // Serialize và lưu
                string json = JsonConvert.SerializeObject(info, Formatting.Indented);
                File.WriteAllText(_settingsFile, json);
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Load thông tin dự án từ file JSON
        /// </summary>
        public ProjectInfo LoadProjectInfo()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    string json = File.ReadAllText(_settingsFile);
                    return JsonConvert.DeserializeObject<ProjectInfo>(json);
                }
            }
            catch
            {
                // Ignore errors
            }

            // Return default if no saved data
            return new ProjectInfo
            {
                ProjectName = "",
                Designer = "",
                DeliveryLocation = "",
                DeliverySchedule = "",
                LogoPath = ""
            };
        }
    }
}