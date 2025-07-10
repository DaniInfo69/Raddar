using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Avisen.Models
{
    public class Categoria : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int idcategoria { get; set; }
        public string Nombre { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}