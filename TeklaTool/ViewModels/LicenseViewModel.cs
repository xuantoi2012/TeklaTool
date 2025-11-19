using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TeklaTool.Services;
using TeklaTool.Helpers;

namespace TeklaTool.ViewModels
{
    public class LicenseViewModel : BaseViewModel
    {
        private readonly LicenseService _licenseService;
        private string _machineIdentity;
        private string _licenseKey;
        private string _licenseStatus = "Chưa kiểm tra bản quyền";

        public string MachineIdentity
        {
            get => _machineIdentity;
            set { _machineIdentity = value; OnPropertyChanged(); }
        }

        public string LicenseKey
        {
            get => _licenseKey;
            set { _licenseKey = value; OnPropertyChanged(); }
        }

        public string LicenseStatus
        {
            get => _licenseStatus;
            set { _licenseStatus = value; OnPropertyChanged(); }
        }

        public ICommand CopyIdCommand { get; }
        public ICommand CheckLicenseCommand { get; }

        // Constructor with LicenseService parameter
        public LicenseViewModel(LicenseService licenseService)
        {
            _licenseService = licenseService ?? new LicenseService();
            MachineIdentity = _licenseService.GetMachineIdentity();

            // If a saved license exists and is valid at startup, update status immediately
            try
            {
                var (loaded, days) = _licenseService.TryLoadLicenseFromFile();
                if (loaded)
                {
                    if (days == int.MaxValue)
                        LicenseStatus = "✓ Đã kích hoạt bản quyền (vĩnh viễn)";
                    else if (days > 0)
                        LicenseStatus = $"✓ Đã kích hoạt bản quyền. Còn {days} ngày";
                    else if (days == 0)
                        LicenseStatus = "⚠ Key hết hạn hôm nay";
                    else
                        LicenseStatus = $"✗ Key đã hết hạn {-days} ngày trước";
                }
            }
            catch { /* ignore */ }

            CopyIdCommand = new RelayCommand(_ => CopyId());
            CheckLicenseCommand = new RelayCommand(_ => CheckKey());
        }

        // Parameterless constructor for XAML designer
        public LicenseViewModel() : this(new LicenseService())
        {
        }

        private void CopyId()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(MachineIdentity))
                {
                    Clipboard.SetText(MachineIdentity);
                    LicenseStatus = "✓ Đã copy mã máy vào clipboard!";
                }
            }
            catch (Exception ex)
            {
                LicenseStatus = $"✗ Lỗi khi copy: {ex.Message}";
            }
        }

        private void CheckKey()
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(LicenseKey))
                {
                    LicenseStatus = "✗ Vui lòng nhập mã kích hoạt";
                    MessageBox.Show("Vui lòng nhập mã kích hoạt!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(MachineIdentity))
                {
                    LicenseStatus = "✗ Không thể lấy mã máy";
                    MessageBox.Show("Không thể lấy thông tin máy!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Normalize and validate key format
                string normalizedKey = LicenseKey.Replace("-", "").Replace(" ", "").Trim();
                if (normalizedKey.Length < 8)
                {
                    LicenseStatus = "✗ Key không hợp lệ (quá ngắn)";
                    MessageBox.Show("Key không hợp lệ! Key phải có ít nhất 8 ký tự.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LicenseStatus = "⏳ Đang kiểm tra key...";

                // Validate license key
                var (isValid, daysRemaining) = _licenseService.ValidateLicenseKey(LicenseKey, MachineIdentity);

                if (!isValid)
                {
                    LicenseStatus = "✗ Key không hợp lệ cho máy này";
                    MessageBox.Show(
                        $"Key không hợp lệ!\n\n" +
                        $"Key: {LicenseKey}\n" +
                        $"Machine ID: {MachineIdentity}\n\n" +
                        $"Vui lòng kiểm tra lại key hoặc liên hệ hỗ trợ.",
                        "Key không hợp lệ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Key is valid - now check expiry
                DateTime? expireDate = null;
                string statusMessage = "";
                bool allowAccess = false;

                if (daysRemaining == int.MaxValue)
                {
                    // Permanent license
                    expireDate = null;
                    statusMessage = "✓ Đã kích hoạt bản quyền (vĩnh viễn)";
                    allowAccess = true;
                }
                else if (daysRemaining > 0)
                {
                    // Valid with days remaining
                    expireDate = DateTime.Now.Date.AddDays(daysRemaining);
                    statusMessage = $"✓ Đã kích hoạt bản quyền. Còn {daysRemaining} ngày";
                    allowAccess = true;
                }
                else if (daysRemaining == 0)
                {
                    // Expires today - still allow access but warn
                    expireDate = DateTime.Now.Date;
                    statusMessage = "⚠ Key hết hạn hôm nay";
                    allowAccess = true; // Allow access on expiry day
                }
                else
                {
                    // Already expired
                    expireDate = DateTime.Now.Date.AddDays(daysRemaining);
                    statusMessage = $"✗ Key đã hết hạn {-daysRemaining} ngày trước";
                    allowAccess = false;
                }

                LicenseStatus = statusMessage;

                // Save license file (even if expired, for record)
                try
                {
                    _licenseService.SaveLicenseToFile(LicenseKey, MachineIdentity, expireDate);

                    // DEBUG: Verify file was created
                    string licenseFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "TeklaTool");
                    string licenseFile = System.IO.Path.Combine(licenseFolder, "license.lic");

                    if (System.IO.File.Exists(licenseFile))
                    {
                        LicenseStatus = $"✓ File đã lưu: {licenseFile}";
                    }
                    else
                    {
                        throw new Exception($"File không được tạo tại: {licenseFile}");
                    }
                }
                catch (Exception ex)
                {
                    LicenseStatus = $"✗ Lỗi lưu file: {ex.Message}";
                    MessageBox.Show(
                        $"Không thể lưu license file!\n\n" +
                        $"Lỗi: {ex.Message}\n\n" +
                        $"Đường dẫn: {System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TeklaTool", "license.lic")}\n\n" +
                        $"Vui lòng chạy ứng dụng với quyền Administrator.",
                        "Lỗi lưu file",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return; // Don't proceed if file save failed
                }

                // Show result message
                string expiryText = expireDate.HasValue ? expireDate.Value.ToString("dd/MM/yyyy") : "Vĩnh viễn";

                if (allowAccess)
                {
                    MessageBox.Show(
                        $"✓ Kích hoạt thành công!\n\n" +
                        $"Machine ID: {MachineIdentity}\n" +
                        $"Ngày hết hạn: {expiryText}\n" +
                        $"{(daysRemaining == int.MaxValue ? "Bản quyền vĩnh viễn" : $"Còn {daysRemaining} ngày")}",
                        "Kích hoạt thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Close window with success
                    var win = Application.Current.Windows.OfType<Window>()
                        .FirstOrDefault(w => w.DataContext == this);

                    if (win != null)
                    {
                        win.DialogResult = true; // Allow app to proceed
                        win.Close();
                    }
                }
                else
                {
                    // Key valid but expired
                    MessageBox.Show(
                        $"Key đã hết hạn!\n\n" +
                        $"Machine ID: {MachineIdentity}\n" +
                        $"Ngày hết hạn: {expiryText}\n" +
                        $"Đã hết hạn {-daysRemaining} ngày trước\n\n" +
                        $"Vui lòng liên hệ để gia hạn.",
                        "Key hết hạn",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // Don't close window - let user enter new key
                }
            }
            catch (Exception ex)
            {
                LicenseStatus = $"✗ Lỗi: {ex.Message}";
                MessageBox.Show(
                    $"Lỗi khi kiểm tra key!\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}