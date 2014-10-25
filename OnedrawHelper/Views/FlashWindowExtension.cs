using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OnedrawHelper.Views
{
    static class FlashWindowExtension
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        #region const
        private const UInt32 FLASHW_STOP = 0;
        private const UInt32 FLASHW_CAPTION = 1;
        private const UInt32 FLASHW_TRAY = 2;
        private const UInt32 FLASHW_ALL = 3;
        private const UInt32 FLASHW_TIMER = 4;
        private const UInt32 FLASHW_TIMERNOFG = 12;
        #endregion

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        private static bool IsSupported
        {
            get { return System.Environment.OSVersion.Version.Major >= 5; }
        }


        public static bool BeginFlash(this Window wnd)
        {
            if (wnd.IsActive || !IsSupported) return false;

            var h = new WindowInteropHelper(wnd);
            var info = new FLASHWINFO()
            {
                hwnd = h.Handle,
                dwFlags = FLASHW_TRAY,
                uCount = 2,
                dwTimeout = 500
            };
            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));

            return FlashWindowEx(ref info);
        }
    }
}
