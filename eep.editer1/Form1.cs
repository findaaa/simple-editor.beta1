#nullable disable // null警告を無視

using System;
using System.Drawing;
using System.Runtime.InteropServices; // DllImport用
using System.Windows.Forms;
using System.Text;

namespace eep.editer1
{
    public partial class Form1 : Form
    {
        // =========================================================
        //  Windows API 定義
        // =========================================================

        // --- 1. キャレット隠蔽用 ---
#pragma warning disable SYSLIB1054
        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);
#pragma warning restore SYSLIB1054

        // --- 2. IME判定用 (imm32.dll) ---
        [DllImport("imm32.dll")]
        private static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll")]
        private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll")]
        private static extern int ImmGetCompositionString(IntPtr hIMC, int dwIndex, StringBuilder lpBuf, int dwBufLen);

        // IME定数
        private const int GCS_COMPSTR = 0x0008;

        // --- 3. アクセントカラー取得用 (dwmapi.dll) ---
        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern void DwmGetColorizationColor(out int pcrColorization, out bool pfOpaqueBlend);

        // =========================================================
        //  パラメータ & メンバ変数
        // =========================================================

        // 物理演算
        private float posX = 0;
        private float posY = 0;
        private float velX = 0;

        // バネ設定
        private const float Y_SMOOTH = 0.3f;
        private const float X_TENSION = 0.15f;
        private const float RAPID_TENSION = 0.02f;
        private const float RAPID_FRICTION = 0.85f;
        private const float FRICTION_FORWARD = 0.65f;
        private const float FRICTION_BACKWARD = 0.45f;
        private const float SNAP_THRESHOLD = 0.5f;
        private const float STOP_VELOCITY = 0.5f;

        // 状態管理
        private long lastShiftReleaseTime = 0;
        private Keys lastKeyDown = Keys.None;
        private long lastInputTime = 0;
        private float maxTargetX = 0;
        private const int TYPING_TIMEOUT = 1500;

        // ★色管理
        private Color systemAccentColor = Color.DeepSkyBlue; // 初期値（取得失敗時の保険）

        public Form1()
        {
            InitializeComponent();

            // --- Windowsのアクセントカラーを取得して保存 ---
            GetSystemAccentColor();

            // --- エディタ初期設定 ---
            richTextBox1.Text = "";
            richTextBox1.Font = new Font("Meiryo UI", 14, FontStyle.Regular);
            richTextBox1.ImeMode = ImeMode.On;

            // --- イベント登録 ---
            richTextBox1.SelectionChanged += RichTextBox1_SelectionChanged;
            richTextBox1.Click += RichTextBox1_SelectionChanged;
            richTextBox1.TextChanged += RichTextBox1_TextChanged;
            richTextBox1.KeyUp += RichTextBox1_KeyUp;
            richTextBox1.KeyDown += RichTextBox1_KeyDown;

            // --- カーソル設定 ---
            if (cursorBox != null)
            {
                cursorBox.BackColor = Color.Black;
                cursorBox.Width = 2;
                cursorBox.Visible = true;
                cursorBox.BringToFront();
            }

            // --- タイマー開始 ---
            timer1.Interval = 10;
            timer1.Tick += Timer1_Tick;
            timer1.Start();
        }

        // --- アクセントカラー取得ロジック ---
        private void GetSystemAccentColor()
        {
            try
            {
                int colorRaw;
                bool opaque;
                // Windowsの設定色を取得
                DwmGetColorizationColor(out colorRaw, out opaque);

                // アルファ値(透明度)を255(不透明)に強制してColor型に変換
                systemAccentColor = Color.FromArgb(255, Color.FromArgb(colorRaw));
            }
            catch
            {
                // 失敗したら明るめの青にする
                systemAccentColor = Color.DodgerBlue;
            }
        }

        // =========================================================
        //  イベントハンドラ
        // =========================================================

