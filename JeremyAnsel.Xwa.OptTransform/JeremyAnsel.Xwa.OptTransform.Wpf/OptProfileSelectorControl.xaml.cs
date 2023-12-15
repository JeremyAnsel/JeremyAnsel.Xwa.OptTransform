using JeremyAnsel.Xwa.Opt;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JeremyAnsel.Xwa.OptTransform.Wpf
{
    /// <summary>
    /// Logique d'interaction pour OptProfileSelectorControl.xaml
    /// </summary>
    public partial class OptProfileSelectorControl : UserControl
    {
        public OptProfileSelectorControl()
        {
            InitializeComponent();
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
                selectorGrid.ColumnDefinitions[0].Width = value ? GridLength.Auto : new GridLength(0);
            }
        }

        public string OptFileName { get; private set; }

        public List<int> OptVersions { get; private set; }

        public List<string> OptObjectProfiles { get; private set; }

        public List<string> OptSkins { get; private set; }

        public int SelectedVersion { get; set; } = 0;

        public string SelectedObjectProfile { get; set; } = "Default";

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
