using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Microsoft.FSharp.Core;
using System;
using System.Globalization;

namespace Andromeda.AvaloniaApp.Converter {
    public class FloatToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture) {
            if (value != null) {
                // Unwrap optiontype if necessary
                if (value is FSharpOption<float>) {
                    value = (value as FSharpOption<float>).Value;
                }
                return string.Format("{0:0.#}", value);
            }
            else {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture) {
            return 0;
        }
    }
}
