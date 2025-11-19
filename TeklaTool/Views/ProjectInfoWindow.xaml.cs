using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TeklaTool.ViewModels;

namespace TeklaTool.Views
{
    public partial class ProjectInfoWindow : Window
    {
        private string _logoPath;

        public ProjectInfoWindow()
        {
            InitializeComponent();
        }

        public string LogoPath => _logoPath;

        private void BrowseLogo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Chọn logo công ty",
                    Filter = "Ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
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

                    _logoPath = openFileDialog.FileName;

                    // Update UI
                    LogoFileName.Text = Path.GetFileName(_logoPath);

                    // Load and display preview
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_logoPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 120; // Optimize for preview
                    bitmap.EndInit();
                    bitmap.Freeze();

                    LogoPreview.Source = bitmap;
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

            // Store logo path in ViewModel
            viewModel.LogoPath = _logoPath;

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