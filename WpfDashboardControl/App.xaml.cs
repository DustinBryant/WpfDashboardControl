using System.Windows;

namespace WpfDashboardControl
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        #region Protected Methods

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Startup" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs" /> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create a new MainWindow and set its DataContext to a new MainWindowViewModel which binds the view to the viewmodel
            new MainWindow { DataContext = new MainWindowViewModel() }.Show();
        }

        #endregion Protected Methods
    }
}