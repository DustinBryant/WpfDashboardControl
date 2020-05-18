using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WpfDashboardControl.Models;
using DragEventArgs = System.Windows.DragEventArgs;

namespace WpfDashboardControl.Controls
{
    /// <summary>
    /// Custom ItemsControl that represents a dynamic Dashboard similar to the one used on TFS(Azure DevOps Server) Dashboards
    /// </summary>
    public partial class DashboardHost
    {
        #region Public Fields

        /// <summary>
        /// The editing complete property
        /// </summary>
        public static readonly DependencyProperty EditingCompleteProperty =
            DependencyProperty.Register("EditingComplete", typeof(ICommand), typeof(DashboardHost));

        /// <summary>
        /// The edit mode enabled property
        /// </summary>
        public static readonly DependencyProperty EditModeEnabledProperty =
            DependencyProperty.Register("EditModeEnabled", typeof(ICommand), typeof(DashboardHost));

        /// <summary>
        /// The edit mode key
        /// </summary>
        public static readonly DependencyPropertyKey EditModeKey =
            DependencyProperty.RegisterReadOnly("EditMode", typeof(bool), typeof(DashboardHost),
                new PropertyMetadata(false));

        /// <summary>
        /// The edit mode property
        /// </summary>
        public static readonly DependencyProperty EditModeProperty = EditModeKey.DependencyProperty;

        #endregion Public Fields

        #region Private Fields

        private const int ScrollIncrement = 15;
        private readonly List<WidgetHost> _widgetHosts = new List<WidgetHost>();
        private RowAndColumn _closestRowColumn;
        private ScrollViewer _dashboardScrollViewer;
        private DragAdorner _draggingAdorner;
        private WidgetHost _draggingHost;
        private WidgetBase _draggingWidgetBase;
        private Grid _gridEditingBackground;
        private Border _widgetDestinationHighlight;

        // To change the overall size of the widgets change the value here. This size is considered a block.
        private Size _widgetHostMinimumSize = new Size(100, 120);

        private Grid _widgetsGridHost;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the editing complete.
        /// </summary>
        /// <value>The editing complete.</value>
        public ICommand EditingComplete
        {
            get => (ICommand)GetValue(EditingCompleteProperty);
            set => SetValue(EditingCompleteProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the dashboard is in [edit mode].
        /// </summary>
        /// <value><c>true</c> if [edit mode]; otherwise, <c>false</c>.</value>
        public bool EditMode => (bool)GetValue(EditModeProperty);

        /// <summary>
        /// Gets or sets the edit mode enabled.
        /// </summary>
        /// <value>The edit mode enabled.</value>
        public ICommand EditModeEnabled
        {
            get => (ICommand)GetValue(EditModeEnabledProperty);
            set => SetValue(EditModeEnabledProperty, value);
        }

        #endregion Public Properties

        #region Private Properties

        /// <summary>
        /// Gets the dashboard scroll viewer.
        /// </summary>
        /// <value>The dashboard scroll viewer.</value>
        private ScrollViewer DashboardScrollViewer => _dashboardScrollViewer ?? (_dashboardScrollViewer = this.FindChildElementByName<ScrollViewer>("DashboardHostScrollViewer"));

        /// <summary>
        /// Get the grid used to show empty spaces (gray square in the UI) for editing
        /// </summary>
        /// <value>The grid background grid.</value>
        private Grid GridEditingBackground => _gridEditingBackground ?? (_gridEditingBackground = this.FindChildElementByName<Grid>("GridEditingBackground"));

        /// <summary>
        /// Gets the widgets grid host.
        /// </summary>
        /// <value>The widgets grid host.</value>
        private Grid WidgetsGridHost
        {
            get
            {
                if (_widgetsGridHost != null)
                    return _widgetsGridHost;

                // We have to **cheat** in order to get the ItemsHost of this ItemsControl by
                // using reflection to gain access to the NonPublic member
                _widgetsGridHost = (Grid)typeof(ItemsControl).InvokeMember("ItemsHost",
                    BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance,
                    null, this, null);

                SetupMainGrid();

                return _widgetsGridHost;
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

            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(Grid)));
            Loaded += DashboardHost_Loaded;
            Unloaded += DashboardHost_Unloaded;

            ItemsSourceChangedEventSubscriber();
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
        }

        /// <summary>
        /// Creates or identifies the element that is used to display the given item.
        /// </summary>
        /// <returns>The element that is used to display the given item.</returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new WidgetHost();
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

            if (!(element is WidgetHost widgetHost) || WidgetsGridHost == null)
                return;

            // Find the next available spot for the item and place it there
            var widgetBase = (WidgetBase)widgetHost.DataContext;

            // Check if widget is new by seeing if ColumnIndex or RowIndex are set
            // If it is new find the next available row and column and place the
            // widget then scroll to it if it's offscreen
            if (widgetBase.ColumnIndex == null || widgetBase.RowIndex == null)
            {
                var nextAvailable = GetNextAvailableRowColumn(widgetBase.WidgetSize);

                SetRow(widgetHost, nextAvailable.Row);
                SetColumn(widgetHost, nextAvailable.Column);

                // Scroll to the new item if it is off screen
                var widgetsHeight = GetSpans(widgetBase.WidgetSize).RowSpan * _widgetHostMinimumSize.Height;
                var widgetEndVerticalLocation = nextAvailable.Row * _widgetHostMinimumSize.Height + widgetsHeight;

                var scrollViewerVerticalScrollPosition =
                    DashboardScrollViewer.ViewportHeight + DashboardScrollViewer.VerticalOffset;

                if (!(widgetEndVerticalLocation >= DashboardScrollViewer.VerticalOffset) ||
                    !(widgetEndVerticalLocation <= scrollViewerVerticalScrollPosition))
                    DashboardScrollViewer.ScrollToVerticalOffset(widgetEndVerticalLocation - widgetsHeight - ScrollIncrement);
            }
            else
            {
                SetRow(widgetHost, (int)widgetBase.RowIndex);
                SetColumn(widgetHost, (int)widgetBase.ColumnIndex);
            }

            // Subscribe to the widgets drag started and add the widget
            // to the _widgetHosts to keep tabs on it
            widgetHost.DragStarted += WidgetHost_DragStarted;
            _widgetHosts.Add(widgetHost);
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
                case WidgetSize.TwoByOne:
                    return new RowSpanColumnSpan(2, 1);
                case WidgetSize.TwoByTwo:
                    return new RowSpanColumnSpan(2, 2);
                default:
                    throw new ArgumentOutOfRangeException(nameof(widgetSize), widgetSize, null);
            }
        }

