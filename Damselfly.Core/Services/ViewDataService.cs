using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Service to maintain state around the toolbars - such as whether
    /// we show the folder list or not.
    /// </summary>
    public class ViewDataService : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _showfolder = true;

        public bool ShowFolderList
        {
            get => _showfolder;
            set
            {
                if (_showfolder != value)
                {
                    _showfolder = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showTags = true;

        public bool ShowTags
        {
            get => _showTags;
            set
            {
                if (_showTags != value)
                {
                    _showTags = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showBasket = true;

        public bool ShowBasket
        {
            get => _showBasket;
            set
            {
                if (_showBasket != value)
                {
                    _showBasket = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showExport = true;

        public bool ShowExport
        {
            get => _showExport;
            set
            {
                if (_showExport != value)
                {
                    _showExport = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showLogs = false;

        public bool ShowLogs
        {
            get => _showLogs;
            set
            {
                if (_showLogs != value)
                {
                    _showLogs = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
