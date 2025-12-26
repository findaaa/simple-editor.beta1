using System;
using System.Runtime.InteropServices;
using System.Text;

namespace eep.editer1
{
    public static class NativeMethods
    {
        // --- Caret ---
        [DllImport("user32.dll")]
        public static extern bool HideCaret(IntPtr hWnd);

        // --- IME (imm32.dll) ---
        [DllImport("imm32.dll")]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll")]
        public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll")]
        public static extern int ImmGetCompositionString(IntPtr hIMC, int dwIndex, StringBuilder lpBuf, int dwBufLen);

        public const int GCS_COMPSTR = 0x0008;

        // --- DWM (Accent Color) ---
        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmGetColorizationColor(out int pcrColorization, out bool pfOpaqueBlend);
    }
}