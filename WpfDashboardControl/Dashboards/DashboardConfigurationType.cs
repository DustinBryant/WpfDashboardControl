namespace WpfDashboardControl.Dashboards
{
    /// <summary>
    /// Represents the type of configuration for a Dashboard. New meaning a new one is being generated, and
    /// existing when the dashboard already exists
    /// </summary>
    public enum DashboardConfigurationType
    {
        /// <summary>
        /// New dashboard being generated
        /// </summary>
        New,

        /// <summary>
        /// Existing dashboard being configured
        /// </summary>
        Existing
    }
}