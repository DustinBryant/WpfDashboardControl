using Infrastructure;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Widgets;
using WpfDashboardControl.Resources;
using WpfDashboardControl.Resources.DashboardControl;

namespace WpfDashboardControl.Dashboards
{
    /// <summary>
    /// View model for dashboards
    /// Implements the <see cref="Infrastructure.ViewModelBase" />
    /// Implements the <see cref="WpfDashboardControl.Dashboards.IDashboardConfigurationHandler" />
    /// </summary>
    /// <seealso cref="Infrastructure.ViewModelBase" />
    /// <seealso cref="WpfDashboardControl.Dashboards.IDashboardConfigurationHandler" />
    public class DashboardsViewModel : ViewModelBase, IDashboardConfigurationHandler
    {
        #region Private Fields

        private List<Widget> _availableWidgets = new List<Widget>();
        private DashboardSettingsPromptViewModel _configuringDashboard;
        private TempWidgetHost _configuringWidget;
        private ObservableCollection<DashboardModel> _dashboards = new ObservableCollection<DashboardModel>();
        private bool _dashboardSelectorUncheck;
        private bool _editMode;
        private DashboardModel _selectedDashboard;
        private int _widgetNumber;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the available widgets.
        /// </summary>
        /// <value>The available widgets.</value>
        public List<Widget> AvailableWidgets
        {
            get => _availableWidgets;
            set => RaiseAndSetIfChanged(ref _availableWidgets, value);
        }

        /// <summary>
        /// Gets the command add widget.
        /// </summary>
        /// <value>The command add widget.</value>
        public ICommand CommandAddWidget => new RelayCommand(o =>
        {
            var widgetToAdd = (Widget)o;

            SelectedDashboard.Widgets.Add(widgetToAdd.CreateWidget());
            EditMode = true;
        });

        /// <summary>
        /// Gets the command configure widget.
        /// </summary>
        /// <value>The command configure widget.</value>
        public ICommand CommandConfigureWidget => new RelayCommand(o =>
        {
            var widgetHost = (WidgetHost)o;
            ConfiguringWidget = new TempWidgetHost
            {
                DataContext = widgetHost.DataContext,
                Content = widgetHost.DataContext,
                MaxWidth = widgetHost.MaxWidth,
                MaxHeight = widgetHost.MaxHeight,
                Width = widgetHost.ActualWidth,
                Height = widgetHost.ActualHeight
            };
        });

        /// <summary>
        /// Gets the command done configuring widget.
        /// </summary>
        /// <value>The command done configuring widget.</value>
        public ICommand CommandDoneConfiguringWidget => new RelayCommand(o => ConfiguringWidget = null);

        /// <summary>
        /// Gets the command edit dashboard.
        /// </summary>
        /// <value>The command edit dashboard.</value>
        public ICommand CommandEditDashboard => new RelayCommand(o => EditMode = o.ToString() == "True", o => ConfiguringWidget == null);

        /// <summary>
        /// Gets the command manage dashboard.
        /// </summary>
        /// <value>The command manage dashboard.</value>
        public ICommand CommandManageDashboard => new RelayCommand(o =>
            ConfiguringDashboard =
                new DashboardSettingsPromptViewModel(DashboardConfigurationType.Existing, this,
                    SelectedDashboard.Title));

        /// <summary>
        /// Gets the command new dashboard.
        /// </summary>
        /// <value>The command new dashboard.</value>
        public ICommand CommandNewDashboard => new RelayCommand(o =>
            ConfiguringDashboard = new DashboardSettingsPromptViewModel(DashboardConfigurationType.New, this));

        /// <summary>
        /// Gets the command remove widget.
        /// </summary>
        /// <value>The command remove widget.</value>
        public ICommand CommandRemoveWidget => new RelayCommand(o => SelectedDashboard.Widgets.Remove((WidgetBase)o));

        /// <summary>
        /// Gets or sets the configuring dashboard.
        /// </summary>
        /// <value>The configuring dashboard.</value>
        public DashboardSettingsPromptViewModel ConfiguringDashboard
        {
            get => _configuringDashboard;
            set => RaiseAndSetIfChanged(ref _configuringDashboard, value);
        }

