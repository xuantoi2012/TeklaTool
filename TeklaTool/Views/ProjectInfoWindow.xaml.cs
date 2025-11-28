using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using TeklaTool_2017.ViewModels;

namespace TeklaTool_2017.Views
{
    public partial class ProjectInfoWindow : Window
    {
        public ProjectInfoWindow()
        {
            InitializeComponent();

            // ✅ Load logo preview sau khi DataContext được set
            Loaded += ProjectInfoWindow_Loaded;
        }

        private void ProjectInfoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ Tự động hiển thị logo đã lưu từ JSON (nếu có)
            if (DataContext is ProjectInfoViewModel vm)
            {
                // ViewModel đã tự load logo preview rồi, chỉ cần update UI text
                if (!string.IsNullOrWhiteSpace(vm.LogoPath))
                {
                    LogoFileName.Text = Path.GetFileName(vm.LogoPath);
                }
            }
        }

        private void BrowseLogo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Chọn logo công ty",
                    Filter = "Ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Check file size (max 5MB)
                    FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show(
                            "File quá lớn! Vui lòng chọn file nhỏ hơn 5MB.",
                            "Lỗi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // ✅ Update ViewModel (sẽ tự động trigger LoadLogoPreview())
                    if (DataContext is ProjectInfoViewModel vm)
                    {
                        vm.LogoPath = openFileDialog.FileName;

                        // Update UI text
                        LogoFileName.Text = Path.GetFileName(openFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi tải logo:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ProjectInfoViewModel;
            if (viewModel == null)
            {
                DialogResult = false;
                return;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(viewModel.ProjectName))
            {
                MessageBox.Show(
                    "Vui lòng nhập tên công trình!",
                    "Thiếu thông tin",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(viewModel.Designer))
            {
                MessageBox.Show(
                    "Vui lòng nhập tên người thiết kế!",
                    "Thiếu thông tin",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // ✅ Lưu thông tin vào JSON
            viewModel.SaveCurrentInfo();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}