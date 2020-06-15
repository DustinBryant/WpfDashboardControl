using Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfDashboardControl.Resources.DashboardControl.Models;
using DragEventArgs = System.Windows.DragEventArgs;

namespace WpfDashboardControl.Resources.DashboardControl
{
    /// <summary>
    /// Custom ItemsControl that represents a dynamic Dashboard similar to the one used on TFS(Azure DevOps Server) Dashboards
    /// </summary>
    public partial class DashboardHost
    {
        #region Public Fields

        /// <summary>
        /// The edit mode property
        /// </summary>
        public static readonly DependencyProperty EditModeProperty = DependencyProperty.Register("EditMode",
            typeof(bool), typeof(DashboardHost),
            new PropertyMetadata(false, (d, e) => ((DashboardHost)d).EditEnabler()));

        #endregion Public Fields

        #region Private Fields

        private const int ScrollIncrement = 15;
        private readonly PropertyChangeNotifier _itemsSourceChangeNotifier;
        private readonly List<WidgetHost> _widgetHosts = new List<WidgetHost>();
        private Canvas _canvasEditingBackground;
        private ScrollViewer _dashboardScrollViewer;
        private DragAdorner _draggingAdorner;
        private WidgetHost _draggingHost;
        private WidgetHostData _draggingHostData;
        private int _hostIndex;
        private Border _widgetDestinationHighlight;

        // To change the overall size of the widgets change the value here. This size is considered a block.
        private Size _widgetHostMinimumSize = new Size(165, 165);

        private List<WidgetHostData> _widgetHostsData = new List<WidgetHostData>();
        private Canvas _widgetsCanvasHost;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether the dashboard is in [edit mode].
        /// </summary>
        /// <value><c>true</c> if [edit mode]; otherwise, <c>false</c>.</value>
        public bool EditMode
        {
            get => (bool)GetValue(EditModeProperty);
            set => SetValue(EditModeProperty, value);
        }

        #endregion Public Properties

        #region Private Properties

        /// <summary>
        /// Gets the canvas editing background that shows empty spaces (gray square in the UI) for editing.
        /// </summary>
        /// <value>The canvas editing background.</value>
        private Canvas CanvasEditingBackground => _canvasEditingBackground ??
                                                  (_canvasEditingBackground = this.FindChildElementByName<Canvas>("CanvasEditingBackground"));

        /// <summary>
        /// Gets the dashboard scroll viewer.
        /// </summary>
        /// <value>The dashboard scroll viewer.</value>
        private ScrollViewer DashboardScrollViewer => _dashboardScrollViewer ??
                                                      (_dashboardScrollViewer = this.FindChildElementByName<ScrollViewer>("DashboardHostScrollViewer"));

        /// <summary>
        /// Gets the widgets canvas host.
        /// </summary>
        /// <value>The widgets canvas host.</value>
        private Canvas WidgetsCanvasHost
        {
            get
            {
                if (_widgetsCanvasHost != null)
                    return _widgetsCanvasHost;

                // We have to **cheat** in order to get the ItemsHost of this ItemsControl by
                // using reflection to gain access to the NonPublic member
                _widgetsCanvasHost = (Canvas)typeof(ItemsControl).InvokeMember("ItemsHost",
                    BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance,
                    null, this, null);

                WidgetsCanvasHost.HorizontalAlignment = HorizontalAlignment.Left;
                WidgetsCanvasHost.VerticalAlignment = VerticalAlignment.Top;

                SetupCanvases();

                return _widgetsCanvasHost;
            }
        }

        #endregion Private Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardHost"/> class.
        /// </summary>
        public DashboardHost()
        {
            InitializeComponent();

            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(Canvas)));
            Loaded += DashboardHost_Loaded;
            Unloaded += DashboardHost_Unloaded;

