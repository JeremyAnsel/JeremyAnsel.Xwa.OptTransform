using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace JeremyAnsel.Xwa.OptTransform.Wpf
{
    public sealed class OptSkinItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public OptSkinItem()
        {
        }

        public OptSkinItem(string name)
        {
            this.name = name;
        }

        public OptSkinItem(string name, int opacity)
        {
            opacity = Math.Min(Math.Max(opacity, 0), 100);

            this.name = name;
            this.opacity = opacity;
        }

        private string name = string.Empty;

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (value != name)
                {
                    name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        private int opacity = 100;

        public int Opacity
        {
            get
            {
                return opacity;
            }

            set
            {
                if (value != opacity)
                {
                    value = Math.Min(Math.Max(value, 0), 100);

                    opacity = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Opacity)));
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(this.Name);

            if (this.Opacity != 100)
            {
                sb.Append('-');
                sb.Append(this.Opacity.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }
    }
}
