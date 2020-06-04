using Infrastructure;
using System.Windows.Input;

namespace WpfDashboardControl.Dashboards
{
    /// <summary>
    /// View model for the dashboard settings prompt
    /// Implements the <see cref="Infrastructure.ViewModelBase" />
    /// </summary>
    /// <seealso cref="Infrastructure.ViewModelBase" />
    public class DashboardSettingsPromptViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly DashboardConfigurationType _configType;
        private readonly IDashboardConfigurationHandler _dashboardNameValidChecker;
        private string _dashboardName;
        private string _invalidReason;
        private bool _isValid;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets the command cancel dashboard configuration.
        /// </summary>
        /// <value>The command cancel dashboard configuration.</value>
        public ICommand CommandCancelDashboardConfiguration => new RelayCommand(o =>
            _dashboardNameValidChecker.DashboardConfigurationComplete(_configType, false, ""));

        /// <summary>
        /// Gets the name of the command save dashboard.
        /// </summary>
        /// <value>The name of the command save dashboard.</value>
        public ICommand CommandSaveDashboardName => new RelayCommand(
            o => _dashboardNameValidChecker.DashboardConfigurationComplete(_configType, true, DashboardName),
            o => IsValid);

        /// <summary>
        /// Gets or sets the name of the dashboard.
        /// </summary>
        /// <value>The name of the dashboard.</value>
        public string DashboardName
        {
            get => _dashboardName;
            set
            {
                if (!RaiseAndSetIfChanged(ref _dashboardName, value))
                    return;

                var validResponse = _dashboardNameValidChecker.DashboardNameValid(DashboardName);
                IsValid = validResponse.Valid;
                InvalidReason = validResponse.InvalidReason;
            }
        }

        /// <summary>
        /// Gets or sets the invalid reason.
        /// </summary>
        /// <value>The invalid reason.</value>
        public string InvalidReason
        {
            get => _invalidReason;
            set => RaiseAndSetIfChanged(ref _invalidReason, value);
        }

        /// <summary>
        /// Returns true if dashboard name is valid.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid
        {
            get => _isValid;
            set => RaiseAndSetIfChanged(ref _isValid, value);
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardSettingsPromptViewModel"/> class.
        /// </summary>
        /// <param name="configType">Type of the configuration.</param>
        /// <param name="dashboardNameValidChecker">The dashboard name valid checker.</param>
        /// <param name="dashboardName">Name of the dashboard.</param>
        public DashboardSettingsPromptViewModel(DashboardConfigurationType configType,
            IDashboardConfigurationHandler dashboardNameValidChecker, string dashboardName = "")
        {
            _configType = configType;
            _dashboardNameValidChecker = dashboardNameValidChecker;

            switch (configType)
            {
                case DashboardConfigurationType.New:
                    Title = "Create a dashboard";
                    break;
                case DashboardConfigurationType.Existing:
                    Title = $"Settings for {dashboardName}";
                    break;
            }

            DashboardName = dashboardName;
        }

        #endregion Public Constructors
    }
}