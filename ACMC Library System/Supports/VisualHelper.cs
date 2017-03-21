using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ACMC_Library_System.Supports
{
    internal static class VisualHelper
    {
        public static double Dpi;
        private static Brush _currentWindowColor;

        public static void ApplyBlurEffect(Window window)
        {
            var blur = new BlurEffect {Radius = 10};
            _currentWindowColor = window.Background;
            window.Background = new SolidColorBrush(Color.FromRgb(243, 243, 243));
            window.Effect = blur;
        }

        public static void ClearBlurEffect(Window window)
        {
            window.Effect = null;
            window.Background = _currentWindowColor;
        }
    }
}