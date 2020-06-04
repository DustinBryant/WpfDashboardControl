using Infrastructure;
using WpfDashboardControl.Dashboards;

namespace WpfDashboardControl
{
    /// <summary>
    /// Represents the main viewmodel of the applications bound to the MainWindow view
    /// Implements the <see cref="ViewModelBase" />
    /// </summary>
    /// <seealso cref="ViewModelBase" />
    public class MainWindowViewModel : ViewModelBase
    {
        #region Private Fields

        private DashboardsViewModel _dashboardsContent;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the content of the dashboards.
        /// </summary>
        /// <value>The content of the dashboards.</value>
        public DashboardsViewModel DashboardsContent
        {
            get => _dashboardsContent;
            set => RaiseAndSetIfChanged(ref _dashboardsContent, value);
        }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            DashboardsContent = new DashboardsViewModel();
            DashboardsContent.Start();
        }

        #endregion Public Constructors
    }
}