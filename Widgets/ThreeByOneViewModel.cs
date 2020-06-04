using Infrastructure;

namespace Widgets
{
    /// <summary>
    /// Represents a Widget that is a size of three by one
    /// Implements the <see cref="WidgetBase" />
    /// </summary>
    /// <seealso cref="WidgetBase" />
    public class ThreeByOneViewModel : WidgetBase
    {
        #region Private Fields

        private string _widgetTitle;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the widget title.
        /// </summary>
        /// <value>The widget title.</value>
        public sealed override string WidgetTitle
        {
            get => _widgetTitle;
            set => RaiseAndSetIfChanged(ref _widgetTitle, value);
        }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeByOneViewModel"/> class.
        /// </summary>
        public ThreeByOneViewModel(int widgetNumber) : base(WidgetSize.ThreeByOne)
        {
            WidgetTitle = $"ThreeOne{widgetNumber}";
        }

        #endregion Public Constructors
    }
}