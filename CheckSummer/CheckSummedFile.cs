using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CheckSummer
{
    public class CheckSummedFile
    {
        #region Properties
        public string Md5 { get; private set; }
        public string Sha1 { get; private set; }
        public string Sha256 { get; private set; }
        public string Sha512 { get; private set; }
        public string Filename { get; private set; }
        public long FileSize { get; private set; }
        public TimeSpan SummedTime { get; private set; }
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

            var fs = File.OpenRead(Filename);
            var bytes = new byte[fs.Length];
            try
            {
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
            }
            finally
            {
                fs.Close();
            }

            _md5Task = Task.Factory.StartNew(() =>
            {
                try
                {
                    Md5 = BitConverter.ToString(MD5.Create().ComputeHash(bytes)).Replace("-", String.Empty).ToLower();
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
                    Sha1 = BitConverter.ToString(SHA1.Create().ComputeHash(bytes)).Replace("-", String.Empty).ToLower();
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
                    Sha256 = BitConverter.ToString(SHA256.Create().ComputeHash(bytes)).Replace("-", String.Empty).ToLower();

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
                    Sha512 = BitConverter.ToString(SHA512.Create().ComputeHash(bytes)).Replace("-", String.Empty).ToLower();

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
