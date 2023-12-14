using JeremyAnsel.Xwa.Opt;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace JeremyAnsel.Xwa.OptTransform.Wpf
{
    /// <summary>
    /// Logique d'interaction pour OptProfileSelectorDialog.xaml
    /// </summary>
    public partial class OptProfileSelectorDialog : Window
    {
        public OptProfileSelectorDialog(string optFileName, bool topmost = false)
        {
            InitializeComponent();

            Topmost = topmost;

            this.OptFileName = optFileName;

            OptFile optFile = OptFile.FromFile(optFileName, false);

            this.OptVersions = Enumerable.Range(0, optFile.MaxTextureVersion).ToList();
            this.OptObjectProfiles = OptTransformHelpers.GetObjectProfiles(OptFileName).Keys.ToList();
            this.OptSkins = OptTransformHelpers.GetSkins(OptFileName);

            this.DataContext = this;
        }

        public string OptFileName { get; }

        public List<int> OptVersions { get; }

        public List<string> OptObjectProfiles { get; }

        public List<string> OptSkins { get; }

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
