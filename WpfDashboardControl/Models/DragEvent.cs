using WpfDashboardControl.Controls;

namespace WpfDashboardControl.Models
{
    /// <summary>
    /// Delegate for creating drag events providing a widgetHost as the parameter
    /// </summary>
    /// <param name="widgetHost">The widget host.</param>
    public delegate void DragEventHandler(WidgetHost widgetHost);
}