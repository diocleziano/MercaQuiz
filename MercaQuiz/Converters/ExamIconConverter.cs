using System;
using System.Globalization;
using MercaQuiz.MVVM.Models;
using Microsoft.Maui.Controls;

namespace MercaQuiz.Converters;

public class ExamIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Materia m)
        {
            var date = m.DataProssimoEsame;
            // null -> no date defined
            if (!date.HasValue)
            {
                return "???"; // no date
            }

            // Normalize to local date to avoid UTC/local mismatch
            DateTime dt = date.Value;
            DateTime dtLocal;
            if (dt.Kind == DateTimeKind.Utc)
                dtLocal = dt.ToLocalTime();
            else if (dt.Kind == DateTimeKind.Local)
                dtLocal = dt;
            else
                // Unspecified: assume stored as local date (common with SQLite) -> treat as DateTime.SpecifyKind Local
                dtLocal = DateTime.SpecifyKind(dt, DateTimeKind.Local);

            var today = DateTime.Today;

            // upcoming if in future or if today but not yet marked as passed
            if (dtLocal.Date > today || (dtLocal.Date == today && !m.IsSuperato))
            {
                return "?"; // upcoming
            }

            // date is in the past (or today and marked passed) -> show success/failure
            return m.IsSuperato ? "?" : "?";
        }

        // fallback
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
