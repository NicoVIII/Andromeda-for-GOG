using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Microsoft.FSharp.Core;
using System;
using System.Globalization;

namespace Andromeda.AvaloniaApp.Converter
{
    public class PathToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value != null) {
                // Unwrap optiontype if necessary
                if (value is FSharpOption<string>) {
                    value = (value as FSharpOption<string>).Value;
                }
                return new Bitmap((string) value);
            } else {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var bitmap = (Bitmap) value;
            return bitmap.ToString();
        }
    }
}
