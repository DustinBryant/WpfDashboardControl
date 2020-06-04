using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;

namespace WpfDashboardControl.Resources.Converters
{
    /// <summary>
    /// Provides a delayed multi-binding. This class cannot be inherited.
    /// Implements the <see cref="System.Windows.Markup.MarkupExtension" />
    /// Implements the <see cref="System.Windows.Data.IMultiValueConverter" />
    /// Implements the <see cref="System.ComponentModel.INotifyPropertyChanged" />
    /// </summary>
    /// <seealso cref="System.Windows.Markup.MarkupExtension" />
    /// <seealso cref="System.Windows.Data.IMultiValueConverter" />
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    [ContentProperty("Bindings")]
    internal sealed class DelayedMultiBindingExtension : MarkupExtension, IMultiValueConverter, INotifyPropertyChanged
    {
        #region Private Fields

        private readonly DispatcherTimer _timer;

        private object _delayedValue;

        private object _startingValue;

        private bool _startingValueInitialSet;

        private object _unDelayedValue;

        #endregion Private Fields

        #region Public Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Gets the bindings.
        /// </summary>
        /// <value>The bindings.</value>
        public Collection<BindingBase> Bindings { get; }

        /// <summary>
        /// Gets the change count.
        /// </summary>
        /// <value>The change count.</value>
        public int ChangeCount { get; private set; }

        /// <summary>
        /// Gets or sets the converter.
        /// </summary>
        /// <value>The converter.</value>
        public IMultiValueConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets the converter culture.
        /// </summary>
        /// <value>The converter culture.</value>
        public CultureInfo ConverterCulture { get; set; }

        /// <summary>
        /// Gets or sets the converter parameter.
        /// </summary>
        /// <value>The converter parameter.</value>
        public object ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        /// <value>The current value.</value>
        public object CurrentValue
        {
            get => _delayedValue;
            set
            {
                _delayedValue = _unDelayedValue = value;
                _timer.Stop();
            }
        }

        /// <summary>
        /// Gets or sets the delay.
        /// </summary>
        /// <value>The delay.</value>
        public TimeSpan Delay
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>The mode.</value>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the starting value.
        /// </summary>
        /// <value>The starting value.</value>
        public object StartingValue
        {
            get => _startingValue;
            set
            {
                if (_startingValueInitialSet)
                    return;

                _startingValue = value;
                CurrentValue = value;
                _startingValueInitialSet = true;
            }
        }

        /// <summary>
        /// Gets or sets the update source trigger.
        /// </summary>
        /// <value>The update source trigger.</value>
        public UpdateSourceTrigger UpdateSourceTrigger { get; set; }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedMultiBindingExtension"/> class.
        /// </summary>
        public DelayedMultiBindingExtension()
        {
            Bindings = new Collection<BindingBase>();
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromMilliseconds(10);
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding" /> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value.If the method returns <see langword="null" />, the valid <see langword="null" /> value is used.A return value of <see cref="T:System.Windows.DependencyProperty" />.<see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the converter did not produce a value, and that the binding will use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> if it is available, or else will use the default value.A return value of <see cref="T:System.Windows.Data.Binding" />.<see cref="F:System.Windows.Data.Binding.DoNothing" /> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> or the default value.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var newValue = Converter.Convert(values.Take(values.Length - 1).ToArray(),
                                             targetType,
                                             ConverterParameter,
                                             ConverterCulture ?? culture);

            if (Equals(newValue, _unDelayedValue))
                return _delayedValue;

            _unDelayedValue = newValue;
            _timer.Stop();
            _timer.Start();

            return _delayedValue;
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <param name="value">The value that the binding target produces.</param>
        /// <param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>An array of values that have been converted from the target value back to the source values.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Converter.ConvertBack(value, targetTypes, ConverterParameter, ConverterCulture ?? culture)
                            .Concat(new object[] { ChangeCount }).ToArray();
        }

        /// <summary>
        /// When implemented in a derived class, returns an object that is provided as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
        /// <returns>The object value to set on the property where the extension is applied.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (!(serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget valueProvider))
                return null;

            var bindingTarget = valueProvider.TargetObject as DependencyObject;
            var bindingProperty = valueProvider.TargetProperty as DependencyProperty;

            var multi = new MultiBinding
            {
                Converter = this,
                Mode = Mode,
                UpdateSourceTrigger = UpdateSourceTrigger
            };

            foreach (var binding in Bindings)
                multi.Bindings.Add(binding);

            multi.Bindings.Add(new Binding("ChangeCount")
            {
                Source = this,
                Mode = BindingMode.OneWay
            });

            if (bindingTarget != null && bindingProperty != null)
                BindingOperations.SetBinding(bindingTarget, bindingProperty, multi);

            return bindingProperty == null ? multi : bindingTarget?.GetValue(bindingProperty);
        }

        #endregion Public Methods

        #region Private Methods

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            _delayedValue = _unDelayedValue;
            ChangeCount++;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChangeCount)));
        }

        #endregion Private Methods
    }
}