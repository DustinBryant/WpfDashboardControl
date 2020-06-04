namespace WpfDashboardControl.Dashboards
{
    /// <summary>
    /// Provides properties detailing the validity of a dashboard name
    /// </summary>
    public class DashboardNameValidResponse
    {
        #region Public Properties

        /// <summary>
        /// Gets the invalid reason.
        /// </summary>
        /// <value>The invalid reason.</value>
        public string InvalidReason { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="DashboardNameValidResponse"/> is valid.
        /// </summary>
        /// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
        public bool Valid { get; }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardNameValidResponse"/> class.
        /// </summary>
        /// <param name="valid">if set to <c>true</c> [valid].</param>
        /// <param name="invalidReason">The invalid reason.</param>
        public DashboardNameValidResponse(bool valid, string invalidReason = null)
        {
            Valid = valid;
            InvalidReason = invalidReason;
        }

        #endregion Public Constructors
    }
}