#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace eep.editer1
{
    public class CursorRenderer
    {
        private readonly PictureBox _cursorBox;
        private Color _systemAccentColor = Color.DeepSkyBlue;

        private int _blinkTimer = 0;
        private const int BLINK_INTERVAL = 53;

        public CursorRenderer(PictureBox cursorBox)
        {
            _cursorBox = cursorBox;
            InitializeStyle();
            GetSystemAccentColor();
        }

        private void InitializeStyle()
        {
            if (_cursorBox != null)
            {
                _cursorBox.BackColor = Color.Black;
                _cursorBox.Width = 2;
                _cursorBox.Visible = true;
                _cursorBox.BringToFront();
            }
        }

        private void GetSystemAccentColor()
        {
            try
            {
                int colorRaw;
                bool opaque;
                NativeMethods.DwmGetColorizationColor(out colorRaw, out opaque);
                _systemAccentColor = Color.FromArgb(255, Color.FromArgb(colorRaw));
            }
            catch
            {
                _systemAccentColor = Color.DodgerBlue;
            }
        }

        public void ResetBlink()
        {
            _blinkTimer = 0;
        }

        public void Render(float x, float y, int height, bool isImeComposing, bool isTyping, Color currentColor)
        {
            if (_cursorBox == null) return;

            _cursorBox.Location = new Point((int)x, (int)y);
            _cursorBox.Height = height;

            if (isImeComposing)
            {
                // ★修正: IME入力中の色決定ロジック
                // 黒のときだけアクセントカラー、それ以外はその色を使う
                bool isBlack = (currentColor.R == 0 && currentColor.G == 0 && currentColor.B == 0);

                _cursorBox.BackColor = isBlack ? _systemAccentColor : currentColor;

                // 幅は太いままにして「入力中」であることを伝える
                _cursorBox.Width = 5;
                _cursorBox.Visible = true;
                _blinkTimer = 0;
            }
            else
            {
                // 通常時: 指定された色をそのまま使う
                _cursorBox.BackColor = currentColor;
                _cursorBox.Width = 2;

                if (isTyping)
                {
                    _cursorBox.Visible = true;
                    _blinkTimer = 0;
                }
                else
                {
                    _blinkTimer++;
                    bool isVisible = (_blinkTimer % (BLINK_INTERVAL * 2)) < BLINK_INTERVAL;
                    _cursorBox.Visible = isVisible;
                }
            }
        }
    }
}