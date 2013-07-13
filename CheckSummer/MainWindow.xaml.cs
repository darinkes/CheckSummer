using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace CheckSummer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Properties
        public ObservableCollection<CheckSummedFile> CheckSummedFiles { get; private set; }

        private string _status;

        public string Status
        {
            get { return _status; }
            set { _status = value; RaisePropertyChanged("Status"); }
        }

        private double _progress;

        public double Progress
        {
            get { return _progress; }
            set { _progress = value; RaisePropertyChanged("Progress"); }
        }

        private bool _calculating;

        public bool Calculating
        {
            get { return _calculating; }
            set { _calculating = value; RaisePropertyChanged("Calculating"); }
        }

        private string _filter;

        public string Filter
        {
            get { return _filter; }
            set { _filter = value; RaisePropertyChanged("Filter"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        #endregion

        #region Fields
        private readonly Stopwatch _stopwatch;
        private ICollectionView _collectionView;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            CheckSummedFiles = new ObservableCollection<CheckSummedFile>();
            Status = "Ready!";
            _stopwatch = new Stopwatch();
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (Calculating)
            {
                MessageBox.Show("Please wait till Calculation has finished", "Calculation running", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                Task.Factory.StartNew(() =>
                    {
                        long calcedsize = 0;
                        var files2Calc = new List<String>();
                        try
                        {
                            Calculating = true;
                            Progress = 0;
                            _stopwatch.Start();
                            var filenames =
                                (string[]) e.Data.GetData(DataFormats.FileDrop, true);
                            foreach (var filename in filenames)
                            {
                                FileAttributes attr = File.GetAttributes(filename);
                                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                                    files2Calc.AddRange(Directory.GetFiles(filename, "*.*",
                                        SearchOption.AllDirectories));
                                else
                                    files2Calc.Add(filename);
                            }

                            for (int index = 0; index < files2Calc.Count; index++)
                            {
                                var file = files2Calc[index];
                                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                    new Action(() =>
                                        Status = String.Format("Calculating {0}", file)));
                                var checkfile = new CheckSummedFile(file);
                                checkfile.CalcCheckSums();
                                checkfile.Wait();
                                calcedsize += checkfile.FileSize;
                                int index1 = index;
                                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                    new Action(() => {
                                            CheckSummedFiles.Add(checkfile);
                                            Progress = (index1/(double)files2Calc.Count) * 100;
                                            if (DataGrid.SelectedItem == null)
                                                DataGrid.SelectedItem = checkfile;
                                    }));
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format("{0}\n{1}", ex.Message, ex.StackTrace),
                                "Error while calculating", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            Calculating = false;
                            _stopwatch.Stop();
                            var converter = new ByteConverter();
                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    Status = String.Format("Ready! Time: {0} Size: {1} Count: {2}",
                                        _stopwatch.Elapsed,
                                        converter.Convert(calcedsize, null, null, null),
                                        files2Calc.Count);
                                }));
                        }
                    });
            }
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Calculating)
                CheckSummedFiles.Clear();
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = sender as TextBox;
            _collectionView = CollectionViewSource.GetDefaultView(CheckSummedFiles);
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
            Filter = "";
        }
    }
}
