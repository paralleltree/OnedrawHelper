using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;

namespace OnedrawHelper.Behaviors
{
    class SystemMenuBehavior : Behavior<Window>
    {
        [DllImport("user32.dll")]
        private extern static int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private extern static int SetWindowLong(IntPtr hwnd, int index, int value);

        #region const
        //--- GetWindowLong
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        //--- Window Style
        private const int WS_EX_CONTEXTHELP = 0x00400;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int WS_SYSMENU = 0x80000;
        //--- Window Message
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSCOMMAND = 0x0112;
        //--- System Command
        private const int SC_CONTEXTHELP = 0xF180;
        //--- Virtual Keyboard
        private const int VK_F4 = 0x73;
        #endregion

        public event EventHandler ContextHelpClick = null;


        #region properties
        public bool? IsVisible
        {
            get { return (bool?)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible", typeof(bool?), typeof(SystemMenuBehavior), new PropertyMetadata(null, OnPropertyChanged));

        public bool? CanMinimize
        {
            get { return (bool?)GetValue(CanMinimizeProperty); }
            set { SetValue(CanMinimizeProperty, value); }
        }
        public static readonly DependencyProperty CanMinimizeProperty =
            DependencyProperty.Register("CanMinimize", typeof(bool?), typeof(SystemMenuBehavior), new PropertyMetadata(null, OnPropertyChanged));

        public bool? CanMaximize
        {
            get { return (bool?)GetValue(CanMaximizeProperty); }
            set { SetValue(CanMaximizeProperty, value); }
        }
        public static readonly DependencyProperty CanMaximizeProperty =
            DependencyProperty.Register("CanMaximize", typeof(bool?), typeof(SystemMenuBehavior), new PropertyMetadata(null, OnPropertyChanged));

        public bool? ShowContextHelp
        {
            get { return (bool?)GetValue(ShowContextHelpProperty); }
            set { SetValue(ShowContextHelpProperty, value); }
        }
        public static readonly DependencyProperty ShowContextHelpProperty =
            DependencyProperty.Register("ShowContextHelp", typeof(bool?), typeof(SystemMenuBehavior), new PropertyMetadata(null, OnPropertyChanged));

        public bool EnableAltF4
        {
            get { return (bool)GetValue(EnableAltF4Property); }
            set { SetValue(EnableAltF4Property, value); }
        }
        public static readonly DependencyProperty EnableAltF4Property =
            DependencyProperty.Register("EnableAltF4", typeof(bool), typeof(SystemMenuBehavior), new PropertyMetadata(true));
        #endregion


        protected override void OnAttached()
        {
            this.AssociatedObject.SourceInitialized += this.OnSourceInitialized;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            var source = (HwndSource)HwndSource.FromVisual(this.AssociatedObject);
            source.RemoveHook(this.HookProcedure);
            this.AssociatedObject.SourceInitialized -= this.OnSourceInitialized;
            base.OnDetaching();
        }


        private static void OnPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var self = obj as SystemMenuBehavior;
            if (self != null) self.Apply();
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            this.Apply();
            var source = (HwndSource)HwndSource.FromVisual(this.AssociatedObject);
            source.AddHook(this.HookProcedure);
        }

        private IntPtr HookProcedure(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND)
                if (wParam.ToInt32() == SC_CONTEXTHELP)
                {
                    handled = true;
                    var handler = this.ContextHelpClick;
                    if (handler != null)
                        handler(this.AssociatedObject, EventArgs.Empty);
                }

            if (!this.EnableAltF4)
                if (msg == WM_SYSKEYDOWN)
                    if (wParam.ToInt32() == VK_F4)
                        handled = true;

            return IntPtr.Zero;
        }


        private void Apply()
        {
            if (this.AssociatedObject == null) return;

            var hwnd = new WindowInteropHelper(this.AssociatedObject).Handle;
            var style = GetWindowLong(hwnd, GWL_STYLE);
            if (this.IsVisible.HasValue)
            {
                if (this.IsVisible.Value)
                    style |= WS_SYSMENU;
                else
                    style &= ~WS_SYSMENU;
            }
            if (this.CanMinimize.HasValue)
            {
                if (this.CanMinimize.Value)
                    style |= WS_MINIMIZEBOX;
                else
                    style &= ~WS_MINIMIZEBOX;
            }
            if (this.CanMaximize.HasValue)
            {
                if (this.CanMaximize.Value)
                    style |= WS_MAXIMIZEBOX;
                else
                    style &= ~WS_MAXIMIZEBOX;
            }
            SetWindowLong(hwnd, GWL_STYLE, style);

            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (this.ShowContextHelp.HasValue)
            {
                if (this.ShowContextHelp.Value)
                    exStyle |= WS_EX_CONTEXTHELP;
                else
                    exStyle &= WS_EX_CONTEXTHELP;
            }
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }
    }
}
