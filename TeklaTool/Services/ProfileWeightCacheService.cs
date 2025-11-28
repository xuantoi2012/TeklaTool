using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TeklaTool_2017.Services
{
    public class ProfileWeightCacheService
    {
        private readonly string _cacheFilePath;
        private Dictionary<string, double> _cache;

        public ProfileWeightCacheService()
        {
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TeklaTool_2017"
            );

            if (!Directory.Exists(appDataFolder))
                Directory.CreateDirectory(appDataFolder);

            _cacheFilePath = Path.Combine(appDataFolder, "profile_weights_cache.json");
            _cache = LoadCache();
        }

        private Dictionary<string, double> LoadCache()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    string json = File.ReadAllText(_cacheFilePath);
                    var cache = JsonConvert.DeserializeObject<Dictionary<string, double>>(json);
                    System.Diagnostics.Debug.WriteLine($"✓ Loaded {cache?.Count ?? 0} cached profile weights");
                    return cache ?? new Dictionary<string, double>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profile cache: {ex.Message}");
            }

            return new Dictionary<string, double>();
        }

        private void SaveCache()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                File.WriteAllText(_cacheFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile cache: {ex.Message}");
            }
        }

        public double? GetWeight(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return null;

            if (_cache.TryGetValue(profileName, out double weight))
                return weight;

            return null;
        }

        public void SaveWeight(string profileName, double weight)
        {
            if (string.IsNullOrWhiteSpace(profileName) || weight <= 0)
                return;

            _cache[profileName] = weight;
            SaveCache();
        }

        public Dictionary<string, double> GetAllCachedWeights()
        {
            return new Dictionary<string, double>(_cache);
        }

        public void ClearAllCachedWeights()
        {
            _cache.Clear();
            SaveCache();
        }

        public int GetCacheCount()
        {
            return _cache.Count;
        }

        public string GetCacheFilePath()
        {
            return _cacheFilePath;
        }

        public void ReloadCache()
        {
            _cache = LoadCache();
        }

        public void ExportToCSV(string filePath)
        {
            try
            {
                var lines = new List<string> { "Profile,Weight (kg/m)" };
                lines.AddRange(_cache.OrderBy(x => x.Key).Select(x => $"{x.Key},{x.Value}"));
                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting cache: {ex.Message}");
            }
        }

        public void ImportFromCSV(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                int imported = 0;

                foreach (var line in lines.Skip(1)) // Skip header
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2 && double.TryParse(parts[1], out double weight))
                    {
                        _cache[parts[0].Trim()] = weight;
                        imported++;
                    }
                }

                SaveCache();
                System.Diagnostics.Debug.WriteLine($"✓ Imported {imported} profiles from CSV");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing cache: {ex.Message}");
            }
        }

        public void BackupCache(string backupPath)
        {
            try
            {
                File.Copy(_cacheFilePath, backupPath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error backing up cache: {ex.Message}");
            }
        }
    }
}