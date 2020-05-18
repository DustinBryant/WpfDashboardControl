using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WpfDashboardControl.Models;
using WpfDashboardControl.Widgets;

namespace WpfDashboardControl
{
    /// <summary>
    /// Represents the main viewmodel of the applications bound to the MainWindow view
    /// Implements the <see cref="WpfDashboardControl.Models.ViewModelBase" />
    /// </summary>
    /// <seealso cref="WpfDashboardControl.Models.ViewModelBase" />
    public class MainWindowViewModel : ViewModelBase
    {
        #region Private Fields

        private bool _editing;
        private int _widgetNumber;
        private ObservableCollection<WidgetBase> _widgets = new ObservableCollection<WidgetBase>();

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets the add one by one command that adds a OneByOneViewModel to the Widgets collection.
        /// </summary>
        /// <value>The command add one by one.</value>
        public ICommand CommandAddOneByOne =>
            new RelayCommand(o => Widgets.Add(new OneByOneViewModel(_widgetNumber++)), o => _editing);

        /// <summary>
        /// Gets the add one by two command that adds a OneByTwoViewModel to the Widgets collection.
        /// </summary>
        /// <value>The command add one by two.</value>
        public ICommand CommandAddOneByTwo =>
            new RelayCommand(o => Widgets.Add(new OneByTwoViewModel(_widgetNumber++)), o => _editing);

        /// <summary>
        /// Gets the add two by one command that adds a TwoByOneViewModel to the Widgets collection.
        /// </summary>
        /// <value>The command add two by one.</value>
        public ICommand CommandAddTwoByOne =>
            new RelayCommand(o => Widgets.Add(new TwoByOneViewModel(_widgetNumber++)), o => _editing);

        /// <summary>
        /// Gets the add two by two command that adds a TwoByTwoViewModel to the Widgets collection.
        /// </summary>
        /// <value>The command add two by two.</value>
        public ICommand CommandAddTwoByTwo =>
            new RelayCommand(o => Widgets.Add(new TwoByTwoViewModel(_widgetNumber++)), o => _editing);

        /// <summary>
        /// Gets the command editing complete.
        /// </summary>
        /// <value>The command editing complete.</value>
        public ICommand CommandEditingComplete => new RelayCommand(o =>
        {
            // Do some save logic here
            _editing = false;
        });

        /// <summary>
        /// Gets the command edit mode enabled.
        /// </summary>
        /// <value>The command edit mode enabled.</value>
        public ICommand CommandEditModeEnabled => new RelayCommand(o => _editing = true);

        /// <summary>
        /// Gets the remove last element command that remove the last element in the Widgets collection.
        /// </summary>
        /// <value>The command remove last element.</value>
        public ICommand CommandRemoveLastElement => new RelayCommand(o => _widgets.Remove(_widgets.Last()),
            o => _editing && _widgets.Count > 0);

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

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            _widgets.Add(new OneByOneViewModel(_widgetNumber++) { RowIndex = 0, ColumnIndex = 0});
            _widgets.Add(new OneByTwoViewModel(_widgetNumber++) { RowIndex = 0, ColumnIndex = 1});
        }

        #endregion Public Constructors
    }
}