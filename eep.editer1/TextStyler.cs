#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace eep.editer1
{
    public class TextStyler
    {
        private readonly RichTextBox _richTextBox;

        private long _lastShiftReleaseTime = 0;
        private const int SHIFT_DOUBLE_TAP_SPEED = 600;

        private readonly List<(Color Color, string[] Keywords)> _colorDefinitions;

        public TextStyler(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox;

            _colorDefinitions = new List<(Color, string[])>
            {
                (Color.Red,            new[] { "red", "赤色", "あか", "赤" }),
                (Color.Orange,         new[] { "orange", "橙色", "だいだい", "橙" }),
                (Color.DarkGoldenrod,  new[] { "yellow", "黄色", "きいろ", "黄" }),
                (Color.Green,          new[] { "green", "緑色", "みどり", "緑" }),
                (Color.Blue,           new[] { "blue", "青色", "あお", "青" }),
                (Color.Indigo,         new[] { "indigo", "navy", "藍色", "あいいろ", "藍" }),
                (Color.Purple,         new[] { "purple", "violet", "紫色", "むらさき", "紫" }),
                (Color.HotPink,        new[] { "pink", "桃色", "ピンク" }),
                (Color.DeepSkyBlue,    new[] { "cyan", "sky", "水色", "空色" }),
                (Color.Gray,           new[] { "gray", "grey", "灰色", "グレー" }),
                (Color.Black,          new[] {"black ","黒色","くろ","黒"}   ),
            };
        }

        public void HandleShiftKeyUp()
        {
            long now = DateTime.Now.Ticks / 10000;
            if (now - _lastShiftReleaseTime < SHIFT_DOUBLE_TAP_SPEED)
            {
                ToggleHeading();
                _lastShiftReleaseTime = 0;
            }
            else
            {
                _lastShiftReleaseTime = now;
            }
        }

        private void ToggleHeading()
        {
            Font currentFont = _richTextBox.SelectionFont;
            bool isHeading = (currentFont != null && currentFont.Size >= 20);

            if (isHeading)
            {
                _richTextBox.SelectionFont = new Font("Meiryo UI", 14, FontStyle.Regular);
            }
            else
            {
                int originalIndex = _richTextBox.SelectionStart;
                SelectCurrentLine(out int startPos, out int lineLength);

                if (lineLength > 0)
                {
                    _richTextBox.Select(startPos, lineLength);
                    _richTextBox.SelectionFont = new Font("Meiryo UI", 24, FontStyle.Bold);
                }

                _richTextBox.Select(originalIndex, 0);
                _richTextBox.SelectionFont = new Font("Meiryo UI", 24, FontStyle.Bold);
            }
            _richTextBox.Focus();
        }

        // =========================================================
        //  文字色切り替え (Tabキー) ★修正: 文字残しフラグ追加
        // =========================================================
        public bool ToggleColor(bool keepTriggerWord)
        {
            int originalIndex = _richTextBox.SelectionStart;

            SelectCurrentLine(out int startPos, out int lineLength);
            if (lineLength <= 0) return false;

            string lineText = _richTextBox.Text.Substring(startPos, lineLength).TrimEnd('\r', '\n');

            var matchedDef = _colorDefinitions
                .SelectMany(def => def.Keywords.Select(k => new { def.Color, Keyword = k }))
                .OrderByDescending(x => x.Keyword.Length)
                .FirstOrDefault(x => lineText.EndsWith(x.Keyword, StringComparison.OrdinalIgnoreCase));

            if (matchedDef != null)
            {
                // 1. 色変更処理
                _richTextBox.Select(startPos, lineLength);
                Color currentColor = _richTextBox.SelectionColor;

                Color targetColor = (currentColor.ToArgb() == matchedDef.Color.ToArgb())
                    ? Color.Black
                    : matchedDef.Color;

                _richTextBox.SelectionColor = targetColor;

                int triggerStart = startPos + lineLength - matchedDef.Keyword.Length;

                // 2. キーワード削除処理 (Shiftが押されていない場合のみ実行)
                if (!keepTriggerWord)
                {
                    _richTextBox.Select(triggerStart, matchedDef.Keyword.Length);
                    _richTextBox.SelectedText = "";
                    // 削除したのでカーソル位置は詰まる
                    _richTextBox.Select(triggerStart, 0);
                }
                else
                {
                    // 削除しない場合は元の位置に戻す
                    _richTextBox.Select(originalIndex, 0);
                }

                // 3. 今後の入力色設定
                _richTextBox.SelectionColor = targetColor;

                return true;
            }

            return false;
        }

        public void ResetToDefault()
        {
            _richTextBox.SelectionFont = new Font("Meiryo UI", 14, FontStyle.Regular);
            _richTextBox.SelectionColor = Color.Black;
        }

        public void CheckEmptyLineAndReset()
        {
            SelectCurrentLine(out int startPos, out int lineLength);

            if (lineLength <= 0 || (lineLength == 1 && _richTextBox.Text.Substring(startPos, 1) == "\n"))
            {
                string text = _richTextBox.Text.Substring(startPos, lineLength).Trim();
                if (string.IsNullOrEmpty(text))
                {
                    _richTextBox.SelectionColor = Color.Black;
                    _richTextBox.SelectionFont = new Font("Meiryo UI", 14, FontStyle.Regular);
                }
            }
        }

        private void SelectCurrentLine(out int startPos, out int lineLength)
        {
            int index = _richTextBox.SelectionStart;
            int lineIndex = _richTextBox.GetLineFromCharIndex(index);
            startPos = _richTextBox.GetFirstCharIndexFromLine(lineIndex);
            int nextLineStartPos = _richTextBox.GetFirstCharIndexFromLine(lineIndex + 1);

            lineLength = (nextLineStartPos == -1)
                ? _richTextBox.TextLength - startPos
                : nextLineStartPos - startPos;
        }
    }
}