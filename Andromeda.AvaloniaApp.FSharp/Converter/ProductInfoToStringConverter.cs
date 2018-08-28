using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Microsoft.FSharp.Core;
using System;
using System.Globalization;

namespace Andromeda.AvaloniaApp.Converter
{
    public class ProductInfoToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value != null) {
                var info = (GogApi.DotNet.FSharp.Listing.ProductInfo) value;
                return info.title;
            } else {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