        private void RichTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            lastKeyDown = e.KeyCode;
        }

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            lastInputTime = DateTime.Now.Ticks / 10000;
            HideCaret(richTextBox1.Handle);
            RichTextBox1_SelectionChanged(sender, e);
        }

        // ★★★ メインループ ★★★
        private void Timer1_Tick(object sender, EventArgs e)
        {
            // -----------------------------------------------------
            // 1. IME判定 & 見た目変更 (色・太さ)
            // -----------------------------------------------------
            bool isComposing = IsImeComposing(richTextBox1.Handle);

            if (isComposing)
            {
                // IME入力中: ★アクセントカラー & ★太くする(5px)
                cursorBox.BackColor = systemAccentColor;
                cursorBox.Width = 5;
            }
            else
            {
                // 通常時: 黒 & 細くする(2px)
                cursorBox.BackColor = Color.Black;
                cursorBox.Width = 2;
            }

            // -----------------------------------------------------
            // 2. 物理演算
            // -----------------------------------------------------
            var realTargetPos = GetCaretPosition();
            float effectiveTargetX = realTargetPos.X;

            long now = DateTime.Now.Ticks / 10000;
            long elapsed = now - lastInputTime;

            bool isTyping = (elapsed < TYPING_TIMEOUT);
            bool isDeleting = (lastKeyDown == Keys.Back || lastKeyDown == Keys.Left);

            // ラチェット
            if (isTyping && !isDeleting)
            {
                if (realTargetPos.X >= maxTargetX)
                {
                    maxTargetX = realTargetPos.X;
                    effectiveTargetX = realTargetPos.X;
                }
                else
                {
                    float jumpDistance = maxTargetX - realTargetPos.X;
                    Font currentFont = richTextBox1.SelectionFont;
                    if (currentFont == null) currentFont = richTextBox1.Font;
                    float threshold = currentFont.Size * 3.0f;

                    if (jumpDistance < threshold) effectiveTargetX = maxTargetX;
                    else
                    {
                        maxTargetX = realTargetPos.X;
                        effectiveTargetX = realTargetPos.X;
                    }
                }
            }
            else if (!isTyping)
            {
                maxTargetX = realTargetPos.X;
                effectiveTargetX = realTargetPos.X;
            }
            else
            {
                maxTargetX = realTargetPos.X;
                effectiveTargetX = realTargetPos.X;
            }

            // Y軸
            posY += (realTargetPos.Y - posY) * Y_SMOOTH;

            // X軸
            float diffX = effectiveTargetX - posX;
            float diffY = Math.Abs(realTargetPos.Y - posY);

            if (diffY > 5.0f)
            {
                posX += diffX * 0.3f;
                velX = 0;
            }
            else if (Math.Abs(diffX) < SNAP_THRESHOLD && Math.Abs(velX) < STOP_VELOCITY)
            {
                posX = effectiveTargetX;
                velX = 0;
            }
            else
            {
                float tension;
                float friction;
                bool isMovingLeft = (diffX < 0);

                if (isTyping && !isMovingLeft)
                {
                    tension = RAPID_TENSION;
                    friction = RAPID_FRICTION;
                }
                else if (isMovingLeft)
                {
                    tension = X_TENSION;
                    friction = FRICTION_BACKWARD;
                }
                else
                {
                    tension = X_TENSION;
                    friction = FRICTION_FORWARD;
                }

                float force = diffX * tension;
                velX += force;
                velX *= friction;
                posX += velX;
            }

            // 描画
            cursorBox.Location = new Point((int)posX, (int)posY);
            Font f = richTextBox1.SelectionFont;
            int h = (f != null) ? f.Height : richTextBox1.Font.Height;
            cursorBox.Height = h;

            HideCaret(richTextBox1.Handle);
        }

        // --- ヘルパー: IME判定 ---
        private bool IsImeComposing(IntPtr hWnd)
        {
            IntPtr hIMC = ImmGetContext(hWnd);
            if (hIMC == IntPtr.Zero) return false;

            try
            {
                // 未確定文字列の長さを取得
                int strLen = ImmGetCompositionString(hIMC, GCS_COMPSTR, null, 0);
                return (strLen > 0);
            }
            finally
            {
                ImmReleaseContext(hWnd, hIMC);
            }
        }

        // --- ヘルパー: 座標 ---
        private Point GetCaretPosition()
        {
            int index = richTextBox1.SelectionStart;
            Point p = richTextBox1.GetPositionFromCharIndex(index);
            p.X += richTextBox1.Location.X;
            p.Y += richTextBox1.Location.Y;
            return p;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            HideCaret(richTextBox1.Handle);
        }

        private void RichTextBox1_SelectionChanged(object sender, EventArgs e)
        {
            HideCaret(richTextBox1.Handle);
        }

        private void RichTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
            {
                long now = DateTime.Now.Ticks / 10000;
                if (now - lastShiftReleaseTime < 600)
                {
                    ToggleHeadingStyle();
                    lastShiftReleaseTime = 0;
                }
                else
                {
                    lastShiftReleaseTime = now;
                }
            }

            if (e.KeyCode == Keys.Enter)
            {
                richTextBox1.SelectionFont = new Font("Meiryo UI", 14, FontStyle.Regular);
            }
        }

        private void ToggleHeadingStyle()
        {
            int originalIndex = richTextBox1.SelectionStart;
            Font currentFont = richTextBox1.SelectionFont;
            bool toHeading = (currentFont == null || currentFont.Size < 20);

            Font targetFont = toHeading
                ? new Font("Meiryo UI", 24, FontStyle.Bold)
                : new Font("Meiryo UI", 14, FontStyle.Regular);

            int lineIndex = richTextBox1.GetLineFromCharIndex(originalIndex);
            int startPos = richTextBox1.GetFirstCharIndexFromLine(lineIndex);
            int nextLineStartPos = richTextBox1.GetFirstCharIndexFromLine(lineIndex + 1);

            int lineLength = (nextLineStartPos == -1)
                ? richTextBox1.TextLength - startPos
                : nextLineStartPos - startPos;

            if (lineLength > 0)
            {
                richTextBox1.Select(startPos, lineLength);
                richTextBox1.SelectionFont = targetFont;
            }

            richTextBox1.Select(originalIndex, 0);
            richTextBox1.SelectionFont = targetFont;
            richTextBox1.Focus();
        }
    }
}