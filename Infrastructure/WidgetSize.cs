using System.ComponentModel;

namespace Infrastructure
{
    /// <summary>
    /// Enum for WidgetSizes
    /// </summary>
    public enum WidgetSize
    {
        /// <summary>
        /// 165 x 165 size
        /// </summary>
        [Description("165x165")]
        OneByOne,

        /// <summary>
        /// 165 x 330 size
        /// </summary>
        [Description("165x330")]
        OneByTwo,

        /// <summary>
        /// 120 x 300 size
        /// </summary>
        [Description("165x495")]
        OneByThree,

        /// <summary>
        /// 330 x 165 size
        /// </summary>
        [Description("330x165")]
        TwoByOne,

        /// <summary>
        /// 330 x 330 size
        /// </summary>
        [Description("330x330")]
        TwoByTwo,

        /// <summary>
        /// 220 x 300 size
        /// </summary>
        [Description("220x495")]
        TwoByThree,

        /// <summary>
        /// 320 x 100 size
        /// </summary>
        [Description("495x165")]
        ThreeByOne,

        /// <summary>
        /// 320 x 200 size
        /// </summary>
        [Description("495x330")]
        ThreeByTwo,

        /// <summary>
        /// 320 x 300 size
        /// </summary>
        [Description("495x495")]
        ThreeByThree
    }
}