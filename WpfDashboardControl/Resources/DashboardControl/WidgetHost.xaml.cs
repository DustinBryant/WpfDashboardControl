using System.Windows;
using System.Windows.Input;
using DragEventHandler = WpfDashboardControl.Resources.DashboardControl.Models.DragEventHandler;

namespace WpfDashboardControl.Resources.DashboardControl
{
    /// <summary>
    /// Custom ContentControl for a DashboardHost that represents the item data for the DashboardHost's ItemsSource
    /// </summary>
    public partial class WidgetHost
    {
        #region Private Fields

        private Point? _mouseDownPoint;

        #endregion Private Fields

        #region Public Events

        /// <summary>
        /// Occurs when [drag started].
        /// </summary>
        public event DragEventHandler DragStarted;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Gets the index of the host.
        /// </summary>
        /// <value>The index of the host.</value>
        public int HostIndex { get; set; }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WidgetHost"/> class.
        /// </summary>
        public WidgetHost()
        {
            InitializeComponent();

            Loaded += WidgetHost_Loaded;
            Unloaded += WidgetHost_Unloaded;
        }

        #endregion Public Constructors

        #region Private Methods

        /// <summary>
        /// Handles the MouseLeftButtonDown event of the Host control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        private void Host_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownPoint = e.GetPosition(this);
        }

        /// <summary>
        /// Handles the MouseMove event of the Host control. Used to invoke a drag started if the proper
        /// conditions have been met
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void Host_MouseMove(object sender, MouseEventArgs e)
        {
            var mouseMovePoint = e.GetPosition(this);

            // Check if we're "dragging" this control around. If not the return, otherwise invoke DragStarted event.
            if (!(_mouseDownPoint.HasValue) ||
                e.LeftButton == MouseButtonState.Released ||
                Point.Subtract(_mouseDownPoint.Value, mouseMovePoint).Length < SystemParameters.MinimumHorizontalDragDistance &&
                Point.Subtract(_mouseDownPoint.Value, mouseMovePoint).Length < SystemParameters.MinimumVerticalDragDistance)
                return;

            DragStarted?.Invoke(this);
        }

        /// <summary>
        /// Handles the Loaded event of the WidgetHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void WidgetHost_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= WidgetHost_Loaded;

            PreviewMouseLeftButtonDown += Host_MouseLeftButtonDown;
            PreviewMouseMove += Host_MouseMove;
        }

        /// <summary>
        /// Handles the Unloaded event of the WidgetHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void WidgetHost_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= WidgetHost_Unloaded;

            PreviewMouseLeftButtonDown += Host_MouseLeftButtonDown;
            PreviewMouseMove += Host_MouseMove;
        }

        #endregion Private Methods
    }
}