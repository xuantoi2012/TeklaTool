using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TeklaTool.Models
{
    public class PlateDataRow : INotifyPropertyChanged
    {
        private int _stt;
        private double _chieuDay;
        private double _chieuRong;
        private double _chieuDai;
        private double _soLuong;
        private double _khoiLuong;
        private string _vatLieu;

        public int STT
        {
            get => _stt;
            set
            {
                _stt = value;
                OnPropertyChanged();
            }
        }

        public double ChieuDay
        {
            get => _chieuDay;
            set
            {
                _chieuDay = value;
                OnPropertyChanged();
            }
        }

        public double ChieuRong
        {
            get => _chieuRong;
            set
            {
                _chieuRong = value;
                OnPropertyChanged();
            }
        }

        public double ChieuDai
        {
            get => _chieuDai;
            set
            {
                _chieuDai = value;
                OnPropertyChanged();
            }
        }

        public double SoLuong
        {
            get => _soLuong;
            set
            {
                _soLuong = value;
                OnPropertyChanged();
            }
        }

        public double KhoiLuong
        {
            get => _khoiLuong;
            set
            {
                _khoiLuong = value;
                OnPropertyChanged();
            }
        }

        public string VatLieu
        {
            get => _vatLieu;
            set
            {
                _vatLieu = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ShapeDataRow : INotifyPropertyChanged
    {
        private int _stt;
        private string _quyCach;
        private double _chieuDai;
        private double _soLuong;
        private double _khoiLuongDonVi;
        private double _khoiLuong;
        private string _vatLieu;

        public int STT
        {
            get => _stt;
            set
            {
                _stt = value;
                OnPropertyChanged();
            }
        }

        public string QuyCach
        {
            get => _quyCach;
            set
            {
                _quyCach = value;
                OnPropertyChanged();
            }
        }

        public double ChieuDai
        {
            get => _chieuDai;
            set
            {
                _chieuDai = value;
                OnPropertyChanged();
            }
        }

        public double SoLuong
        {
            get => _soLuong;
            set
            {
                _soLuong = value;
                OnPropertyChanged();
            }
        }

        public double KhoiLuongDonVi
        {
            get => _khoiLuongDonVi;
            set
            {
                _khoiLuongDonVi = value;
                OnPropertyChanged();
            }
        }

        public double KhoiLuong
        {
            get => _khoiLuong;
            set
            {
                _khoiLuong = value;
                OnPropertyChanged();
            }
        }

        public string VatLieu
        {
            get => _vatLieu;
            set
            {
                _vatLieu = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BoltDataRow : INotifyPropertyChanged
    {
        private int _stt;
        private string _kichThuoc;
        private string _cuongDo;
        private int _soLuong;

        public int STT
        {
            get => _stt;
            set
            {
                _stt = value;
                OnPropertyChanged();
            }
        }

        public string KichThuoc
        {
            get => _kichThuoc;
            set
            {
                _kichThuoc = value;
                OnPropertyChanged();
            }
        }

        public string CuongDo
        {
            get => _cuongDo;
            set
            {
                _cuongDo = value;
                OnPropertyChanged();
            }
        }

        public int SoLuong
        {
            get => _soLuong;
            set
            {
                _soLuong = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PurlinDataRow : INotifyPropertyChanged
    {
        private int _stt;
        private string _assembly;
        private string _quyCach;
        private double _soLuong;
        private double _chieuDai;
        private double _khoiLuongMotCauKien;
        private double _khoiLuong;
        private string _vatLieu;

        public int STT
        {
            get => _stt;
            set
            {
                _stt = value;
                OnPropertyChanged();
            }
        }

        public string Assembly
        {
            get => _assembly;
            set
            {
                _assembly = value;
                OnPropertyChanged();
            }
        }

        public string QuyCach
        {
            get => _quyCach;
            set
            {
                _quyCach = value;
                OnPropertyChanged();
            }
        }

        public double SoLuong
        {
            get => _soLuong;
            set
            {
                _soLuong = value;
                OnPropertyChanged();
            }
        }

        public double ChieuDai
        {
            get => _chieuDai;
            set
            {
                _chieuDai = value;
                OnPropertyChanged();
            }
        }

        public double KhoiLuongMotCauKien
        {
            get => _khoiLuongMotCauKien;
            set
            {
                _khoiLuongMotCauKien = value;
                OnPropertyChanged();
            }
        }

        public double KhoiLuong
        {
            get => _khoiLuong;
            set
            {
                _khoiLuong = value;
                OnPropertyChanged();
            }
        }

        public string VatLieu
        {
            get => _vatLieu;
            set
            {
                _vatLieu = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}