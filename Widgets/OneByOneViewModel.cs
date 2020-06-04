using Infrastructure;

namespace Widgets
{
    /// <summary>
    /// Represents a Widget that is a size of one by one
    /// Implements the <see cref="WidgetBase" />
    /// </summary>
    /// <seealso cref="WidgetBase" />
    public class OneByOneViewModel : WidgetBase
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
        /// Initializes a new instance of the <see cref="OneByOneViewModel"/> class.
        /// </summary>
        public OneByOneViewModel(int widgetNumber) : base(WidgetSize.OneByOne)
        {
            WidgetTitle = $"OneOne{widgetNumber}";
        }

        #endregion Public Constructors
    }
}