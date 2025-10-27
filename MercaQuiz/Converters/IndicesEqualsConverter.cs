using System.Globalization;

namespace MercaQuiz.Converters;

public class IndicesEqualConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
        => values?.Length >= 2 && values[0] is int item && values[1] is int sel && item == sel;
    public object[] ConvertBack(object v, Type[] ts, object p, CultureInfo c) => throw new NotImplementedException();
}
