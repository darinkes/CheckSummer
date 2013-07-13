using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CheckSummer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Fields
        private ICollectionView _collectionView;
        private readonly MainWindowViewModel _mainWindowViewModel;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            _mainWindowViewModel = (MainWindowViewModel)DataContext;
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (_mainWindowViewModel.Calculating)
            {
                MessageBox.Show("Please wait till Calculation has finished", "Calculation running", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                _mainWindowViewModel.CalcChecksums((string[])e.Data.GetData(DataFormats.FileDrop, true));

            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_mainWindowViewModel.Calculating)
                _mainWindowViewModel.CheckSummedFiles.Clear();
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = sender as TextBox;
            _collectionView = CollectionViewSource.GetDefaultView(_mainWindowViewModel.CheckSummedFiles);
            _collectionView.Filter =
                w =>
                    {
                        var file = (CheckSummedFile) w;
                        return textbox != null && (file.Filename.ToLower().Contains(textbox.Text.ToLower()) ||
                                                   file.Md5.Contains(textbox.Text.ToLower()) ||
                                                   file.Sha1.Contains(textbox.Text.ToLower()) ||
                                                   file.Sha256.Contains(textbox.Text.ToLower()) ||
                                                   file.Sha512.Contains(textbox.Text.ToLower()));
                    };
        }

        private void ButtonBase_OnClick2(object sender, RoutedEventArgs e)
        {
            _mainWindowViewModel.Filter = "";
        }
    }
}
