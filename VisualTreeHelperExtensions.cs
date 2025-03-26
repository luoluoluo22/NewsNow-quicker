using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace NewsNow
{
    public static class VisualTreeHelperExtensions
    {
        // 辅助方法：查找视觉树中的子元素
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T children)
                {
                    yield return children;
                }

                foreach (var grandChild in FindVisualChildren<T>(child))
                {
                    yield return grandChild;
                }
            }
        }
    }
} 