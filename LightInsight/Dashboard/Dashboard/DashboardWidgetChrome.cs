using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LightInsight.Dashboard.Dashboard
{
    /// <summary>
    /// Re-applies <see cref="MainBorder"/> brush after theme dictionary swap so
    /// <c>CardBorder</c> resolves to the current theme (code-assigned brushes do not auto-update).
    /// </summary>
    internal static class DashboardWidgetChrome
    {
        public static void SyncMainBorderBrush(FrameworkElement widget, bool isEditMode)
        {
            if (widget == null) return;
            var mainBorder = widget.FindName("MainBorder") as Border;
            if (mainBorder == null) return;

            if (isEditMode)
            {
                if (mainBorder.Tag is Brush originalBorderBrush)
                    mainBorder.BorderBrush = originalBorderBrush;
                mainBorder.BorderThickness = new Thickness(1);
            }
            else
            {
                if (!(mainBorder.Tag is Brush))
                    mainBorder.Tag = mainBorder.BorderBrush;
                mainBorder.BorderBrush =
                    widget.TryFindResource("CardBorder") as Brush
                    ?? new SolidColorBrush(Color.FromRgb(60, 60, 60));
                mainBorder.BorderThickness = new Thickness(1);
            }
        }
    }
}
