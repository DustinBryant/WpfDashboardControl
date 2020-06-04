using Infrastructure;
using System;

namespace WpfDashboardControl.Dashboards
{
    /// <summary>
    /// Model representation of a widget
    /// </summary>
    public class Widget
    {
        #region Private Fields

        private readonly Func<WidgetBase> _createWidget;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Widget"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="createWidget">The create widget.</param>
        public Widget(string name, string description, Func<WidgetBase> createWidget)
        {
            Name = name;
            Description = description;
            _createWidget = createWidget;
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Creates the widget.
        /// </summary>
        /// <returns>WidgetBase.</returns>
        public WidgetBase CreateWidget()
        {
            return _createWidget.Invoke();
        }

        #endregion Public Methods
    }
}