        /// <summary>
        /// Gets or sets the configuring widget.
        /// </summary>
        /// <value>The configuring widget.</value>
        public TempWidgetHost ConfiguringWidget
        {
            get => _configuringWidget;
            set => RaiseAndSetIfChanged(ref _configuringWidget, value);
        }

        /// <summary>
        /// Gets or sets the dashboards.
        /// </summary>
        /// <value>The dashboards.</value>
        public ObservableCollection<DashboardModel> Dashboards
        {
            get => _dashboards;
            set => RaiseAndSetIfChanged(ref _dashboards, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [dashboard selector uncheck].
        /// </summary>
        /// <value><c>true</c> if [dashboard selector uncheck]; otherwise, <c>false</c>.</value>
        public bool DashboardSelectorUncheck
        {
            get => _dashboardSelectorUncheck;
            set => RaiseAndSetIfChanged(ref _dashboardSelectorUncheck, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [edit mode].
        /// </summary>
        /// <value><c>true</c> if [edit mode]; otherwise, <c>false</c>.</value>
        public bool EditMode
        {
            get => _editMode;
            set => RaiseAndSetIfChanged(ref _editMode, value);
        }

        /// <summary>
        /// Gets or sets the selected dashboard.
        /// </summary>
        /// <value>The selected dashboard.</value>
        public DashboardModel SelectedDashboard
        {
            get => _selectedDashboard;
            set
            {
                if (!RaiseAndSetIfChanged(ref _selectedDashboard, value))
                    return;

                DashboardSelectorUncheck = true;
                DashboardSelectorUncheck = false;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Completes dashboard configuration
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="save">if set to <c>true</c> [save].</param>
        /// <param name="newName">The new name.</param>
        public void DashboardConfigurationComplete(DashboardConfigurationType type, bool save, string newName)
        {
            ConfiguringDashboard = null;

            if (!save)
                return;

            switch (type)
            {
                case DashboardConfigurationType.New:
                    var dashboardModel = new DashboardModel { Title = newName };
                    Dashboards.Add(dashboardModel);
                    SelectedDashboard = dashboardModel;
                    return;
                case DashboardConfigurationType.Existing:
                    SelectedDashboard.Title = newName;
                    return;
            }
        }

        /// <summary>
        /// Dashboards the name valid.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>DashboardNameValidResponse.</returns>
        public DashboardNameValidResponse DashboardNameValid(string name)
        {
            return Dashboards.Any(dashboard => dashboard.Title == name)
                ? new DashboardNameValidResponse(false, $"That Dashboard Name [{name}] already exists.")
                : new DashboardNameValidResponse(true);
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public Task Start()
        {
            Dashboards.Add(new DashboardModel { Title = "My Dashboard" });
            SelectedDashboard = Dashboards[0];

            AvailableWidgets = new List<Widget> {
                new Widget("One By One", "Provides a one by one square widget.",
                    () => new OneByOneViewModel(_widgetNumber++)),
                new Widget("One By Two", "Provides a one by two square widget.",
                    () => new OneByTwoViewModel(_widgetNumber++)),
                new Widget("One By Three", "Provides a one by three square widget.",
                    () => new OneByThreeViewModel(_widgetNumber++)),
                new Widget("Two By One", "Provides a two by one square widget.",
                    () => new TwoByOneViewModel(_widgetNumber++)),
                new Widget("Two By Two", "Provides a two by two square widget.",
                    () => new TwoByTwoViewModel(_widgetNumber++)),
                new Widget("Two By Three", "Provides a two by three square widget.",
                    () => new TwoByThreeViewModel(_widgetNumber++)),
                new Widget("Three By One", "Provides a three by one square widget.",
                    () => new ThreeByOneViewModel(_widgetNumber++)),
                new Widget("Three By Two", "Provides a three by one two widget.",
                    () => new ThreeByTwoViewModel(_widgetNumber++)),
                new Widget("Three By Three", "Provides a three by one square widget.",
                    () => new ThreeByThreeViewModel(_widgetNumber++))
            };

            return Task.CompletedTask;
        }

        #endregion Public Methods
    }
}