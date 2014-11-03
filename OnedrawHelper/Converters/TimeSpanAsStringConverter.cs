using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OnedrawHelper.Converters
{
    class TimeSpanAsStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan ts = (TimeSpan)value;
            int m = (int)ts.TotalMinutes;
            int s = ts.Seconds;
            return string.Format("{0}{1:00}:{2:00}",
                ts.TotalSeconds < 0 ? "-" : "",
                m * (m < 0 ? -1 : 1),
                s * (s < 0 ? -1 : 1));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
