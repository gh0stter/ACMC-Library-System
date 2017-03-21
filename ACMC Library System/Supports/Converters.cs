using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ACMC_Library_System.Supports
{
    /// <summary>
    /// Invert bool converter
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
            {
                throw new InvalidOperationException("The target must be a boolean");
            }
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("InverseBooleanConverter is a OneWay converter.");
        }
    }

    /// <summary>
    /// Circular Progress Bar Converter
    /// </summary>
    public class ValueToProcessConverter : IValueConverter
    {
        private const double Thickness = 5;
        private const double Padding = 0;
        private const double Margin = 2;
        private const double InitializingValue = 30;
        private const double MidRangeValue = 65;
        private const double FinalizeValue = 80;
        private const int FinalFontSize = 55;
        private static readonly Color BackGroundColor;
        private static readonly SolidColorBrush StartBrush;
        private static readonly SolidColorBrush MidRangeBrush;
        private static readonly SolidColorBrush FinalizeBrush;
        private static readonly Typeface TextTypeface;
        private Point _centerPoint;

        private string _percentString;
        private double _radius;

        static ValueToProcessConverter()
        {
            BackGroundColor = Color.FromRgb(255, 245, 245);
            StartBrush = new SolidColorBrush(Color.FromRgb(211, 72, 54));
            MidRangeBrush = new SolidColorBrush(Color.FromRgb(242, 95, 41));
            FinalizeBrush = new SolidColorBrush(Color.FromRgb(97, 191, 94));
            TextTypeface = new Typeface(new FontFamily("MSYH"), new FontStyle(), FontWeights.Bold, new FontStretch());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double) || string.IsNullOrEmpty((string)parameter))
            {
                throw new ArgumentException();
            }
            double arg = (double)value;
            double width = double.Parse((string)parameter);
            _radius = width / 2;
            _centerPoint = new Point(_radius, _radius);
            return DrawBrush(arg, 100, _radius, _radius, Thickness, Padding, Margin);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ValueToProcessConverter is a OneWay converter.");
        }

        /// <summary>
        /// 根据角度获取坐标
        /// </summary>
        /// <param name="centerPoint"></param>
        /// <param name="r"></param>
        /// <param name="angel"></param>
        /// <returns></returns>
        private static Point GetPointByAngel(Point centerPoint, double r, double angel)
        {
            var p = new Point
            {
                X = Math.Sin(angel * Math.PI / 180) * r + centerPoint.X,
                Y = centerPoint.Y - Math.Cos(angel * Math.PI / 180) * r
            };

            return p;
        }

        /// <summary>
        /// 根据4个坐标画出扇形
        /// </summary>
        /// <param name="bigFirstPoint"></param>
        /// <param name="bigSecondPoint"></param>
        /// <param name="smallFirstPoint"></param>
        /// <param name="smallSecondPoint"></param>
        /// <param name="bigRadius"></param>
        /// <param name="smallRadius"></param>
        /// <param name="isLargeArc"></param>
        /// <returns></returns>
        private static Geometry DrawingArcGeometry(Point bigFirstPoint, Point bigSecondPoint, Point smallFirstPoint, Point smallSecondPoint, double bigRadius, double smallRadius, bool isLargeArc)
        {
            var pathFigure = new PathFigure
            {
                IsClosed = true,
                StartPoint = bigFirstPoint
            };
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = bigSecondPoint,
                IsLargeArc = isLargeArc,
                Size = new Size(bigRadius, bigRadius),
                SweepDirection = SweepDirection.Clockwise
            });
            pathFigure.Segments.Add(new LineSegment { Point = smallSecondPoint });
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = smallFirstPoint,
                IsLargeArc = isLargeArc,
                Size = new Size(smallRadius, smallRadius),
                SweepDirection = SweepDirection.Counterclockwise
            });
            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }

        /// <summary>
        /// 根据当前值和最大值获取扇形
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="radiusX"></param>
        /// <param name="radiusY"></param>
        /// <param name="thickness"></param>
        /// <param name="padding"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        private Geometry GetGeometry(double value, double maxValue, double radiusX, double radiusY, double thickness, double padding, double margin)
        {
            bool isLargeArc = false;
            double percent = value / maxValue;
            _percentString = $"{Math.Round(percent * 100)}%";
            double angel = percent * 360D;
            if (angel > 180)
            {
                isLargeArc = true;
            }
            double bigR = radiusX + thickness - margin;
            double smallR = radiusX - thickness + margin + padding;
            var firstpoint = GetPointByAngel(_centerPoint, bigR, 0);
            var secondpoint = GetPointByAngel(_centerPoint, bigR, angel);
            var thirdpoint = GetPointByAngel(_centerPoint, smallR, 0);
            var fourpoint = GetPointByAngel(_centerPoint, smallR, angel);
            return DrawingArcGeometry(firstpoint, secondpoint, thirdpoint, fourpoint, bigR, smallR, isLargeArc);
        }

        private void DrawingGeometry(DrawingContext drawingContext, double value, double maxValue, double radiusX, double radiusY, double thickness, double padding, double margin)
        {
            if (Math.Abs(value - maxValue) > 0.000001)
            {
                var brush = value < InitializingValue ? StartBrush : value < MidRangeValue ? MidRangeBrush : FinalizeBrush;
                drawingContext.DrawEllipse(null, new Pen(new SolidColorBrush(BackGroundColor), thickness), _centerPoint, radiusX, radiusY);
                drawingContext.DrawGeometry(brush, new Pen(), GetGeometry(value, maxValue, radiusX, radiusY, thickness, padding, margin));
                var formatWords = new FormattedText(_percentString, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, TextTypeface, FinalFontSize, brush, VisualHelper.Dpi);
                var startPoint = new Point(_centerPoint.X - formatWords.Width / 2, _centerPoint.Y - formatWords.Height / 2);
                drawingContext.DrawText(formatWords, startPoint);
            }
            else
            {
                drawingContext.DrawEllipse(null, new Pen(FinalizeBrush, thickness), _centerPoint, radiusX, radiusY);
                var formatWords = new FormattedText("100%", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, TextTypeface, FinalFontSize, FinalizeBrush, VisualHelper.Dpi);
                var startPoint = new Point(_centerPoint.X - formatWords.Width / 2, _centerPoint.Y - formatWords.Height / 2);
                drawingContext.DrawText(formatWords, startPoint);
            }

            drawingContext.Close();
        }

        /// <summary>
        /// 根据当前值和最大值画出进度条
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="radiusX"></param>
        /// <param name="radiusY"></param>
        /// <param name="thickness"></param>
        /// <param name="padding"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        private Visual DrawShape(double value, double maxValue, double radiusX, double radiusY, double thickness, double padding, double margin)
        {
            var drawingWordsVisual = new DrawingVisual();
            var drawingContext = drawingWordsVisual.RenderOpen();

            DrawingGeometry(drawingContext, value, maxValue, radiusX, radiusY, thickness, padding, margin);

            return drawingWordsVisual;
        }

        /// <summary>
        /// 根据当前值和最大值画出进度条
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="radiusX"></param>
        /// <param name="radiusY"></param>
        /// <param name="thickness"></param>
        /// <param name="padding"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        private Brush DrawBrush(double value, double maxValue, double radiusX, double radiusY, double thickness, double padding, double margin)
        {
            var drawingGroup = new DrawingGroup();
            var drawingContext = drawingGroup.Open();

            DrawingGeometry(drawingContext, value, maxValue, radiusX, radiusY, thickness, padding, margin);

            var brush = new DrawingBrush(drawingGroup);

            return brush;
        }
    }

    /// <summary>
    /// Bool to visibility converter
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;
            if (value is bool)
            {
                result = (bool)value;
            }
            return result ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BooleanToVisibilityConverter is a OneWay converter.");
        }
    }

    /// <summary>
    /// Invert bool to visibility converter
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InvertBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;
            if (value is bool)
            {
                result = (bool)value;
            }
            return !result ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("InvertBooleanToVisibilityConverter is a OneWay converter.");
        }
    }

    /// <summary>
    /// True-True to visible converter, order matters, only accept two condictions
    /// </summary>
    public class TrueTrueToVisibleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
            {
                throw new NotSupportedException("FalseTrueToVisibleConverter only accept two values at a time.");
            }
            if (!values.All(value => value is bool))
            {
                return Visibility.Hidden;
            }
            return values.All(value => (bool)value) ? Visibility.Visible : Visibility.Hidden;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("FalseTrueToVisibleConverter is a OneWay converter.");
        }
    }

    /// <summary>
    /// False-True to visible converter, order matters, only accept two condictions
    /// </summary>
    public class FalseTrueToVisibleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
            {
                throw new NotSupportedException("FalseTrueToVisibleConverter only accept two values at a time.");
            }
            if (!values.All(value => value is bool))
            {
                return Visibility.Hidden;
            }
            if ((bool)values[0])
            {
                return Visibility.Hidden;
            }
            if ((bool)values[1])
            {
                return Visibility.Visible;
            }
            return Visibility.Hidden;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("FalseTrueToVisibleConverter is a OneWay converter.");
        }
    }

    /// <summary>
    /// False-False to visible converter, order matters, only accept two condictions
    /// </summary>
    public class FalseFalseToVisibleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
            {
                throw new NotSupportedException("FalseFalseToVisibleConverter only accept two values at a time.");
            }
            if (!values.All(value => value is bool))
            {
                return Visibility.Hidden;
            }
            if ((bool)values[0])
            {
                return Visibility.Hidden;
            }
            if ((bool)values[1])
            {
                return Visibility.Hidden;
            }
            return Visibility.Visible;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("FalseFalseToVisibleConverter is a OneWay converter.");
        }
    }
}