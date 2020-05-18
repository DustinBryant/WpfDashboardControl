namespace WpfDashboardControl.Models
{
    /// <summary>
    /// Represents a Row Span and Column Span
    /// </summary>
    public class RowSpanColumnSpan
    {
        #region Public Properties

        /// <summary>
        /// Gets the column span.
        /// </summary>
        /// <value>The column span.</value>
        public int ColumnSpan { get; }

        /// <summary>
        /// Gets the row span.
        /// </summary>
        /// <value>The row span.</value>
        public int RowSpan { get; }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RowSpanColumnSpan"/> class.
        /// </summary>
        /// <param name="rowSpan">The row span.</param>
        /// <param name="columnSpan">The column span.</param>
        public RowSpanColumnSpan(int rowSpan, int columnSpan)
        {
            RowSpan = rowSpan;
            ColumnSpan = columnSpan;
        }

        #endregion Public Constructors
    }
}