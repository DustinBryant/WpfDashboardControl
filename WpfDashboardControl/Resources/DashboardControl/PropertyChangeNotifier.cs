using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace WpfDashboardControl.Resources.DashboardControl
{
    /// <summary>
    /// Using this vs using AddValueChanged since AddValueChanged results in memory leaks.
    /// Implements the <see cref="System.Windows.DependencyObject" />
    /// Implements the <see cref="System.IDisposable" />
    /// </summary>
    /// <seealso cref="System.Windows.DependencyObject" />
    /// <seealso cref="System.IDisposable" />
    internal sealed class PropertyChangeNotifier : DependencyObject, IDisposable
    {
        #region Public Fields

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property
        /// </summary>
        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register("Value", typeof(object), typeof(PropertyChangeNotifier),
                                          new FrameworkPropertyMetadata(null, OnPropertyChanged));

        #endregion Public Fields

        #region Private Fields

        private readonly WeakReference _propertySource;

        #endregion Private Fields

        #region Public Events

        /// <summary>
        /// Occurs when [value changed].
        /// </summary>
        public event EventHandler ValueChanged;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Gets the property source.
        /// </summary>
        /// <value>The property source.</value>
        public DependencyObject PropertySource
        {
            get
            {
                try
                {
                    // note, it is possible that accessing the target property
                    // will result in an exception so i’ve wrapped this check
                    // in a try catch
                    return _propertySource.IsAlive
                        ? _propertySource.Target as DependencyObject
                        : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [raise value changed].
        /// </summary>
        /// <value><c>true</c> if [raise value changed]; otherwise, <c>false</c>.</value>
        public bool RaiseValueChanged { get; set; } = true;

        /// <summary>
        /// Returns/sets the value of the property
        /// </summary>
        /// <seealso cref="ValueProperty"/>
        [Description("Returns/sets the value of the property")]
        [Category("Behavior")]
        [Bindable(true)]
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangeNotifier"/> class.
        /// </summary>
        /// <param name="propertySource">The property source.</param>
        /// <param name="property">The property.</param>
        /// <exception cref="ArgumentNullException">propertySource</exception>
        /// <exception cref="ArgumentNullException">propertyPath</exception>
        public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
        {
            var propertyPath = new PropertyPath(property);

            if (propertySource == null)
                throw new ArgumentNullException(nameof(propertySource));

            if (propertyPath == null)
                throw new ArgumentNullException(nameof(propertyPath));

            _propertySource = new WeakReference(propertySource);

            var binding = new Binding
            {
                Path = propertyPath,
                Mode = BindingMode.OneWay,
                Source = propertySource
            };

            BindingOperations.SetBinding(this, ValueProperty, binding);
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            BindingOperations.ClearBinding(this, ValueProperty);
        }

        #endregion Public Methods

        #region Private Methods

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var notifier = (PropertyChangeNotifier)d;

            if (notifier.RaiseValueChanged)
                notifier.ValueChanged?.Invoke(notifier.PropertySource, EventArgs.Empty);
        }

        #endregion Private Methods
    }
}