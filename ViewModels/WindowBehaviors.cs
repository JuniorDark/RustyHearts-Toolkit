using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RHToolkit.ViewModels
{
    public static class WindowBehaviors
    {
        public static readonly DependencyProperty CloseWindowOnClickProperty =
            DependencyProperty.RegisterAttached("CloseWindowOnClick", typeof(bool), typeof(WindowBehaviors),
                new PropertyMetadata(false, OnCloseWindowOnClickChanged));

        public static bool GetCloseWindowOnClick(DependencyObject obj)
        {
            return (bool)obj.GetValue(CloseWindowOnClickProperty);
        }

        public static void SetCloseWindowOnClick(DependencyObject obj, bool value)
        {
            obj.SetValue(CloseWindowOnClickProperty, value);
        }

        private static void OnCloseWindowOnClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                if ((bool)e.NewValue)
                {
                    button.Click += CloseWindowOnClick;
                }
                else
                {
                    button.Click -= CloseWindowOnClick;
                }
            }
        }

        private static void CloseWindowOnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Window window = Window.GetWindow(button);
                window?.Close();
            }
        }

        public static readonly DependencyProperty EnableDragMoveProperty =
            DependencyProperty.RegisterAttached("EnableDragMove", typeof(bool), typeof(WindowBehaviors),
                new PropertyMetadata(false, OnEnableDragMoveChanged));

        public static bool GetEnableDragMove(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableDragMoveProperty);
        }

        public static void SetEnableDragMove(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableDragMoveProperty, value);
        }

        private static void OnEnableDragMoveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                if ((bool)e.NewValue)
                {
                    window.MouseDown += Window_MouseDown;
                }
                else
                {
                    window.MouseDown -= Window_MouseDown;
                }
            }
        }

        private static void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is Window window)
                {
                    window.DragMove();
                }
            }
        }

        public static readonly DependencyProperty EnableDoubleClickMaximizeProperty =
            DependencyProperty.RegisterAttached("EnableDoubleClickMaximize", typeof(bool), typeof(WindowBehaviors),
                new PropertyMetadata(false, OnEnableDoubleClickMaximizeChanged));

        public static bool GetEnableDoubleClickMaximize(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableDoubleClickMaximizeProperty);
        }

        public static void SetEnableDoubleClickMaximize(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableDoubleClickMaximizeProperty, value);
        }

        private static void OnEnableDoubleClickMaximizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                if ((bool)e.NewValue)
                {
                    window.MouseLeftButtonDown += Window_MouseLeftButtonDown;
                }
                else
                {
                    window.MouseLeftButtonDown -= Window_MouseLeftButtonDown;
                }
            }
        }

        private static void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // Double-click to maximize
            {
                if (sender is Window window)
                {
                    if (window.WindowState == WindowState.Maximized)
                        window.WindowState = WindowState.Normal;
                    else
                        window.WindowState = WindowState.Maximized;
                }
            }
        }
    }
}
