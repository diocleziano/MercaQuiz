using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MercaQuiz.Converters;
public class BoolToTitleConverter : IValueConverter
{
    // ConverterParameter = "TitoloEdit|TitoloAdd"
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var p = (parameter as string ?? "Modifica|Nuova").Split('|');
        var edit = p.Length > 0 ? p[0] : "Modifica";
        var add = p.Length > 1 ? p[1] : "Nuova";
        var isEdit = value is bool b && b;
        return isEdit ? edit : add;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
