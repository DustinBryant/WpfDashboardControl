namespace WpfDashboardControl.Resources.DashboardControl.Models
{
    /// <summary>
    /// Represents a Row and Column position
    /// </summary>
    public class RowAndColumn
    {
        #region Public Properties

        /// <summary>
        /// Gets the column.
        /// </summary>
        /// <value>The column.</value>
        public int Column { get; }

        /// <summary>
        /// Gets the row.
        /// </summary>
        /// <value>The row.</value>
        public int Row { get; }

        #endregion Public Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RowAndColumn"/> class.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="column">The column.</param>
        public RowAndColumn(int row, int column)
        {
            Row = row;
            Column = column;
        }

        #endregion Public Constructors
    }
}