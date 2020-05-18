using System.Windows;
using System.Windows.Media;

namespace WpfDashboardControl
{
    /// <summary>
    /// Provides extension methods for DependencyObject's
    /// </summary>
    public static class DependencyObjectExtensions
    {
        #region Public Methods

        /// <summary>
        /// Recursively finds a child element of the parent with generic T type provided (FrameworkElement) and the provided childName.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent">The parent.</param>
        /// <param name="childName">Name of the child.</param>
        /// <returns>T.</returns>
        public static T FindChildElementByName<T>(this DependencyObject parent, string childName) where T : FrameworkElement
        {
            // Since this is a recursive call check if the parent parameter is the FrameworkElement we want!
            switch (parent)
            {
                case null:
                    return null;
                case T parentType when parentType.Name == childName:
                    return parentType;
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            // Loop through each child of the parent and recursively check them through FindChildElementByName
            // until the request child is found
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                var foundChild = FindChildElementByName<T>(child, childName);

                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }

        #endregion Public Methods
    }
}