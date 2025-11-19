using System;
using System.IO;
using System.Windows;
using TeklaTool.Services;
using TeklaTool.Views;
using TeklaTool.ViewModels;

namespace TeklaTool
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var licenseService = new LicenseService();

                // Check if license file exists
                string licenseFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TeklaTool");
                string licenseFile = Path.Combine(licenseFolder, "license.lic");

                bool canProceed = false;

                // ALWAYS use the SAME instance for checks and activation
                var (isValid, daysRemaining) = licenseService.TryLoadLicenseFromFile();

                if (isValid)
                {
                    if (daysRemaining == int.MaxValue)
                    {
                        // Permanent license
                        canProceed = true;
                    }
                    else if (daysRemaining > 0)
                    {
                        // Valid with days remaining
                        canProceed = true;

                        if (daysRemaining <= 7)
                        {
                            MessageBox.Show(
                                $"Cảnh báo: Bản quyền sẽ hết hạn trong {daysRemaining} ngày.\nVui lòng liên hệ để gia hạn.",
                                "Cảnh báo bản quyền",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        // License expired
                        MessageBox.Show(
                            $"Bản quyền đã hết hạn {Math.Abs(daysRemaining)} ngày trước.\nVui lòng nhập mã kích hoạt mới.",
                            "Bản quyền hết hạn",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        canProceed = false;
                    }
                }
                else
                {
                    if (File.Exists(licenseFile))
                    {
                        // License file exists but invalid
                        MessageBox.Show(
                            "File bản quyền không hợp lệ hoặc bị hỏng.\nVui lòng nhập lại mã kích hoạt.",
                            "Lỗi bản quyền",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Chào mừng bạn đến với Tekla Material Takeoff Tool!\n\n" +
                            "Vui lòng nhập mã kích hoạt để sử dụng phần mềm.",
                            "Kích hoạt bản quyền",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    canProceed = false;
                }

                // If can't proceed, show license window
                if (!canProceed)
                {
                    var licenseWindow = new LicenseWindow();
                    var licenseViewModel = new LicenseViewModel(licenseService);
                    licenseWindow.DataContext = licenseViewModel;
                    var result = licenseWindow.ShowDialog();

                    if (result == true)
                    {
                        // Reload after activation
                        var (newValid, newDays) = licenseService.TryLoadLicenseFromFile();

                        if (!newValid)
                        {
                            MessageBox.Show(
                                "Không thể xác thực bản quyền sau khi lưu.\nVui lòng thử lại.",
                                "Lỗi bản quyền",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            Shutdown();
                            return;
                        }

                        if (newDays != int.MaxValue && newDays <= 0)
                        {
                            MessageBox.Show(
                                $"Bản quyền đã hết hạn.\nVui lòng liên hệ để gia hạn.",
                                "Bản quyền hết hạn",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            Shutdown();
                            return;
                        }

                        // OK, proceed to main window
                        // --
                    }
                    else
                    {
                        MessageBox.Show(
                            "Bạn chưa kích hoạt bản quyền.\nỨng dụng sẽ đóng.",
                            "Chưa kích hoạt",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        Shutdown();
                        return;
                    }
                }

                // License is valid, show main window
                var mainWindow = new MainWindow();
                mainWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi khởi động ứng dụng:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Lỗi nghiêm trọng",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}