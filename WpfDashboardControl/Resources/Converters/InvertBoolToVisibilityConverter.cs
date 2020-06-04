using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfDashboardControl.Resources.Converters
{
    /// <inheritdoc />
    /// <summary>
    /// Converter for if bool value provided is true then set Visibility.Collapsed else Visibility.Visible
    /// </summary>
    /// <seealso cref="T:System.Windows.Data.IValueConverter" />
    public class InvertBoolToVisibilityConverter : IValueConverter
    {
        #region Public Methods

        /// <inheritdoc />
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <see langword="null" />, the valid null value is used.
        /// </returns>
        /// <exception cref="T:System.Exception">InvertBoolToVisibilityConverter</exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is bool boolValue))
                throw new Exception($"{nameof(InvertBoolToVisibilityConverter)} expects a boolean to passed in through its value parameter");

            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns <see langword="null" />, the valid null value is used.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion Public Methods
    }
}