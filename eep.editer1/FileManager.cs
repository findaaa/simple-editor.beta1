#nullable disable
using System;
using System.IO;
using System.Windows.Forms;

namespace eep.editer1
{
    public class FileManager
    {
        private readonly RichTextBox _richTextBox;
        private readonly string _autoSavePath;

        public FileManager(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox;

            // マイドキュメントのパスを取得して結合
            _autoSavePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "eep_autosave.rtf"
            );
        }

        // 起動時の読み込み
        public void AutoLoad()
        {
            if (File.Exists(_autoSavePath))
            {
                try
                {
                    // RTF形式で読み込み
                    _richTextBox.LoadFile(_autoSavePath, RichTextBoxStreamType.RichText);

                    // カーソルを末尾に移動（続きから書けるように）
                    _richTextBox.Select(_richTextBox.TextLength, 0);
                }
                catch (Exception ex)
                {
                    // 読み込みエラー時は何もしない（あるいはログ出力）
                    System.Diagnostics.Debug.WriteLine("Load Error: " + ex.Message);
                }
            }
        }

        // 終了時の保存
        public void AutoSave()
        {
            try
            {
                // RTF形式(色情報含む)で保存
                _richTextBox.SaveFile(_autoSavePath, RichTextBoxStreamType.RichText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save Error: " + ex.Message);
            }
        }
    }
}