﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class NativeMethods
    {
        #region WindowsAPI

        [StructLayout(LayoutKind.Sequential)]
        public struct DwmMargin
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
 
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);
        public static Vector2Int GetWindowsMousePosition()
        {
            return GetCursorPos(out POINT pos) ?
                new Vector2Int(pos.X, pos.Y) :
                Vector2Int.zero;
            //POINT pos;
            //if (GetCursorPos(out pos)) return new Vector2(pos.X, pos.Y);
            //return Vector2.zero;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string windowName);
        public static IntPtr CurrentWindowHandle = IntPtr.Zero;

        public static IntPtr GetUnityWindowHandle()
        {
            if (CurrentWindowHandle == IntPtr.Zero)
            {
                int id = System.Diagnostics.Process.GetCurrentProcess().Id;
                CurrentWindowHandle = GetSelfWindowHandle(id);
            }
            return CurrentWindowHandle;
        }
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong); /*x uint o int unchecked*/
        [DllImport("user32.dll")]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hwnd, String lpString);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
        
        delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
        
        //GCAllocが起きにくいように + delegateとの相性を踏まえて、モニターの列挙情報をフィールドで持ってしまう
        private static readonly List<RECT> _monitorRects = new List<RECT>(8);

        private static bool ProcEnumMonitors(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            //refのをそのままaddすると気持ち悪いので一応値コピーを挟む
            var copied = lprcMonitor;
            _monitorRects.Add(copied);
            return true;
        }
        
        public static List<RECT> LoadAllMonitorRects()
        {
            _monitorRects.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, ProcEnumMonitors, IntPtr.Zero);
            return _monitorRects;
        }


        [Flags()]
        public enum SetWindowPosFlags : uint
        {
            AsynchronousWindowPosition = 0x4000,
            DeferErase = 0x2000,
            DrawFrame = 0x0020,
            FrameChanged = 0x0020,
            HideWindow = 0x0080,
            DoNotActivate = 0x0010,
            DoNotCopyBits = 0x0100,
            IgnoreMove = 0x0002,
            DoNotChangeOwnerZOrder = 0x0200,
            DoNotRedraw = 0x0008,
            DoNotReposition = 0x0200,
            DoNotSendChangingEvent = 0x0400,
            IgnoreResize = 0x0001,
            IgnoreZOrder = 0x0004,
            ShowWindow = 0x0040,
            NoFlag = 0x0000,
            IgnoreMoveAndResize = IgnoreMove | IgnoreResize,
        }

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int index);
        
        public static class SystemMetricsConsts
        {
            //NOTE: プライマリモニターの左上座標を取るキーがないのは、プライマリモニター左上 = (0, 0)で固定だから
            public const int SM_CXSCREEN = 0;
            public const int SM_CYSCREEN = 1;

            public const int SM_XVIRTUALSCREEN = 76;
            public const int SM_YVIRTUALSCREEN = 77;
            public const int SM_CXVIRTUALSCREEN = 78;
            public const int SM_CYVIRTUALSCREEN = 79;
        }

        /// <summary>
        /// プライマリモニターの位置とサイズを取得します。
        /// </summary>
        /// <returns></returns>
        public static RECT GetPrimaryWindowRect() =>
            new RECT()
            {
                left = 0,
                top = 0,
                right = GetSystemMetrics(SystemMetricsConsts.SM_CXSCREEN),
                bottom = GetSystemMetrics(SystemMetricsConsts.SM_CYSCREEN),
            };

        public static Vector2Int GetUnityWindowPosition()
        {
            GetWindowRect(GetUnityWindowHandle(), out RECT rect);
            return new Vector2Int(rect.left, rect.top);
        }

        public static void SetUnityWindowActive() => SetForegroundWindow(GetUnityWindowHandle());
        public static void SetUnityWindowPosition(int x, int y) => SetWindowPos(GetUnityWindowHandle(), IntPtr.Zero, x, y, 0, 0, SetWindowPosFlags.IgnoreResize);
        public static void SetUnityWindowSize(int width, int height) => SetWindowPos(GetUnityWindowHandle(), IntPtr.Zero, 0, 0, width, height, SetWindowPosFlags.IgnoreMove);
        public static void SetUnityWindowTopMost(bool enable) => SetWindowPos(GetUnityWindowHandle(), enable ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SetWindowPosFlags.IgnoreMoveAndResize);
        public static void SetUnityWindowTitle(string title) => SetWindowText(GetUnityWindowHandle(), title);

        [DllImport("Dwmapi.dll")]
        public static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref DwmMargin margins);
        public static void SetDwmTransparent(bool enable)
        {
            int margin = enable ? -1 : 0;
            var margins = new DwmMargin()
            {
                cxLeftWidth = margin,
                cxRightWidth = margin,
                cyTopHeight = margin,
                cyBottomHeight = margin,
            };
            DwmExtendFrameIntoClientArea(GetUnityWindowHandle(), ref margins);
        }

        public const int GWL_STYLE = -16;
        public const uint WS_POPUP = 0x8000_0000;
        public const uint WS_VISIBLE = 0x1000_0000;
        public const int GWL_EXSTYLE = -20;
        public const uint WS_EX_LAYERED = 0x0008_0000;
        public const uint WS_EX_TRANSPARENT = 0x0000_0020;

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int LWA_COLORKEY = 0x0001;
        private const int LWA_ALPHA = 0x0002;

        public static void SetWindowAlpha(byte alpha)
        {
            SetLayeredWindowAttributes(GetUnityWindowHandle(), 0, alpha, LWA_ALPHA);
        }

        /// <summary>
        /// ウィンドウサイズを設定します。
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        public static void RefreshWindowSize(int cx, int cy)
        {
            SetWindowPos(GetUnityWindowHandle(),
                IntPtr.Zero,
                0, 0, cx, cy,
                SetWindowPosFlags.IgnoreMove | 
                    SetWindowPosFlags.IgnoreZOrder | 
                    SetWindowPosFlags.FrameChanged | 
                    SetWindowPosFlags.DoNotChangeOwnerZOrder |
                    SetWindowPosFlags.DoNotActivate | 
                    SetWindowPosFlags.AsynchronousWindowPosition
            );
        }

        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, ref int processId);
        
        private static IntPtr GetSelfWindowHandle(int processId)
        {
            var ret = IntPtr.Zero;

            bool Func(IntPtr hWnd, IntPtr lParam)
            {
                int id = -1;
                GetWindowThreadProcessId(hWnd, ref id);
                if (id == processId)
                {
                    ret = hWnd;
                    return false;
                }
                return true;
            }
            
            EnumWindows(Func, IntPtr.Zero);
            return ret;            
        }

        #endregion
    }
}
