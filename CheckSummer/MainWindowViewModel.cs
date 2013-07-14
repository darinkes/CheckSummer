using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CheckSummer.Properties;
using Microsoft.Win32;

namespace CheckSummer
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Properties
        public ObservableCollection<CheckSummedFile> CheckSummedFiles { get; private set; }

        public List<Language> Languages { get; private set; }

        private Language _language;

        public Language Language
        {
            get { return _language; }
            set
            {
                _language = value;
                Settings.Default["Language"] = Language.ConfigName;
                Settings.Default.Save();
                RaisePropertyChanged("Language");
            }
        }

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

        private TimeSpan _time;

        public TimeSpan Time
        {
            get { return _time; }
            set { _time = value; RaisePropertyChanged("Time"); }
        }

        private string _calcedsize;

        public string CalcedSize
        {
            get { return _calcedsize; }
            set { _calcedsize = value; RaisePropertyChanged("CalcedSize"); }
        }

        private bool _shortcutExists;

        public bool ShortcutExists
        {
            get { return _shortcutExists; }
            set { _shortcutExists = value; RaisePropertyChanged("ShortcutExists"); }
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
            Status = "";
            Languages = new List<Language>
                        {
                            new Language
                            {
                                ConfigName = "de-DE",
                                CultureName = "de-DE",
                                DisplayName = "Deutsch"
                            },
                            new Language
                            {
                                ConfigName = "en-US",
                                CultureName = "en-US",
                                DisplayName = "English"
                            }
                        };
            ShortcutExists = CheckShortcut();
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
                    Thread.Sleep(100); // XXX: give the GUI some time to be drawn before using 100% CPU ;)
                    _stopwatch.Reset();
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
                                Status = String.Format(Resources.MainWindowViewModel_CalcChecksums_Calculating__0_, file)));
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
                        Resources.MainWindowViewModel_CalcChecksums_Error_while_calculating, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Calculating = false;
                    _stopwatch.Stop();
                    var converter = new ByteConverter();
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        Time = _stopwatch.Elapsed;
                        CalcedSize = converter.Convert(calcedsize, null, null, null).ToString();
                        Status = "";
                    }));
                }
            });
        }

        private bool CheckShortcut()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell\CheckSummer\command");
            RegistryKey key2 = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Directory\shell\CheckSummer\command");
            return key != null && key2 != null;
        }

        internal void SetShortcut()
        {
            if (!CheckShortcut())
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\*\shell\CheckSummer\command");
                RegistryKey key2 = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\CheckSummer\command");
                if (key != null)
                    key.SetValue("", System.Reflection.Assembly.GetEntryAssembly().Location + " \"%1\"");
                if (key2 != null)
                    key2.SetValue("", System.Reflection.Assembly.GetEntryAssembly().Location + " \"%1\"");
            }
            else
            {
                Registry.CurrentUser.DeleteSubKey(@"Software\Classes\*\shell\CheckSummer\command");
                Registry.CurrentUser.DeleteSubKey(@"Software\Classes\*\shell\CheckSummer");
                Registry.CurrentUser.DeleteSubKey(@"Software\Classes\Directory\shell\CheckSummer\command");
                Registry.CurrentUser.DeleteSubKey(@"Software\Classes\Directory\shell\CheckSummer\");
            }
            ShortcutExists = CheckShortcut();
        }
    }
}
