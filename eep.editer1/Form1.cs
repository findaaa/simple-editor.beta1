#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
// using System.IO; ← これはFileManagerに移動したので不要です

namespace eep.editer1
{
    public partial class metier : Form
    {
        private readonly CursorPhysics _physics;
        private readonly CursorRenderer _renderer;
        private readonly CursorInputState _inputState;
        private readonly TextStyler _textStyler;
        private readonly FileManager _fileManager; // ★追加

        private Stopwatch _stopwatch;
        private const float BASE_INTERVAL = 10.0f;

        private const long TIMEOUT_PHYSICS = 1200;
        private const long TIMEOUT_BLINK = 400;

        public metier()
        {
            InitializeComponent();

            // --- モジュール初期化 ---
            _physics = new CursorPhysics();
            _inputState = new CursorInputState();
            _renderer = new CursorRenderer(cursorBox);
            _textStyler = new TextStyler(richTextBox1);
            _fileManager = new FileManager(richTextBox1); // ★追加: 管理人雇う

            _stopwatch = new Stopwatch();

            // エディタ設定
            richTextBox1.Text = "";
            richTextBox1.Font = new Font("Meiryo UI", 14, FontStyle.Regular);
            richTextBox1.ImeMode = ImeMode.On;
            richTextBox1.AcceptsTab = true;

            // ★変更: 読み込みをマネージャーに依頼
            _fileManager.AutoLoad();

            // ★変更: アプリ終了時に保存をマネージャーに依頼
            this.FormClosing += (s, e) =>
            {
                _fileManager.AutoSave();
            };

            // イベント登録
            richTextBox1.SelectionChanged += (s, e) =>
            {
                NativeMethods.HideCaret(richTextBox1.Handle);
                _renderer.ResetBlink();
            };
            richTextBox1.Click += (s, e) =>
            {
                NativeMethods.HideCaret(richTextBox1.Handle);
                _renderer.ResetBlink();
            };

            richTextBox1.TextChanged += RichTextBox1_TextChanged;
            richTextBox1.KeyDown += RichTextBox1_KeyDown;
            richTextBox1.KeyUp += RichTextBox1_KeyUp;

            timer1.Interval = 10;
            timer1.Tick += Timer1_Tick;

            _stopwatch.Start();
            timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            float elapsedMs = (float)_stopwatch.Elapsed.TotalMilliseconds;
            _stopwatch.Restart();
            if (elapsedMs > 100f) elapsedMs = 10.0f;
            else if (elapsedMs <= 0f) elapsedMs = 10.0f;
            float deltaTime = elapsedMs / BASE_INTERVAL;

            var realTargetPos = GetCaretPosition();
            bool isComposing = _inputState.IsImeComposing(richTextBox1.Handle);
            bool isDeleting = _inputState.IsDeleting();

            long elapsedInput = _inputState.GetMillisecondsSinceLastInput();
            bool isTypingForPhysics = (elapsedInput < TIMEOUT_PHYSICS);
            bool isTypingForBlink = (elapsedInput < TIMEOUT_BLINK);

            Font currentFont = richTextBox1.SelectionFont ?? richTextBox1.Font;
            float ratchetThreshold = currentFont.Size * 3.0f;
            int cursorHeight = currentFont.Height;

            Color currentColor = richTextBox1.SelectionColor;

            _physics.Update(realTargetPos, isTypingForPhysics, isDeleting, ratchetThreshold, deltaTime);
            _renderer.Render(_physics.PosX, _physics.PosY, cursorHeight, isComposing, isTypingForBlink, currentColor);

            NativeMethods.HideCaret(richTextBox1.Handle);
        }

        private void RichTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            _inputState.RegisterKeyDown(e.KeyCode);
            _renderer.ResetBlink();

            if (e.KeyCode == Keys.Tab)
            {
                bool keepTriggerWord = e.Shift;
                if (_textStyler.ToggleColor(keepTriggerWord))
                {
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void RichTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
            {
                _textStyler.HandleShiftKeyUp();
            }
        }

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            _inputState.RegisterInput();
            NativeMethods.HideCaret(richTextBox1.Handle);
            _textStyler.CheckEmptyLineAndReset();
        }

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
            NativeMethods.HideCaret(richTextBox1.Handle);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}