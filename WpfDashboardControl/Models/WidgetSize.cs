using System.ComponentModel;

namespace WpfDashboardControl.Models
{
    /// <summary>
    /// Enum for WidgetSizes
    /// </summary>
    public enum WidgetSize
    {
        /// <summary>
        /// 120 x 100 size
        /// </summary>
        [Description("120x100")]
        OneByOne,

        /// <summary>
        /// 120 x 200 size
        /// </summary>
        [Description("120x200")]
        OneByTwo,

        /// <summary>
        /// 220 x 100 size
        /// </summary>
        [Description("220x100")]
        TwoByOne,

        /// <summary>
        /// 220 x 200 size
        /// </summary>
        [Description("220x200")]
        TwoByTwo,
    }
}