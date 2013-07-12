using System;
using System.Windows.Data;

namespace CheckSummer
{
    class ByteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var val = value as long?;

            if (val == null)
                return value;

            if (val >= 1073741824)
                return String.Format("{0:0.00} GB", (double)val / 1073741824);
            if (val >= 1048576)
                return String.Format("{0:0.00} MB", (double)val / 1048576);
            if (val >= 1024)
                return String.Format("{0:0.00} KB", (double)val / 1024);
            return (double)val + " B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
