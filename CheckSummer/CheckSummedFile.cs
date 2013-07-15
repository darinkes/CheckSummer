using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CheckSummer
{
    public class CheckSummedFile : INotifyPropertyChanged
    {
        #region Properties
        public string Md5 { get; private set; }
        public string Sha1 { get; private set; }
        public string Sha256 { get; private set; }
        public string Sha512 { get; private set; }
        public string Filename { get; private set; }
        public long FileSize { get; private set; }
        public TimeSpan SummedTime { get; private set; }

        private bool _md5Verified;

        public bool Md5Verified
        {
            get { return _md5Verified; }
            set { _md5Verified = value; RaisePropertyChanged("Md5Verified"); }
        }

        private bool _sha1Verified;

        public bool Sha1Verified
        {
            get { return _sha1Verified; }
            set { _sha1Verified = value; RaisePropertyChanged("Sha1Verified"); }
        }

        private bool _sha256Verified;

        public bool Sha256Verified
        {
            get { return _sha256Verified; }
            set { _sha256Verified = value; RaisePropertyChanged("Sha256Verified"); }
        }

        private bool _sha512Verified;

        public bool Sha512Verified
        {
            get { return _sha512Verified; }
            set { _sha512Verified = value; RaisePropertyChanged("Sha512Verified"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        #endregion

        #region Fields
        private Task _md5Task;
        private Task _sha1Task;
        private Task _sha256Task;
        private Task _sha512Task;
        private readonly Stopwatch _stopwatch;
        #endregion

        public CheckSummedFile(string filename)
        {
            Filename = filename;
            _stopwatch = new Stopwatch();
            var fileInfo = new FileInfo(Filename);
            FileSize = fileInfo.Length;
        }

        public void CalcCheckSums()
        {
            _stopwatch.Start();

            _md5Task = Task.Factory.StartNew(() =>
            {
                try
                {
                    using(var stream = new BufferedStream(File.OpenRead(Filename), 1200000))
                    {
                        Md5 =
                            BitConverter.ToString(MD5.Create().ComputeHash(stream))
                            .Replace("-", String.Empty)
                            .ToLower();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Md5 = ex.Message;
                }
            });

            _sha1Task = Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var stream = new BufferedStream(File.OpenRead(Filename), 1200000))
                    {
                        Sha1 =
                            BitConverter.ToString(SHA1.Create().ComputeHash(stream))
                            .Replace("-", String.Empty)
                            .ToLower();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Sha1 = ex.Message;
                }
            });

            _sha256Task = Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var stream = new BufferedStream(File.OpenRead(Filename), 1200000))
                    {
                        Sha256 =
                            BitConverter.ToString(SHA256.Create().ComputeHash(stream))
                                        .Replace("-", String.Empty)
                                        .ToLower();
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Sha256 = ex.Message;
                }
            });

            _sha512Task = Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var stream = new BufferedStream(File.OpenRead(Filename), 1200000))
                    {
                        Sha512 =
                            BitConverter.ToString(SHA512.Create().ComputeHash(stream))
                                        .Replace("-", String.Empty)
                                        .ToLower();
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Sha512 = ex.Message;
                }
            });
        }

        public void Wait()
        {
            _md5Task.Wait();
            _sha1Task.Wait();
            _sha256Task.Wait();
            _sha512Task.Wait();
            _stopwatch.Stop();
            SummedTime = _stopwatch.Elapsed;
        }
    }
}
