using JeremyAnsel.Xwa.Opt;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JeremyAnsel.Xwa.OptTransform.Wpf
{
    /// <summary>
    /// Logique d'interaction pour OptProfileSelectorControl.xaml
    /// </summary>
    public partial class OptProfileSelectorControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChangedEvent(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public OptProfileSelectorControl()
        {
            InitializeComponent();

            this.SelectedSkins.CollectionChanged += SelectedSkins_CollectionChanged;
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

        private bool showVersions = true;

        public bool ShowVersions
        {
            get
            {
                return this.showVersions;
            }

            set
            {
                if (value == this.showVersions)
                {
                    return;
                }

                this.showVersions = value;
                this.RaisePropertyChangedEvent(nameof(showVersions));
                selectorGrid.ColumnDefinitions[0].Width = value ? GridLength.Auto : new GridLength(0);
            }
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

        public void LoadOpt(string filename)
        {
            this.DataContext = null;
            this.OptFileName = null;
            this.OptVersions = null;
            this.OptObjectProfiles = null;
            this.OptSkins = null;

            OptFile optFile = OptFile.FromFile(filename, false);

            List<int> optVersions = Enumerable.Range(0, optFile.MaxTextureVersion).ToList();
            List<string> optObjectProfiles = OptTransformHelpers.GetObjectProfiles(filename).Keys.ToList();
            List<string> optSkins = OptTransformHelpers.GetSkins(filename);

            this.OptFileName = filename;
            this.OptVersions = optVersions;
            this.OptObjectProfiles = optObjectProfiles;
            this.OptSkins = optSkins;
            this.DataContext = this;
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
