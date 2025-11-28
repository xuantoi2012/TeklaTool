// Models/DataRowModels.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TeklaTool_2017.Services;

namespace TeklaTool_2017.Models
{
    #region Base DataRow
    /// <summary>
    /// ✅ Base class cho tất cả DataRow models
    /// Quản lý Raw values và Display formatting
    /// </summary>
    public abstract class BaseDataRow : INotifyPropertyChanged
    {
        protected readonly SettingsService _settingsService;

        protected BaseDataRow(SettingsService settingsService = null)
        {
            _settingsService = settingsService ?? new SettingsService();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// ✅ Refresh tất cả display values
        /// </summary>
        public abstract void RefreshDisplay();

        /// <summary>
        /// ✅ Làm tròn số lượng theo loại vật liệu
        /// </summary>
        protected double RoundQuantity(double value, string materialType)
        {
            // ✅ THAY ĐỔI: Dùng method có sẵn từ SettingsService
            return _settingsService.RoundQuantity(value, materialType);
        }

        /// <summary>
        /// ✅ Làm tròn khối lượng
        /// </summary>
        protected double RoundWeight(double value)
        {
            // ✅ THAY ĐỔI: Dùng method có sẵn từ SettingsService
            return _settingsService.RoundWeight(value);
        }

        /// <summary>
        /// ✅ Làm tròn chiều dài (luôn 0 thập phân)
        /// </summary>
        protected double RoundLength(double value)
        {
            // ✅ THAY ĐỔI: Dùng method có sẵn từ SettingsService
            return _settingsService.RoundLength(value);
        }
    }
    #endregion

    #region Plate DataRow
    public class PlateDataRow : BaseDataRow
    {
        private int _stt;
        private double _chieuDay;
        private double _chieuRong;
        private double _chieuDai;
        private double _rawSoLuong;
        private double _rawKhoiLuong;
        private string _vatLieu;
        private string _ghiChu;

        public int STT
        {
            get => _stt;
            set { _stt = value; OnPropertyChanged(); }
        }

        public double ChieuDay
        {
            get => _chieuDay;
            set { _chieuDay = value; OnPropertyChanged(); }
        }

        public double ChieuRong
        {
            get => _chieuRong;
            set
            {
                _chieuRong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoLuong)); // Recalc số lượng
            }
        }

