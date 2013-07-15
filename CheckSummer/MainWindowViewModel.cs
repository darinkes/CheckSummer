using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public ObservableCollection<CheckSumFileInfo> CheckSumFileInfos { get; private set; } 

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

        private CheckSummedFile _selectedCheckSummedFile;

        public CheckSummedFile SelectedCheckSummedFile
        {
            get { return _selectedCheckSummedFile; }
            set { _selectedCheckSummedFile = value; RaisePropertyChanged("SelectedCheckSummedFile"); }
        }

        private string _BasePath;

        public string BasePath
        {
            get { return _BasePath; }
            set { _BasePath = value; RaisePropertyChanged("BasePath"); }
        }

        private bool _verified;

        public bool Verified
        {
            get { return _verified; }
            set { _verified = value; RaisePropertyChanged("Verified"); }
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
        private readonly Regex _checksumfileRegex = new Regex(@"(?<type>.+)\s+\((?<filename>.+)\) = (?<checksum>.+)", RegexOptions.Compiled);
        #endregion

        public MainWindowViewModel()
        {
            CheckSummedFiles = new ObservableCollection<CheckSummedFile>();
            CheckSumFileInfos = new ObservableCollection<CheckSumFileInfo>();
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

        internal void CalcChecksums(string filename)
        {
            CalcChecksums(new[] {filename});
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
                        {
                            files2Calc.AddRange(Directory.GetFiles(filename, "*.*",
                                                                   SearchOption.AllDirectories));
                        }
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
                                VerifyChecksums(new[] {checkfile});
                                CheckSummedFiles.Add(checkfile);
                                Progress = (index1 / (double)files2Calc.Count) * 100;
                                if (SelectedCheckSummedFile == null)
                                    SelectedCheckSummedFile = checkfile;
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

        private void VerifyChecksums(IEnumerable<CheckSummedFile> checkSummedFiles)
        {
            foreach (var checkfile in checkSummedFiles)
            {
                var toverify = CheckSumFileInfos.Where(cf => checkfile.Filename == cf.Filename).ToList();

                Debug.WriteLine("Verifying Checksum: " + checkfile.Filename + " " + toverify.Count());

                foreach (var checkSumFileInfo in toverify)
                {
                    Debug.WriteLine(checkSumFileInfo.Type + " " + checkSumFileInfo.Filename + " " + checkSumFileInfo.Checksum);
                    switch (checkSumFileInfo.Type)
                    {
                        case "MD5":
                            checkfile.Md5Verified = checkfile.Md5 == checkSumFileInfo.Checksum;
                            break;
                        case "SHA1":
                            checkfile.Sha1Verified = checkfile.Sha1 == checkSumFileInfo.Checksum;
                            break;
                        case "SHA256":
                            checkfile.Sha256Verified = checkfile.Sha256 == checkSumFileInfo.Checksum;
                            break;
                        case "SHA512":
                            checkfile.Sha512Verified = checkfile.Sha512 == checkSumFileInfo.Checksum;
                            break;
                    }
                }
            }
        }

        private bool CheckShortcut()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell\CheckSummer\command");
                RegistryKey key2 =
                    Registry.CurrentUser.OpenSubKey(@"Software\Classes\Directory\shell\CheckSummer\command");
                if ((key == null ||
                     key.GetValue("").ToString() != System.Reflection.Assembly.GetEntryAssembly().Location + " \"%1\"") ||
                    (key2 == null ||
                     key2.GetValue("").ToString() != System.Reflection.Assembly.GetEntryAssembly().Location + " \"%1\""))
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("{0}\n{1}", ex.Message, ex.StackTrace),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShortcutExists = false;
            }
            return false;
        }

        internal void SetShortcut()
        {
            try
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("{0}\n{1}", ex.Message, ex.StackTrace),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ShortcutExists = CheckShortcut();
        }

        internal void SaveChecksums(string filename)
        {
            var tosave = new StringBuilder();
            foreach (var entry in CheckSummedFiles)
            {
                tosave.AppendLine(string.Format("MD5 ({0}) = {1}",
                                entry.Filename,
                                entry.Md5
                      ));
                tosave.AppendLine(string.Format("SHA1 ({0}) = {1}",
                                entry.Filename,
                                entry.Sha1
                      ));
                tosave.AppendLine(string.Format("SHA256 ({0}) = {1}",
                                                entry.Filename,
                                                entry.Sha256
                                      ));
                tosave.AppendLine(string.Format("SHA512 ({0}) = {1}",
                                                entry.Filename,
                                                entry.Sha512
                      ));
            }
            File.WriteAllText(filename, tosave.ToString(), Encoding.Default);
        }

        internal void LoadChecksums(string filename)
        {
            var checksumlist = File.ReadAllLines(filename);
            CheckSumFileInfos.Clear();
            foreach (var entry in checksumlist)
            {
                var match = _checksumfileRegex.Match(entry);
                if (!match.Success)
                {
                    Debug.WriteLine("Line does not Match: " + entry);
                    continue;
                }
                var info = new CheckSumFileInfo
                    {
                        Checksum = match.Groups["checksum"].Value,
                        Filename = match.Groups["filename"].Value,
                        Type = match.Groups["type"].Value
                    };
                CheckSumFileInfos.Add(info);
            }
            Debug.WriteLine("Imported " + CheckSumFileInfos.Count + " CheckSumFileInfos");
            VerifyChecksums(CheckSummedFiles);
        }
    }
}
