using JeremyAnsel.Xwa.Opt;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace JeremyAnsel.Xwa.OptTransform.Wpf
{
    /// <summary>
    /// Logique d'interaction pour OptProfileSelectorDialog.xaml
    /// </summary>
    public partial class OptProfileSelectorDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChangedEvent(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public OptProfileSelectorDialog(string optFileName, bool topmost = false)
        {
            InitializeComponent();

            this.SelectedSkins.CollectionChanged += SelectedSkins_CollectionChanged;

            Topmost = topmost;

            this.OptFileName = optFileName;

            OptFile optFile = OptFile.FromFile(optFileName, false);

            this.OptVersions = Enumerable.Range(0, optFile.MaxTextureVersion).ToList();
            this.OptObjectProfiles = OptTransformHelpers.GetObjectProfiles(OptFileName).Keys.ToList();
            this.OptSkins = OptTransformHelpers.GetSkins(OptFileName);

            this.DataContext = this;
        }

        private void SelectedSkins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
            {
                foreach (INotifyPropertyChanged added in e.NewItems)
                {
                    added.PropertyChanged += SelectedSkins_PropertyChanged;
                }
            }

            if (e.OldItems is not null)
            {
                foreach (INotifyPropertyChanged removed in e.OldItems)
                {
                    removed.PropertyChanged -= SelectedSkins_PropertyChanged;
                }
            }

            this.RaisePropertyChangedEvent(nameof(SelectedSkins));
            this.RaisePropertyChangedEvent(nameof(SelectedSkinsKeys));
        }

        private void SelectedSkins_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.RaisePropertyChangedEvent(nameof(SelectedSkins));
            this.RaisePropertyChangedEvent(nameof(SelectedSkinsKeys));
        }

        private string optFileName;

        public string OptFileName
        {
            get
            {
                return this.optFileName;
            }

            private set
            {
                if (value == this.optFileName)
                {
                    return;
                }

                this.optFileName = value;
                this.RaisePropertyChangedEvent(nameof(OptFileName));
            }
        }

        private List<int> optVersions;

        public List<int> OptVersions
        {
            get
            {
                return this.optVersions;
            }

            private set
            {
                if (value == this.optVersions)
                {
                    return;
                }

                this.optVersions = value;
                this.RaisePropertyChangedEvent(nameof(OptVersions));
            }
        }

        private List<string> optObjectProfiles;

        public List<string> OptObjectProfiles
        {
            get
            {
                return this.optObjectProfiles;
            }

            private set
            {
                if (value == this.optObjectProfiles)
                {
                    return;
                }

                this.optObjectProfiles = value;
                this.RaisePropertyChangedEvent(nameof(OptObjectProfiles));
            }
        }

        private List<string> optSkins;

        public List<string> OptSkins
        {
            get
            {
                return this.optSkins;
            }

            private set
            {
                if (value == this.optSkins)
                {
                    return;
                }

                this.optSkins = value;
                this.RaisePropertyChangedEvent(nameof(OptSkins));
            }
        }

        private int selectedVersion = 0;

        public int SelectedVersion
        {
            get
            {
                return this.selectedVersion;
            }

            set
            {
                if (value == this.selectedVersion)
                {
                    return;
                }

                this.selectedVersion = value;
                this.RaisePropertyChangedEvent(nameof(SelectedVersion));
            }
        }

        private string selectedObjectProfile = "Default";

        public string SelectedObjectProfile
        {
            get
            {
                return this.selectedObjectProfile;
            }

            set
            {
                if (value == this.selectedObjectProfile)
                {
                    return;
                }

                this.selectedObjectProfile = value;
                this.RaisePropertyChangedEvent(nameof(SelectedObjectProfile));
            }
        }

        public ObservableCollection<OptSkinItem> SelectedSkins { get; } = new();

        public List<string> SelectedSkinsKeys
        {
            get
            {
                return SelectedSkins
                    .Select(t => t.ToString())
                    .ToList();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {

            this.DialogResult = true;
        }

        private void ClearSelectedSkinsButton_Click(object sender, RoutedEventArgs e)
        {
            this.SelectedSkins.Clear();
        }

        private void AddSelectedSkinsButton_Click(object sender, RoutedEventArgs e)
        {
            this.AddSelectedSkin();
        }

        private void OptSkinsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.AddSelectedSkin();
        }

        private void AddSelectedSkin()
        {
            if (this.optSkinsListBox.SelectedItem is not string item)
            {
                return;
            }

            OptSkinItem pair = new(item, 100);

            this.SelectedSkins.Add(pair);
        }
    }
}
