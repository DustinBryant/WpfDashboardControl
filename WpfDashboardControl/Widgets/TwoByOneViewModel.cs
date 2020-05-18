using WpfDashboardControl.Models;

namespace WpfDashboardControl.Widgets
{
    /// <summary>
    /// Represents a Widget that is a size of two by one (220 x 100)
    /// Implements the <see cref="WpfDashboardControl.Models.WidgetBase" />
    /// </summary>
    /// <seealso cref="WpfDashboardControl.Models.WidgetBase" />
    public class TwoByOneViewModel : WidgetBase
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
        /// Initializes a new instance of the <see cref="TwoByOneViewModel"/> class.
        /// </summary>
        public TwoByOneViewModel(int widgetNumber) : base(WidgetSize.TwoByOne)
        {
            WidgetTitle = $"TwoOne{widgetNumber}";
        }

        #endregion Public Constructors
    }
}