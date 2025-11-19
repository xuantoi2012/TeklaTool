using System.Windows;
using System.Windows.Controls;
using TeklaTool.ViewModels;
using TeklaTool.Views;
using TeklaTool.Services; // Cần thêm using này để truy cập LicenseService

namespace TeklaTool
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid != null)
                {
                    // Get the edited item
                    var editedItem = e.Row.Item;
                    var column = e.Column as DataGridBoundColumn;

                    if (column != null)
                    {
                        // Schedule recalculation after the edit is complete
                        Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            var binding = column.Binding as System.Windows.Data.Binding;
                            if (binding != null)
                            {
                                // Đảm bảo MainViewModel có phương thức OnCellEdited
                                // và editedItem là kiểu dữ liệu mà MainViewModel mong đợi
                                _viewModel.OnCellEdited(editedItem, binding.Path.Path);
                            }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
            }
        }

        private void OpenLicenseWindow_Click(object sender, RoutedEventArgs e)
        {
            // 1. Tạo instance của LicenseService (hoặc lấy từ IoC container nếu có)
            var licenseService = new LicenseService();

            // 2. Sử dụng hàm tạo mới của LicenseViewModel (nhận 1 tham số)
            var licenseViewModel = new LicenseViewModel(licenseService);

            var licenseWindow = new LicenseWindow();
            licenseWindow.DataContext = licenseViewModel;
            licenseWindow.Owner = this;

            // 3. Hiển thị cửa sổ
            licenseWindow.ShowDialog();

            // 4. Nếu cửa sổ kích hoạt thành công, bạn có thể muốn refresh trạng thái license 
            // trong MainViewModel hoặc hiển thị lại thông báo.
            if (licenseWindow.DialogResult == true)
            {
                MessageBox.Show("Kích hoạt bản quyền thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Thêm logic cập nhật trạng thái license trong MainViewModel nếu cần
            }
        }
    }
}