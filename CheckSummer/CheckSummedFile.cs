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
        private FileStream _stream;
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
            Debug.WriteLine("CalcCheckSums: " + Filename);

            _stream = new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            _stopwatch.Start();

            _md5Task = Task.Factory.StartNew(() =>
            {
                var md5 = MD5.Create();
                Md5 = BitConverter.ToString(md5.ComputeHash(_stream)).Replace("-", "").ToLower();
            });

            _sha1Task = Task.Factory.StartNew(() =>
            {
                var sha1 = SHA1.Create();
                Sha1 = BitConverter.ToString(sha1.ComputeHash(_stream)).Replace("-", "").ToLower();
            });

            _sha256Task = Task.Factory.StartNew(() =>
            {
                var sha256 = SHA256.Create();
                Sha256 = BitConverter.ToString(sha256.ComputeHash(_stream)).Replace("-", "").ToLower();
            });

            _sha512Task = Task.Factory.StartNew(() =>
            {
                var sha512 = SHA512.Create();
                Sha512 = BitConverter.ToString(sha512.ComputeHash(_stream)).Replace("-", "").ToLower();
            });
        }

        public void Wait()
        {
            _md5Task.Wait();
            _sha1Task.Wait();
            _sha256Task.Wait();
            _sha512Task.Wait();
            _stopwatch.Stop();
            _stream.Close();
            SummedTime = _stopwatch.Elapsed;
        }
    }
}
