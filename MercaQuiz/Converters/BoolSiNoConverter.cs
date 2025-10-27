using System.Globalization;

namespace MercaQuiz.Converters;

public class BoolToSiNoConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is bool b && b ? "Sì" : "No";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
