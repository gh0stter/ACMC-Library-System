using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ACMC_Library_System.Supports
{
    internal class WindowBlurEffect
    {
        private Brush _currentWindowColor;

        public void ApplyEffect(Window window)
        {
            var blur = new BlurEffect {Radius = 10};
            _currentWindowColor = window.Background;
            window.Background = new SolidColorBrush(Color.FromRgb(243, 243, 243));
            window.Effect = blur;
        }

        public void ClearEffect(Window window)
        {
            window.Effect = null;
            window.Background = _currentWindowColor;
        }
    }
}