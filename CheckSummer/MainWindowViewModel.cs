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
    class MainWindowViewModel : INotifyPropertyChanged
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
        #endregion

        public MainWindowViewModel()
        {
            CheckSummedFiles = new ObservableCollection<CheckSummedFile>();
            _stopwatch = new Stopwatch();
        }

        internal void CalcChecksums(string[] filenames)
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

                    foreach (var filename in filenames)
                    {
                        var attr = File.GetAttributes(filename);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                            files2Calc.AddRange(Directory.GetFiles(filename, "*.*",
                                SearchOption.AllDirectories));
                        else
                            files2Calc.Add(filename);
                    }

                    for (var index = 0; index < files2Calc.Count; index++)
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
                            new Action(() =>
                            {
                                CheckSummedFiles.Add(checkfile);
                                Progress = (index1 / (double)files2Calc.Count) * 100;
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
    }
}
