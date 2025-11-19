using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TeklaTool.ViewModels
{
    public class ProjectInfoViewModel : INotifyPropertyChanged
    {
        private string _projectName;
        private string _designer;
        private string _deliveryLocation;
        private string _logoPath;

        public string ProjectName
        {
            get => _projectName;
            set
            {
                _projectName = value;
                OnPropertyChanged();
            }
        }

        public string Designer
        {
            get => _designer;
            set
            {
                _designer = value;
                OnPropertyChanged();
            }
        }

        public string DeliveryLocation
        {
            get => _deliveryLocation;
            set
            {
                _deliveryLocation = value;
                OnPropertyChanged();
            }
        }

        public string LogoPath
        {
            get => _logoPath;
            set
            {
                _logoPath = value;
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