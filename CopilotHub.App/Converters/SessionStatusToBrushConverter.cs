using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CopilotHub.Core.Models;

namespace CopilotHub.App.Converters;

public class SessionStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SessionStatus status)
        {
            return status switch
            {
                SessionStatus.Running => new SolidColorBrush(Color.FromRgb(66, 165, 245)),
                SessionStatus.Completed => new SolidColorBrush(Color.FromRgb(102, 187, 106)),
                SessionStatus.Failed => new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
