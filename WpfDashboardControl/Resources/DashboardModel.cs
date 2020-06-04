using Infrastructure;
using System.Collections.ObjectModel;

namespace WpfDashboardControl.Resources
{
    /// <summary>
    /// Represents a dashboard model containing widgets and a title
    /// Implements the <see cref="Infrastructure.ViewModelBase" />
    /// </summary>
    /// <seealso cref="Infrastructure.ViewModelBase" />
    public class DashboardModel : ViewModelBase
    {
        #region Private Fields

        private string _title;
        private ObservableCollection<WidgetBase> _widgets = new ObservableCollection<WidgetBase>();

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title
        {
            get => _title;
            set => RaiseAndSetIfChanged(ref _title, value);
        }

        /// <summary>
        /// Gets or sets the widgets.
        /// </summary>
        /// <value>The widgets.</value>
        public ObservableCollection<WidgetBase> Widgets
        {
            get => _widgets;
            set => RaiseAndSetIfChanged(ref _widgets, value);
        }

        #endregion Public Properties
    }
}