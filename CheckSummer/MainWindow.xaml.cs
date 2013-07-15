using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using WPFLocalizeExtension.Engine;

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

            Properties.Settings.Default.Upgrade();

            if (String.IsNullOrEmpty(Properties.Settings.Default["Language"].ToString()))
            {
                _mainWindowViewModel.Language =
                    _mainWindowViewModel.Languages.FirstOrDefault(
                        l => l.CultureName == Thread.CurrentThread.CurrentUICulture.ToString());
            }
            else
            {
                _mainWindowViewModel.Language =
                    _mainWindowViewModel.Languages.FirstOrDefault(
                        l => l.ConfigName == Properties.Settings.Default["Language"].ToString());
            }

            var args = new List<String>(Environment.GetCommandLineArgs());
            if (args.Any())
            {
                args.RemoveAt(0);
                _mainWindowViewModel.CalcChecksums(args.ToArray());
            }
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (_mainWindowViewModel.Calculating)
            {
                MessageBox.Show(
                    Properties.Resources.MainWindow_MainWindow_OnDrop_Please_wait_till_Calculation_has_finished,
                    Properties.Resources.MainWindow_MainWindow_OnDrop_Calculation_running, MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                _mainWindowViewModel.CalcChecksums((string[]) e.Data.GetData(DataFormats.FileDrop, true));

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

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combobox = sender as ComboBox;
            if (combobox == null)
                return;
            var language = combobox.SelectedItem as Language;
            if (language == null)
                return;
            if (language.CultureName == LocalizeDictionary.Instance.Culture.ToString())
                return;
            LocalizeDictionary.Instance.Culture = CultureInfo.GetCultureInfo(language.CultureName);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _mainWindowViewModel.SetShortcut();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void MenuItemExit_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void MenuItemOpen_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();

            var result = dlg.ShowDialog();

            if (result.Value)
            {
                _mainWindowViewModel.CalcChecksums(dlg.FileName);
            }
        }
    }
}