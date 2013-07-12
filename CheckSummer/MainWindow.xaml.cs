using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
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
            set { _status = value; RaisePropertyChanged("Status");}
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        #endregion

        #region Fields

        private Task _calcTask;
        private readonly Stopwatch _stopwatch;
        private bool _calculating;

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
            if (_calculating)
            {
                MessageBox.Show("Please wait till Calculation has finished", "Calculation running", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                _calcTask = Task.Factory.StartNew(() =>
                    {
                        long calcedsize = 0;
                        var files2Calc = new List<String>();
                        try
                        {
                            _calculating = true;
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

                            foreach (var file in files2Calc)
                            {
                                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                                    Status = String.Format("Calculating {0}", file))); var checkfile = new CheckSummedFile(file);
                                checkfile.CalcCheckSums();
                                checkfile.Wait();
                                calcedsize += checkfile.FileSize;
                                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                                    CheckSummedFiles.Add(checkfile)));
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format("{0}\n{1}", ex.Message, ex.StackTrace),
                                "Error while calculating", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            _calculating = false;
                            _stopwatch.Stop();
                            var converter = new ByteConverter();
                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                                Status = String.Format("Ready! Time: {0} Size: {1} Count: {2}",
                                _stopwatch.Elapsed,
                                converter.Convert(calcedsize, null, null, null),
                                files2Calc.Count)));
                        }

                    });
            }
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_calculating)
                CheckSummedFiles.Clear();
        }
    }
}
