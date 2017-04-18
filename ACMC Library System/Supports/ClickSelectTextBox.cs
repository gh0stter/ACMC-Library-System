using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ACMC_Library_System.Supports
{
    public class ClickSelectTextBox : TextBox
    {
        public ClickSelectTextBox()
        {
            AddHandler(PreviewMouseLeftButtonDownEvent,new MouseButtonEventHandler(SelectivelyIgnoreMouseButton), true);
            AddHandler(GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText), true);
            AddHandler(MouseDoubleClickEvent,new RoutedEventHandler(SelectAllText), true);
        }

        private static void SelectivelyIgnoreMouseButton(object sender,MouseButtonEventArgs e)
        {
            TextBox targeTextBox;
            var eventBox = e.Source as TextBox;
            if (eventBox != null)
            {
                targeTextBox = eventBox;
            }
            else
            {
                // Find the TextBox
                DependencyObject parent = e.OriginalSource as UIElement;
                while (parent != null && !(parent is TextBox))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                if (parent == null)
                {
                    return;
                }
                targeTextBox = (TextBox)parent;
            }
            if (targeTextBox.IsKeyboardFocusWithin)
            {
                return;
            }
            // If the text box is not yet focussed, give it the focus and
            // stop further processing of this click event.
            targeTextBox.Focus();
            e.Handled = true;
        }

        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            textBox?.SelectAll();
        }
    }
}
