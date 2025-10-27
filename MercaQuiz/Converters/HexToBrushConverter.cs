using System.Globalization;

namespace MercaQuiz.Converters;
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            var hex = value as string;
            if (string.IsNullOrWhiteSpace(hex)) return new SolidColorBrush(Colors.Gray);
            return new SolidColorBrush(Color.FromArgb(hex.Trim()));
        }
        catch { return new SolidColorBrush(Colors.Gray); }
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
