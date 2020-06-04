namespace Infrastructure
{
    /// <summary>
    /// Widget being hosted within a WidgetHost of a DashboardHost
    /// Implements the <see cref="ViewModelBase" />
    /// </summary>
    /// <seealso cref="ViewModelBase" />
    public abstract class WidgetBase : ViewModelBase
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the index of the column.
        /// </summary>
        /// <value>The index of the column.</value>
        public int? ColumnIndex { get; set; } = null;

        /// <summary>
        /// Gets or sets the index of the row.
        /// </summary>
        /// <value>The index of the row.</value>
        public int? RowIndex { get; set; } = null;

        /// <summary>
        /// Gets the size of the widget.
        /// </summary>
        /// <value>The size of the widget.</value>
        public WidgetSize WidgetSize { get; }

        /// <summary>
        /// Gets or sets the widget title.
        /// </summary>
        /// <value>The widget title.</value>
        public abstract string WidgetTitle { get; set; }

        #endregion Public Properties

        #region Protected Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WidgetBase"/> class.
        /// </summary>
        /// <param name="widgetSize">Size of the widget.</param>
        protected WidgetBase(WidgetSize widgetSize)
        {
            WidgetSize = widgetSize;
        }

        #endregion Protected Constructors
    }
}