            _itemsSourceChangeNotifier = new PropertyChangeNotifier(this, ItemsSourceProperty);
            _itemsSourceChangeNotifier.ValueChanged += ItemsSource_Changed;
        }

        #endregion Public Constructors

        #region Protected Methods

        /// <summary>
        /// When overridden in a derived class, undoes the effects of the <see cref="M:System.Windows.Controls.ItemsControl.PrepareContainerForItemOverride(System.Windows.DependencyObject,System.Object)" /> method.
        /// </summary>
        /// <param name="element">The container element.</param>
        /// <param name="item">The item.</param>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            if (!(element is WidgetHost widgetHost))
                return;

            widgetHost.DragStarted -= WidgetHost_DragStarted;
            _widgetHosts.Remove(widgetHost);
            _widgetHostsData = _widgetHostsData.Where(widgetData => widgetData.HostIndex != widgetHost.HostIndex)
                .ToList();

			if (EditMode)
				FixArrangements();
        }

        /// <summary>
        /// Creates or identifies the element that is used to display the given item.
        /// </summary>
        /// <returns>The element that is used to display the given item.</returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new WidgetHost { HostIndex = _hostIndex ++};
        }

        /// <summary>
        /// Determines if the specified item is (or is eligible to be) its own container.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns><see langword="true" /> if the item is (or is eligible to be) its own container; otherwise, <see langword="false" />.</returns>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is WidgetHost;
        }

        /// <summary>
        /// Prepares the specified element to display the specified item.
        /// </summary>
        /// <param name="element">Element used to display the specified item.</param>
        /// <param name="item">Specified item.</param>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (!(element is WidgetHost widgetHost) || WidgetsCanvasHost == null)
                return;

            var widgetBase = widgetHost.DataContext as WidgetBase;

            Debug.Assert(widgetBase != null, nameof(widgetBase) + " != null");

            var widgetSpans = GetSpans(widgetBase.WidgetSize);

            // Set min/max dimensions of host so it isn't allowed to grow any larger or smaller
            var hostHeight = _widgetHostMinimumSize.Height * widgetSpans.RowSpan - widgetHost.Margin.Top - widgetHost.Margin.Bottom;
            var hostWidth = _widgetHostMinimumSize.Width * widgetSpans.ColumnSpan - widgetHost.Margin.Left - widgetHost.Margin.Right;

            widgetHost.MinHeight = hostHeight;
            widgetHost.MaxHeight = hostHeight;
            widgetHost.MinWidth = hostWidth;
            widgetHost.MaxWidth = hostWidth;

            // Subscribe to the widgets drag started and add the widget
            // to the _widgetHosts to keep tabs on it
            widgetHost.DragStarted += WidgetHost_DragStarted;
            _widgetHosts.Add(widgetHost);
            _widgetHostsData.Add(new WidgetHostData(widgetHost.HostIndex, widgetBase, widgetSpans));

            // Check if widget is new by seeing if ColumnIndex or RowIndex are set
            // If it isn't new then just set its location
            if (widgetBase.ColumnIndex != null && widgetBase.RowIndex != null)
            {
                SetWidgetRowAndColumn(widgetHost, (int)widgetBase.RowIndex, (int)widgetBase.ColumnIndex, widgetSpans);
                return;
            }

            // widget is new. Find the next available row and column and place the
            // widget then scroll to it if it's offscreen
            var nextAvailable = GetNextAvailableRowColumn(widgetSpans);

            SetWidgetRowAndColumn(widgetHost, nextAvailable.Row, nextAvailable.Column, widgetSpans);

            // Scroll to the new item if it is off screen
            var widgetsHeight = widgetSpans.RowSpan * _widgetHostMinimumSize.Height;
            var widgetEndVerticalLocation = nextAvailable.Row * _widgetHostMinimumSize.Height + widgetsHeight;

            var scrollViewerVerticalScrollPosition =
                DashboardScrollViewer.ViewportHeight + DashboardScrollViewer.VerticalOffset;

            if (!(widgetEndVerticalLocation >= DashboardScrollViewer.VerticalOffset) ||
                !(widgetEndVerticalLocation <= scrollViewerVerticalScrollPosition))
                DashboardScrollViewer.ScrollToVerticalOffset(
                    widgetEndVerticalLocation - widgetsHeight - ScrollIncrement);
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Handles the GiveFeedback event of the DraggingHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="GiveFeedbackEventArgs"/> instance containing the event data.</param>
        private static void DraggingHost_GiveFeedback(object sender, GiveFeedbackEventArgs args)
        {
            // Due to the DragDrop we have to use the GiveFeedback on the first parameter DependencyObject
            // passed into the DragDrop.DoDragDrop to force the cursor to show SizeAll
            args.UseDefaultCursors = false;
            Mouse.SetCursor(Cursors.SizeAll);
            args.Handled = true;
        }

        /// <summary>
        /// Gets the row and column spans of a specific WidgetSize.
        /// </summary>
        /// <param name="widgetSize">Size of the widget.</param>
        /// <returns>RowSpanColumnSpan.</returns>
        /// <exception cref="ArgumentOutOfRangeException">widgetSize - null</exception>
        private static RowSpanColumnSpan GetSpans(WidgetSize widgetSize)
        {
            switch (widgetSize)
            {
                case WidgetSize.OneByOne:
                    return new RowSpanColumnSpan(1, 1);
                case WidgetSize.OneByTwo:
                    return new RowSpanColumnSpan(1, 2);
                case WidgetSize.OneByThree:
                    return new RowSpanColumnSpan(1, 3);
                case WidgetSize.TwoByOne:
                    return new RowSpanColumnSpan(2, 1);
                case WidgetSize.TwoByTwo:
                    return new RowSpanColumnSpan(2, 2);
                case WidgetSize.TwoByThree:
                    return new RowSpanColumnSpan(2, 3);
                case WidgetSize.ThreeByOne:
                    return new RowSpanColumnSpan(3, 1);
                case WidgetSize.ThreeByTwo:
                    return new RowSpanColumnSpan(3, 2);
                case WidgetSize.ThreeByThree:
                    return new RowSpanColumnSpan(3, 3);
                default:
                    throw new ArgumentOutOfRangeException(nameof(widgetSize), widgetSize, null);
            }
        }

        /// <summary>
        /// Adds a column to the editing background canvas.
        /// </summary>
        private void AddCanvasEditingBackgroundColumn(int rowCount, int columnCount)
        {
            CanvasEditingBackground.Width += _widgetHostMinimumSize.Width;

            for (var i = 0; i < rowCount; i++)
            {
                var rectangleBackground = CreateGrayRectangleBackground();
                CanvasEditingBackground.Children.Add(rectangleBackground);
                Canvas.SetTop(rectangleBackground, i * _widgetHostMinimumSize.Height);
                Canvas.SetLeft(rectangleBackground, columnCount * _widgetHostMinimumSize.Width);
            }
        }

        /// <summary>
        /// Adds a row to the editing background canvas.
        /// </summary>
        private void AddCanvasEditingBackgroundRow(int rowCount, int columnCount)
        {
            CanvasEditingBackground.Height += _widgetHostMinimumSize.Height;

            for (var i = 0; i < columnCount; i++)
            {
                var rectangleBackground = CreateGrayRectangleBackground();
                CanvasEditingBackground.Children.Add(rectangleBackground);
                Canvas.SetTop(rectangleBackground, rowCount * _widgetHostMinimumSize.Height);
                Canvas.SetLeft(rectangleBackground, i * _widgetHostMinimumSize.Width);
            }
        }

        /// <summary>
        /// Returns a Rectangle that has a background that is gray. Used for the CanvasEditingBackground Canvas.
        /// </summary>
        /// <returns>Rectangle.</returns>
        private Rectangle CreateGrayRectangleBackground()
        {
            return new Rectangle
            {
                Height = Math.Floor(_widgetHostMinimumSize.Height * 90 / 100),
                Width = Math.Floor(_widgetHostMinimumSize.Width * 90 / 100),
                Fill = Brushes.LightGray
            };
        }

        /// <summary>
        /// Handles the Loaded event of the DashboardHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void DashboardHost_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DashboardHost_Loaded;

            // We only check WidgetsCanvasHost to initialize just in case it wasn't initialized
            // with pre-existing widgets being generated before load
            if (WidgetsCanvasHost == null)
                return;

            SizeChanged += DashboardHost_SizeChanged;
            PreviewDragOver += DashboardHost_PreviewDragOver;
        }

        /// <summary>
        /// Handles the PreviewDragOver event of the DashboardHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void DashboardHost_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Only continue if the item being dragged over the DashboardHost is a WidgetHost and
            // the widget host is within the _widgetHosts list
            if (!(e.Data.GetData(typeof(WidgetHost)) is WidgetHost draggingWidgetHost) ||
                _widgetHostsData.FirstOrDefault(widgetData => widgetData.HostIndex == draggingWidgetHost.HostIndex) == null)
                return;

            // Move the adorner to the appropriate position
            _draggingAdorner.LeftOffset = e.GetPosition(WidgetsCanvasHost).X;
            _draggingAdorner.TopOffset = e.GetPosition(WidgetsCanvasHost).Y;

            var adornerPosition = _draggingAdorner.TransformToVisual(WidgetsCanvasHost).Transform(new Point(0, 0));

            // The adorner will typically start out at X == 0 and Y == 0 which causes an unwanted effect of re-positioning
            // items when it isn't necessary.
            if (adornerPosition.X == 0 && adornerPosition.Y == 0)
                return;

            // When dragging and the adorner gets close to the sides of the scroll viewer then have the scroll viewer
            // automatically scroll in the direction of adorner's edges
            var adornerPositionRelativeToScrollViewer =
                _draggingAdorner.TransformToVisual(DashboardScrollViewer).Transform(new Point(0, 0));

            if (adornerPositionRelativeToScrollViewer.Y + _draggingAdorner.ActualHeight + ScrollIncrement >= DashboardScrollViewer.ViewportHeight)
                DashboardScrollViewer.ScrollToVerticalOffset(DashboardScrollViewer.VerticalOffset + ScrollIncrement);
            if (adornerPositionRelativeToScrollViewer.X + _draggingAdorner.ActualWidth + ScrollIncrement >= DashboardScrollViewer.ViewportWidth)
                DashboardScrollViewer.ScrollToHorizontalOffset(DashboardScrollViewer.HorizontalOffset + ScrollIncrement);
            if (adornerPositionRelativeToScrollViewer.Y - ScrollIncrement <= 0 && DashboardScrollViewer.VerticalOffset >= ScrollIncrement)
                DashboardScrollViewer.ScrollToVerticalOffset(DashboardScrollViewer.VerticalOffset - ScrollIncrement);
            if (adornerPositionRelativeToScrollViewer.X - ScrollIncrement <= 0 && DashboardScrollViewer.HorizontalOffset >= ScrollIncrement)
                DashboardScrollViewer.ScrollToHorizontalOffset(DashboardScrollViewer.HorizontalOffset - ScrollIncrement);

            // We need to get the adorner imaginary position or position in which we'll use to determine what cell it is hovering over.
            // We do this by getting the width of the host and then divide this by the span + 1
            // In a 1x1 widget this would essentially give us the half way point to which would change the _closestRowColumn
            // In a larger widget (2x2) this would give us the point at 1/3 of the size ensuring the widget can get to its destination more seamlessly
            var addToPositionX = draggingWidgetHost.ActualWidth / (_draggingHostData.WidgetSpans.ColumnSpan + 1);
            var addToPositionY = draggingWidgetHost.ActualHeight / (_draggingHostData.WidgetSpans.RowSpan + 1);

            // Get the closest row/column to the adorner "imaginary" position
            var closestRowColumn =
                GetClosestRowColumn(new Point(adornerPosition.X + addToPositionX, adornerPosition.Y + addToPositionY));

            // If there is no change to the stored closestRowColumn against the dragging RowIndex and ColumnIndex then there isn't
            // anything to set or arrange.
            if (_draggingHostData.WidgetBase.RowIndex == closestRowColumn.Row &&
                _draggingHostData.WidgetBase.ColumnIndex == closestRowColumn.Column)
                return;

            // Use the canvas to draw a square around the closestRowColumn to indicate where the _draggingWidgetHost will be when mouse is released
            var top = closestRowColumn.Row < 0 ? 0 : closestRowColumn.Row * _widgetHostMinimumSize.Height;
            var left = closestRowColumn.Column < 0 ? 0 : closestRowColumn.Column * _widgetHostMinimumSize.Width;

            Canvas.SetTop(_widgetDestinationHighlight, top);
            Canvas.SetLeft(_widgetDestinationHighlight, left);

            // Get all the widgets in the path of where the _dragging host will be set
            var movingWidgets = GetWidgetMoveList(_widgetHostsData
                    .FirstOrDefault(widgetData => widgetData == _draggingHostData), closestRowColumn, null)
                .OrderBy(widgetData => widgetData.WidgetBase?.RowIndex)
                .ToArray();

            // Set the _dragging host into its dragging position
            SetWidgetRowAndColumn(_draggingHost, closestRowColumn.Row, closestRowColumn.Column,
                _draggingHostData.WidgetSpans);

            // Move the movingWidgets down in rows the same amount of the _dragging hosts row span
            // unless there is a widget already there in that case increment until there isn't. We
            // used the OrderBy on the movingWidgets to make this work against widgets that have
            // already moved
            var movedWidgets = new List<WidgetHostData>();
            foreach (var widgetData in movingWidgets)
            {
                // Use the initial amount the dragging widget row size is
                var rowIncrease = _draggingHostData.WidgetSpans.RowSpan;

                // Find a row to move it
                Debug.Assert(widgetData.WidgetBase.RowIndex != null, "widgetData.WidgetBase.RowIndex != null");
                Debug.Assert(widgetData.WidgetBase.ColumnIndex != null, "widgetData.WidgetBase.ColumnIndex != null");

                while (true)
                {
                    var widgetAtLoc = WidgetAtLocation(widgetData.WidgetSpans,
                        new RowAndColumn((int)widgetData.WidgetBase.RowIndex + rowIncrease,
                            (int)widgetData.WidgetBase.ColumnIndex))
                        .Where(widgetHostData => !movingWidgets.Contains(widgetHostData) || movedWidgets.Contains(widgetHostData));

                    if (!widgetAtLoc.Any())
                        break;

                    rowIncrease++;
                }

                var movingHost =
                    _widgetHosts.FirstOrDefault(widgetHost => widgetHost.HostIndex == widgetData.HostIndex);

                SetWidgetRowAndColumn(movingHost, (int)widgetData.WidgetBase.RowIndex + rowIncrease,
                    (int)widgetData.WidgetBase.ColumnIndex, widgetData.WidgetSpans);

                movedWidgets.Add(widgetData);
            }

            FixArrangements();
        }

        /// <summary>
        /// Handles the SizeChanged event of the DashboardHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SizeChangedEventArgs"/> instance containing the event data.</param>
        private void DashboardHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!EditMode)
                return;

            RemoveExcessCanvasSize(CanvasEditingBackground);
            FillCanvasEditingBackground();
        }

        /// <summary>
        /// Handles the Unloaded event of the DashboardHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void DashboardHost_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= DashboardHost_Unloaded;
            SizeChanged -= DashboardHost_SizeChanged;
            PreviewDragOver -= DashboardHost_PreviewDragOver;

            if (_itemsSourceChangeNotifier != null)
                _itemsSourceChangeNotifier.ValueChanged -= ItemsSource_Changed;
        }

        /// <summary>
        /// Enabled/Disables edit functionality
        /// </summary>
        private void EditEnabler()
        {
            // Show or hide the CanvasEditingBackground depending on the EditMode
            CanvasEditingBackground.Visibility = EditMode ? Visibility.Visible : Visibility.Collapsed;

            if (EditMode)
                return;

            // We then need to remove all the extra row and column we no longer need
            RemoveExcessCanvasSize(CanvasEditingBackground);
            RemoveExcessCanvasSize(WidgetsCanvasHost);
        }

        /// <summary>
        /// Fills the canvas editing background.
        /// </summary>
        private void FillCanvasEditingBackground()
        {
            var visibleColumns = GetFullyVisibleColumn() + 1;
            var visibleRows = GetFullyVisibleRow() + 1;

            // Fill Visible Columns
            var rowCountForColumnAdditions = GetCanvasEditingBackgroundRowCount();
            while (true)
            {
                var columnCount = GetCanvasEditingBackgroundColumnCount();

                if (columnCount >= visibleColumns)
                    break;

                AddCanvasEditingBackgroundColumn(rowCountForColumnAdditions, columnCount);
            }

            // Fill Visible Rows
            var columnCountForRowAdditions = GetCanvasEditingBackgroundColumnCount();
            while (true)
            {
                var rowCount = GetCanvasEditingBackgroundRowCount();

                if (rowCount >= visibleRows)
                    break;

                AddCanvasEditingBackgroundRow(rowCount, columnCountForRowAdditions);
            }
        }

        private void FixArrangements()
        {
            var arrangementNecessary = true;

            //Need to check for empty spots to see if widgets in rows down from it can possible be placed in those empty spots
            //Once there are no more available to move we set arrangementNecessary to false and we're done
            while (arrangementNecessary)
                arrangementNecessary = ReArrangeFirstEmptySpot();
        }

        /// <summary>
        /// Gets the canvas editing background column count.
        /// </summary>
        /// <returns>System.Int32.</returns>
        private int GetCanvasEditingBackgroundColumnCount()
        {
            return (int)Math.Floor(CanvasEditingBackground.Width / _widgetHostMinimumSize.Width);
        }

        /// <summary>
        /// Gets the canvas editing background row count.
        /// </summary>
        /// <returns>System.Int32.</returns>
        private int GetCanvasEditingBackgroundRowCount()
        {
            return (int)Math.Floor(CanvasEditingBackground.Height / _widgetHostMinimumSize.Height);
        }

        /// <summary>
        /// Gets the closest row column from adornerPosition.
        /// </summary>
        /// <param name="adornerPosition">The adorner position.</param>
        /// <returns>RowAndColumn.</returns>
        private RowAndColumn GetClosestRowColumn(Point adornerPosition)
        {
            // First lets get the "real" closest row and column to the adorner Position
            // This is exact location to the square to which the adornerPosition point provided
            // is within
            var realClosestRow = (int)Math.Floor(adornerPosition.Y / _widgetHostMinimumSize.Height);
            var realClosestColumn = (int)Math.Floor(adornerPosition.X / _widgetHostMinimumSize.Width);

            // If the closest row and column are negatives that means the position is off screen
            // at this point we can return 0's and prevent extra calculations by ending this one here
            if (realClosestRow < 0 && realClosestColumn < 0)
                return new RowAndColumn(0, 0);

            // We need to set any negatives to 0 since we can't place anything off screen
            realClosestRow = realClosestRow < 0 ? 0 : realClosestRow;
            realClosestColumn = realClosestColumn < 0 ? 0 : realClosestColumn;

            var realClosestRowAndColumn = new RowAndColumn(realClosestRow, realClosestColumn);

            if (_draggingHostData.WidgetBase.RowIndex == realClosestRow && _draggingHostData.WidgetBase.ColumnIndex == realClosestColumn)
                return realClosestRowAndColumn;

            // We need to find all the widgets that land within the column that the adorner is currently
            // placed and if that dragging widget has a span we need to calculate that into this.
            // Once we have all those widgets we need to get the max row out of all the widgets
            var lastRowForColumn = _widgetHostsData
                .Where(widgetData =>
                {
                    if (widgetData == _draggingHostData)
                        return false;

                    // Loop through each span of the draggingBase
                    for (var i = 0; i < _draggingHostData.WidgetSpans.ColumnSpan; i++)
                    {
                        // Then loop through each span of the current widget being evaluated
                        for (var j = 0; j < widgetData.WidgetSpans.ColumnSpan; j++)
                        {
                            // If the column is within the adorner position and its span then include it
                            if (widgetData.WidgetBase.ColumnIndex + j == realClosestColumn + i)
                                return true;
                        }
                    }

                    return false;
                })
                // Get the row index and its span and calculated that number as being the row it's actually on
                // this helps in finding the max row the dragging widget can reside
                .Select(widgetData => widgetData.WidgetBase.RowIndex + widgetData.WidgetSpans.RowSpan - 1)
                // If there aren't any widgets is when this comes back null. In that case return 0 to the variable
                .Max() + 1 ?? 0;

            // If the adorner position is on the outside of other widgets and within the columns of that
            // position then return back the last used row + 1 (equates to being lastRowForColumn)
            if (realClosestRow >= lastRowForColumn)
                return new RowAndColumn(lastRowForColumn, realClosestColumn);

            // First lets see if we're moving down in rows and our column hasn't changed. If this is true lets
            // see if any of the widgets at the location can fit where the now dragging widget was.
            // If so then we can assume the ReArrangeFirstEmptySpot will move a widget into the old location
            // and we can just return that realClosestRowAndColumn
            if (realClosestColumn == _draggingHostData.WidgetBase.ColumnIndex &&
                realClosestRow > _draggingHostData.WidgetBase.RowIndex)
            {
                var widgetsAtLocation = WidgetAtLocation(_draggingHostData.WidgetSpans, realClosestRowAndColumn)
                    .Where(widgetData => widgetData != _draggingHostData)
                    .OrderBy(widgetData => widgetData.WidgetBase.RowIndex);

                int? checkedGoodRow = null;
                foreach (var widgetHostData in widgetsAtLocation)
                {
                    if (checkedGoodRow != null && widgetHostData.WidgetBase.RowIndex != checkedGoodRow)
                        continue;

                    Debug.Assert(widgetHostData.WidgetBase.RowIndex != null, "widgetHostData.WidgetBase.RowIndex != null");
                    Debug.Assert(widgetHostData.WidgetBase.ColumnIndex != null, "widgetHostData.WidgetBase.ColumnIndex != null");

                    var difference = realClosestRow + (int)_draggingHostData.WidgetBase.RowIndex +
                        _draggingHostData.WidgetSpans.RowSpan - 1;
                    for (var i = difference; i >= realClosestRow; i--)
                    {
                        var widgetsAtWidgetHostDataLocation = WidgetAtLocation(widgetHostData.WidgetSpans,
                            new RowAndColumn(i, (int)widgetHostData.WidgetBase.ColumnIndex))
                            .Where(widgetData => widgetData != _draggingHostData)
                            .ToArray();

                        if (widgetsAtWidgetHostDataLocation.Contains(widgetHostData))
                        {
                            if (_draggingHostData.WidgetBase.RowIndex + widgetHostData.WidgetSpans.RowSpan <=
                                realClosestRow)
                            {
                                checkedGoodRow = widgetHostData.WidgetBase.RowIndex;
                                if (!WidgetAtLocation(widgetHostData.WidgetSpans,
                                    new RowAndColumn((int)_draggingHostData.WidgetBase.RowIndex,
                                        (int)widgetHostData.WidgetBase.ColumnIndex)).Any(widgetData =>
                                   widgetData != widgetHostData && widgetData != _draggingHostData))
                                    return realClosestRowAndColumn;
                            }

                            //continue;
                        }

                        if (_draggingHostData.WidgetBase.RowIndex + 1 >= realClosestRow &&
                            realClosestRow <= _draggingHostData.WidgetBase.RowIndex +
                            _draggingHostData.WidgetSpans.RowSpan - 1)
                            continue;

                        if (!widgetsAtWidgetHostDataLocation.Any() &&
                            i < realClosestRow + _draggingHostData.WidgetSpans.RowSpan - 1)
                            return realClosestRowAndColumn;
                    }
                }
            }

            // The adorner position is within other widgets. We need to see if the row(s) above the closest are available
            // to fit the draggingHost into it and place it there if we can.
            var potentialMovingWidgets =
                GetWidgetMoveList(
                    _widgetHostsData.FirstOrDefault(widgetData => widgetData == _draggingHostData),
                    realClosestRowAndColumn, null).ToArray();

            RowAndColumn foundBetterSpot = null;
            for (var i = realClosestRow - 1; i >= 0; i--)
            {
                var rowAndColumnToCheck = new RowAndColumn(i, realClosestColumn);

                // See if we can use the location. We keep iterating after this to see if there is even a better spot we can occupy
                var widgetAtLocation = WidgetAtLocation(_draggingHostData.WidgetSpans, rowAndColumnToCheck)
                    .Where(widgetData =>
                        !potentialMovingWidgets.Contains(widgetData) && widgetData != _draggingHostData);

                if (widgetAtLocation.Any())
                    break;

                foundBetterSpot = new RowAndColumn(i, realClosestColumn);
            }

            return foundBetterSpot ?? realClosestRowAndColumn;
        }

        /// <summary>
        /// Gets the count of the Columns that are fully visible taking into account
        /// the DashboardScrollViewer could have a horizontal offset
        /// </summary>
        /// <returns>System.Int32.</returns>
        private int GetFullyVisibleColumn()
        {
            return Convert.ToInt32(Math.Floor(
                (ActualWidth + DashboardScrollViewer.HorizontalOffset) / _widgetHostMinimumSize.Width));
        }

        /// <summary>
        /// Gets the count of the Rows that are fully visible taking into account
        /// the DashboardScrollViewer could have a vertical offset
        /// </summary>
        /// <returns>System.Int32.</returns>
        private int GetFullyVisibleRow()
        {
            return Convert.ToInt32(Math.Floor(
                (ActualHeight + DashboardScrollViewer.VerticalOffset) / _widgetHostMinimumSize.Height));
        }

        /// <summary>
        /// Gets the maximum rows and columns of a grid including their spans as placement.
        /// </summary>
        /// <returns>WpfDashboardControl.Models.RowAndColumn.</returns>
        private RowAndColumn GetMaxRowsAndColumns()
        {
            // Need to get all rows adding their row spans and columns adding their column spans returned back
            // as an array of RowAndColumns
            var widgetsRowsAndColumns = _widgetHostsData
                .Select(widgetData =>
                {
                    Debug.Assert(widgetData.WidgetBase.RowIndex != null, "widgetData.WidgetBase.RowIndex != null");
                    Debug.Assert(widgetData.WidgetBase.ColumnIndex != null, "widgetData.WidgetBase.ColumnIndex != null");

                    return new RowAndColumn((int)widgetData.WidgetBase.RowIndex + widgetData.WidgetSpans.RowSpan,
                        (int)widgetData.WidgetBase.ColumnIndex + widgetData.WidgetSpans.ColumnSpan);
                })
                .ToArray();

            if (!widgetsRowsAndColumns.Any())
                return new RowAndColumn(1, 1);

            // Need to get the max row and max columns from the list of RowAndColumns
            var maxRows = widgetsRowsAndColumns
                .Select(rowColumn => rowColumn.Row)
                .Max();
            var maxColumns = widgetsRowsAndColumns
                .Select(rowColumn => rowColumn.Column)
                .Max();

            return new RowAndColumn(maxRows, maxColumns);
        }

        /// <summary>
        /// Gets the next available row/column.
        /// </summary>
        /// <param name="widgetSpans">The widget spans.</param>
        /// <returns>RowAndColumn.</returns>
        private RowAndColumn GetNextAvailableRowColumn(RowSpanColumnSpan widgetSpans)
        {
            if (_widgetHostsData.Count == 1)
                return new RowAndColumn(0, 0);

            // Get fully visible column count
            var fullyVisibleColumns = GetFullyVisibleColumn();

            // We need to loop through each row and in each row loop through each column
            // to see if the space is currently occupied. When it is available then return
            // what Row and Column the new widget will occupy
            var rowCount = 0;
            while (true)
            {
                for (var column = 0; column < fullyVisibleColumns; column++)
                {
                    var widgetAlreadyThere = WidgetAtLocation(widgetSpans, new RowAndColumn(rowCount, column));

                    if (widgetAlreadyThere != null && widgetAlreadyThere.Any())
                        continue;

                    // Need to check if the new widget when placed would be outside
                    // the visible columns. If so then we move onto the next row/column
                    var newWidgetSpanOutsideVisibleColumn = false;
                    for (var i = 0; i < widgetSpans.ColumnSpan + 1; i++)
                    {
                        if (column + i <= fullyVisibleColumns)
                            continue;

                        newWidgetSpanOutsideVisibleColumn = true;
                        break;
                    }

                    // The newest widget won't cover up an existing row/column so lets
                    // return the specific row/column the widget can occupy
                    if (!newWidgetSpanOutsideVisibleColumn)
                        return new RowAndColumn(rowCount, column);
                }

                rowCount++;
            }
        }

        /// <summary>
        /// Recursively gets all the widgets in the path of the provided widgetHost into a list.
        /// </summary>
        /// <param name="widgetData">The widget data.</param>
        /// <param name="rowAndColumnPlacement">The row and column placement.</param>
        /// <param name="widgetsThatNeedToMove">The widgets that need to move.</param>
        /// <returns>List&lt;WidgetHost&gt;.</returns>
        private IEnumerable<WidgetHostData> GetWidgetMoveList(WidgetHostData widgetData, RowAndColumn rowAndColumnPlacement, List<WidgetHostData> widgetsThatNeedToMove)
        {
            if (widgetsThatNeedToMove == null)
                widgetsThatNeedToMove = new List<WidgetHostData>();

            var widgetsAtLocation = new List<WidgetHostData>();

            // If the widgetHost is the _draggingHost then we only need to get the direct widgets that occupy the
            // provided rowAndColumnPlacement
            if (widgetData == _draggingHostData)
            {
                widgetsAtLocation
                    .AddRange(WidgetAtLocation(widgetData.WidgetSpans, rowAndColumnPlacement)
                        .Where(widgetAtLocationData => widgetAtLocationData != _draggingHostData)
                        .ToList());
            }
            else
            {
                // If we're a widget at the designated widgetHost we need to check how many spaces
                // we're moving and check each widget that could potentially be in those spaces
                var widgetRowMovementCount = rowAndColumnPlacement.Row - widgetData.WidgetBase.RowIndex + 1;

                Debug.Assert(widgetData.WidgetBase.RowIndex != null, "widgetData.WidgetBase.RowIndex != null");

                for (var i = 0; i < widgetRowMovementCount; i++)
                {
                    widgetsAtLocation
                        .AddRange(WidgetAtLocation(widgetData.WidgetSpans,
                            new RowAndColumn((int)widgetData.WidgetBase.RowIndex + i, rowAndColumnPlacement.Column)).Where(
                            widgetHostData => widgetHostData != widgetData && widgetHostData != _draggingHostData));
                }
            }

            // If there aren't any widgets at the location then just return the list we've been maintaining
            if (widgetsAtLocation.Count < 1)
                return widgetsThatNeedToMove.Distinct();

            // Since we have widgets at the designated location we need add to the list any widgets that
            // could potentially move as a result of the widgetHost movement
            for (var widgetAtLocationIndex = 0;
                widgetAtLocationIndex < widgetsAtLocation.Count;
                widgetAtLocationIndex++)
            {
                // If we're already tracking the widget then continue to the next
                if (widgetsThatNeedToMove.IndexOf(widgetsAtLocation[widgetAtLocationIndex]) >= 0)
                    continue;

                var widgetDataAtLocation = widgetsAtLocation[widgetAtLocationIndex];

                Debug.Assert(widgetDataAtLocation.WidgetBase.ColumnIndex != null, "widgetDataAtLocation.WidgetBase.ColumnIndex != null");

                // Need to recursively check if any widgets that are now in the place that this widget was also get moved down to
                // make room
                var proposedRowAndColumn = new RowAndColumn(rowAndColumnPlacement.Row + widgetData.WidgetSpans.RowSpan,
                    (int)widgetDataAtLocation.WidgetBase.ColumnIndex);

                // Get the widgets at the new location this one is moving to
                var currentWidgetsAtNewLocation =
                    WidgetAtLocation(widgetDataAtLocation.WidgetSpans, proposedRowAndColumn)
                        .Where(widget =>
                        {
                            if (widget == widgetsAtLocation[widgetAtLocationIndex])
                                return false;

                            var widgetLocationIndex = widgetsAtLocation.IndexOf(widget);

                            // We check here if the potential widget is already scheduled to move and return false in that case
                            return widgetLocationIndex >= 0 && widgetLocationIndex <= widgetAtLocationIndex;
                        })
                        .ToArray();

                // If there are no widgets at the location then we can just add the widget and continue
                if (!currentWidgetsAtNewLocation.Any())
                {
                    widgetsThatNeedToMove.Add(widgetsAtLocation[widgetAtLocationIndex]);
                    GetWidgetMoveList(widgetsAtLocation[widgetAtLocationIndex], proposedRowAndColumn,
                        widgetsThatNeedToMove);
                    continue;
                }

                // We need to get the max row span or size we're dealing with to offset the change
                var maxAdditionalRows = currentWidgetsAtNewLocation
                    .Select(widgetAtNewLocationData => widgetAtNewLocationData.WidgetSpans.RowSpan)
                    .Max();

                // Add the widget to the list and move to the next item
                widgetsThatNeedToMove.Add(widgetsAtLocation[widgetAtLocationIndex]);
                GetWidgetMoveList(widgetsAtLocation[widgetAtLocationIndex],
                    new RowAndColumn(proposedRowAndColumn.Row + maxAdditionalRows, proposedRowAndColumn.Column),
                    widgetsThatNeedToMove);
            }

            // Return the list we've been maintaining
            return widgetsThatNeedToMove.Distinct();
        }

        /// <summary>
        /// Handles the Changed event of the ItemsSource control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void ItemsSource_Changed(object sender, EventArgs args)
        {
            // Enforce the ItemsSource be of type ICollect<WidgetBase> as most of the code behind
            // relies on there being a WidgetBase as the item
            if (!(ItemsSource is ICollection<WidgetBase>))
                throw new InvalidOperationException(
                    $"{nameof(DashboardHost)} ItemsSource binding must be an ICollection of {nameof(WidgetBase)} type");
        }

        /// <summary>
        /// Finds the first empty spot and if a widget in a row down from the empty spot can be placed there
        /// it will automatically move that widget there. If it can't it will keep going until it finds a widget
        /// that can move. If it gets to the end and no more widgets can be moved it returns false indicated
        /// there aren't any more moves that can occur
        /// </summary>
        /// <returns><c>true</c> if a widget was moved, <c>false</c> otherwise.</returns>
        private bool ReArrangeFirstEmptySpot()
        {
            // Need to get all rows and columns taking up space
            var maxRowsAndColumn = GetMaxRowsAndColumns();

            // We loop through each row and column on the hunt for a blank space
            for (var mainRowIndex = 0; mainRowIndex < maxRowsAndColumn.Row; mainRowIndex++)
            {
                for (var mainColumnIndex = 0; mainColumnIndex < maxRowsAndColumn.Column; mainColumnIndex++)
                {
                    if (WidgetAtLocation(new RowSpanColumnSpan(1, 1), new RowAndColumn(mainRowIndex, mainColumnIndex)).Any())
                        continue;

                    // We need to peak to the next columns to see if they are blank as well
                    var additionalBlankColumnsOnMainRowIndex = 0;
                    for (var subColumnIndex = mainColumnIndex + 1; subColumnIndex < maxRowsAndColumn.Column; subColumnIndex++)
                    {
                        if (WidgetAtLocation(new RowSpanColumnSpan(1, 1),
                            new RowAndColumn(mainRowIndex, subColumnIndex)).Any())
                            break;

                        additionalBlankColumnsOnMainRowIndex++;
                    }

                    var stopChecking = false;

                    // Once we find an empty space we start looping from each row after the mainRowIndex using the same mainColumnIndex + additionalColumnNumber
                    // to find a widget that is a potential candidate to be moved up in rows
                    for (var subRowIndex = mainRowIndex + 1; subRowIndex < maxRowsAndColumn.Row; subRowIndex++)
                    {
                        for (var additionalColumnNumber = 0; additionalColumnNumber < additionalBlankColumnsOnMainRowIndex + 1; additionalColumnNumber++)
                        {
                            var secondaryWidgetAtLocation = WidgetAtLocation(new RowSpanColumnSpan(1, 1),
                                new RowAndColumn(subRowIndex, mainColumnIndex + additionalColumnNumber))
                                .ToArray();

                            // We can move on to next row if there is no widget in the space
                            if (!secondaryWidgetAtLocation.Any() || _draggingHostData != null &&
                                secondaryWidgetAtLocation.First() == _draggingHostData)
                                continue;

                            var possibleCandidateWidgetData = secondaryWidgetAtLocation.First();

                            // Once we find the next widget in the row we're checking, we need to see if that widget has the same RowIndex and ColumnIndex
                            // of the loops above. If it doesn't we can end looking for a replacement for this spot and find the next empty spot
                            if (possibleCandidateWidgetData.WidgetBase.RowIndex != subRowIndex ||
                                possibleCandidateWidgetData.WidgetBase.ColumnIndex != mainColumnIndex)
                            {
                                stopChecking = true;
                                break;
                            }

                            // Now we have a good candidate lets see if it'll fit at the location of the empty spot from mainRowIndex and
                            // mainColumnIndex + additionalColumnNumber
                            var canSecondaryWidgetBePlacedMainRowColumn =
                                WidgetAtLocation(possibleCandidateWidgetData.WidgetSpans, new RowAndColumn(mainRowIndex, mainColumnIndex))
                                    .All(widget => widget == secondaryWidgetAtLocation.First());

                            if (!canSecondaryWidgetBePlacedMainRowColumn)
                            {
                                stopChecking = true;
                                break;
                            }

                            // Everything looks good and the widget can be placed in the empty spot
                            // We also return true here to say that a widget was moved due to this process being ran
                            var movingWidgetHost = _widgetHosts.FirstOrDefault(widgetHost =>
                                widgetHost.HostIndex == possibleCandidateWidgetData.HostIndex);

                            SetWidgetRowAndColumn(movingWidgetHost, mainRowIndex, mainColumnIndex,
                                possibleCandidateWidgetData.WidgetSpans);
                            return true;
                        }

                        if (stopChecking)
                            break;
                    }
                }
            }

            // No more widgets can be moved to fill in empty spots
            return false;
        }

        /// <summary>
        /// Removes the excess canvas size that is no longer needed.
        /// </summary>
        private void RemoveExcessCanvasSize(Canvas canvas)
        {
            var rowAndColumnMax = GetMaxRowsAndColumns();

            if (canvas == CanvasEditingBackground)
            {
                var removeRectangles = CanvasEditingBackground.Children.OfType<Rectangle>()
                    .Where(child =>
                    {
                        var canvasRow = Canvas.GetTop(child) / _widgetHostMinimumSize.Height;
                        var canvasColumn = Canvas.GetLeft(child) / _widgetHostMinimumSize.Width;

                        return canvasRow >= rowAndColumnMax.Row || canvasColumn >= rowAndColumnMax.Column;
                    })
                    .ToArray();

                for (var i = removeRectangles.Length - 1; i >= 0; i--)
                    CanvasEditingBackground.Children.Remove(removeRectangles[i]);

                canvas.Width = rowAndColumnMax.Column * _widgetHostMinimumSize.Width;
                canvas.Height = rowAndColumnMax.Row * _widgetHostMinimumSize.Height;
                return;
            }

            var canvasHasChildren = canvas.Children.Count > 0;
            canvas.Width = (canvasHasChildren ? rowAndColumnMax.Column : 0) * _widgetHostMinimumSize.Width;
            canvas.Height = (canvasHasChildren ? rowAndColumnMax.Row : 0) * _widgetHostMinimumSize.Height;
        }

        /// <summary>
        /// Sets up the canvases within the DashboardHost.
        /// </summary>
        private void SetupCanvases()
        {
            // Get the canvas used to show where a dragging widget host will land when dragging ends
            var highlightWidgetCanvas = this.FindChildElementByName<Canvas>("HighlightWidgetCanvas");

            // Add a border control to the canvas which will be manually position manipulated when there is a dragging host
            _widgetDestinationHighlight = new Border
            {
                BorderBrush = Brushes.DeepSkyBlue,
                Background = Brushes.LightBlue,
                Opacity = 0.4,
                BorderThickness = new Thickness(2),
                Visibility = Visibility.Hidden
            };

            highlightWidgetCanvas.Children.Add(_widgetDestinationHighlight);

            WidgetsCanvasHost.Height = 0;
            WidgetsCanvasHost.Width = 0;

            // Add first rectangle for CanvasEditingBackground
            CanvasEditingBackground.Height = _widgetHostMinimumSize.Height;
            CanvasEditingBackground.Width = _widgetHostMinimumSize.Width;

            var rectangle = CreateGrayRectangleBackground();
            CanvasEditingBackground.Children.Add(rectangle);
            Canvas.SetTop(rectangle, 0);
            Canvas.SetLeft(rectangle, 0);
        }

        /// <summary>
        /// Sets the widget row and column for the WidgetsCanvasHost and changes the RowIndex and ColumnIndex of
        /// the widgetHost's WidgetBase context.
        /// </summary>
        /// <param name="widgetHost">The widget host.</param>
        /// <param name="rowNumber">The row number.</param>
        /// <param name="columnNumber">The column number.</param>
        /// <param name="rowColumnSpan">The row column span.</param>
        private void SetWidgetRowAndColumn(WidgetHost widgetHost, int rowNumber, int columnNumber, RowSpanColumnSpan rowColumnSpan)
        {
            var widgetBase = widgetHost.DataContext as WidgetBase;

            Debug.Assert(widgetBase != null, nameof(widgetBase) + " != null");

            widgetBase.RowIndex = rowNumber;
            widgetBase.ColumnIndex = columnNumber;

            var maxRowsAndColumns = GetMaxRowsAndColumns();
            WidgetsCanvasHost.Height = maxRowsAndColumns.Row * _widgetHostMinimumSize.Height;
            WidgetsCanvasHost.Width = maxRowsAndColumns.Column * _widgetHostMinimumSize.Width;

            Canvas.SetTop(widgetHost, rowNumber * _widgetHostMinimumSize.Height);
            Canvas.SetLeft(widgetHost, columnNumber * _widgetHostMinimumSize.Width);

            var rowCountForColumnAdditions = GetCanvasEditingBackgroundRowCount();
            while (true)
            {
                var columnCount = GetCanvasEditingBackgroundColumnCount();

                if (columnCount - 1 >= columnNumber + rowColumnSpan.ColumnSpan - 1)
                    break;

                AddCanvasEditingBackgroundColumn(rowCountForColumnAdditions, columnCount);
            }

            var columnCountForRowAdditions = GetCanvasEditingBackgroundColumnCount();
            while (GetCanvasEditingBackgroundRowCount() - 1 < rowNumber + rowColumnSpan.RowSpan - 1)
            {
                var rowCount = GetCanvasEditingBackgroundRowCount();

                if (rowCount - 1 >= rowNumber + rowColumnSpan.RowSpan - 1)
                    break;

                AddCanvasEditingBackgroundRow(rowCount, columnCountForRowAdditions);
            }
        }

        /// <summary>
        /// Gets a list of WidgetHosts that occupy the provided rowAndColumnToCheck
        /// </summary>
        /// <param name="widgetSpan">The widget span.</param>
        /// <param name="rowAndColumnToCheck">The row and column to check.</param>
        /// <returns>List&lt;WidgetHost&gt;.</returns>
        private IEnumerable<WidgetHostData> WidgetAtLocation(RowSpanColumnSpan widgetSpan, RowAndColumn rowAndColumnToCheck)
        {
            // Need to see if a widget is already at the specific row and column
            return _widgetHostsData
                .Where(widgetData =>
                {
                    // If there is a specific widget there then return true
                    if (widgetData.WidgetBase.RowIndex == rowAndColumnToCheck.Row &&
                        widgetData.WidgetBase.ColumnIndex == rowAndColumnToCheck.Column)
                        return true;

                    // We need to look at the widgetHost being checked right now
                    // to see if its spans cover up a specific row/column
                    for (var i = 0; i < widgetData.WidgetSpans.RowSpan; i++)
                    {
                        for (var j = 0; j < widgetData.WidgetSpans.ColumnSpan; j++)
                        {
                            // If the span of the widgetHost covers up the next available
                            // row or column then we should consider this widget row/column
                            // already being used
                            if (widgetData.WidgetBase.RowIndex + i == rowAndColumnToCheck.Row &&
                                widgetData.WidgetBase.ColumnIndex + j == rowAndColumnToCheck.Column)
                                return true;

                            // Now, lets check how big the widget going to be added will be
                            // and see if this will cover up an already existing widget
                            // and if so then consider that row/column being used
                            for (var k = 0; k < widgetSpan.RowSpan; k++)
                            {
                                for (var l = 0; l < widgetSpan.ColumnSpan; l++)
                                {
                                    if (widgetData.WidgetBase.RowIndex + i == rowAndColumnToCheck.Row + k &&
                                        widgetData.WidgetBase.ColumnIndex + j == rowAndColumnToCheck.Column + l)
                                        return true;
                                }
                            }
                        }
                    }

                    return false;
                });
        }

        /// <summary>
        /// Handles the DragStarted event of a WidgetHost.
        /// </summary>
        /// <param name="widgetHost">The widget host.</param>
        private void WidgetHost_DragStarted(WidgetHost widgetHost)
        {
            if (!EditMode)
                return;

            try
            {
                // We need to make the DashboardHost allowable to have items dropped on it
                AllowDrop = true;

                _draggingHost = widgetHost;
                _draggingHostData =
                    _widgetHostsData.FirstOrDefault(widgetData => widgetData.HostIndex == _draggingHost.HostIndex);

                _widgetDestinationHighlight.Width =
                    _draggingHost.ActualWidth + _draggingHost.Margin.Left + _draggingHost.Margin.Right;
                _widgetDestinationHighlight.Height =
                    _draggingHost.ActualHeight + _draggingHost.Margin.Top + _draggingHost.Margin.Bottom;
                _widgetDestinationHighlight.Visibility = Visibility.Visible;

                Debug.Assert(_draggingHostData.WidgetBase.RowIndex != null, "_draggingHostData.WidgetBase.RowIndex != null");
                Debug.Assert(_draggingHostData.WidgetBase.ColumnIndex != null, "_draggingHostData.WidgetBase.ColumnIndex != null");
                Canvas.SetTop(_widgetDestinationHighlight, (int)_draggingHostData.WidgetBase.RowIndex * _widgetHostMinimumSize.Height);
                Canvas.SetLeft(_widgetDestinationHighlight, (int)_draggingHostData.WidgetBase.ColumnIndex * _widgetHostMinimumSize.Width);

                // Need to create the adorner that will be used to drag a control around the DashboardHost
                _draggingAdorner = new DragAdorner(_draggingHost, _draggingHost, Mouse.GetPosition(_draggingHost));
                _draggingHost.GiveFeedback += DraggingHost_GiveFeedback;
                AdornerLayer.GetAdornerLayer(_draggingHost)?.Add(_draggingAdorner);

                // Need to hide the _draggingHost to give off the illusion that we're moving it somewhere
                _draggingHost.Visibility = Visibility.Hidden;

                DragDrop.DoDragDrop(_draggingHost, new DataObject(_draggingHost), DragDropEffects.Move);
            }
            finally
            {
                // Need to cleanup after the DoDragDrop ends by setting back everything to its default state
                _draggingHost.GiveFeedback -= DraggingHost_GiveFeedback;
                Mouse.SetCursor(Cursors.Arrow);
                AllowDrop = false;
                AdornerLayer.GetAdornerLayer(_draggingHost)?.Remove(_draggingAdorner);
                _draggingHost.Visibility = Visibility.Visible;
                _draggingHostData = null;
                _draggingHost = null;
                _widgetDestinationHighlight.Visibility = Visibility.Hidden;
            }
        }

        #endregion Private Methods
    }
}