        public double ChieuDai
        {
            get => _chieuDai;
            set
            {
                _chieuDai = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoLuong)); // Recalc số lượng
            }
        }

        // ✅ RAW Số lượng
        public double RawSoLuong
        {
            get => _rawSoLuong;
            set
            {
                _rawSoLuong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoLuong));
            }
        }

        // ✅ DISPLAY Số lượng
        public double SoLuong => RoundQuantity(_rawSoLuong, "Plate");

        // ✅ RAW Khối lượng
        public double RawKhoiLuong
        {
            get => _rawKhoiLuong;
            set
            {
                _rawKhoiLuong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KhoiLuong));
            }
        }

        // ✅ DISPLAY Khối lượng
        public double KhoiLuong => RoundWeight(_rawKhoiLuong);

        public string VatLieu
        {
            get => _vatLieu;
            set { _vatLieu = value; OnPropertyChanged(); }
        }

        public string GhiChu
        {
            get => _ghiChu;
            set { _ghiChu = value; OnPropertyChanged(); }
        }

        public PlateDataRow(SettingsService settingsService = null) : base(settingsService)
        {
        }

        public override void RefreshDisplay()
        {
            OnPropertyChanged(nameof(SoLuong));
            OnPropertyChanged(nameof(KhoiLuong));
        }
    }
    #endregion

    #region Shape DataRow
    public class ShapeDataRow : BaseDataRow
    {
        private int _stt;
        private string _quyCach;
        private double _chieuDai;
        private double _rawTongChieuDai;
        private double _rawKhoiLuongDonVi;
        private double _rawKhoiLuong;
        private string _vatLieu;
        private string _ghiChu;

        public int STT
        {
            get => _stt;
            set { _stt = value; OnPropertyChanged(); }
        }

        public string QuyCach
        {
            get => _quyCach;
            set { _quyCach = value; OnPropertyChanged(); }
        }

        public double ChieuDai
        {
            get => _chieuDai;
            set
            {
                _chieuDai = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoLuong)); // Recalc số lượng
            }
        }

        // ✅ RAW Tổng chiều dài
        public double RawTongChieuDai
        {
            get => _rawTongChieuDai;
            set
            {
                _rawTongChieuDai = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TongChieuDai));
                OnPropertyChanged(nameof(SoLuong));
            }
        }

        // ✅ DISPLAY Tổng chiều dài (0 thập phân)
        public double TongChieuDai => RoundLength(_rawTongChieuDai);

        // ✅ DISPLAY Số lượng (Shape luôn round up)
        public double SoLuong
        {
            get
            {
                double value = _chieuDai > 0 ? (_rawTongChieuDai / _chieuDai) : 0;
                return RoundQuantity(value, "Shape");
            }
        }

        // ✅ RAW Khối lượng đơn vị
        public double RawKhoiLuongDonVi
        {
            get => _rawKhoiLuongDonVi;
            set
            {
                _rawKhoiLuongDonVi = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KhoiLuongDonVi));
            }
        }

        // ✅ DISPLAY Khối lượng đơn vị
        public double KhoiLuongDonVi => RoundWeight(_rawKhoiLuongDonVi);

        // ✅ RAW Khối lượng
        public double RawKhoiLuong
        {
            get => _rawKhoiLuong;
            set
            {
                _rawKhoiLuong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KhoiLuong));
            }
        }

        // ✅ DISPLAY Khối lượng
        public double KhoiLuong => RoundWeight(_rawKhoiLuong);

        public string VatLieu
        {
            get => _vatLieu;
            set { _vatLieu = value; OnPropertyChanged(); }
        }

        public string GhiChu
        {
            get => _ghiChu;
            set { _ghiChu = value; OnPropertyChanged(); }
        }

        public ShapeDataRow(SettingsService settingsService = null) : base(settingsService)
        {
        }

        public override void RefreshDisplay()
        {
            OnPropertyChanged(nameof(TongChieuDai));
            OnPropertyChanged(nameof(SoLuong));
            OnPropertyChanged(nameof(KhoiLuongDonVi));
            OnPropertyChanged(nameof(KhoiLuong));
        }
    }
    #endregion

    #region Bolt DataRow
    public class BoltDataRow : BaseDataRow
    {
        private int _stt;
        private string _kichThuoc;
        private double _chieuDai;
        private string _cuongDo;
        private int _soLuong;
        private string _ghiChu;

        public int STT
        {
            get => _stt;
            set { _stt = value; OnPropertyChanged(); }
        }

        public string KichThuoc
        {
            get => _kichThuoc;
            set { _kichThuoc = value; OnPropertyChanged(); }
        }

        public double ChieuDai
        {
            get => _chieuDai;
            set { _chieuDai = value; OnPropertyChanged(); }
        }

        public string CuongDo
        {
            get => _cuongDo;
            set { _cuongDo = value; OnPropertyChanged(); }
        }

        public int SoLuong
        {
            get => _soLuong;
            set { _soLuong = value; OnPropertyChanged(); }
        }

        public string GhiChu
        {
            get => _ghiChu;
            set { _ghiChu = value; OnPropertyChanged(); }
        }

        public BoltDataRow(SettingsService settingsService = null) : base(settingsService)
        {
        }

        public override void RefreshDisplay()
        {
            // Bolt không cần refresh vì luôn số nguyên
        }
    }
    #endregion

    #region SagRod DataRow
    public class SagRodDataRow : BaseDataRow
    {
        private int _stt;
        private string _assembly;
        private string _quyCach;
        private double _rawSoLuong;
        private double _rawChieuDai;
        private double _rawKhoiLuongMotCauKien;
        private double _rawKhoiLuong;
        private string _vatLieu;
        private string _ghiChu;

        public int STT
        {
            get => _stt;
            set { _stt = value; OnPropertyChanged(); }
        }

        public string Assembly
        {
            get => _assembly;
            set { _assembly = value; OnPropertyChanged(); }
        }

        public string QuyCach
        {
            get => _quyCach;
            set { _quyCach = value; OnPropertyChanged(); }
        }

        // ✅ RAW Số lượng
        public double RawSoLuong
        {
            get => _rawSoLuong;
            set
            {
                _rawSoLuong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoLuong));
            }
        }

        // ✅ DISPLAY Số lượng (SagRod luôn round up)
        public double SoLuong => RoundQuantity(_rawSoLuong, "SagRod");

        // ✅ RAW Chiều dài
        public double RawChieuDai
        {
            get => _rawChieuDai;
            set
            {
                _rawChieuDai = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChieuDai));
            }
        }

        // ✅ DISPLAY Chiều dài (0 thập phân)
        public double ChieuDai => RoundLength(_rawChieuDai);

        // ✅ RAW Khối lượng 1 cấu kiện
        public double RawKhoiLuongMotCauKien
        {
            get => _rawKhoiLuongMotCauKien;
            set
            {
                _rawKhoiLuongMotCauKien = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KhoiLuongMotCauKien));
            }
        }

        // ✅ DISPLAY Khối lượng 1 cấu kiện
        public double KhoiLuongMotCauKien => RoundWeight(_rawKhoiLuongMotCauKien);

        // ✅ RAW Khối lượng
        public double RawKhoiLuong
        {
            get => _rawKhoiLuong;
            set
            {
                _rawKhoiLuong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KhoiLuong));
            }
        }

        // ✅ DISPLAY Khối lượng
        public double KhoiLuong => RoundWeight(_rawKhoiLuong);

        public string VatLieu
        {
            get => _vatLieu;
            set { _vatLieu = value; OnPropertyChanged(); }
        }

        public string GhiChu
        {
            get => _ghiChu;
            set { _ghiChu = value; OnPropertyChanged(); }
        }

        public SagRodDataRow(SettingsService settingsService = null) : base(settingsService)
        {
        }

        public override void RefreshDisplay()
        {
            OnPropertyChanged(nameof(SoLuong));
            OnPropertyChanged(nameof(ChieuDai));
            OnPropertyChanged(nameof(KhoiLuongMotCauKien));
            OnPropertyChanged(nameof(KhoiLuong));
        }
    }
    #endregion

    #region Purlin DataRow
    public class PurlinDataRow : BaseDataRow
    {
        private int _stt;
        private string _assembly;
        private string _quyCach;
        private double _rawSoLuong;
        private double _rawChieuDai;
        private double _rawKhoiLuongMotCauKien;
        private double _rawKhoiLuongDonVi; // ✅ THÊM RAW khối lượng đơn vị
        private double _rawKhoiLuong;
        private string _vatLieu;
        private string _cuongDo;
        private string _ghiChu;

        public int STT
        {
            get => _stt;
            set { _stt = value; OnPropertyChanged(); }
        }

        public string Assembly
        {
            get => _assembly;
            set { _assembly = value; OnPropertyChanged(); }
        }

        public string QuyCach
        {
            get => _quyCach;
            set { _quyCach = value; OnPropertyChanged(); }
        }

        // ✅ RAW Số lượng
        public double RawSoLuong
        {
            get => _rawSoLuong;
            set
            {
                _rawSoLuong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoLuong));
            }
        }

        // ✅ DISPLAY Số lượng (Purlin luôn round up)
        public double SoLuong => RoundQuantity(_rawSoLuong, "Purlin");

        // ✅ RAW Chiều dài
        public double RawChieuDai
        {
            get => _rawChieuDai;
            set
            {
                _rawChieuDai = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChieuDai));
            }
        }

        // ✅ DISPLAY Chiều dài (0 thập phân)
        public double ChieuDai => RoundLength(_rawChieuDai);

        // ✅ RAW Khối lượng 1 cấu kiện
        public double RawKhoiLuongMotCauKien
        {
            get => _rawKhoiLuongMotCauKien;
            set
            {
                _rawKhoiLuongMotCauKien = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KhoiLuongMotCauKien));
            }
        }

        // ✅ DISPLAY Khối lượng 1 cấu kiện
        public double KhoiLuongMotCauKien => RoundWeight(_rawKhoiLuongMotCauKien);

        // ✅ RAW Khối lượng đơn vị (kg/m)
        public double RawKhoiLuongDonVi
        {
            get => _rawKhoiLuongDonVi;
            set
            {
                _rawKhoiLuongDonVi = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KhoiLuongDonVi));
            }
        }

        // ✅ DISPLAY Khối lượng đơn vị (kg/m)
        public double KhoiLuongDonVi => RoundWeight(_rawKhoiLuongDonVi);

        // ✅ RAW Khối lượng
        public double RawKhoiLuong
        {
            get => _rawKhoiLuong;
            set
            {
                _rawKhoiLuong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KhoiLuong));
            }
        }

        // ✅ DISPLAY Khối lượng
        public double KhoiLuong => RoundWeight(_rawKhoiLuong);

        public string VatLieu
        {
            get => _vatLieu;
            set { _vatLieu = value; OnPropertyChanged(); }
        }

        public string CuongDo
        {
            get => _cuongDo;
            set { _cuongDo = value; OnPropertyChanged(); }
        }

        public string GhiChu
        {
            get => _ghiChu;
            set { _ghiChu = value; OnPropertyChanged(); }
        }

        public PurlinDataRow(SettingsService settingsService = null) : base(settingsService)
        {
        }

        public override void RefreshDisplay()
        {
            OnPropertyChanged(nameof(SoLuong));
            OnPropertyChanged(nameof(ChieuDai));
            OnPropertyChanged(nameof(KhoiLuongMotCauKien));
            OnPropertyChanged(nameof(KhoiLuongDonVi)); // ✅ THÊM
            OnPropertyChanged(nameof(KhoiLuong));
        }
    }
    #endregion
}