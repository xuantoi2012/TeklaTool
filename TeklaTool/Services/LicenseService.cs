using Newtonsoft.Json;
using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace TeklaTool_2017.Services
{
    public class LicenseService
    {
        private const string Secret = "TeklaSuperSecret2024";
        private static readonly string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        private const string LicenseFolderName = "TeklaTool_2017";
        private const string LicenseFileName = "license.lic";

        #region Machine identity getters

        public string GetHarddiskSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID = 'C:'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string s = obj["VolumeSerialNumber"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                            return s;
                    }
                }
            }
            catch { }

            return "UnknownDiskSerial";
        }

        public string GetCpuId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string s = obj["ProcessorId"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                            return s;
                    }
                }
            }
            catch { }

            return "UnknownCPU";
        }

        public string GetMainboardSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string s = obj["SerialNumber"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                            return s;
                    }
                }
            }
            catch { }

            return "UnknownBoard";
        }

        public string GetMachineIdentity()
        {
            string d = GetHarddiskSerial();
            if (!d.Contains("Unknown"))
                return $"DISK-{d}";

            string cpu = GetCpuId();
            if (!cpu.Contains("Unknown"))
                return $"CPU-{cpu}";

            string b = GetMainboardSerial();
            if (!b.Contains("Unknown"))
                return $"BOARD-{b}";

            return "UNKNOWN-ID";
        }

        #endregion

        #region Validate license key

        public (bool isValid, int daysRemaining) ValidateLicenseKey(
            string inputKey,
            string machineId,
            int maxForwardDays = 3650,
            int maxBackwardDays = 365)
        {
            if (string.IsNullOrWhiteSpace(inputKey) || string.IsNullOrWhiteSpace(machineId))
                return (false, int.MinValue);

            string normalized = NormalizeKey(inputKey);

            if (normalized.Length != 16)
                return (false, int.MinValue);

            // Permanent key
            if (CheckKeyAgainstExpire(machineId, normalized, ""))
                return (true, int.MaxValue);

            DateTime today = DateTime.Now.Date;

            // Search forward
            for (int d = 0; d <= maxForwardDays; d++)
            {
                string exp = today.AddDays(d).ToString("yyyy-MM-dd");

                if (CheckKeyAgainstExpire(machineId, normalized, exp))
                    return (true, d);
            }

            // Search backward
            for (int d = 1; d <= maxBackwardDays; d++)
            {
                string exp = today.AddDays(-d).ToString("yyyy-MM-dd");

                if (CheckKeyAgainstExpire(machineId, normalized, exp))
                    return (true, -d);
            }

            return (false, int.MinValue);
        }

        #endregion

        #region Save/Load license file

        public void SaveLicenseToFile(string licenseKey, string machineId, DateTime? expireDate)
        {
            try
            {
                var model = new LicenseFileModel
                {
                    Key = NormalizeKey(licenseKey),
                    MachineId = machineId,
                    Expire = expireDate?.ToString("yyyy-MM-dd") ?? "",
                    Created = DateTime.UtcNow
                };

                model.Signature = ComputeHmacSignature(
                    model.Key + "|" + model.MachineId + "|" + model.Expire
                );

                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    LicenseFolderName);

                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, LicenseFileName);

                string json = JsonConvert.SerializeObject(model, Formatting.Indented);

                File.WriteAllText(filePath, json, Encoding.UTF8);

                // Verify file written & not empty
                if (!File.Exists(filePath))
                {
                    throw new UnauthorizedAccessException(
                        $"Không thể ghi file: {filePath}\n" +
                        "File không tồn tại sau khi ghi.");
                }
                string readBack = File.ReadAllText(filePath, Encoding.UTF8);
                if (string.IsNullOrEmpty(readBack))
                {
                    throw new UnauthorizedAccessException(
                        $"File bị rỗng sau khi ghi: {filePath}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let the specific error bubble up
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Lỗi không xác định khi lưu license:\n{ex.Message}", ex);
            }
        }

        public (bool loadedAndValid, int daysRemaining) TryLoadLicenseFromFile()
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    LicenseFolderName);

                string path = Path.Combine(folder, LicenseFileName);

                if (!File.Exists(path))
                    return (false, int.MinValue);

                string json = File.ReadAllText(path, Encoding.UTF8);
                LicenseFileModel model = JsonConvert.DeserializeObject<LicenseFileModel>(json);

                if (model == null)
                    return (false, int.MinValue);

                string expectedSig = ComputeHmacSignature(
                    model.Key + "|" + model.MachineId + "|" + model.Expire
                );

                if (!string.Equals(expectedSig, model.Signature, StringComparison.Ordinal))
                    return (false, int.MinValue);

                if (!string.Equals(model.MachineId, GetMachineIdentity(), StringComparison.OrdinalIgnoreCase))
                    return (false, int.MinValue);

                return ValidateLicenseKey(model.Key, model.MachineId);
            }
            catch
            {
                return (false, int.MinValue);
            }
        }

        #endregion

        #region Internal helpers

        private static string NormalizeKey(string key)
        {
            if (key == null) return "";
            return key.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
        }

        private static bool CheckKeyAgainstExpire(string id, string key16, string expire)
        {
            return string.Equals(
                key16,
                GenerateRawKey(id, expire),
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static string GenerateRawKey(string id, string expire)
        {
            string exp = string.IsNullOrWhiteSpace(expire) ? "" : expire;
            string input = id + "|" + exp + "|" + Secret;

            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));

                string base32 = ToBase32(hash);

                if (base32.Length >= 16)
                    return base32.Substring(0, 16);

                return base32;
            }
        }

        private static string ToBase32(byte[] data)
        {
            if (data == null || data.Length == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            int buffer = 0, bitsLeft = 0;

            foreach (byte b in data)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;

                while (bitsLeft >= 5)
                {
                    sb.Append(Base32Chars[(buffer >> (bitsLeft - 5)) & 31]);
                    bitsLeft -= 5;
                }
            }

            if (bitsLeft > 0)
            {
                sb.Append(Base32Chars[(buffer << (5 - bitsLeft)) & 31]);
            }

            return sb.ToString();
        }

        private static string ComputeHmacSignature(string data)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(Secret);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data ?? "");

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] sig = hmac.ComputeHash(dataBytes);
                return Convert.ToBase64String(sig);
            }
        }

        private class LicenseFileModel
        {
            public string Key { get; set; }
            public string MachineId { get; set; }
            public string Expire { get; set; }
            public string Signature { get; set; }
            public DateTime Created { get; set; }
        }

        #endregion
    }
}
