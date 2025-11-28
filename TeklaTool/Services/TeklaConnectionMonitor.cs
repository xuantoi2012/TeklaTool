using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

namespace TeklaTool_2017.Services
{
    public class TeklaConnectionMonitor : INotifyPropertyChanged, IDisposable
    {
        private readonly TeklaConnectionService _connectionService;
        private readonly Timer _monitorTimer;
        private bool _isConnected;
        private string _modelName = "Chưa kết nối";
        private bool _isMonitoring;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> ConnectionStatusChanged;

        #region Properties
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                    ConnectionStatusChanged?.Invoke(this, value);
                }
            }
        }

        public string ModelName
        {
            get => _modelName;
            private set
            {
                if (_modelName != value)
                {
                    _modelName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            private set
            {
                _isMonitoring = value;
                OnPropertyChanged();
            }
        }

        // ✅ Text hiển thị (Connected/Disconnected)
        public string StatusText => IsConnected ? "Connected" : "Disconnected";

        // ✅ Màu cho indicator dot
        public string StatusColor => IsConnected ? "#10B981" : "#EF4444"; // Green / Red

        // ✅ Icon/Emoji
        public string StatusIcon => IsConnected ? "✓" : "✗";
        #endregion

        #region Constructor
        public TeklaConnectionMonitor(TeklaConnectionService connectionService, int intervalSeconds = 3)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));

            // ✅ Timer để check connection định kỳ (mặc định 3 giây)
            _monitorTimer = new Timer(intervalSeconds * 1000)
            {
                AutoReset = true
            };
            _monitorTimer.Elapsed += OnTimerElapsed;

            // Check ngay lần đầu
            CheckConnection();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Bắt đầu theo dõi connection
        /// </summary>
        public void Start()
        {
            if (!IsMonitoring)
            {
                _monitorTimer.Start();
                IsMonitoring = true;
                System.Diagnostics.Debug.WriteLine("🟢 TeklaConnectionMonitor Started");
            }
        }

        /// <summary>
        /// Dừng theo dõi connection
        /// </summary>
        public void Stop()
        {
            if (IsMonitoring)
            {
                _monitorTimer.Stop();
                IsMonitoring = false;
                System.Diagnostics.Debug.WriteLine("🔴 TeklaConnectionMonitor Stopped");
            }
        }

        /// <summary>
        /// Force check connection ngay lập tức
        /// </summary>
        public void CheckConnection()
        {
            try
            {
                _connectionService.RefreshConnection();
                bool isConnected = _connectionService.IsTeklaConnected();

                IsConnected = isConnected;

                if (isConnected)
                {
                    var modelInfo = _connectionService.GetModelInfo();
                    ModelName = string.IsNullOrEmpty(modelInfo?.ModelName)
                        ? "Chưa đặt tên"
                        : modelInfo.ModelName;

                    System.Diagnostics.Debug.WriteLine($"✓ Tekla Connected: {ModelName}");
                }
                else
                {
                    ModelName = "Chưa kết nối";
                    System.Diagnostics.Debug.WriteLine("✗ Tekla Disconnected");
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ModelName = "Lỗi kết nối";
                System.Diagnostics.Debug.WriteLine($"❌ Connection Check Error: {ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CheckConnection();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Stop();
            _monitorTimer?.Dispose();
        }
        #endregion
    }
}