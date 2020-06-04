namespace WpfDashboardControl.Dashboards
{
    /// <summary>
    /// Interface IDashboardConfigurationHandler
    /// </summary>
    public interface IDashboardConfigurationHandler
    {
        #region Public Methods

        /// <summary>
        /// Complete the dashboard configuration.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="save">if set to <c>true</c> [save].</param>
        /// <param name="newName">The new name.</param>
        void DashboardConfigurationComplete(DashboardConfigurationType type, bool save, string newName);

        /// <summary>
        /// Validate dashboard name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>DashboardNameValidResponse.</returns>
        DashboardNameValidResponse DashboardNameValid(string name);

        #endregion Public Methods
    }
}