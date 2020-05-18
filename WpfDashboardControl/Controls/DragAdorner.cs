using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfDashboardControl.Controls
{
    /// <summary>
    /// Adorner used for dragging
    /// Implements the <see cref="System.Windows.Documents.Adorner" />
    /// </summary>
    /// <seealso cref="System.Windows.Documents.Adorner" />
    public sealed class DragAdorner : Adorner
    {
        #region Private Fields

        private readonly Rectangle _child;
        private double _leftOffset;
        private Point _startPoint;
        private double _topOffset;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the left offset.
        /// </summary>
        /// <value>The left offset.</value>
        public double LeftOffset
        {
            get => _leftOffset;
            set
            {
                _leftOffset = value;
                UpdatePosition();
            }
        }

        /// <summary>
        /// Gets or sets the top offset.
        /// </summary>
        /// <value>The top offset.</value>
        public double TopOffset
        {
            get => _topOffset;
            set
            {
                _topOffset = value;
                UpdatePosition();
            }
        }

        #endregion Public Properties

        #region Protected Properties

        /// <summary>
        /// Gets the number of visual child elements within this element.
        /// </summary>
        /// <value>The visual children count.</value>
        protected override int VisualChildrenCount => 1;

        #endregion Protected Properties

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DragAdorner"/> class.
        /// </summary>
        /// <param name="adornedElement">The adorned element.</param>
        /// <param name="content">The content.</param>
        /// <param name="startPoint">The start point.</param>
        public DragAdorner(UIElement adornedElement, FrameworkElement content, Point startPoint) : base(adornedElement)
        {
            _startPoint = startPoint;
            _child = new Rectangle {
                Width = content.RenderSize.Width,
                Height = content.RenderSize.Height,
                Fill = new ImageBrush(BitmapFrame.Create(RenderToBitmap(content)))
            };
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="T:System.Windows.Media.Transform" /> for the adorner, based on the transform that is currently applied to the adorned element.
        /// </summary>
        /// <param name="transform">The transform that is currently applied to the adorned element.</param>
        /// <returns>A transform to apply to the adorner.</returns>
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(new TranslateTransform(LeftOffset - _startPoint.X, TopOffset - _startPoint.Y));
            return result;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _child.Arrange(new Rect(finalSize));
            return finalSize;
        }

        /// <summary>
        /// Overrides <see cref="M:System.Windows.Media.Visual.GetVisualChild(System.Int32)" />, and returns a child at the specified index from a collection of child elements.
        /// </summary>
        /// <param name="index">The zero-based index of the requested child element in the collection.</param>
        /// <returns>The requested child element. This should not return <see langword="null" />; if the provided index is out of range, an exception is thrown.</returns>
        protected override Visual GetVisualChild(int index)
        {
            return _child;
        }

        /// <summary>
        /// Implements any custom measuring behavior for the adorner.
        /// </summary>
        /// <param name="constraint">A size to constrain the adorner to.</param>
        /// <returns>A <see cref="T:System.Windows.Size" /> object representing the amount of layout space needed by the adorner.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Takes a FrameworkElement and converts it into a Bitmap image to be used as the _child of this DragAdorner
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>RenderTargetBitmap.</returns>
        private static RenderTargetBitmap RenderToBitmap(FrameworkElement element)
        {
            var visual = new DrawingVisual();
            var drawingContext = visual.RenderOpen();

            drawingContext.DrawRectangle(new VisualBrush(element), null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            drawingContext.Close();

            var bitMap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Default);
            bitMap.Render(visual);
            return bitMap;
        }

        /// <summary>
        /// Updates the position.
        /// </summary>
        private void UpdatePosition()
        {
            if (!(Parent is AdornerLayer adornerLayer))
                return;

            adornerLayer.Update(AdornedElement);
        }

        #endregion Private Methods
    }
}