        /// <summary>
        /// Adds a grid column definition to the provided grid. If it is the GridEditingBackground also
        /// creates a border (gray square) child for the new cells that get generated
        /// </summary>
        /// <param name="grid">The grid.</param>
        private void AddGridColumnDefinition(Grid grid)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(_widgetHostMinimumSize.Width),
                MaxWidth = _widgetHostMinimumSize.Width,
                MinWidth = _widgetHostMinimumSize.Width
            });

            if (grid != GridEditingBackground)
                return;

            var rowCount = GridEditingBackground.RowDefinitions.Count;
            var newColumnNumber = GridEditingBackground.ColumnDefinitions.Count;

            for (var i = 0; i < rowCount; i++)
            {
                var borderBackground = CreateGrayBorderBackground();
                GridEditingBackground.Children.Add(borderBackground);
                Grid.SetRow(borderBackground, i);
                Grid.SetColumn(borderBackground, newColumnNumber - 1);
            }
        }

        /// <summary>
        /// Adds a grid row definition to the provided grid. If it is the GridEditingBackground also
        /// creates a border (gray square) child for the new cells that get generated
        /// </summary>
        /// <param name="grid">The grid.</param>
        private void AddGridRowDefinition(Grid grid)
        {
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(_widgetHostMinimumSize.Height),
                MaxHeight = _widgetHostMinimumSize.Height,
                MinHeight = _widgetHostMinimumSize.Height
            });

            if (grid != GridEditingBackground)
                return;

            var newRowNumber = GridEditingBackground.RowDefinitions.Count;
            var columnCount = GridEditingBackground.ColumnDefinitions.Count;

            for (var i = 0; i < columnCount; i++)
            {
                var borderBackground = CreateGrayBorderBackground();
                GridEditingBackground.Children.Add(borderBackground);
                Grid.SetRow(borderBackground, newRowNumber - 1);
                Grid.SetColumn(borderBackground, i);
            }
        }

        /// <summary>
        /// Handles the OnClick event of the DoneEditingButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ButtonDoneEditing_OnClick(object sender, RoutedEventArgs e)
        {
            // Enable Edit mode
            EditEnabler(false);
        }

        /// <summary>
        /// Handles the OnClick event of the EditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ButtonEdit_OnClick(object sender, RoutedEventArgs e)
        {
            // Disable Edit mode
            EditEnabler(true);
        }

        /// <summary>
        /// Returns a Border that has a background that is gray. Used for the GridEditingBackground Grid.
        /// </summary>
        /// <returns>Border.</returns>
        private Border CreateGrayBorderBackground()
        {
            return new Border
            {
                Background = Brushes.LightGray,
                Height = Math.Floor(_widgetHostMinimumSize.Height * 85 / 100),
                Width = Math.Floor(_widgetHostMinimumSize.Width * 85 / 100)
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
                _widgetHosts.FirstOrDefault(host => host == draggingWidgetHost) == null)
                return;

            // Move the adorner to the appropriate position
            _draggingAdorner.LeftOffset = e.GetPosition(WidgetsGridHost).X;
            _draggingAdorner.TopOffset = e.GetPosition(WidgetsGridHost).Y;

            var adornerPosition = _draggingAdorner.TransformToVisual(WidgetsGridHost).Transform(new Point(0, 0));

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
            var addToPositionX = draggingWidgetHost.ActualWidth /
                                (GetSpans(_draggingWidgetBase.WidgetSize).ColumnSpan + 1);

            var addToPositionY = draggingWidgetHost.ActualHeight /
                                 (GetSpans(_draggingWidgetBase.WidgetSize).RowSpan + 1);

            // Get the closest row/column to the adorner "imaginary" position
            _closestRowColumn =
                GetClosestRowColumn(new Point(adornerPosition.X + addToPositionX, adornerPosition.Y + addToPositionY));

            // Use the canvas to draw a square around the _closestRowColumn to indicate where the _draggingWidgetHost will be when mouse is released
            var top = _closestRowColumn.Row < 0 ? 0 : _closestRowColumn.Row * _widgetHostMinimumSize.Height;
            var left = _closestRowColumn.Column < 0 ? 0 : _closestRowColumn.Column * _widgetHostMinimumSize.Width;

            Canvas.SetTop(_widgetDestinationHighlight, top);
            Canvas.SetLeft(_widgetDestinationHighlight, left);

            // Size the canvas highlighter to the size of the _draggingWidgetHost
            _widgetDestinationHighlight.Width =
                _draggingHost.ActualWidth + _draggingHost.Margin.Left + _draggingHost.Margin.Right;
            _widgetDestinationHighlight.Height =
                _draggingHost.ActualHeight + _draggingHost.Margin.Top + _draggingHost.Margin.Bottom;
            _widgetDestinationHighlight.Visibility = Visibility.Visible;

            // If there is no change to the stored _closestRowColumn against the dragging RowIndex and ColumnIndex then there isn't
            // anything to set or arrange.
            if (_draggingWidgetBase.RowIndex == _closestRowColumn.Row &&
                _draggingWidgetBase.ColumnIndex == _closestRowColumn.Column)
                return;

            // Arrange the other WidgetHosts around the _draggingHost
            SetAndArrange(_draggingHost, _closestRowColumn, null, true);
        }

        /// <summary>
        /// Handles the Unloaded event of the DashboardHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void DashboardHost_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= DashboardHost_Unloaded;
            PreviewDragOver -= DashboardHost_PreviewDragOver;

            // Watch if the ItemsSource changes and ensure the ItemsSource is of type ICollection<WidgetBase>
            ItemsSourceChangedEventSubscriber(false);
        }

        /// <summary>
        /// Enabled/Disables edit functionality
        /// </summary>
        /// <param name="enable">if set to <c>true</c> [enable].</param>
        private void EditEnabler(bool enable)
        {
            SetValue(EditModeKey, enable);

            // Show or hide the editing/done editing buttons depending on the EditMode
            var editButton = this.FindChildElementByName<Button>("EditButton");
            var doneEditingButton = this.FindChildElementByName<Button>("DoneEditingButton");

            editButton.Visibility = EditMode ? Visibility.Collapsed : Visibility.Visible;
            doneEditingButton.Visibility = EditMode ? Visibility.Visible : Visibility.Collapsed;

            // Show or hide the GridEditingBackground depending on the EditMode
            GridEditingBackground.Visibility = EditMode ? Visibility.Visible : Visibility.Collapsed;

            if (EditMode)
            {
                // We need to make our entire editing background fill the screen with gray boxes if they aren't already
                var visibleColumns = GetFullyVisibleGridColumn() + 1;
                var visibleRows = GetFullyVisibleGridRow() + 1;

                while (GridEditingBackground.ColumnDefinitions.Count < visibleColumns)
                    AddGridColumnDefinition(GridEditingBackground);

                while (GridEditingBackground.RowDefinitions.Count < visibleRows)
                    AddGridRowDefinition(GridEditingBackground);

                EditModeEnabled?.Execute(null);
                return;
            }

            // We're getting out of edit mode so reduce down the columns and rows to what is contained in them
            var rowAndColumnMax = GetMaxRowsAndColumns();

            // Local method to remove any excess row definitions for the provided grid
            void RemoveExcessRowDefinitions(Grid grid)
            {
                for (var i = grid.RowDefinitions.Count - 1; i >= 0; i--)
                {
                    if (i < rowAndColumnMax.Row)
                        return;

                    grid.RowDefinitions.Remove(grid.RowDefinitions[i]);
                }
            }

            // Local method to remove any excess column definitions for the provided grid
            void RemoveExcessColumnDefinitions(Grid grid)
            {
                for (var i = grid.ColumnDefinitions.Count - 1; i >= 0; i--)
                {
                    if (i < rowAndColumnMax.Column)
                        return;

                    grid.ColumnDefinitions.Remove(grid.ColumnDefinitions[i]);
                }
            }

            // We get all the children to remove from the background grid by seeing if any of those
            // children are in places that the main grid doesn't have content
            var removeBorders = GridEditingBackground.Children.OfType<Border>().Where(child =>
            {
                var gridRow = Grid.GetRow(child);
                var gridColumn = Grid.GetColumn(child);

                return gridRow >= rowAndColumnMax.Row || gridColumn >= rowAndColumnMax.Column;
            }).ToArray();

            // We have to manually remove the children from the background grid
            for (var i = removeBorders.Length - 1; i >= 0; i--)
                GridEditingBackground.Children.Remove(removeBorders[i]);

            // We then remove all the extra row definitions we no longer need
            RemoveExcessRowDefinitions(GridEditingBackground);

            // Then we remove all the extra column definitions we no longer need
            RemoveExcessColumnDefinitions(GridEditingBackground);

            // We need to then remove all the extra WidgetsGridHost row definitions where there is no content
            RemoveExcessRowDefinitions(WidgetsGridHost);

            // Lastly, we remove the column definitions for the WidgetsGridHost where there is no content
            RemoveExcessColumnDefinitions(WidgetsGridHost);

            EditingComplete?.Execute(null);
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

            // Now we need to get an array of the widgets list that contains
            // the entire list without the current _draggingHost
            var widgetsWithoutDragging = _widgetHosts
                .Where(widget => widget != _draggingHost)
                .ToArray();

            var draggingBaseSpans = GetSpans(_draggingWidgetBase.WidgetSize);

            // We need to find all the widgets that land within the column that the adorner is currently
            // placed and if that dragging widget has a span we need to calculate that into this.
            // Once we have all those widgets we need to get the max row out of all the widgets
            var lastRowForColumn = widgetsWithoutDragging
                .Where(widget =>
                {
                    var widgetBase = (WidgetBase)widget.DataContext;

                    // Loop through each span of the draggingBase
                    for (var i = 0; i < draggingBaseSpans.ColumnSpan; i++)
                    {
                        // Then loop through each span of the current widget being evaluated
                        for (var j = 0; j < GetSpans(widgetBase.WidgetSize).ColumnSpan; j++)
                        {
                            // If the column is within the adorner position and its span then include it
                            if (widgetBase.ColumnIndex + j == realClosestColumn + i)
                                return true;
                        }
                    }

                    return false;
                })
                .Select(widget =>
                {
                    // Get the row index and its span and calculated that number as being the row it's actually on
                    // this helps in finding the max row the dragging widget can reside
                    var widgetBase = (WidgetBase)widget.DataContext;
                    return widgetBase.RowIndex + GetSpans(widgetBase.WidgetSize).RowSpan - 1;
                })
                // If there aren't any widgets is when this comes back null. In that case return 0 to the variable
                .Max() + 1 ?? 0;

            // If the adorner position is on the outside of other widgets and within the columns of that
            // position then return back the last used row + 1 (equates to being lastRowForColumn)
            if (realClosestRow >= lastRowForColumn)
                return new RowAndColumn(lastRowForColumn, realClosestColumn);

            // The adorner position is within other widgets. We need to see if the row(s) above the closest are available
            // to fit the draggingHost into it and place it there if we can.
            RowAndColumn foundBetterSpot = null;
            for (var i = realClosestRow - 1; i >= 0; i--)
            {
                var rowAndColumnToCheck = new RowAndColumn(i, realClosestColumn);
                if (WidgetAtLocation(new RowSpanColumnSpan(1, 1), rowAndColumnToCheck)
                    .Where(widget => widget != _draggingHost)
                    .ToArray().Length > 0)
                    break;

                var widgetsAtRealLocation = WidgetAtLocation(draggingBaseSpans,
                        new RowAndColumn(realClosestRow, realClosestColumn))
                    .Where(widget => widget != _draggingHost)
                    .ToArray();

                // We need to reverse loop from the realClosestRow looking for blank/open spots
                // If we find one we typically want to check if any of the widgets at the
                // real location can swap up to the potential dead spot.
                for (var j = realClosestRow - 1; j >= 0; j--)
                {
                    foreach (var widgetHost in widgetsAtRealLocation)
                    {
                        var widgetBase = (WidgetBase)widgetHost.DataContext;
                        var widgetSpans = GetSpans(widgetBase.WidgetSize);
                        Debug.Assert(widgetBase.ColumnIndex != null, "widgetBase.ColumnIndex != null");
                        var widgetCheckLocation = new RowAndColumn(j, (int)widgetBase.ColumnIndex);

                        //  If there are widgets already there then continue onto the next widgetAtRealLocation
                        if (WidgetAtLocation(widgetSpans, widgetCheckLocation)
                            .Count(widget => widget != _draggingHost) > 0)
                            continue;

                        // This is a convenience action to assist in getting out of nested for loops. It's purpose is to
                        // make certain that the dragging hosts new spot would still allow the widgetsAtRealLocation
                        // to move into its place. We do that by looping the widgetsAtRealLocations spans and then
                        // the dragging widgets spans to see if the dragging widgets new spot takes up any spot the
                        // widgetsAtRealLocation could possibly occupy. If we can fit even with the new proposed
                        // dragging location then return true
                        bool CanFit()
                        {
                            for (var k = 0; k < widgetSpans.RowSpan; k++)
                            {
                                for (var l = 0; l < widgetSpans.ColumnSpan; l++)
                                {
                                    for (var m = 0; m < draggingBaseSpans.RowSpan; m++)
                                    {
                                        for (var n = 0; n < draggingBaseSpans.ColumnSpan; n++)
                                        {
                                            if (widgetCheckLocation.Row + k == realClosestRow + m &&
                                                widgetCheckLocation.Column + l == realClosestColumn + n)
                                                return false;
                                        }
                                    }
                                }
                            }

                            return true;
                        }

                        var canItFit = CanFit();

                        return canItFit ? new RowAndColumn(realClosestRow, realClosestColumn) : new RowAndColumn(i, realClosestColumn);
                    }
                }

                // See if we can use the location. We keep iterating after this to see if there is even a better spot we can occupy
                if (WidgetAtLocation(draggingBaseSpans, rowAndColumnToCheck)
                        .Where(widget => widget != _draggingHost)
                        .ToArray().Length < 1)
                    foundBetterSpot = new RowAndColumn(i, realClosestColumn);
            }

            return foundBetterSpot ?? new RowAndColumn(realClosestRow, realClosestColumn);
        }

        /// <summary>
        /// Gets the count of the Grid Columns that are fully visible taking into account
        /// the DashboardScrollViewer could have a horizontal offset
        /// </summary>
        /// <returns>System.Int32.</returns>
        private int GetFullyVisibleGridColumn()
        {
            return Convert.ToInt32(Math.Floor(
                (ActualWidth + DashboardScrollViewer.HorizontalOffset) /
                WidgetsGridHost.ColumnDefinitions[0].MaxWidth));
        }

        /// <summary>
        /// Gets the count of the Grid Rows that are fully visible taking into account
        /// the DashboardScrollViewer could have a vertical offset
        /// </summary>
        /// <returns>System.Int32.</returns>
        private int GetFullyVisibleGridRow()
        {
            return Convert.ToInt32(Math.Floor(
                (ActualHeight + DashboardScrollViewer.VerticalOffset) /
                WidgetsGridHost.RowDefinitions[0].MaxHeight));
        }

        /// <summary>
        /// Gets the maximum rows and columns of a grid including their spans as placement.
        /// </summary>
        /// <returns>WpfDashboardControl.Models.RowAndColumn.</returns>
        private RowAndColumn GetMaxRowsAndColumns()
        {
            // Need to get all rows adding their row spans and columns adding their column spans returned back
            // as an array of RowAndColumns
            var widgetsRowsAndColumns = _widgetHosts
                .Select(widget =>
                {
                    var widgetBase = (WidgetBase)widget.DataContext;
                    var widgetSpans = GetSpans(widgetBase.WidgetSize);
                    Debug.Assert(widgetBase.RowIndex != null, "widgetBase.RowIndex != null");
                    Debug.Assert(widgetBase.ColumnIndex != null, "widgetBase.ColumnIndex != null");

                    return new RowAndColumn((int)widgetBase.RowIndex + widgetSpans.RowSpan,
                        (int)widgetBase.ColumnIndex + widgetSpans.ColumnSpan);
                })
                .ToArray();

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
        /// <param name="widgetSize">Size of the widget.</param>
        /// <returns>RowAndColumn.</returns>
        private RowAndColumn GetNextAvailableRowColumn(WidgetSize widgetSize)
        {
            if (_widgetHosts.Count < 1)
                return new RowAndColumn(0, 0);

            // Get fully visible grid column count
            var fullyVisibleGridsColumns = GetFullyVisibleGridColumn();

            // We need to know the size the widget plans to occupy
            var widgetSpans = GetSpans(widgetSize);

            // We need to loop through each row and in each row loop through each column
            // to see if the space is currently occupied. When it is available then return
            // what Row and Column the new widget will occupy
            var rowCount = 0;
            while (true)
            {
                for (var column = 0; column < fullyVisibleGridsColumns; column++)
                {
                    var widgetAlreadyThere = WidgetAtLocation(widgetSpans, new RowAndColumn(rowCount, column));

                    if (widgetAlreadyThere != null && widgetAlreadyThere.Count > 0)
                        continue;

                    // Need to check if the new widget when placed would be outside
                    // the visible columns. If so then we move onto the next row/column
                    var newWidgetSpanOutsideVisibleColumn = false;
                    for (var i = 0; i < widgetSpans.ColumnSpan + 1; i++)
                    {
                        if (column + i <= fullyVisibleGridsColumns)
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
        /// Adds or removes event handling for ItemsSource changed of this ItemsControl
        /// </summary>
        /// <param name="addEvent">if set to <c>true</c> [add event].</param>
        private void ItemsSourceChangedEventSubscriber(bool addEvent = true)
        {
            var dependencyPropertyDescriptor =
                DependencyPropertyDescriptor.FromProperty(ItemsSourceProperty, typeof(ItemsControl));

            if (addEvent)
                dependencyPropertyDescriptor.AddValueChanged(this, ItemsSource_Changed);
            else
                dependencyPropertyDescriptor.RemoveValueChanged(this, ItemsSource_Changed);
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
            // Need to get all rows adding their row spans and columns adding their column spans returned back
            // as an array of RowAndColumns
            var maxRowsAndColumn = GetMaxRowsAndColumns();

            // We loop through each row and column on the hunt for a blank space
            for (var mainRowIndex = 0; mainRowIndex < maxRowsAndColumn.Row; mainRowIndex++)
            {
                for (var mainColumnIndex = 0; mainColumnIndex < maxRowsAndColumn.Column; mainColumnIndex++)
                {
                    var widgetAtLocation =
                        WidgetAtLocation(new RowSpanColumnSpan(1, 1), new RowAndColumn(mainRowIndex, mainColumnIndex));

                    if (widgetAtLocation.Count > 0)
                        continue;

                    // We need to peak to the next columns to see if they are blank as well
                    var additionalBlankColumnsOnMainRowIndex = 0;
                    for (var subColumnIndex = mainColumnIndex + 1; subColumnIndex < maxRowsAndColumn.Column; subColumnIndex++)
                    {
                        if (WidgetAtLocation(new RowSpanColumnSpan(1, 1),
                            new RowAndColumn(mainRowIndex, subColumnIndex)).Count > 0)
                            break;

                        additionalBlankColumnsOnMainRowIndex++;
                    }

                    var breakOutOfNestedFor = false;

                    // Once we find an empty space we start looping from each row after the mainRowIndex using the same mainColumnIndex + additionalColumnNumber
                    // to find a widget that is a potential candidate to be moved up in rows
                    for (var subRowIndex = mainRowIndex + 1; subRowIndex < maxRowsAndColumn.Row; subRowIndex++)
                    {
                        if (breakOutOfNestedFor)
                            break;

                        for (var additionalColumnNumber = 0; additionalColumnNumber < additionalBlankColumnsOnMainRowIndex + 1; additionalColumnNumber++)
                        {
                            var secondaryWidgetAtLocation = WidgetAtLocation(new RowSpanColumnSpan(1, 1),
                                new RowAndColumn(subRowIndex, mainColumnIndex + additionalColumnNumber));

                            // We can move on to next row if there is no widget in the space
                            if (secondaryWidgetAtLocation.Count < 1 || secondaryWidgetAtLocation[0] == _draggingHost)
                                continue;

                            var possibleCandidateWidgetBase = (WidgetBase)secondaryWidgetAtLocation[0].DataContext;

                            // Once we find the next widget in the row we're checking, we need to see if that widget has the same RowIndex and ColumnIndex
                            // of the loops above. If it doesn't we can end looking for a replacement for this spot and find the next empty spot
                            if (possibleCandidateWidgetBase.RowIndex != subRowIndex ||
                                possibleCandidateWidgetBase.ColumnIndex != mainColumnIndex)
                            {
                                breakOutOfNestedFor = true;
                                break;
                            }

                            // Now we have a good candidate lets see if it'll fit at the location of the empty spot from mainRowIndex and
                            // mainColumnIndex + additionalColumnNumber
                            var canSecondaryWidgetBePlacedMainRowColumn =
                                WidgetAtLocation(GetSpans(possibleCandidateWidgetBase.WidgetSize),
                                        new RowAndColumn(mainRowIndex, mainColumnIndex))
                                    .Where(widget => widget != secondaryWidgetAtLocation[0]).ToArray().Length < 1;

                            if (!canSecondaryWidgetBePlacedMainRowColumn)
                            {
                                breakOutOfNestedFor = true;
                                break;
                            }

                            // Everything looks good and the widget can be placed in the empty spot
                            // We also return true here to say that a widget was moved due to this process being ran
                            SetRow(secondaryWidgetAtLocation[0], mainRowIndex);
                            SetColumn(secondaryWidgetAtLocation[0], mainColumnIndex);
                            return true;
                        }
                    }
                }
            }

            // No more widgets can be moved to fill in empty spots
            return false;
        }

        /// <summary>
        /// Sets a widgetHost to the specified rowAndColumnPlacement and recursively forces any widget already
        /// in that space to then move down row(s) until it doesn't take up the new rowAndColumnPlacement. Then,
        /// if widgetHostIsDraggingWidget it goes through each row and column filling empty spaces between
        /// widgets with available sized widgets in the next rows if it can. Keeps the dashboard tight and together.
        /// </summary>
        /// <param name="widgetHost">The widget host.</param>
        /// <param name="rowAndColumnPlacement">The row and column placement.</param>
        /// <param name="ignoreAdditionalMovementHosts">The ignore additional movement hosts.</param>
        /// <param name="widgetHostIsDraggingHost">if set to <c>true</c> [widget host is dragging host].</param>
        private void SetAndArrange(WidgetHost widgetHost, RowAndColumn rowAndColumnPlacement,
            ICollection<WidgetHost> ignoreAdditionalMovementHosts = null, bool widgetHostIsDraggingHost = false)
        {
            if (widgetHost == null)
                return;

            // This is a local action used for convenience it basically sets the row and column
            // and if its the dragging host we re-arrange empty spots and move everything up
            // that can
            void SetThenReArrange()
            {
                SetRow(widgetHost, rowAndColumnPlacement.Row);
                SetColumn(widgetHost, rowAndColumnPlacement.Column);

                if (!widgetHostIsDraggingHost)
                    return;

                var arrangementNecessary = true;

                //Need to check for empty spots to see if widgets in rows down from it can possible be placed in those empty spots
                //Once there are no more available to move we set arrangementNecessary to false and we're done
                while (arrangementNecessary)
                    arrangementNecessary = ReArrangeFirstEmptySpot();
            }

            // Need to get the WidgetSize from the WidgetBase and find the size it needs to occupy
            var widgetBase = (WidgetBase)widgetHost.DataContext;
            Debug.Assert(widgetBase.RowIndex != null, "widgetBase.RowIndex != null");

            var widgetSpans = GetSpans(widgetBase.WidgetSize);

            // Need to get the widgets that are currently occupying the new rowAndColumnPlacement
            // And move them down if there are any
            var widgetsAtLocation = new List<WidgetHost>();

            // If its the widgetHostIsDraggingHost then we only need to check other widgets that occupy the location
            if (widgetHostIsDraggingHost)
            {
                widgetsAtLocation
                    .AddRange(WidgetAtLocation(widgetSpans, rowAndColumnPlacement)
                        .Where(widget => widget != widgetHost)
                        .ToList());
            }
            else
            {
                // If we're a widget at the designated widgetHost we need to check how many spaces
                // we're moving and check each widget that could potentially be in those spaces
                var widgetRowMovementCount = rowAndColumnPlacement.Row - widgetBase.RowIndex + 1;

                for (var i = 0; i < widgetRowMovementCount; i++)
                {
                    widgetsAtLocation
                        .AddRange(WidgetAtLocation(widgetSpans, new RowAndColumn((int)widgetBase.RowIndex + i, rowAndColumnPlacement.Column))
                            .Where(widget =>
                            {
                                if (widget == widgetHost)
                                    return false;

                                if (ignoreAdditionalMovementHosts == null)
                                    return true;

                                // We use the ignoreAdditionalMovement to prevent an already about to move
                                // widget from being moved again prematurely
                                return !ignoreAdditionalMovementHosts.Contains(widget);
                            })
                            .ToList());
                }
            }

            // Since there isn't anything there just set it directly and get out
            if (widgetsAtLocation.Count < 1)
            {
                SetThenReArrange();
                return;
            }

            // Since we have widgets at the designated location we need to move them down row(s) to make room
            // for the widget being placed. The main dragging widget will then re-arrange once its placed
            for (var widgetAtLocationIndex = 0; widgetAtLocationIndex < widgetsAtLocation.Count; widgetAtLocationIndex++)
            {
                var widgetAtLocationBase = (WidgetBase)widgetsAtLocation[widgetAtLocationIndex].DataContext;

                Debug.Assert(widgetAtLocationBase.ColumnIndex != null, "widgetAtLocationBase.ColumnIndex != null");

                // Need to recursively check if any widgets that are now in the place that this widget was also get moved down to
                // make room
                var proposedRowAndColumn = new RowAndColumn(rowAndColumnPlacement.Row + widgetSpans.RowSpan,
                    (int)widgetAtLocationBase.ColumnIndex);

                // Get the widgets at the new location this one is moving to unless the widget is already scheduled to move later
                var currentWidgetsAtNewLocation =
                    WidgetAtLocation(GetSpans(widgetAtLocationBase.WidgetSize), proposedRowAndColumn)
                        .Where(widget =>
                        {
                            if (widget == widgetsAtLocation[widgetAtLocationIndex])
                                return false;

                            var widgetLocationIndex = widgetsAtLocation.IndexOf(widget);

                            // We check here if the potential widget is already scheduled to move and return false in that case
                            return widgetLocationIndex >= 0 && widgetLocationIndex <= widgetAtLocationIndex;
                        })
                        .ToArray();

                // If there are no widgets at the location then we can just set and continue forward
                if (currentWidgetsAtNewLocation.Length < 1)
                {
                    SetAndArrange(widgetsAtLocation[widgetAtLocationIndex], proposedRowAndColumn, widgetsAtLocation);
                    continue;
                }

                // We need to get the max row span or size we're dealing with to offset the change
                var maxAdditionalRows = currentWidgetsAtNewLocation
                    .Select(widget =>
                    {
                        var atLocationBase = (WidgetBase)widget.DataContext;
                        return GetSpans(atLocationBase.WidgetSize).RowSpan;
                    })
                    .Max();

                // Recursively check the next widget movement and if additional widgets need to move
                SetAndArrange(widgetsAtLocation[widgetAtLocationIndex],
                    new RowAndColumn(proposedRowAndColumn.Row + maxAdditionalRows, proposedRowAndColumn.Column),
                    widgetsAtLocation);
            }

            // All done, lets set the last one in the recursion
            SetThenReArrange();
        }

        /// <summary>
        /// Sets the widgetHost column index and column span to the grid and changes the ColumnIndex of the
        /// widgetHost's WidgetBase context.
        /// </summary>
        /// <param name="widgetHost">The widget host.</param>
        /// <param name="columnNumber">The column number.</param>
        private void SetColumn(WidgetHost widgetHost, int columnNumber)
        {
            var widgetBase = (WidgetBase)widgetHost.DataContext;
            widgetBase.ColumnIndex = columnNumber;
            var widgetSpans = GetSpans(widgetBase.WidgetSize);

            while (WidgetsGridHost.ColumnDefinitions.Count - 1 < columnNumber + widgetSpans.ColumnSpan - 1)
                AddGridColumnDefinition(WidgetsGridHost);

            while (GridEditingBackground.ColumnDefinitions.Count - 1 < columnNumber + widgetSpans.ColumnSpan - 1)
                AddGridColumnDefinition(GridEditingBackground);

            Grid.SetColumn(widgetHost, columnNumber);
            Grid.SetColumnSpan(widgetHost, GetSpans(widgetBase.WidgetSize).ColumnSpan);
        }

        /// <summary>
        /// Sets the widgetHost row index and row span to the grid and changes the RowIndex of the
        /// widgetHost's WidgetBase context.
        /// </summary>
        /// <param name="widgetHost">The widget host.</param>
        /// <param name="rowNumber">The row number.</param>
        private void SetRow(WidgetHost widgetHost, int rowNumber)
        {
            var widgetBase = (WidgetBase)widgetHost.DataContext;
            widgetBase.RowIndex = rowNumber;
            var widgetSpans = GetSpans(widgetBase.WidgetSize);

            while (WidgetsGridHost.RowDefinitions.Count - 1 < rowNumber + widgetSpans.RowSpan - 1)
                AddGridRowDefinition(WidgetsGridHost);

            while (GridEditingBackground.RowDefinitions.Count - 1 < rowNumber + widgetSpans.RowSpan - 1)
                AddGridRowDefinition(GridEditingBackground);

            Grid.SetRow(widgetHost, rowNumber);
            Grid.SetRowSpan(widgetHost, GetSpans(widgetBase.WidgetSize).RowSpan);
        }

        /// <summary>
        /// Sets up the main grid within the DashboardHost.
        /// </summary>
        private void SetupMainGrid()
        {
            // Get the canvas used to show where a dragging widget host will land when dragging ends
            var gridHighlightCanvas = this.FindChildElementByName<Canvas>("GridHighlightCanvas");

            // Add a border control to the canvas which will be manually position manipulated when there is a dragging host
            _widgetDestinationHighlight = new Border
            {
                BorderBrush = Brushes.DeepSkyBlue,
                Background = Brushes.LightBlue,
                Opacity = 0.4,
                BorderThickness = new Thickness(2),
                Visibility = Visibility.Hidden
            };

            gridHighlightCanvas.Children.Add(_widgetDestinationHighlight);

            // Add a single row and column for both grids
            AddGridRowDefinition(WidgetsGridHost);
            AddGridColumnDefinition(WidgetsGridHost);

            AddGridRowDefinition(GridEditingBackground);
            AddGridColumnDefinition(GridEditingBackground);
        }

        /// <summary>
        /// Gets a list of WidgetHosts that occupy the provided rowAndColumnToCheck
        /// </summary>
        /// <param name="widgetSpan">The widget span.</param>
        /// <param name="rowAndColumnToCheck">The row and column to check.</param>
        /// <returns>List&lt;WidgetHost&gt;.</returns>
        private List<WidgetHost> WidgetAtLocation(RowSpanColumnSpan widgetSpan, RowAndColumn rowAndColumnToCheck)
        {
            // Need to see if a widget is already at the specific row and column
            return _widgetHosts
                .Where(host =>
                {
                    var widgetBase = (WidgetBase) host.DataContext;
                    var widgetSpans = GetSpans(widgetBase.WidgetSize);

                    // If there is a specific widget there then return true
                    if (widgetBase.RowIndex == rowAndColumnToCheck.Row &&
                        widgetBase.ColumnIndex == rowAndColumnToCheck.Column)
                        return true;

                    // We need to look at the widgetHost being checked right now
                    // to see if its spans cover up a specific grid row/column
                    for (var i = 0; i < widgetSpans.RowSpan; i++)
                    {
                        for (var j = 0; j < widgetSpans.ColumnSpan; j++)
                        {
                            // If the span of the widgetHost covers up the next available
                            // row or column then we should consider this widget row/column
                            // already being used
                            if (widgetBase.RowIndex + i == rowAndColumnToCheck.Row &&
                                widgetBase.ColumnIndex + j == rowAndColumnToCheck.Column)
                                return true;

                            // Now, lets check how big the widget going to be added will be
                            // and see if this will cover up an already existing widget
                            // and if so then consider that row/column being used
                            for (var k = 0; k < widgetSpan.RowSpan; k++)
                            {
                                for (var l = 0; l < widgetSpan.ColumnSpan; l++)
                                {
                                    if (widgetBase.RowIndex + i == rowAndColumnToCheck.Row + k &&
                                        widgetBase.ColumnIndex + j == rowAndColumnToCheck.Column + l)
                                        return true;
                                }
                            }
                        }
                    }

                    return false;
                })
                .ToList();
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
                _draggingWidgetBase = (WidgetBase) _draggingHost.DataContext;

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
                _draggingWidgetBase = null;
                _draggingHost = null;
                _widgetDestinationHighlight.Visibility = Visibility.Hidden;
                _closestRowColumn = null;
            }
        }

        #endregion Private Methods
    }
}