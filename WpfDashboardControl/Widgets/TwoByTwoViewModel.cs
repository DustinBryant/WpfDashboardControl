using WpfDashboardControl.Models;

namespace WpfDashboardControl.Widgets
{
    /// <summary>
    /// Represents a Widget that is a size of two by two (220 x 200)
    /// Implements the <see cref="WpfDashboardControl.Models.WidgetBase" />
    /// </summary>
    /// <seealso cref="WpfDashboardControl.Models.WidgetBase" />
    public class TwoByTwoViewModel : WidgetBase
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
        /// Initializes a new instance of the <see cref="TwoByTwoViewModel"/> class.
        /// </summary>
        public TwoByTwoViewModel(int widgetNumber) : base(WidgetSize.TwoByTwo)
        {
            WidgetTitle = $"TwoTwo{widgetNumber}";
        }

        #endregion Public Constructors
    }
}