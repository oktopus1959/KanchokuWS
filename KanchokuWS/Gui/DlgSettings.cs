using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;

using KanchokuWS.Domain;
using Utils;

namespace KanchokuWS.Gui
{
    public partial class DlgSettings : Form
    {
        private static Logger logger = Logger.GetLogger();

        public static DlgSettings ShownDlg { get; private set; } = null;

        private static int SelectedTabIndex = 0;

        public static bool BringTopMostShownDlg()
        {
            if (ShownDlg != null) {
                ShownDlg.TopMost = true;
                ShownDlg.TopMost = false;
                return true;
            }
            return false;
        }

        private FrmKanchoku frmMain;

        private FrmVirtualKeyboard frmVkb;

        private FrmModeMarker frmMode;

        private GuiStatusChecker checkerAll;
        private GuiStatusChecker checkerBasic;
        private GuiStatusChecker checkerAdvanced;
        private GuiStatusChecker checkerImeCombo;
        private GuiStatusChecker checkerFontColor;
        private GuiStatusChecker checkerKeyAssign;
        private GuiStatusChecker checkerCtrlKeys;
        private GuiStatusChecker checkerHistory;
        private GuiStatusChecker checkerMiscSettings;
        private GuiStatusChecker checkerDevelop;

        private const int timerInterval = 200;

        //-----------------------------------------------------------------------------------
        public bool RestartRequired { get; set; } = false;

        public bool NoSave { get; set; } = false;

        //-----------------------------------------------------------------------------------
        public DlgSettings(FrmKanchoku form, FrmVirtualKeyboard vkb, FrmModeMarker mode)
        {
            frmMain = form;
            frmVkb = vkb;
            frmMode = mode;

            ScreenInfo.CreateSingleton();

            InitializeComponent();
        }

        public void ShowInitialMessage()
        {
            label_initialMsg.Show();
        }

        //-----------------------------------------------------------------------------------
        private void DlgSettings_Load(object sender, EventArgs e)
        {
            ShownDlg = this;

            AcceptButton = button_basicEnter;
            CancelButton = button_basicClose;

            button_showPaddingsDesc.Enabled = frmVkb != null;
            button_setModConversion.Enabled = checkBox_extraModifiersEnabled.Checked;

            tabControl1.SelectedIndex = SelectedTabIndex;
            changeGlobalCtrlKeysCheckBoxState();

            // 情報タブの DataGridView
            initializeAboutDgv();

            // 各タブの状態チェッカーをまとめる
            checkerAll = new GuiStatusChecker("All");
            checkerBasic = new GuiStatusChecker("Basic");
            checkerAdvanced = new GuiStatusChecker("Advanced");
            checkerImeCombo = new GuiStatusChecker("ImeCombo");
            checkerFontColor = new GuiStatusChecker("FontAndColor");
            checkerKeyAssign = new GuiStatusChecker("KeyAssign");
            checkerCtrlKeys = new GuiStatusChecker("CtrlKeys");
            checkerHistory = new GuiStatusChecker("History");
            checkerMiscSettings = new GuiStatusChecker("MiscSettings");
            checkerDevelop = new GuiStatusChecker("Develop");

            readSettings_tabBasic();
            setBasicStatusChecker();

            readSettings_tabAdvanced();
            setAdvancedStatusChecker();

            readSettings_tabImeCombo();
            setImeComboStatusChecker();

            readSettings_tabFontColor();
            setFontColortatusChecker();

            readSettings_tabKeyAssign();
            setKeyAssignStatusChecker();

            readSettings_tabCtrlKeys();
            setCtrlKeysStatusChecker();

            readSettings_tabHistory();
            setHistoryStatusChecker();

            readSettings_tabMiscSettings();
            setMiscSettingsStatusChecker();

            readSettings_tabDevelop();
            setDevelopStatusChecker();

            checkerAll.Reinitialize();

            // ".txt" に関連付けられたプログラムのパスを取得
            getTxtAssociatedProgramPath();

            // フォームタイマーの開始
            timer1.Interval = timerInterval;
            timer1.Start();
            logger.Info("Timer Started");
        }

        private void DlgSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CancelButton == null) {
                // CancelButton が null のときは閉じない
                e.Cancel = true;
            } else {
                logger.InfoH("ENTER");
                frmMain?.CloseDlgStrokeLog();
                timer1.Stop();
                logger.Info("Timer Stopped");
                ShownDlg = null;
                checkerAll.Dispose();
                Helper.WaitMilliSeconds(100);       // 微妙なタイミングで invoke されるのを防ぐ
                logger.InfoH("LEAVE");
            }
        }

        private IButtonControl prevCancelButton = null;

        private void changeCancelButton(bool flag, Button button)
        {
            if (flag && CancelButton == button) {
                prevCancelButton = button;
                CancelButton = null;
            } else if (!flag && CancelButton == null && prevCancelButton == button) {
                CancelButton = button;
            }
        }

        //----------------------------------------------------------------------------------------
        // 拡張子に関連付けられたプログラムのパスを取得
        // cf. https://stackoverflow.com/questions/162331/finding-the-default-application-for-opening-a-particular-file-type-on-windows
        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern uint AssocQueryString(
            AssocF flags,
            AssocStr str,
            string pszAssoc,
            string pszExtra,
            [Out] StringBuilder pszOut,
            ref uint pcchOut
        );

        [Flags]
        public enum AssocF
        {
            None = 0,
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200,
            Init_IgnoreUnknown = 0x400,
            Init_Fixed_ProgId = 0x800,
            Is_Protocol = 0x1000,
            Init_For_File = 0x2000
        }

        public enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic,
            InfoTip,
            QuickTip,
            TileInfo,
            ContentType,
            DefaultIcon,
            ShellExtension,
            DropTarget,
            DelegateExecute,
            Supported_Uri_Protocols,
            ProgID,
            AppID,
            AppPublisher,
            AppIconReference,
            Max
        }

        static string AssocQueryString(AssocStr association, string extension)
        {
            const int S_OK = 0;
            const int S_FALSE = 1;

            uint length = 0;
            uint ret = AssocQueryString(AssocF.None, association, extension, null, null, ref length);
            if (ret != S_FALSE) {
                throw new InvalidOperationException("Could not determine associated string");
            }

            var sb = new StringBuilder((int)length); // (length-1) will probably work too as the marshaller adds null termination
            ret = AssocQueryString(AssocF.None, association, extension, null, sb, ref length);
            if (ret != S_OK) {
                throw new InvalidOperationException("Could not determine associated string");
            }

            return sb.ToString();
        }

        private static string txtAssociatedProgramPath = "";

        private static  void getTxtAssociatedProgramPath()
        {
            var path = AssocQueryString(AssocStr.Executable, ".txt");
            logger.Info($"Txt Associated Program Path={path}");
            if (path._endsWith(".exe")) {
                txtAssociatedProgramPath = path;
            }
        }

        private void openFileByTxtAssociatedProgram(string filename)
        {
            try {
                if (filename._notEmpty() && txtAssociatedProgramPath._notEmpty()) {
                    System.Diagnostics.Process.Start(txtAssociatedProgramPath, KanchokuIni.Singleton.KanchokuDir._joinPath(filename));
                }
            } catch { }
        }

        //----------------------------------------------------------------------------------------

        private void openDocumentUrl(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        /// <summary> tabPage に含まれるコントロールをチェッカーに登録する</summary>
        /// <param name="tabPage"></param>
        /// <param name="checker"></param>
        private void addContorlsToChecker(Form tabPage, GuiStatusChecker checker)
        {
            foreach (var ctl in tabPage.Controls) {
                if (ctl is TextBox || ctl is CheckBox || ctl is RadioButton) {
                    checker.Add((Control)ctl);
                }
            }
        }

        //-----------------------------------------------------------------------------------
        // 基本設定
        //-----------------------------------------------------------------------------------
        void readSettings_tabBasic()
        {
            // 漢直モードトグル/OFFキー
            selectModeToggleKeyItem(comboBox_unmodifiedToggleKey, Settings.GetString("unmodifiedHotKey").Replace("X", ""));
            selectModeToggleKeyItem(comboBox_modifiedToggleKey, Settings.GetString("hotKey").Replace("X", ""));
            if (comboBox_unmodifiedToggleKey.Text._isEmpty() && comboBox_modifiedToggleKey.Text._isEmpty()) {
                selectModeToggleKeyItem(comboBox_modifiedToggleKey, "dc");
            }
            selectModeToggleKeyItem(comboBox_unmodifiedOffKey, Settings.GetString("unmodifiedOffHotKey").Replace("X", ""));
            selectModeToggleKeyItem(comboBox_modifiedOffKey, Settings.GetString("offHotKey").Replace("X", ""));

            // 仮想鍵盤表示
            radioButton_noVkb.Checked = !Settings.ShowVkbOrMaker;
            radioButton_normalVkb.Checked = Settings.ShowVkbOrMaker && Settings.VirtualKeyboardShowStrokeCount > 0;
            radioButton_modeMarker.Checked = Settings.ShowVkbOrMaker && !radioButton_normalVkb.Checked;
            textBox_vkbShowStrokeCount.Text = $"{Math.Abs(Settings.VirtualKeyboardShowStrokeCount)}";
            //checkBox_hideTopText.Checked = Settings.GetString("topBoxMode")._toLower()._equalsTo("hideonselect");
            //checkBox_hideTopText.Enabled = radioButton_modeMarker.Checked;

            // 開始・終了
            textBox_splashWindowShowDuration.Text = $"{Settings.SplashWindowShowDuration}";
            checkBox_confirmOnClose.Checked = Settings.ConfirmOnClose;

            // ファイル
            comboBox_keyboardFile.Text = getKeyboardName(Settings.KeyboardFile);
            comboBox_deckeyCharsFile.Text = getKeyboardName(Settings.GetString("charsDefFile"));
            textBox_easyCharsFile.Text = Settings.EasyCharsFile;
            textBox_strokeHelpFile.Text = Settings.StrokeHelpFile;
            textBox_bushuCompFile.Text = Settings.BushuFile;
            textBox_bushuAssocFile.Text = Settings.BushuAssocFile;
            textBox_mazegakiFile.Text = Settings.MazegakiFile;
            textBox_historyFile.Text = Settings.HistoryFile;

            if (Settings.TableFile._notEmpty()) comboBox_tableFile.Text = getTableName(KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.TableFile));
            if (Settings.TableFile2._notEmpty()) comboBox_tableFile2.Text = getTableName(KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.TableFile2));
        }

        private void selectModeToggleKeyItem(ComboBox cbx, string hex)
        {
            if (hex._notEmpty()) {
                for (int i = 0; i < cbx.Items.Count; ++i) {
                    if (cbx.Items[i].ToString()._startsWith(hex)) {
                        cbx.SelectedIndex = i;
                        return;
                    }
                }
            }
            cbx.Text = hex;
        }

        private void setBasicStatusChecker()
        {
            button_basicEnter.Enabled = false;
            checkerBasic.CtlToBeEnabled = button_basicEnter;
            checkerBasic.ControlEnabler = tabBasicStatusChanged;

            // 漢直モードトグルキー
            checkerBasic.Add(comboBox_unmodifiedToggleKey);
            checkerBasic.Add(comboBox_modifiedToggleKey);
            checkerBasic.Add(comboBox_unmodifiedOffKey);
            checkerBasic.Add(comboBox_modifiedOffKey);

            // 仮想鍵盤表示
            checkerBasic.Add(radioButton_normalVkb);
            checkerBasic.Add(radioButton_modeMarker);
            checkerBasic.Add(radioButton_noVkb);
            checkerBasic.Add(textBox_vkbShowStrokeCount);
            //checkerBasic.Add(checkBox_hideTopText);

            // 開始・終了
            checkerBasic.Add(textBox_splashWindowShowDuration);
            checkerBasic.Add(checkBox_confirmOnClose);

            // ファイル
            checkerBasic.Add(comboBox_keyboardFile);
            checkerBasic.Add(comboBox_deckeyCharsFile);
            checkerBasic.Add(textBox_easyCharsFile);
            checkerBasic.Add(comboBox_tableFile);
            checkerBasic.Add(comboBox_tableFile2);
            checkerBasic.Add(textBox_strokeHelpFile);
            checkerBasic.Add(textBox_bushuCompFile);
            checkerBasic.Add(textBox_bushuAssocFile);
            checkerBasic.Add(textBox_mazegakiFile);
            checkerBasic.Add(textBox_historyFile);

            checkerAll.Add(checkerBasic);
        }

        private void tabBasicStatusChanged(bool flag)
        {
            button_basicClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_basicClose);
        }

        string getTableFileName(string tableFileSpec)
        {
            var items = tableFileSpec._strip()._reScan(@" \(([^()]+)\)$");
            logger.DebugH(() => $"{items._getFirst()}, {items._getNth(1)}, {items._getNth(2)}, {items._getNth(3)}");
            return items._getNth(1)._orElse(tableFileSpec._strip());
        }

        string getKeyboardFileName(string keyboardFileSpec)
        {
            var items = keyboardFileSpec._strip()._reScan(@" \(([^()]+)\)$");
            logger.DebugH(() => $"{items._getFirst()}, {items._getNth(1)}, {items._getNth(2)}, {items._getNth(3)}");
            return items._getNth(1)._orElse(keyboardFileSpec._strip());
        }

        private void button_basicEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");

            frmMain?.DeactivateDecoderWithModifiersOff();

            label_initialMsg.Hide();

            // スプラッシュウィンドウ
            Settings.SetUserIni("splashWindowShowDuration", textBox_splashWindowShowDuration.Text.Trim());
            Settings.SetUserIni("confirmOnClose", checkBox_confirmOnClose.Checked);

            // 漢直モードトグルキー
            Settings.SetUserIni("unmodifiedHotKey", comboBox_unmodifiedToggleKey.Text.Trim()._reReplace(" .*", "")._orElse("X"));
            Settings.SetUserIni("hotKey", comboBox_modifiedToggleKey.Text.Trim()._reReplace(" .*", "")._orElse("X"));
            Settings.SetUserIni("unmodifiedOffHotKey", comboBox_unmodifiedOffKey.Text.Trim()._reReplace(" .*", "")._orElse("X"));
            Settings.SetUserIni("offHotKey", comboBox_modifiedOffKey.Text.Trim()._reReplace(" .*", "")._orElse("X"));

            // 仮想鍵盤表示
            Settings.SetUserIni("vkbShowStrokeCount", $"{textBox_vkbShowStrokeCount.Text._parseInt(1)._lowLimit(0) * (radioButton_normalVkb.Checked ? 1 : -1)}");
            Settings.SetUserIni("showVkbOrMaker", !radioButton_noVkb.Checked);
            //Settings.SetUserIni("topBoxMode", checkBox_hideTopText.Checked ? "hideOnSelect" : "showAlways");

            // ファイル
            Settings.SetUserIni("keyboard", getKeyboardFileName(comboBox_keyboardFile.Text));
            Settings.SetUserIni("charsDefFile", getKeyboardFileName(comboBox_deckeyCharsFile.Text));
            Settings.SetUserIni("easyCharsFile", textBox_easyCharsFile.Text.Trim());
            Settings.SetUserIni("tableFile", getTableFileName(comboBox_tableFile.Text));
            Settings.SetUserIni("tableFile2", getTableFileName(comboBox_tableFile2.Text));
            Settings.SetUserIni("strokeHelpFile", textBox_strokeHelpFile.Text.Trim());
            Settings.SetUserIni("bushuFile", textBox_bushuCompFile.Text.Trim());
            Settings.SetUserIni("bushuAssocFile", textBox_bushuAssocFile.Text.Trim());
            Settings.SetUserIni("mazegakiFile", textBox_mazegakiFile.Text.Trim());
            Settings.SetUserIni("historyFile", textBox_historyFile.Text.Trim());

            //Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabBasic();

            checkerBasic.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            // 機能キー割当も呼んでおく
            readSettings_tabKeyAssign();
            checkerKeyAssign.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            // 各種定義ファイルの再読み込み
            //frmMain?.ReloadDefFiles();

            //frmMain?.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            //SystemHelper.ShowInfoMessageBox("設定しました");
            label_okResultBasic.Show();

            logger.InfoH("LEAVE");
        }

        /// <summary> INIファイルのリロード </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Reload_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");

            label_initialMsg.Hide();

            reloadIniFileAndDefFiles();
            label_reloadBasic.Show();

            logger.InfoH("LEAVE");
        }

        private void reloadIniFileAndDefFiles()
        {
            //Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabBasic();
            readSettings_tabAdvanced();
            readSettings_tabImeCombo();
            readSettings_tabFontColor();
            readSettings_tabKeyAssign();
            readSettings_tabCtrlKeys();
            readSettings_tabHistory();
            readSettings_tabMiscSettings();
            readSettings_tabDevelop();

            checkerAll.Reinitialize();

            // 各種定義ファイルの再読み込み
            //frmMain?.ReloadDefFiles();

            //frmMain?.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);
        }

        private void button_basicClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            if (button_basicClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabBasic();
                checkerBasic.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
                logger.InfoH("LEAVE");
            }
        }

        // テーブル表示名を取得
        string getTableName(string filepath)
        {
            var filename = filepath._getFileName();
            var parentDir = filepath._getDirPath();
            var dirname = parentDir._getFileName();
            while (parentDir._notEmpty() && dirname._notEmpty() && !dirname._equalsTo(Settings.TableFileDir)) {
                filename = dirname._joinPath(filename);
                parentDir = parentDir._getDirPath();
                dirname = parentDir._getFileName();
            }
            if (dirname._equalsTo(Settings.TableFileDir)) filename = dirname._joinPath(filename);
            logger.DebugH(() => $"filepath={filepath}, filename={filename}, parentDir={parentDir}");

            var content = Helper.GetFileHead(filepath, 2048);
            if (content._notEmpty()) {
                //logger.DebugH(() => $"content={content}");
                var list = content._reScan(@"#define\s+display-name\s+""([^""]+)");
                logger.DebugH(() => $"match: {list._getFirst()}, {list._getSecond()}, list.Count={list._safeCount()}");
                if (list._safeCount() >= 2) {
                    // DisplayName が見つかった
                    return $"{list[1]} ({filename})";
                }
            }
            return filename;
        }

        // キーボード表示名を取得
        string getKeyboardName(string filepath)
        {
            var filename = filepath._getFileName();
            if (filename._notEmpty() && filename._toLower() != "jp" && filename._toLower() != "us") {
                filepath = KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.KeyboardFileDir, filename);

                var content = Helper.GetFileHead(filepath, 2048)._strip();
                if (content._notEmpty()) {
                    //logger.DebugH(() => $"content={content}");
                    var list = content._reScan(@"# *NAME\s*[=:]\s*(.+)");
                    logger.DebugH(() => $"match: {list._getFirst()}, {list._getSecond()}, list.Count={list._safeCount()}");
                    if (list._safeCount() >= 2) {
                        // DisplayName が見つかった
                        return $"{list[1]} ({filename})";
                    }
                }
            }
            return filename;
        }

        private string[] getDirectoryNames(string tableDir)
        {
            IEnumerable<string> getEffectiveDirNames(string dirPath)
            {
                return Helper.GetDirectories(dirPath, "*").Select(x => x._getFileName()).Where(x => x._notEmpty() && (Settings.ShowHiddleFolder || x[0] != '_' && x[0] != '.'));
            }

            var result = new List<string>();
            if (!tableDir._endsWith(@"\tables") && !tableDir._endsWith(@"/tables")) {
                result.Add("..");
                //result.AddRange(getEffectiveDirNames(tableDir._getDirPath()).Select(x => ".."._joinPath(x)));
            }
            result.AddRange(getEffectiveDirNames(tableDir).Select(x => "."._joinPath(x)));
            return result.ToArray();
        }

        string tableDirectory1 = null;
        string tableDirectory2 = null;

        private void comboBox_tableFile_DropDown(object sender, EventArgs e)
        {
            tableDirectory1 = comboBox_table_DropDown(comboBox_tableFile, tableDirectory1);
            logger.InfoH(() => $"tableDirectory1={tableDirectory1}");
        }

        private void comboBox_tableFile2_DropDown(object sender, EventArgs e)
        {
            tableDirectory2 = comboBox_table_DropDown(comboBox_tableFile2, tableDirectory2);
            logger.InfoH(() => $"tableDirectory2={tableDirectory2}");
        }

        private string comboBox_table_DropDown(ComboBox comboBox, string tableDir)
        {
            (var absTableDir, var relTabledir) = getTableFileDir(comboBox, tableDir);
            var dirList = getDirectoryNames(absTableDir);
            var fileList = Helper.GetFiles(absTableDir, "*.tbl").Select(x => getTableName(x)).ToArray();
            comboBox.Items.Clear();
            comboBox.Items.AddRange(dirList);
            comboBox.Items.AddRange(fileList);
            return relTabledir;
        }

        private void comboBox_keyboardFile_DropDown(object sender, EventArgs e)
        {
            comboBox_keyboardFile.Items.Clear();
            comboBox_keyboardFile.Items.Add("JP");
            comboBox_keyboardFile.Items.Add("US");
            var fileList = Helper.GetFiles(KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.KeyboardFileDir), "*.key").Select(x => getKeyboardName(x)).ToArray();
            comboBox_keyboardFile.Items.AddRange(fileList);
        }

        private void comboBox_deckeyCharsFile_DropDown(object sender, EventArgs e)
        {
            comboBox_deckeyCharsFile.Items.Clear();
            comboBox_deckeyCharsFile.Items.Add("");
            var fileList = Helper.GetFiles(KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.KeyboardFileDir), "chars.*.txt").Select(x => getKeyboardName(x)).ToArray();
            comboBox_deckeyCharsFile.Items.AddRange(fileList);
        }

        private void comboBox_tableFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkDirectory(comboBox_tableFile);
        }

        private void comboBox_tableFile2_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkDirectory(comboBox_tableFile2);
        }

        private (string, string) getTableFileDir(ComboBox comboBox, string tableDir)
        {
            logger.InfoH(() => $"ENTER: tableDir={tableDir}");
            var rootDir = KanchokuIni.Singleton.KanchokuDir;
            var tableFile = getTableFileName(comboBox.Text);
            if (tableFile._startsWith(".")) {
                tableDir = Helper.JoinPathAndCanonicalize(tableDir._orElse(Settings.TableFileDir), tableFile);
                if (!tableDir._startsWith(Settings.TableFileDir)) tableDir = Settings.TableFileDir;
            } else {
                tableDir = tableFile._getDirPath()._orElse(Settings.TableFileDir);
            }
            var tableFileDir = rootDir._joinPath(tableDir);
            if (Helper.DirectoryExists(tableFileDir)) {
                logger.InfoH(() => $"LEAVE: tableFileDir={tableFileDir}, tableDir={tableDir}");
                return (tableFileDir, tableDir);
            }

            tableDir = Settings.TableFileDir;
            tableFileDir = rootDir._joinPath(tableDir);
            logger.InfoH(() => $"LEAVE: tableFileDir={(Helper.DirectoryExists(tableFileDir) ? tableFileDir : rootDir)}, tableDir={tableDir}");
            return (Helper.DirectoryExists(tableFileDir) ? tableFileDir : rootDir, tableDir);
        }

        private void checkDirectory(ComboBox comboBox)
        {
            if (getTableFileName(comboBox.Text)._startsWith(".")) {
                comboBox.DroppedDown = true;
            }
        }

        //-----------------------------------------------------------------------------------
        // 詳細設定
        //-----------------------------------------------------------------------------------
        /// <summary> 詳細設定</summary>
        void readSettings_tabAdvanced()
        {
            // 仮想鍵盤表示位置 
            textBox_vkbOffsetX.Text = $"{Settings.VirtualKeyboardOffsetX}";
            textBox_vkbOffsetY.Text = $"{Settings.VirtualKeyboardOffsetY}";
            textBox_vkbFixedPosX.Text = $"{Math.Abs(Settings.VirtualKeyboardFixedPosX)}";
            textBox_vkbFixedPosY.Text = $"{Math.Abs(Settings.VirtualKeyboardFixedPosY)}";
            radioButton_vkbRelativePos.Checked = Settings.VirtualKeyboardFixedPosX < 0 || Settings.VirtualKeyboardFixedPosY < 0;
            radioButton_vkbFixedPos.Checked = !radioButton_vkbRelativePos.Checked;
            textBox_fixedPosClassNames.Text = Settings.FixedPosClassNames._reReplace(@"\|", "\r\n");

            comboBox_selectCtrlKeyItem(comboBox_vkbShowHideTemporaryKey, $"{Settings.VkbShowHideTemporaryKey}");

            //textBox_displayScale.Text = $"{Settings.DisplayScale:f3}";
            // モード標識
            textBox_kanjiModeInterval.Text = $"{Settings.KanjiModeMarkerShowIntervalSec}";
            textBox_alphaModeShowTime.Text = $"{Settings.AlphaModeMarkerShowMillisec}";

            // 各種待ち時間
            textBox_preWmCharGuardMillisec.Text = $"{Settings.PreWmCharGuardMillisec}";
            textBox_activeWinInfoIntervalMillisec.Text = $"{Settings.GetActiveWindowInfoIntervalMillisec}";
            textBox_vkbMoveGuardMillisec.Text = $"{Settings.VirtualKeyboardMoveGuardMillisec}";
            textBox_cancelSecondStrokeMillisec.Text = $"{Settings.CancelSecondStrokeMillisec}";

            // ノード重複の警告
            checkBox_duplicateWarningEnabled.Checked = Settings.DuplicateWarningEnabled;

            // 無限ループ対策
            textBox_deckeyInfiniteLoopDetectCount.Text = $"{Settings.DeckeyInfiniteLoopDetectCount}";

            // クリップボード経由
            textBox_minLeghthViaClipboard.Text = $"{Settings.MinLeghthViaClipboard}";

            // 自身以外のキーボードフックツールからの出力を無視する
            checkBox_ignoreOtherHooker.Checked = Settings.IgnoreOtherHooker;

            // ファイル保存
            textBox_backFileRotationGeneration.Text = $"{Settings.BackFileRotationGeneration}";
            checkBox_dictsAutoSaveEnabled.Checked = Settings.SaveDictsIntervalTime > 0;
            textBox_saveDictsIntervalTime.Text = $"{Math.Abs(Settings.SaveDictsIntervalTime)}";
            textBox_saveDictsCalmTime.Text = $"{Settings.SaveDictsCalmTime}";

            //checkBox_autoOffWhenBurstKeyIn.Checked = Settings.GetString("autoOffWhenBurstKeyIn")._parseBool();
        }

        private void setAdvancedStatusChecker()
        {
            button_advancedEnter.Enabled = false;
            checkerAdvanced.CtlToBeEnabled = button_advancedEnter;
            checkerAdvanced.ControlEnabler = tabAdvancedStatusChanged;

            // モード標識
            checkerAdvanced.Add(textBox_vkbOffsetX);
            checkerAdvanced.Add(textBox_vkbOffsetY);
            checkerAdvanced.Add(textBox_vkbFixedPosX);
            checkerAdvanced.Add(textBox_vkbFixedPosY);
            checkerAdvanced.Add(radioButton_vkbRelativePos);
            checkerAdvanced.Add(radioButton_vkbFixedPos);
            checkerAdvanced.Add(textBox_fixedPosClassNames);
            checkerAdvanced.Add(comboBox_vkbShowHideTemporaryKey);
            //checkerAdvanced.Add(textBox_displayScale);
            checkerAdvanced.Add(textBox_kanjiModeInterval);
            checkerAdvanced.Add(textBox_alphaModeShowTime);

            // 各種待ち時間
            checkerAdvanced.Add(textBox_preWmCharGuardMillisec);
            checkerAdvanced.Add(textBox_activeWinInfoIntervalMillisec);
            checkerAdvanced.Add(textBox_vkbMoveGuardMillisec);
            checkerAdvanced.Add(textBox_cancelSecondStrokeMillisec);

            // ノード重複の警告
            checkerAdvanced.Add(checkBox_duplicateWarningEnabled);

            // 無限ループ対策
            checkerAdvanced.Add(textBox_deckeyInfiniteLoopDetectCount);

            // クリップボード経由
            checkerAdvanced.Add(textBox_minLeghthViaClipboard);

            // 自身以外のキーボードフックツールからの出力を無視する
            checkerAdvanced.Add(checkBox_ignoreOtherHooker);

            // ファイル保存世代数
            checkerAdvanced.Add(textBox_backFileRotationGeneration);
            checkerAdvanced.Add(checkBox_dictsAutoSaveEnabled);
            checkerAdvanced.Add(textBox_saveDictsIntervalTime);
            checkerAdvanced.Add(textBox_saveDictsCalmTime);

            //checkerAdvanced.Add(checkBox_autoOffWhenBurstKeyIn);

            checkerAll.Add(checkerAdvanced);
        }

        private void radioButton_vkbRelativePos_CheckedChanged(object sender, EventArgs e)
        {
            textBox_vkbOffsetX.Enabled = radioButton_vkbRelativePos.Checked;
            textBox_vkbOffsetY.Enabled = radioButton_vkbRelativePos.Checked;
            textBox_vkbFixedPosX.Enabled = !radioButton_vkbRelativePos.Checked;
            textBox_vkbFixedPosY.Enabled = !radioButton_vkbRelativePos.Checked;
        }

        private void checkBox_dictsAutoSaveEnabled_CheckedChanged(object sender, EventArgs e)
        {
            textBox_saveDictsIntervalTime.Enabled = checkBox_dictsAutoSaveEnabled.Checked;
            textBox_saveDictsCalmTime.Enabled = checkBox_dictsAutoSaveEnabled.Checked;
        }

        private void tabAdvancedStatusChanged(bool flag)
        {
            button_advancedClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_advancedClose);
        }

        private void button_advancedEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            frmMain?.DeactivateDecoderWithModifiersOff();

            // モード標識表示時間
            Settings.SetUserIni("vkbOffsetX", textBox_vkbOffsetX.Text.Trim());
            Settings.SetUserIni("vkbOffsetY", textBox_vkbOffsetY.Text.Trim());
            int factor = radioButton_vkbFixedPos.Checked ? 1 : -1;
            Settings.SetUserIni("vkbFixedPos", $"{textBox_vkbFixedPosX.Text.Trim()._parseInt(-1) * factor},{textBox_vkbFixedPosY.Text.Trim()._parseInt(-1) * factor}");
            Settings.SetUserIni("fixedPosClassNames", textBox_fixedPosClassNames.Text.Trim()._reReplace(@"[ \r\n]+", "|"));
            //Settings.SetUserIni("displayScale", textBox_displayScale.Text.Trim());
            Settings.SetUserIni("vkbShowHideTemporaryKey", comboBox_vkbShowHideTemporaryKey._getSelectedItemSplittedFirst(""));
            Settings.SetUserIni("kanjiModeMarkerShowIntervalSec", textBox_kanjiModeInterval.Text.Trim());
            Settings.SetUserIni("alphaModeMarkerShowMillisec", textBox_alphaModeShowTime.Text.Trim());

            // 各種待ち時間
            Settings.SetUserIni("preWmCharGuardMillisec", textBox_preWmCharGuardMillisec.Text.Trim());
            Settings.SetUserIni("activeWindowInfoIntervalMillisec", textBox_activeWinInfoIntervalMillisec.Text.Trim());
            Settings.SetUserIni("virtualKeyboardMoveGuardMillisec", textBox_vkbMoveGuardMillisec.Text.Trim());
            Settings.SetUserIni("cancelSecondStrokeMillisec", textBox_cancelSecondStrokeMillisec.Text.Trim());

            // ノード重複の警告
            Settings.SetUserIni("duplicateWarningEnabled", checkBox_duplicateWarningEnabled.Checked);

            // 無限ループ対策
            Settings.SetUserIni("deckeyInfiniteLoopDetectCount", textBox_deckeyInfiniteLoopDetectCount.Text.Trim());

            // クリップボード経由
            Settings.SetUserIni("minLeghthViaClipboard", textBox_minLeghthViaClipboard.Text.Trim());

            // 自身以外のキーボードフックツールからの出力を無視する
            Settings.SetUserIni("ignoreOtherHooker", checkBox_ignoreOtherHooker.Checked);

            // ファイル保存世代数
            Settings.SetUserIni("backFileRotationGeneration", textBox_backFileRotationGeneration.Text.Trim());
            Settings.SetUserIni("saveDictsIntervalTime", (checkBox_dictsAutoSaveEnabled.Checked ? "" : "-") + textBox_saveDictsIntervalTime.Text.Trim());
            Settings.SetUserIni("saveDictsCalmTime", textBox_saveDictsCalmTime.Text.Trim());

            //Settings.SetUserIni("autoOffWhenBurstKeyIn", checkBox_autoOffWhenBurstKeyIn.Checked);

            //Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabAdvanced();
            checkerAdvanced.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            //frmVkb?.SetNormalCellBackColors();
            frmMode?.ShowImmediately();

            // 各種定義ファイルの再読み込み
            //frmMain?.ReloadDefFiles();

            //frmMain?.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultAdvanced.Show();

            logger.InfoH("LEAVE");
        }

        // 仮想鍵盤の現在位置を取得
        private void button_getCurrentPos_Click(object sender, EventArgs e)
        {
            if (frmVkb != null) {
                textBox_vkbFixedPosX.Text = frmVkb.Left.ToString();
                textBox_vkbFixedPosY.Text = frmVkb.Top.ToString();
            }
        }

        private void button_advancedClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            if (button_advancedClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabAdvanced();
                checkerAdvanced.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
                logger.InfoH("LEAVE");
            }
        }

        //-----------------------------------------------------------------------------------
        // IME・同時打鍵
        //-----------------------------------------------------------------------------------
        void readSettings_tabImeCombo()
        {
            // SandS
            checkBox_SandSEnabled.Checked = Settings.SandSEnabled;
            checkBox_SandSEnabledWhenOffMode.Checked = Settings.SandSEnabledWhenOffMode;
            comboBox_SandSAssignedPlane.SelectedIndex = Settings.SandSAssignedPlane._lowLimit(0)._highLimit(7);
            checkBox_OneshotSandSEnabled.Checked = Settings.OneshotSandSEnabled;
            textBox_SandSEnableSpaceOrRepeatMillisec.Text = $"{Settings.SandSEnableSpaceOrRepeatMillisec}";
            checkBox_SandSEnablePostShift.Checked = Settings.SandSEnablePostShift;

            // 同時打鍵
            textBox_combinationMaxAllowedLeadTimeMs.Text = $"{Settings.CombinationKeyMaxAllowedLeadTimeMs}";
            textBox_comboMaxAllowedPostfixTimeMs.Text = $"{Settings.ComboKeyMaxAllowedPostfixTimeMs}";
            textBox_combinationKeyTimeMs.Text = $"{Settings.CombinationKeyMinOverlappingTimeMs}";
            textBox_comboDisableIntervalTimeMs.Text = $"{Settings.ComboDisableIntervalTimeMs}";
            checkBox_combinationKeyTimeOnlyAfterSecond.Checked = Settings.CombinationKeyMinTimeOnlyAfterSecond;
            checkBox_useCombinationKeyTimer1.Checked = Settings.UseCombinationKeyTimer1;
            checkBox_useCombinationKeyTimer2.Checked = Settings.UseCombinationKeyTimer2;
            checkBox_useComboExtModKeyAsSingleHit.Checked = Settings.UseComboExtModKeyAsSingleHit;
            checkBox_threeKeysComboUnconditional.Checked = Settings.ThreeKeysComboUnconditional;
            textBox_sequentialPriorityWords.Text = Settings.SequentialPriorityWords._reReplace(@"\|", "\r\n");

            // IME連携
            checkBox_imeCooperationEnabled.Checked = Settings.ImeCooperationEnabled;
            checkBox_imeKatakanaToHiragana.Checked = Settings.ImeKatakanaToHiragana;
            radioButton_imeSendInputInRoman.Checked = Settings.ImeSendInputInRoman;
            radioButton_imeSendInputInKana.Checked = Settings.ImeSendInputInKana;
            radioButton_imeSendInputInUnicode.Checked = !(Settings.ImeSendInputInRoman || Settings.ImeSendInputInKana);
            //textBox_imeUnicodeClassNames.Text = Settings.ImeUnicodeClassNames._reReplace(@"\|", "\r\n");

        }

        private void setImeComboStatusChecker()
        {
            button_imeComboEnter.Enabled = false;
            checkerImeCombo.CtlToBeEnabled = button_imeComboEnter;
            checkerImeCombo.ControlEnabler = tabImeComboStatusChanged;

            // SandS
            checkerImeCombo.Add(checkBox_SandSEnabled);
            checkerImeCombo.Add(checkBox_SandSEnabledWhenOffMode);
            checkerImeCombo.Add(comboBox_SandSAssignedPlane);
            checkerImeCombo.Add(checkBox_OneshotSandSEnabled);
            checkerImeCombo.Add(textBox_SandSEnableSpaceOrRepeatMillisec);
            checkerImeCombo.Add(checkBox_SandSEnablePostShift);

            // 同時打鍵
            checkerImeCombo.Add(textBox_combinationMaxAllowedLeadTimeMs);
            checkerImeCombo.Add(textBox_comboMaxAllowedPostfixTimeMs);
            checkerImeCombo.Add(textBox_combinationKeyTimeMs);
            checkerImeCombo.Add(textBox_comboDisableIntervalTimeMs);
            checkerImeCombo.Add(checkBox_combinationKeyTimeOnlyAfterSecond);
            checkerImeCombo.Add(checkBox_useCombinationKeyTimer1);
            checkerImeCombo.Add(checkBox_useCombinationKeyTimer2);
            checkerImeCombo.Add(checkBox_useComboExtModKeyAsSingleHit);
            checkerImeCombo.Add(checkBox_threeKeysComboUnconditional);
            checkerImeCombo.Add(textBox_sequentialPriorityWords);

            // IME連携
            checkerImeCombo.Add(checkBox_imeCooperationEnabled);
            checkerImeCombo.Add(checkBox_imeKatakanaToHiragana);
            checkerImeCombo.Add(radioButton_imeSendInputInRoman);
            checkerImeCombo.Add(radioButton_imeSendInputInKana);
            checkerImeCombo.Add(radioButton_imeSendInputInUnicode);
            //checkerImeCombo.Add(textBox_imeUnicodeClassNames);

            checkerAll.Add(checkerImeCombo);
        }

        private void tabImeComboStatusChanged(bool flag)
        {
            button_imeComboClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_imeComboClose);
        }

        private void button_imeComboEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            frmMain?.DeactivateDecoderWithModifiersOff();

            // SandS
            Settings.SetUserIni("sandsEnabled", checkBox_SandSEnabled.Checked);
            Settings.SetUserIni("sandsEnabledWhenOffMode", checkBox_SandSEnabledWhenOffMode.Checked);
            Settings.SetUserIni("sandsAssignedPlane", comboBox_SandSAssignedPlane.SelectedIndex);
            Settings.SetUserIni("oneshotSandSEnabled", checkBox_OneshotSandSEnabled.Checked);
            Settings.SetUserIni("sandsEnableSpaceOrRepeatMillisec", textBox_SandSEnableSpaceOrRepeatMillisec.Text);
            Settings.SetUserIni("sandsEnablePostShift", checkBox_SandSEnablePostShift.Checked);

            // 同時打鍵
            Settings.SetUserIni("combinationMaxAllowedLeadTimeMs", textBox_combinationMaxAllowedLeadTimeMs.Text.Trim());
            Settings.SetUserIni("comboMaxAllowedPostfixTimeMs", textBox_comboMaxAllowedPostfixTimeMs.Text.Trim());
            Settings.SetUserIni("combinationKeyTimeMs", textBox_combinationKeyTimeMs.Text.Trim());
            Settings.SetUserIni("comboDisableIntervalTimeMs", textBox_comboDisableIntervalTimeMs.Text.Trim());
            Settings.SetUserIni("combinationKeyTimeOnlyAfterSecond", checkBox_combinationKeyTimeOnlyAfterSecond.Checked);
            Settings.SetUserIni("useCombinationKeyTimer1", checkBox_useCombinationKeyTimer1.Checked);
            Settings.SetUserIni("useCombinationKeyTimer2", checkBox_useCombinationKeyTimer2.Checked);
            Settings.SetUserIni("useComboExtModKeyAsSingleHit", checkBox_useComboExtModKeyAsSingleHit.Checked);
            Settings.SetUserIni("threeKeysComboUnconditional", checkBox_threeKeysComboUnconditional.Checked);
            Settings.SetUserIni("sequentialPriorityWords", textBox_sequentialPriorityWords.Text.Trim()._reReplace(@"[\r\n]+", "|"));

            // IME連携
            Settings.SetUserIni("imeCooperationEnabled", checkBox_imeCooperationEnabled.Checked);
            Settings.SetUserIni("imeKatakanaToHiragana", checkBox_imeKatakanaToHiragana.Checked);
            Settings.SetUserIni("imeSendInputInRoman", radioButton_imeSendInputInRoman.Checked);
            Settings.SetUserIni("imeSendInputInKana", radioButton_imeSendInputInKana.Checked);
            //Settings.SetUserIni("imeUnicodeClassNames", textBox_imeUnicodeClassNames.Text.Trim()._reReplace(@"[ \r\n]+", "|"));

            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabImeCombo();

            checkerImeCombo.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            label_okResultImeCombo.Show();

            logger.InfoH("LEAVE");
        }

        private void button_imeComboClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            if (button_imeComboClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabImeCombo();
                checkerImeCombo.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
                logger.InfoH("LEAVE");
            }
        }

        private void button_imeComboReload_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            reloadIniFileAndDefFiles();
            label_imeComboReload.Show();
            logger.InfoH("LEAVE");
        }

        //-----------------------------------------------------------------------------------
        // フォント・色設定
        //-----------------------------------------------------------------------------------
        void readSettings_tabFontColor()
        {
            // 仮想鍵盤フォント
            textBox_normalFont.Text = Settings.NormalVkbFontSpec;
            textBox_centerFont.Text = Settings.CenterVkbFontSpec;
            textBox_verticalFont.Text = Settings.VerticalVkbFontSpec;
            textBox_horizontalFont.Text = Settings.HorizontalVkbFontSpec;
            textBox_minibufFont.Text = Settings.MiniBufVkbFontSpec;
            textBox_verticalFontHeightFactor.Text = $"{Settings.VerticalFontHeightFactor:f2}";

            // 通常鍵盤背景色
            textBox_topLevelBackColor.Text = Settings.BgColorTopLevelCells;
            textBox_centerSideBackColor.Text = Settings.BgColorCenterSideCells;
            textBox_highLowLevelBackColor.Text = Settings.BgColorHighLowLevelCells;
            textBox_middleLevelBackColor.Text = Settings.BgColorMiddleLevelCells;
            textBox_nextStrokeBackColor.Text = Settings.BgColorNextStrokeCell;

            // モード標識文字色
            textBox_modeForeColor.Text = Settings.KanjiModeMarkerForeColor;
            textBox_2ndStrokeForeColor.Text = Settings.KanjiModeMarker2ndForeColor;
            textBox_alphaModeForeColor.Text = Settings.AlphaModeForeColor;

            // 中央鍵盤背景色
            textBox_on2ndStrokeBackColor.Text= Settings.BgColorOnWaiting2ndStroke;
            textBox_onMazegaki.Text= Settings.BgColorForMazegaki;
            textBox_onHistAssoc.Text= Settings.BgColorForHistOrAssoc;
            textBox_onBushuCompHelp.Text= Settings.BgColorForBushuCompHelp;
            textBox_onSecondaryTable.Text= Settings.BgColorForSecondaryTable;
            textBox_onKanaTrainingMode.Text= Settings.BgColorForKanaTrainingMode;

            // 縦列・横列鍵盤背景色
            textBox_firstCandidateBackColor.Text = Settings.BgColorForFirstCandidate;
            textBox_onSelectedBackColor.Text = Settings.BgColorOnSelected;

        }

        private void setFontColortatusChecker()
        {
            button_fontColorEnter.Enabled = false;
            checkerFontColor.CtlToBeEnabled = button_fontColorEnter;
            checkerFontColor.ControlEnabler = tabFontColorStatusChanged;

            // フォント
            checkerFontColor.Add(textBox_normalFont);
            checkerFontColor.Add(textBox_centerFont);
            checkerFontColor.Add(textBox_verticalFont);
            checkerFontColor.Add(textBox_horizontalFont);
            checkerFontColor.Add(textBox_minibufFont);
            checkerFontColor.Add(textBox_verticalFontHeightFactor);

            // 通常鍵盤背景色
            checkerFontColor.Add(textBox_topLevelBackColor);
            checkerFontColor.Add(textBox_centerSideBackColor);
            checkerFontColor.Add(textBox_highLowLevelBackColor);
            checkerFontColor.Add(textBox_middleLevelBackColor);
            checkerFontColor.Add(textBox_nextStrokeBackColor);

            // モード標識文字色
            checkerFontColor.Add(textBox_modeForeColor);
            checkerFontColor.Add(textBox_2ndStrokeForeColor);
            checkerFontColor.Add(textBox_alphaModeForeColor);

            // 中央鍵盤背景色
            checkerFontColor.Add(textBox_on2ndStrokeBackColor);
            checkerFontColor.Add(textBox_onMazegaki);
            checkerFontColor.Add(textBox_onHistAssoc);
            checkerFontColor.Add(textBox_onBushuCompHelp);
            checkerFontColor.Add(textBox_onSecondaryTable);
            checkerFontColor.Add(textBox_onKanaTrainingMode);

            // 縦列・横列鍵盤背景色
            checkerFontColor.Add(textBox_firstCandidateBackColor);
            checkerFontColor.Add(textBox_onSelectedBackColor);

            checkerAll.Add(checkerFontColor);
        }

        private void button_fontColrEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            frmMain?.DeactivateDecoderWithModifiersOff();

            // フォント
            Settings.SetUserIni("normalFont", textBox_normalFont.Text.Trim());
            Settings.SetUserIni("centerFont", textBox_centerFont.Text.Trim());
            Settings.SetUserIni("verticalFont", textBox_verticalFont.Text.Trim());
            Settings.SetUserIni("horizontalFont", textBox_horizontalFont.Text.Trim());
            Settings.SetUserIni("minibufFont", textBox_minibufFont.Text.Trim());
            Settings.SetUserIni("verticalFontHeightFactor", textBox_verticalFontHeightFactor.Text.Trim());

            // 通常鍵盤背景色
            Settings.SetUserIni("bgColorTopLevelCells", textBox_topLevelBackColor.Text.Trim());
            Settings.SetUserIni("bgColorCenterSideCells", textBox_centerSideBackColor.Text.Trim());
            Settings.SetUserIni("bgColorHighLowLevelCells", textBox_highLowLevelBackColor.Text.Trim());
            Settings.SetUserIni("bgColorMiddleLevelCells", textBox_middleLevelBackColor.Text.Trim());
            Settings.SetUserIni("bgColorNextStrokeCell", textBox_nextStrokeBackColor.Text.Trim());

            // モード標識色
            Settings.SetUserIni("kanjiModeMarkerForeColor", textBox_modeForeColor.Text.Trim());
            Settings.SetUserIni("kanjiModeMarker2ndForeColor", textBox_2ndStrokeForeColor.Text.Trim());
            Settings.SetUserIni("alphaModeForeColor", textBox_alphaModeForeColor.Text.Trim());

            // 中央鍵盤背景色
            Settings.SetUserIni("bgColorOnWaiting2ndStroke", textBox_on2ndStrokeBackColor.Text.Trim());
            Settings.SetUserIni("bgColorForMazegaki", textBox_onMazegaki.Text.Trim());
            Settings.SetUserIni("bgColorForHistOrAssoc", textBox_onHistAssoc.Text.Trim());
            Settings.SetUserIni("bgColorForBushuCompHelp", textBox_onBushuCompHelp.Text.Trim());
            Settings.SetUserIni("bgColorForSecondaryTable", textBox_onSecondaryTable.Text.Trim());
            Settings.SetUserIni("bgColorForKanaTrainingMode", textBox_onKanaTrainingMode.Text.Trim());

            // 縦列・横列鍵盤背景色
            Settings.SetUserIni("bgColorForFirstCandidate", textBox_firstCandidateBackColor.Text.Trim());
            Settings.SetUserIni("bgColorOnSelected", textBox_onSelectedBackColor.Text.Trim());

            //Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabFontColor();

            checkerFontColor.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            // 各種定義ファイルの再読み込み
            //frmMain?.ReloadDefFiles();

            //frmMain?.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultFontColor.Show();

            logger.InfoH("LEAVE");
        }

        private void tabFontColorStatusChanged(bool flag)
        {
            button_fontColorClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_fontColorClose);
        }

        private void button_fontColorClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            if (button_fontColorClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabFontColor();
                checkerFontColor.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
                logger.InfoH("LEAVE");
            }
        }

        //-----------------------------------------------------------------------------------
        // 機能キー割り当て
        //-----------------------------------------------------------------------------------
        void readSettings_tabKeyAssign()
        {
            textBox_zenkakuModeKeySeq.Text = Settings.ZenkakuModeKeySeq._orElse(() => makePresetString(Settings.ZenkakuModeKeySeq_Preset));
            textBox_zenkakuOneCharKeySeq.Text = Settings.ZenkakuOneCharKeySeq._orElse(() => makePresetString(Settings.ZenkakuOneCharKeySeq_Preset));
            textBox_katakanaModeKeySeq.Text = Settings.KatakanaModeKeySeq._orElse(() => makePresetString(Settings.KatakanaModeKeySeq_Preset));
            textBox_nextThroughKeySeq.Text = Settings.NextThroughKeySeq._orElse(() => makePresetString(Settings.NextThroughKeySeq_Preset));
            textBox_historyKeySeq.Text = Settings.HistoryKeySeq._orElse(() => makePresetString(Settings.HistoryKeySeq_Preset));
            textBox_historyOneCharKeySeq.Text = Settings.HistoryOneCharKeySeq._orElse(() => makePresetString(Settings.HistoryOneCharKeySeq_Preset));
            textBox_historyFewCharsKeySeq.Text = Settings.HistoryFewCharsKeySeq._orElse(() => makePresetString(Settings.HistoryFewCharsKeySeq_Preset));
            textBox_mazegakiKeySeq.Text = Settings.MazegakiKeySeq._orElse(() => makePresetString(Settings.MazegakiKeySeq_Preset));
            textBox_bushuCompKeySeq.Text = Settings.BushuCompKeySeq._orElse(() => makePresetString(Settings.BushuCompKeySeq_Preset));
            textBox_bushuAssocKeySeq.Text = Settings.BushuAssocKeySeq._orElse(() => makePresetString(Settings.BushuAssocKeySeq_Preset));
            textBox_bushuAssocDirectKeySeq.Text = Settings.BushuAssocDirectKeySeq._orElse(() => makePresetString(Settings.BushuAssocDirectKeySeq_Preset));
            textBox_katakanaOneShotKeySeq.Text = Settings.KatakanaOneShotKeySeq._orElse(() => makePresetString(Settings.KatakanaOneShotKeySeq_Preset));
            textBox_hankakuKatakanaOneShotKeySeq.Text = Settings.HankakuKatakanaOneShotKeySeq._orElse(() => makePresetString(Settings.HankakuKatakanaOneShotKeySeq_Preset));
            textBox_blockerSetterOneShotKeySeq.Text = Settings.BlockerSetterOneShotKeySeq._orElse(() => makePresetString(Settings.BlockerSetterOneShotKeySeq_Preset));
        }

        string makePresetString(string preset)
        {
            return preset._notEmpty() ? "(" + preset + ")" : "";
        }

        private string revertPresetString(string str)
        {
            return str._getFirst() == '(' && str._getLast() == ')' ? "" : str;
        }

        private void setKeyAssignStatusChecker()
        {
            button_keyAssignEnter.Enabled = false;
            checkerKeyAssign.CtlToBeEnabled = button_keyAssignEnter;
            checkerKeyAssign.ControlEnabler = tabKeyAssignStatusChanged;

            checkerKeyAssign.Add(textBox_zenkakuModeKeySeq);
            checkerKeyAssign.Add(textBox_zenkakuOneCharKeySeq);
            checkerKeyAssign.Add(textBox_katakanaModeKeySeq);
            checkerKeyAssign.Add(textBox_nextThroughKeySeq);
            checkerKeyAssign.Add(textBox_historyKeySeq);
            checkerKeyAssign.Add(textBox_historyOneCharKeySeq);
            checkerKeyAssign.Add(textBox_historyFewCharsKeySeq);
            checkerKeyAssign.Add(textBox_mazegakiKeySeq);
            checkerKeyAssign.Add(textBox_bushuCompKeySeq);
            checkerKeyAssign.Add(textBox_bushuAssocKeySeq);
            checkerKeyAssign.Add(textBox_bushuAssocDirectKeySeq);
            checkerKeyAssign.Add(textBox_katakanaOneShotKeySeq);
            checkerKeyAssign.Add(textBox_hankakuKatakanaOneShotKeySeq);
            checkerKeyAssign.Add(textBox_blockerSetterOneShotKeySeq);

            checkerAll.Add(checkerKeyAssign);
        }

        private void tabKeyAssignStatusChanged(bool flag)
        {
            button_keyAssignClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_keyAssignClose);
        }

        private void button_keyAssignReload_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            reloadIniFileAndDefFiles();
            label_keyAssignReload.Show();
            logger.InfoH("LEAVE");
        }

        private void button_keyAssignEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            frmMain?.DeactivateDecoderWithModifiersOff();

            Settings.SetUserIni("zenkakuModeKeySeq", revertPresetString(textBox_zenkakuModeKeySeq.Text));
            Settings.SetUserIni("zenkakuOneCharKeySeq", revertPresetString(textBox_zenkakuOneCharKeySeq.Text));
            Settings.SetUserIni("katakanaModeKeySeq", revertPresetString(textBox_katakanaModeKeySeq.Text));
            Settings.SetUserIni("nextThroughKeySeq", revertPresetString(textBox_nextThroughKeySeq.Text));
            Settings.SetUserIni("historyKeySeq", revertPresetString(textBox_historyKeySeq.Text));
            Settings.SetUserIni("historyOneCharKeySeq", revertPresetString(textBox_historyOneCharKeySeq.Text));
            Settings.SetUserIni("historyFewCharsKeySeq", revertPresetString(textBox_historyFewCharsKeySeq.Text));
            Settings.SetUserIni("mazegakiKeySeq", revertPresetString(textBox_mazegakiKeySeq.Text));
            Settings.SetUserIni("bushuCompKeySeq", revertPresetString(textBox_bushuCompKeySeq.Text));
            Settings.SetUserIni("bushuAssocKeySeq", revertPresetString(textBox_bushuAssocKeySeq.Text));
            Settings.SetUserIni("bushuAssocDirectKeySeq", revertPresetString(textBox_bushuAssocDirectKeySeq.Text));
            Settings.SetUserIni("katakanaOneShotKeySeq", revertPresetString(textBox_katakanaOneShotKeySeq.Text));
            Settings.SetUserIni("hankakuKatakanaOneShotKeySeq", revertPresetString(textBox_hankakuKatakanaOneShotKeySeq.Text));
            Settings.SetUserIni("blockerSetterOneShotKeySeq", revertPresetString(textBox_blockerSetterOneShotKeySeq.Text));

            Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabKeyAssign();
            checkerKeyAssign.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            //frmVkb?.SetNormalCellBackColors();
            frmMode?.ShowImmediately();

            // 各種定義ファイルの再読み込み
            //frmMain?.ReloadDefFiles();

            //frmMain?.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);
            //frmMain?.MakeInitialVkbTable();

            label_okResultKeyAssign.Show();

            logger.InfoH("LEAVE");
        }

        private void button_keyAssignClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            if (button_keyAssignClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabKeyAssign();
                checkerKeyAssign.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
                logger.InfoH("LEAVE");
            }
        }

        private void button_keyAssignTable_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openDocumentUrl(Settings.KeyboardUrl);
        }

        //-----------------------------------------------------------------------------------
        //  Ctrlキー変換
        //-----------------------------------------------------------------------------------
        void readSettings_tabCtrlKeys()
        {
            // Ctrlキー変換
            checkBox_globalCtrlKeysEnabled.Checked = Settings.GlobalCtrlKeysEnabled;

            initializeCtrlKeyConversionComboBox();

            checkBox_backSpaceKey.Checked = Settings.CtrlKeyConvertedToBackSpace._notEmpty() && !Settings.CtrlKeyConvertedToBackSpace.StartsWith("#");
            checkBox_deleteKey.Checked = Settings.CtrlKeyConvertedToDelete._notEmpty() && !Settings.CtrlKeyConvertedToDelete.StartsWith("#");
            checkBox_leftArrowKey.Checked = Settings.CtrlKeyConvertedToLeftArrow._notEmpty() && !Settings.CtrlKeyConvertedToLeftArrow.StartsWith("#");
            checkBox_rightArrowKey.Checked = Settings.CtrlKeyConvertedToRightArrow._notEmpty() && !Settings.CtrlKeyConvertedToRightArrow.StartsWith("#");
            checkBox_upArrowKey.Checked = Settings.CtrlKeyConvertedToUpArrow._notEmpty() && !Settings.CtrlKeyConvertedToUpArrow.StartsWith("#");
            checkBox_downArrowKey.Checked = Settings.CtrlKeyConvertedToDownArrow._notEmpty() && !Settings.CtrlKeyConvertedToDownArrow.StartsWith("#");
            checkBox_escKey.Checked = Settings.CtrlKeyConvertedToEsc._notEmpty() && !Settings.CtrlKeyConvertedToEsc.StartsWith("#");
            checkBox_tabKey.Checked = Settings.CtrlKeyConvertedToTab._notEmpty() && !Settings.CtrlKeyConvertedToTab.StartsWith("#");
            checkBox_enterKey.Checked = Settings.CtrlKeyConvertedToEnter._notEmpty() && !Settings.CtrlKeyConvertedToEnter.StartsWith("#");
            checkBox_homeKey.Checked = Settings.CtrlKeyConvertedToHome._notEmpty() && !Settings.CtrlKeyConvertedToHome.StartsWith("#");
            checkBox_endKey.Checked = Settings.CtrlKeyConvertedToEnd._notEmpty() && !Settings.CtrlKeyConvertedToEnd.StartsWith("#");

            comboBox_selectCtrlKeyItem(comboBox_backSpaceKey, $"{Settings.CtrlKeyConvertedToBackSpace.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_deleteKey, $"{Settings.CtrlKeyConvertedToDelete.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_leftArrowKey, $"{Settings.CtrlKeyConvertedToLeftArrow.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_rightArrowKey, $"{Settings.CtrlKeyConvertedToRightArrow.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_upArrowKey, $"{Settings.CtrlKeyConvertedToUpArrow.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_downArrowKey, $"{Settings.CtrlKeyConvertedToDownArrow.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_escKey, $"{Settings.CtrlKeyConvertedToEsc.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_tabKey, $"{Settings.CtrlKeyConvertedToTab.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_enterKey, $"{Settings.CtrlKeyConvertedToEnter.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_homeKey, $"{Settings.CtrlKeyConvertedToHome.Replace("#", "")}");
            comboBox_selectCtrlKeyItem(comboBox_endKey, $"{Settings.CtrlKeyConvertedToEnd.Replace("#", "")}");

            checkBox_useLeftCtrl.Checked = Settings.UseLeftControlToConversion;
            checkBox_useRightCtrl.Checked = Settings.UseRightControlToConversion;

            radioButton_excludeFollowings.Checked = !Settings.UseClassNameListAsInclusion;
            radioButton_includeFollowings.Checked = Settings.UseClassNameListAsInclusion;

            textBox_targetClassNames.Text = Settings.CtrlKeyTargetClassNames._reReplace(@"\|", "\r\n");

            checkBox_ctrlJasEnter.Checked = Settings.UseCtrlJasEnter;
            //checkBox_ctrlMasEnter.Checked = Settings.UseCtrlMasEnter;

            comboBox_selectCtrlKeyItem(comboBox_fullEscapeKey, $"{Settings.FullEscapeKey}");
            comboBox_selectCtrlKeyItem(comboBox_strokeHelpRotationKey, $"{Settings.StrokeHelpRotationKey}");

            checkBox_dateStringKey.Checked = Settings.CtrlKeyConvertedToDateString._notEmpty() && !Settings.CtrlKeyConvertedToDateString.StartsWith("#");
            comboBox_selectCtrlKeyItem(comboBox_dateStringKey, $"{Settings.CtrlKeyConvertedToDateString.Replace("#", "")}");
            textBox_dateStringFormat.Text = Settings.DateStringFormat._reReplace(@"\|", "\r\n");

            checkBox_extraModifiersEnabled.Checked = Settings.ExtraModifiersEnabled;
            textBox_modConversionFile.Text = Settings.ModConversionFile;
        }

        private void setCtrlKeysStatusChecker()
        {
            button_ctrlEnter.Enabled = false;
            checkerCtrlKeys.CtlToBeEnabled = button_ctrlEnter;
            checkerCtrlKeys.ControlEnabler = tabCtrlKeyStatusChanged;

            checkerCtrlKeys.Add(checkBox_globalCtrlKeysEnabled);

            checkerCtrlKeys.Add(checkBox_backSpaceKey);
            checkerCtrlKeys.Add(checkBox_deleteKey);
            checkerCtrlKeys.Add(checkBox_leftArrowKey);
            checkerCtrlKeys.Add(checkBox_rightArrowKey);
            checkerCtrlKeys.Add(checkBox_upArrowKey);
            checkerCtrlKeys.Add(checkBox_downArrowKey);
            checkerCtrlKeys.Add(checkBox_escKey);
            checkerCtrlKeys.Add(checkBox_tabKey);
            checkerCtrlKeys.Add(checkBox_enterKey);
            checkerCtrlKeys.Add(checkBox_homeKey);
            checkerCtrlKeys.Add(checkBox_endKey);

            checkerCtrlKeys.Add(comboBox_backSpaceKey);
            checkerCtrlKeys.Add(comboBox_deleteKey);
            checkerCtrlKeys.Add(comboBox_leftArrowKey);
            checkerCtrlKeys.Add(comboBox_rightArrowKey);
            checkerCtrlKeys.Add(comboBox_upArrowKey);
            checkerCtrlKeys.Add(comboBox_downArrowKey);
            checkerCtrlKeys.Add(comboBox_escKey);
            checkerCtrlKeys.Add(comboBox_tabKey);
            checkerCtrlKeys.Add(comboBox_enterKey);
            checkerCtrlKeys.Add(comboBox_homeKey);
            checkerCtrlKeys.Add(comboBox_endKey);

            checkerCtrlKeys.Add(checkBox_dateStringKey);
            checkerCtrlKeys.Add(comboBox_dateStringKey);
            checkerCtrlKeys.Add(textBox_dateStringFormat);

            checkerCtrlKeys.Add(checkBox_useLeftCtrl);
            checkerCtrlKeys.Add(checkBox_useRightCtrl);

            checkerCtrlKeys.Add(radioButton_excludeFollowings);
            checkerCtrlKeys.Add(radioButton_includeFollowings);

            checkerCtrlKeys.Add(textBox_targetClassNames);

            checkerCtrlKeys.Add(checkBox_ctrlJasEnter);
            //checkerCtrlKeys.Add(checkBox_ctrlMasEnter);

            checkerCtrlKeys.Add(comboBox_fullEscapeKey);
            checkerCtrlKeys.Add(comboBox_strokeHelpRotationKey);

            checkerCtrlKeys.Add(checkBox_extraModifiersEnabled);
            checkerCtrlKeys.Add(textBox_modConversionFile);

            checkerAll.Add(checkerCtrlKeys);
        }

        private void tabCtrlKeyStatusChanged(bool flag)
        {
            button_ctrlClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_ctrlClose);
        }

        private string makeCtrlKeyConversion(CheckBox checkBox, ComboBox comboBox)
        {
            return $"{(checkBox.Checked ? "" : "#")}{comboBox._getSelectedItemSplittedFirst()}";
        }

        private void button_ctrlEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            frmMain?.DeactivateDecoderWithModifiersOff();

            Settings.SetUserIni("globalCtrlKeysEnabled", checkBox_globalCtrlKeysEnabled.Checked);

            Settings.SetUserIni("ctrlKeyToBackSpace", makeCtrlKeyConversion(checkBox_backSpaceKey, comboBox_backSpaceKey));
            Settings.SetUserIni("ctrlKeyToDelete", makeCtrlKeyConversion(checkBox_deleteKey, comboBox_deleteKey));
            Settings.SetUserIni("ctrlKeyToLeftArrowKey", makeCtrlKeyConversion(checkBox_leftArrowKey, comboBox_leftArrowKey));
            Settings.SetUserIni("ctrlKeyToRightArrowKey", makeCtrlKeyConversion(checkBox_rightArrowKey, comboBox_rightArrowKey));
            Settings.SetUserIni("ctrlKeyToUpArrowKey", makeCtrlKeyConversion(checkBox_upArrowKey, comboBox_upArrowKey));
            Settings.SetUserIni("ctrlKeyToDownArrowKey", makeCtrlKeyConversion(checkBox_downArrowKey, comboBox_downArrowKey));
            Settings.SetUserIni("ctrlKeyToEsc", makeCtrlKeyConversion(checkBox_escKey, comboBox_escKey));
            Settings.SetUserIni("ctrlKeyToTab", makeCtrlKeyConversion(checkBox_tabKey, comboBox_tabKey));
            Settings.SetUserIni("ctrlKeyToEnter", makeCtrlKeyConversion(checkBox_enterKey, comboBox_enterKey));
            Settings.SetUserIni("ctrlKeyToHome", makeCtrlKeyConversion(checkBox_homeKey, comboBox_homeKey));
            Settings.SetUserIni("ctrlKeyToEnd", makeCtrlKeyConversion(checkBox_endKey, comboBox_endKey));

            Settings.SetUserIni("ctrlKeyToDateString", makeCtrlKeyConversion(checkBox_dateStringKey, comboBox_dateStringKey));
            Settings.SetUserIni("dateStringFormat", textBox_dateStringFormat.Text.Trim()._reReplace(@"[ \r\n]+", "|"));

            Settings.SetUserIni("useLeftControlToConversion", checkBox_useLeftCtrl.Checked);
            Settings.SetUserIni("useRightControlToConversion", checkBox_useRightCtrl.Checked);
            Settings.SetUserIni("useClassNameListAsInclusion", radioButton_includeFollowings.Checked);
            Settings.SetUserIni("ctrlKeyTargetClassNames", textBox_targetClassNames.Text.Trim()._reReplace(@"[ \r\n]+", "|"));

            Settings.SetUserIni("useCtrlJasEnter", checkBox_ctrlJasEnter.Checked);
            //Settings.SetUserIni("useCtrlMasEnter", checkBox_ctrlMasEnter.Checked);

            Settings.SetUserIni("fullEscapeKey", comboBox_fullEscapeKey._getSelectedItemSplittedFirst("G"));
            Settings.SetUserIni("strokeHelpRotationKey", comboBox_strokeHelpRotationKey._getSelectedItemSplittedFirst("T"));

            Settings.SetUserIni("extraModifiersEnabled", checkBox_extraModifiersEnabled.Checked);
            Settings.SetUserIni("modConversionFile", textBox_modConversionFile.Text);

            Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabCtrlKeys();
            checkerCtrlKeys.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            label_okResultCtrlKeys.Show();

            logger.InfoH("LEAVE");
        }

        private void button_ctrlClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            if (button_ctrlClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabCtrlKeys();
                checkerCtrlKeys.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
                logger.InfoH("LEAVE");
            }
        }

        private void checkBox_extraModifiersEnabled_CheckedChanged(object sender, EventArgs e)
        {
            button_setModConversion.Enabled = checkBox_extraModifiersEnabled.Checked;
        }

        private void button_openModConversionFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            //try {
            //    if (Settings.ModConversionFile._notEmpty()) {
            //        System.Diagnostics.Process.Start(TableFileDir._joinPath(Settings.ModConversionFile));
            //    }
            //} catch { }
            openFileByTxtAssociatedProgram(textBox_modConversionFile.Text);
        }

        private void button_ctrlReload_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            reloadIniFileAndDefFiles();
            label_ctrlReload.Show();
        }

        //-----------------------------------------------------------------------------------
        //  履歴・交ぜ書き
        //-----------------------------------------------------------------------------------
        void readSettings_tabHistory()
        {
            // 履歴関連
            textBox_histKanjiWordMinLength.Text = $"{Settings.HistKanjiWordMinLength}";
            textBox_histKanjiWordMinLengthEx.Text = $"{Settings.HistKanjiWordMinLengthEx}";
            textBox_histKatakanaWordMinLength.Text = $"{Settings.HistKatakanaWordMinLength}";
            textBox_histMaxLength.Text = $"{Settings.HistMaxLength}";
            textBox_histKanjiKeyLen.Text = $"{Settings.HistKanjiKeyLength}";
            textBox_histKatakanaKeyLen.Text = $"{Settings.HistKatakanaKeyLength}";
            textBox_histHiraganaKeyLen.Text = $"{Settings.HistHiraganaKeyLength}";
            textBox_histHorizontalCandMax.Text = $"{Settings.HistHorizontalCandMax}";
            checkBox_autoHistEnabled.Checked = Settings.AutoHistSearchEnabled;
            comboBox_historySearchKey.Enabled = checkBox_historySearchKey.Checked;
            checkBox_historySearchKey.Checked = Settings.HistorySearchCtrlKey._notEmpty() && !Settings.HistorySearchCtrlKey.StartsWith("#");
            comboBox_selectCtrlKeyItem(comboBox_historySearchKey, $"{Settings.HistorySearchCtrlKey.Replace("#", "")}");
            //checkBox_histSearchByShiftSpace.Checked = Settings.HistSearchByShiftSpace;
            checkBox_selectFirstCandByEnter.Checked = Settings.SelectFirstCandByEnter;
            //checkBox_autoHistEnabled_CheckedChanged(null, null);
            checkBox_useArrowKeyToSelectCand.Checked = Settings.UseArrowKeyToSelectCandidate;
            checkBox_selectHistCandByTab.Checked = Settings.SelectHistCandByTab;
            comboBox_histDelDeckeyId.SelectedIndex = Settings.HistDelDeckeyId._lowLimit(41)._highLimit(48) - 41;
            comboBox_histNumDeckeyId.SelectedIndex = Settings.HistNumDeckeyId._lowLimit(41)._highLimit(48) - 41;

            // 交ぜ書き
            //checkBox_mazegakiByShiftSpace.Checked = Settings.MazegakiByShiftSpace;
            checkBox_mazegakiSelectFirstCand.Checked = Settings.MazegakiSelectFirstCand;
            checkBox_mazeBlockerTail.Checked = !Settings.MazeBlockerTail;
            checkBox_mazeRemoveHeadSpace.Checked = Settings.MazeRemoveHeadSpace;
            checkBox_mazeRightShiftYomiPos.Checked = Settings.MazeRightShiftYomiPos;
            checkBox_mazeNoIfxConnectKanji.Checked = Settings.MazeNoIfxConnectKanji;
            checkBox_mazeNoIfxConnectAny.Checked = Settings.MazeNoIfxConnectAny;
            textBox_mazeYomiMaxLen.Text = $"{Settings.MazeYomiMaxLen}";
            textBox_mazeGobiMaxLen.Text = $"{Settings.MazeGobiMaxLen}";
            textBox_mazeGobiLikeTailLen.Text = $"{Settings.MazeGobiLikeTailLen}";
            textBox_histMapGobiMaxLength.Text = $"{Settings.HistMapGobiMaxLength}";
            textBox_mazeHistRegisterMinLen.Text = $"{Settings.MazeHistRegisterMinLen}";
        }

        private void setHistoryStatusChecker()
        {
            // 履歴関連
            button_histEnter.Enabled = false;
            checkerHistory.CtlToBeEnabled = button_histEnter;
            checkerHistory.ControlEnabler = tabHistoryStatusChanged;
            checkerHistory.Add(textBox_histKanjiWordMinLength);
            checkerHistory.Add(textBox_histKanjiWordMinLengthEx);
            checkerHistory.Add(textBox_histKatakanaWordMinLength);
            checkerHistory.Add(textBox_histMaxLength);
            checkerHistory.Add(textBox_histKanjiKeyLen);
            checkerHistory.Add(textBox_histKatakanaKeyLen);
            checkerHistory.Add(textBox_histHiraganaKeyLen);
            checkerHistory.Add(textBox_histHorizontalCandMax);
            checkerHistory.Add(checkBox_autoHistEnabled);
            checkerHistory.Add(checkBox_historySearchKey);
            checkerHistory.Add(comboBox_historySearchKey);
            //checkerHistory.Add(checkBox_histSearchByShiftSpace);
            checkerHistory.Add(checkBox_selectFirstCandByEnter);
            //checkerHistory.Add(checkBox_autoHistEnabled_CheckedChanged);
            checkerHistory.Add(checkBox_useArrowKeyToSelectCand);
            checkerHistory.Add(checkBox_selectHistCandByTab);
            checkerHistory.Add(comboBox_histDelDeckeyId);
            checkerHistory.Add(comboBox_histNumDeckeyId);

            // 交ぜ書き
            //checkerHistory.Add(checkBox_mazegakiByShiftSpace);
            checkerHistory.Add(checkBox_mazegakiSelectFirstCand);
            checkerHistory.Add(checkBox_mazeBlockerTail);
            checkerHistory.Add(checkBox_mazeRemoveHeadSpace);
            checkerHistory.Add(checkBox_mazeRightShiftYomiPos);
            checkerHistory.Add(checkBox_mazeNoIfxConnectKanji);
            checkerHistory.Add(checkBox_mazeNoIfxConnectAny);
            checkerHistory.Add(textBox_mazeYomiMaxLen);
            checkerHistory.Add(textBox_mazeGobiMaxLen);
            checkerHistory.Add(textBox_mazeGobiLikeTailLen);
            checkerHistory.Add(textBox_histMapGobiMaxLength);
            checkerHistory.Add(textBox_mazeHistRegisterMinLen);

            checkerAll.Add(checkerHistory);
        }

        private void tabHistoryStatusChanged(bool flag)
        {
            button_histClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_histClose);
        }

        private void button_histEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            frmMain?.DeactivateDecoderWithModifiersOff();

            Settings.SetUserIni("histKanjiWordMinLength", textBox_histKanjiWordMinLength.Text.Trim());
            Settings.SetUserIni("histKanjiWordMinLengthEx", textBox_histKanjiWordMinLengthEx.Text.Trim());
            Settings.SetUserIni("histKatakanaWordMinLength", textBox_histKatakanaWordMinLength.Text.Trim());
            Settings.SetUserIni("histMaxLength", textBox_histMaxLength.Text.Trim());
            Settings.SetUserIni("histHiraganaKeyLength", textBox_histHiraganaKeyLen.Text.Trim());
            Settings.SetUserIni("histHorizontalCandMax", textBox_histHorizontalCandMax.Text.Trim());
            Settings.SetUserIni("histKatakanaKeyLength", textBox_histKatakanaKeyLen.Text.Trim());
            Settings.SetUserIni("histKanjiKeyLength", textBox_histKanjiKeyLen.Text.Trim());
            Settings.SetUserIni("autoHistSearchEnabled", checkBox_autoHistEnabled.Checked);
            Settings.SetUserIni("histSearchCtrlKey", makeCtrlKeyConversion(checkBox_historySearchKey, comboBox_historySearchKey));
            //Settings.SetUserIni("histSearchByShiftSpace", checkBox_histSearchByShiftSpace.Checked);
            Settings.SetUserIni("selectFirstCandByEnter", checkBox_selectFirstCandByEnter.Checked);
            Settings.SetUserIni("useArrowKeyToSelectCandidate", checkBox_useArrowKeyToSelectCand.Checked);
            Settings.SetUserIni("selectHistCandByTab", checkBox_selectHistCandByTab.Checked);
            Settings.SetUserIni("histDelDeckeyId", comboBox_histDelDeckeyId.Text.Trim()._substring(0, 2));
            Settings.SetUserIni("histNumDeckeyId", comboBox_histNumDeckeyId.Text.Trim()._substring(0, 2));

            //Settings.SetUserIni("mazegakiByShiftSpace", checkBox_mazegakiByShiftSpace.Checked);
            Settings.SetUserIni("mazegakiSelectFirstCand", checkBox_mazegakiSelectFirstCand.Checked);
            Settings.SetUserIni("mazeBlockerTail", !checkBox_mazeBlockerTail.Checked);
            Settings.SetUserIni("mazeRemoveHeadSpace", checkBox_mazeRemoveHeadSpace.Checked);
            Settings.SetUserIni("mazeRightShiftYomiPos", checkBox_mazeRightShiftYomiPos.Checked);
            Settings.SetUserIni("mazeNoIfxConnectKanji", checkBox_mazeNoIfxConnectKanji.Checked);
            Settings.SetUserIni("mazeNoIfxConnectAny", checkBox_mazeNoIfxConnectAny.Checked);
            Settings.SetUserIni("mazeGobiMaxLen", textBox_mazeGobiMaxLen.Text.Trim());
            Settings.SetUserIni("mazeYomiMaxLen", textBox_mazeYomiMaxLen.Text.Trim());
            Settings.SetUserIni("mazeGobiLikeTailLen", textBox_mazeGobiLikeTailLen.Text.Trim());
            Settings.SetUserIni("histMapGobiMaxLength", textBox_histMapGobiMaxLength.Text.Trim());
            Settings.SetUserIni("mazeHistRegisterMinLen", textBox_mazeHistRegisterMinLen.Text.Trim());

            Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabHistory();
            checkerHistory.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            // 各種定義ファイルの再読み込み
            //frmMain?.ReloadDefFiles();

            //frmMain?.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultHist.Show();

            logger.InfoH("LEAVE");
        }

        private void button_histClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            if (button_histClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabHistory();
                checkerHistory.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
                logger.InfoH("LEAVE");
            }
        }

        private void checkBox_autoHistEnabled_CheckedChanged(object sender, EventArgs e)
        {
            //checkBox_histSearchByCtrlSpace.Enabled = !checkBox_autoHistEnabled.Checked;
            //checkBox_histSearchByShiftSpace.Enabled = !checkBox_autoHistEnabled.Checked;
        }

        //-----------------------------------------------------------------------------------
        //  その他設定
        //-----------------------------------------------------------------------------------
        void readSettings_tabMiscSettings()
        {
            // その他変換
            checkBox_yamanobeEnabled.Checked = Settings.YamanobeEnabled;
            checkBox_autoBushuComp.Checked = Settings.AutoBushuComp;
            textBox_bushuAssocSelectCount.Text = $"{Settings.BushuAssocSelectCount}";
            checkBox_convertShiftedHiraganaToKatakana.Checked = Settings.ConvertShiftedHiraganaToKatakana;
            switch (Settings.HiraganaToKatakanaShiftPlane) {
                case 2: radioButton_shiftA.Checked = true; break;
                case 3: radioButton_shiftB.Checked = true; break;
                default: radioButton_normalShift.Checked = true; break;
            }
            changeShiftPlaneSectionRadioButtonsState();
            checkBox_convertHiraganaToKatakanaNormalPlane.Checked = Settings.HiraganaToKatakanaNormalPlane;
            checkBox_convertJaPeriod.Checked = Settings.ConvertJaPeriod;
            checkBox_convertJaComma.Checked = Settings.ConvertJaComma;
            checkBox_removeOneStrokeByBackspace.Checked = Settings.RemoveOneStrokeByBackspace;
            checkBox_eisuModeEnabled.Checked = Settings.EisuModeEnabled;
            textBox_eisuHistSearchChar.Text = Settings.EisuHistSearchChar;
            textBox_eisuExitCapitalCharNum.Text = $"{Settings.EisuExitCapitalCharNum}";
            textBox_eisuExitSpaceNum.Text = $"{Settings.EisuExitSpaceNum}";
            checkBox_upperRomanStrokeGuide.Checked = Settings.UpperRomanStrokeGuide;
            textBox_kanjiYomiFile.Text = Settings.KanjiYomiFile;
            textBox_romanBushuCompPrefix.Text = Settings.RomanBushuCompPrefix;
            textBox_romanSecPlanePrefix.Text = Settings.RomanSecPlanePrefix;
            textBox_preRewriteTargetChars.Text = $"{Settings.PreRewriteTargetChars}";
            textBox_preRewriteAllowedDelayTimeMs.Text = $"{Settings.PreRewriteAllowedDelayTimeMs}";
        }

        private void setMiscSettingsStatusChecker()
        {
            // その他変換
            button_miscEnter.Enabled = false;
            checkerMiscSettings.CtlToBeEnabled = button_miscEnter;
            checkerMiscSettings.ControlEnabler = tabMiscStatusChanged;
            checkerMiscSettings.Add(checkBox_yamanobeEnabled);
            checkerMiscSettings.Add(checkBox_autoBushuComp);
            checkerMiscSettings.Add(textBox_bushuAssocSelectCount);
            checkerMiscSettings.Add(checkBox_convertShiftedHiraganaToKatakana);
            checkerMiscSettings.Add(radioButton_normalShift);
            checkerMiscSettings.Add(radioButton_shiftA);
            checkerMiscSettings.Add(radioButton_shiftB);
            checkerMiscSettings.Add(checkBox_convertHiraganaToKatakanaNormalPlane);
            checkerMiscSettings.Add(checkBox_convertJaPeriod);
            checkerMiscSettings.Add(checkBox_convertJaComma);
            checkerMiscSettings.Add(checkBox_removeOneStrokeByBackspace);
            checkerMiscSettings.Add(checkBox_eisuModeEnabled);
            checkerMiscSettings.Add(textBox_eisuHistSearchChar);
            checkerMiscSettings.Add(textBox_eisuExitCapitalCharNum);
            checkerMiscSettings.Add(textBox_eisuExitSpaceNum);
            checkerMiscSettings.Add(checkBox_upperRomanStrokeGuide);
            checkerMiscSettings.Add(textBox_kanjiYomiFile);
            checkerMiscSettings.Add(textBox_romanBushuCompPrefix);
            checkerMiscSettings.Add(textBox_romanSecPlanePrefix);
            checkerMiscSettings.Add(textBox_preRewriteTargetChars);
            checkerMiscSettings.Add(textBox_preRewriteAllowedDelayTimeMs);

            checkerAll.Add(checkerMiscSettings);
        }

        private void tabMiscStatusChanged(bool flag)
        {
            button_miscClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_miscClose);
            //button_saveRomanTableFile.Enabled = !flag;
        }

        private void button_miscEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            Settings.SetUserIni("yamanobeEnabled", checkBox_yamanobeEnabled.Checked);
            Settings.SetUserIni("autoBushuComp", checkBox_autoBushuComp.Checked);
            Settings.SetUserIni("bushuAssocSelectCount", textBox_bushuAssocSelectCount.Text);
            Settings.SetUserIni("convertShiftedHiraganaToKatakana", checkBox_convertShiftedHiraganaToKatakana.Checked);
            Settings.SetUserIni("hiraToKataShiftPlane", radioButton_shiftA.Checked ? 2 : radioButton_shiftB.Checked ? 3 : 1);
            Settings.SetUserIni("hiraToKataNormalPlane", checkBox_convertHiraganaToKatakanaNormalPlane.Checked);
            Settings.SetUserIni("convertJaPeriod", checkBox_convertJaPeriod.Checked);
            Settings.SetUserIni("convertJaComma", checkBox_convertJaComma.Checked);
            Settings.SetUserIni("removeOneStrokeByBackspace", checkBox_removeOneStrokeByBackspace.Checked);
            Settings.SetUserIni("eisuModeEnabled", checkBox_eisuModeEnabled.Checked);
            Settings.SetUserIni("eisuHistSearchChar", textBox_eisuHistSearchChar.Text);
            Settings.SetUserIni("eisuExitCapitalCharNum", textBox_eisuExitCapitalCharNum.Text);
            Settings.SetUserIni("eisuExitSpaceNum", textBox_eisuExitSpaceNum.Text);
            Settings.SetUserIni("upperRomanStrokeGuide", checkBox_upperRomanStrokeGuide.Checked);
            Settings.SetUserIni("kanjiYomiFile", textBox_kanjiYomiFile.Text);
            Settings.SetUserIni("romanBushuCompPrefix", textBox_romanBushuCompPrefix.Text);
            Settings.SetUserIni("romanSecPlanePrefix", textBox_romanSecPlanePrefix.Text);
            Settings.SetUserIni("preRewriteTargetChars", textBox_preRewriteTargetChars.Text.Trim());
            Settings.SetUserIni("preRewriteAllowedDelayTimeMs", textBox_preRewriteAllowedDelayTimeMs.Text.Trim());

            Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabMiscSettings();
            checkerMiscSettings.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            // 各種定義ファイルの再読み込み
            //frmMain?.ReloadDefFiles();

            //frmMain?.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultMisc.Show();

            logger.InfoH("LEAVE");
        }

        private void checkBox_convertShiftedHiraganaToKatakana_CheckedChanged(object sender, EventArgs e)
        {
            changeShiftPlaneSectionRadioButtonsState();
        }

        private void changeShiftPlaneSectionRadioButtonsState()
        {
            radioButton_normalShift.Enabled = checkBox_convertShiftedHiraganaToKatakana.Checked;
            radioButton_shiftA.Enabled = checkBox_convertShiftedHiraganaToKatakana.Checked;
            radioButton_shiftB.Enabled = checkBox_convertShiftedHiraganaToKatakana.Checked;
        }

        private void button_openKanjiYomiFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            //try {
            //    if (Settings.KanjiYomiFile._notEmpty()) {
            //        System.Diagnostics.Process.Start(TableFileDir._joinPath(Settings.KanjiYomiFile));
            //    }
            //} catch { }
            openFileByTxtAssociatedProgram(Settings.KanjiYomiFile);
        }

        private void button_miscReload_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            reloadIniFileAndDefFiles();
            label_miscReload.Show();
        }

        private void button_saveRomanTableFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            frmMain?.ExecCmdDecoder("SaveRomanStrokeTable", $"{textBox_romanBushuCompPrefix.Text}\t{textBox_romanSecPlanePrefix.Text}");
            label_miscRomanOut.Show();
        }

        private void button_saveEelllJsTableFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            frmMain?.ExecCmdDecoder("SaveEelllJsTable", null);
            label_miscEelllJsOut.Show();
        }

        /// <summary> 閉じる </summary>
        private void button_miscClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            if (button_miscClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabMiscSettings();
                checkerMiscSettings.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
            }
        }

        //-----------------------------------------------------------------------------------
        // 辞書登録
        //-----------------------------------------------------------------------------------
        /// <summary> 履歴辞書登録 </summary>
        private void button_enterHistory_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            var line = textBox_history.Text.Trim().Replace(" ", "");
            if (line._notEmpty()) {
                //frmMain?.ExecCmdDecoder("addHistEntry", line);
                frmMain?.AddHistEntry(line);
                label_saveHist.Hide();
                label_history.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            }
        }

        private void button_saveHistoryFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            frmMain?.ExecCmdDecoder("saveHistoryDic", null);
            label_history.Hide();
            label_saveHist.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        private void textBox_history_TextChanged(object sender, EventArgs e)
        {
            label_saveHist.Hide();
            label_history.Hide();
        }

        /// <summary> 交ぜ書き辞書登録 </summary>
        private void button_enterMazegaki_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            var line = textBox_mazegaki.Text.Trim()._reReplace(@" +", " ");
            if (line._reMatch(@"^[^ ]+ ")) {
                frmMain?.ExecCmdDecoder("addMazegakiEntry", line);
                label_saveMaze.Hide();
                label_mazegaki.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            } else {
                SystemHelper.ShowWarningMessageBox("形式が間違っています。\r\n「読み<空白>単語/...」という形式で入力してください。");
            }
        }

        private void button_saveMazegakiFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            frmMain?.ExecCmdDecoder("saveMazegakiDic", null);
            label_mazegaki.Hide();
            label_saveMaze.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        private void textBox_mazegaki_TextChanged(object sender, EventArgs e)
        {

            label_saveMaze.Hide();
            label_mazegaki.Hide();
        }

        /// <summary> 部首連想辞書登録 </summary>
        private void button_enterBushuAssoc_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            var line = textBox_bushuAssoc.Text.Trim().Replace(" ", "");
            if (line._reMatch(@"^[^=]=.")) {
                frmMain?.ExecCmdDecoder("mergeBushuAssocEntry", line);
                button_readBushuAssoc.Hide();
                label_saveAssoc.Hide();
                label_bushuAssoc.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            } else {
                SystemHelper.ShowWarningMessageBox("形式が間違っています。\r\n「文字=文字...」という形式で入力してください。");
            }
        }

        private void button_saveBushuAssocFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            frmMain?.ExecCmdDecoder("saveBushuAssocDic", null);
            button_readBushuAssoc.Hide();
            label_bushuAssoc.Hide();
            label_saveAssoc.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        /// <summary>連想辞書からエントリを読み出す</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_readBushuAssoc_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            if (textBox_bushuAssoc.Text._notEmpty()) {
                var s = textBox_bushuAssoc.Text.Substring(0, 1);
                char[] result = frmMain?.CallDecoderFunc("readBushuAssoc", s);
                if (result._notEmpty()) {
                    var sb = new StringBuilder();
                    sb.Append(s).Append('=');
                    foreach (var ch in result) {
                        if (ch == 0) break;
                        sb.Append(ch);
                    }
                    textBox_bushuAssoc.Text = sb.ToString();
                }
            }
        }

        private void textBox_bushuAssoc_TextChanged(object sender, EventArgs e)
        {
            button_readBushuAssoc.Show();
            label_saveAssoc.Hide();
            label_bushuAssoc.Hide();
        }

        /// <summary> 部首合成辞書登録 </summary>
        private void button_enterBushu_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            var line = textBox_bushuComp.Text.Trim().Replace(" ", "");
            int n = 0;
            foreach (var ch in line) {
                if (Char.IsHighSurrogate(ch)) continue;
                ++n;
            }
            if (n == 2 || n == 3) {
                frmMain?.ExecCmdDecoder("addBushuEntry", line);
                label_saveBushu.Hide();
                label_bushuComp.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            } else {
                SystemHelper.ShowWarningMessageBox("形式が間違っています。\r\n3文字または2文字で入力してください。");
            }
        }

        private void button_saveBushuCompFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            frmMain?.ExecCmdDecoder("saveBushuDic", null);
            label_bushuComp.Hide();
            label_saveBushu.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        private void textBox_bushuComp_TextChanged(object sender, EventArgs e)
        {
            label_saveBushu.Hide();
            label_bushuComp.Hide();
        }

        /// <summary> 自動部首合成辞書登録 </summary>
        private void button_enterAutoBushu_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            var line = textBox_autoBushuComp.Text.Trim().Replace(" ", "");
            int n = 0;
            foreach (var ch in line) {
                if (Char.IsHighSurrogate(ch)) continue;
                ++n;
            }
            if (n == 3) {
                frmMain?.ExecCmdDecoder("addAutoBushuEntry", line);
                label_saveAutoBushu.Hide();
                label_autoBushuComp.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            } else {
                SystemHelper.ShowWarningMessageBox("形式が間違っています。\r\n3文字で入力してください。");
            }
        }

        private void button_saveAutoBushuCompFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            frmMain?.ExecCmdDecoder("saveAutoBushuDic", null);
            label_autoBushuComp.Hide();
            label_saveAutoBushu.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        private void textBox_autoBushuComp_TextChanged(object sender, EventArgs e)
        {
            label_saveAutoBushu.Hide();
            label_autoBushuComp.Hide();
        }

        private void button_registerClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            this.Close();
        }

        //-----------------------------------------------------------------------------------
        // 共通・ヘルパー他
        //-----------------------------------------------------------------------------------
        // 一定時間後にOKリザルトラベルを非表示にする
        int okResultCount = 0;

        private const int okResultCountMax = 5000 / timerInterval;    // 5秒

        private void hideOkResultLabel()
        {
            if (okResultCount > 0) {
                --okResultCount;
                if (okResultCount == 0) {
                    label_okResultBasic.Hide();
                    label_reloadBasic.Hide();
                    label_okResultAdvanced.Hide();
                    label_okResultImeCombo.Hide();
                    label_okResultFontColor.Hide();
                    label_okResultHist.Hide();
                    label_okResultKeyAssign.Hide();
                    label_okResultCtrlKeys.Hide();
                    label_okResultMisc.Hide();
                    label_imeComboReload.Hide();
                    label_keyAssignReload.Hide();
                    label_ctrlReload.Hide();
                    label_miscRomanOut.Hide();
                    label_miscEelllJsOut.Hide();
                    label_miscReload.Hide();
                    label_execResultFile.Hide();
                    label_okResultDevelop.Hide();
                }
            }
        }

        private void label_okResultBasic_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_reloadBasic_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultFontColor_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultAdvanced_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultImeCombo_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultKeyAssign_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultHistory_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultCtrlKeys_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }
        
        private void label_execResultFile_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_imeComboReload_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_keyAssignReload_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_ctrlReload_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_miscReload_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_miscRomanOut_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_miscEelllJsOut_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultMisc_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        //-----------------------------------------------------------------------------------
        // ヘルパー他
        //-----------------------------------------------------------------------------------
        // 一定時間後にリザルトラベルを非表示にする
        int dicRegLabelCount = 0;

        private const int dicRegLabelCountMax = 5000 / timerInterval;    // 5秒

        private void hideDicRegLabel()
        {
            if (dicRegLabelCount > 0) {
                --dicRegLabelCount;
                if (dicRegLabelCount == 0) {
                    label_saveHist.Hide();
                    label_history.Hide();
                    label_saveMaze.Hide();
                    label_mazegaki.Hide();
                    label_saveAssoc.Hide();
                    label_bushuAssoc.Hide();
                    label_saveBushu.Hide();
                    label_bushuComp.Hide();
                    label_saveAutoBushu.Hide();
                    label_autoBushuComp.Hide();
                    button_readBushuAssoc.Show();
                }
            }
        }

        //-----------------------------------------------------------------------------------
        private void radioButton_modeMarker_CheckedChanged(object sender, EventArgs e)
        {
            //checkBox_hideTopText.Enabled = radioButton_modeMarker.Checked;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedTabIndex = tabControl1.SelectedIndex;

            switch (tabControl1.SelectedTab.Name) {
                case "tabPage_basic":
                    AcceptButton = button_basicEnter;
                    CancelButton = button_basicClose;
                    break;
                case "tabPage_advanced":
                    AcceptButton = button_advancedEnter;
                    CancelButton = button_advancedClose;
                    break;
                case "tabPage_imeCombo":
                    AcceptButton = button_imeComboEnter;
                    CancelButton = button_imeComboClose;
                    break;
                case "tabPage_fontColor":
                    AcceptButton = button_fontColorEnter;
                    CancelButton = button_fontColorClose;
                    break;
                case "tabPage_keyAssign":
                    AcceptButton = button_keyAssignEnter;
                    CancelButton = button_keyAssignClose;
                    break;
                case "tabPage_ctrlKeys":
                    AcceptButton = button_ctrlEnter;
                    CancelButton = button_ctrlClose;
                    break;
                case "tabPage_history":
                    AcceptButton = button_histEnter;
                    CancelButton = button_histClose;
                    break;
                case "tabPage_misc":
                    AcceptButton = button_miscEnter;
                    CancelButton = button_miscClose;
                    break;
                case "tabPage_register":
                    AcceptButton = button_registerClose;
                    CancelButton = button_registerClose;
                    break;
                case "tabPage_develop":
                    AcceptButton = button_developEnter;
                    CancelButton = button_developClose;
                    break;
            }
        }

        // フォームタイマー処理
        private void timer1_Tick(object sender, EventArgs e)
        {
            checkerAll.CheckStatus();
            hideOkResultLabel();
            hideDicRegLabel();
        }

        private void button_saveAll_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            frmMain?.SaveAllFiles();
            label_execResultFile.Show();
        }

        private string makeFontSpec(string oldSpec, bool bVertical)
        {
            var result = oldSpec;

            var items = oldSpec._split('|').Select(x => x._strip()).ToArray();

            Font fnt = null;
            var fd = new FontDialog();
            var fontName = items._getNth(0);
            if (fontName._notEmpty()) {
                fnt = new Font(fontName, items._getNth(1)._parseInt(9)._lowLimit(8));
                fd.Font = fnt;
            }
            fd.FontMustExist = true;
            fd.ShowEffects = false;
            if (fd.ShowDialog() == DialogResult.OK) {
                result = $"{(fd.Font.GdiVerticalFont ? "@" : "")}{fd.Font.Name} | {Math.Round(fd.Font.SizeInPoints)}";
                if (bVertical) result = result +$" | {items._getNth(2)._orElse("0")} | {items._getNth(3)._orElse("0")}";
            }
            fd.Dispose();
            fnt?.Dispose();

            return result;
        }

        private void button_normalDlg_Click(object sender, EventArgs e)
        {
            textBox_normalFont.Text = makeFontSpec(textBox_normalFont.Text, true);
        }

        private void button_centerDlg_Click(object sender, EventArgs e)
        {
            textBox_centerFont.Text = makeFontSpec(textBox_centerFont.Text, true);
        }

        private void button_verticalDlg_Click(object sender, EventArgs e)
        {
            textBox_verticalFont.Text = makeFontSpec(textBox_verticalFont.Text, true);
        }

        private void button_horizontalDlg_Click(object sender, EventArgs e)
        {
            textBox_horizontalFont.Text = makeFontSpec(textBox_horizontalFont.Text, false);
        }

        private void button_minibufDlg_Click(object sender, EventArgs e)
        {
            textBox_minibufFont.Text = makeFontSpec(textBox_minibufFont.Text, false);
        }

        private void textBox_dateStringFormat_Enter(object sender, EventArgs e)
        {
            AcceptButton = null;
        }

        private void textBox_dateStringFormat_Leave(object sender, EventArgs e)
        {
            AcceptButton = button_ctrlEnter;
        }

        private void textBox_targetClassNames_Enter(object sender, EventArgs e)
        {
            AcceptButton = null;
        }

        private void textBox_targetClassNames_Leave(object sender, EventArgs e)
        {
            AcceptButton = button_ctrlEnter;
        }

        private void button_restart_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            RestartRequired = true;
            Close();
        }

        private void button_restartWithNoSave_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            RestartRequired = true;
            NoSave = true;
            Close();
        }

        private void checkBox_globalCtrlKeysEnabled_CheckedChanged(object sender, EventArgs e)
        {
            changeGlobalCtrlKeysCheckBoxState();
        }

        private void changeGlobalCtrlKeysCheckBoxState()
        {
            //groupBox_globalCtrlKeys.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_backSpaceKey.Enabled = comboBox_backSpaceKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_deleteKey.Enabled = comboBox_deleteKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_leftArrowKey.Enabled = comboBox_leftArrowKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_rightArrowKey.Enabled = comboBox_rightArrowKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_upArrowKey.Enabled = comboBox_upArrowKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_downArrowKey.Enabled = comboBox_downArrowKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_escKey.Enabled = comboBox_escKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_tabKey.Enabled = comboBox_tabKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_enterKey.Enabled = comboBox_enterKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_homeKey.Enabled = comboBox_homeKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_endKey.Enabled = comboBox_endKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            //checkBox_dateStringKey.Enabled = comboBox_dateStringKey.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
        }

        private void button_document_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openDocumentUrl(Settings.FaqUrl);
        }

        private void button_imeFAQ_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openDocumentUrl(Settings.FaqBasicUrl);
        }

        private void radioButton_normalVkb_CheckedChanged(object sender, EventArgs e)
        {
            textBox_vkbShowStrokeCount.Enabled = radioButton_normalVkb.Checked;
        }

        /// <summary>
        /// Ctrlキー割り当てで、ドロップダウンに使われる項目
        /// </summary>
        private string[] ctrlKeyItems = new string[] {
            "なし",
            "SPACE",
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
            "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T",
            "U", "V", "W", "X", "Y", "Z",
            "COLON (:)",    // ba
            "PLUS (+)",     // bb
            "COMMA (,)",    // bc
            "MINUS (-)",    // bd
            "PERIOD (.)",   // be
            "SLASH (/)",    // bf
            "BQUOTE (`)",   // c0/106
            "OEM4 ([)",     // db
            "OEM5 (|)",     // dc
            "OEM6 (]})",    // dd
            "OEM7 (^ ')",   // de
            "OEM102 (\\)",  // e2/106
        };

        private string[] getCtrlKeyItems()
        {
            //if (ctrlKeyItems._isEmpty()) {
            //    var items = new List<string>(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" });
            //    char[] data = frmMain?.CallDecoderFunc("getCharsOrderedByDeckey", null);
            //    if (data._notEmpty()) {
            //        for (int i = 0; i < DecoderKeys.NUM_STROKE_DECKEY; ++i) {
            //            if ((data[i] > ' ' && data[i] < '0') || (data[i] > '9' && data[i] < 'A') || (data[i] > 'Z' && data[i] < 'a') || data[i] > 'z') {
            //                items.Add($"{data[i]}{data[i+DecoderKeys.NUM_STROKE_DECKEY]}");
            //            }
            //        }
            //    }
            //    ctrlKeyItems = items.Select(x => " " + x).ToArray();
            //}
            return ctrlKeyItems;
        }

        private void comboBox_ctrlKey_setItems(ComboBox comboBox)
        {
            var text = comboBox.Text;
            if (comboBox.Items.Count < 2) {
                comboBox.Items.Clear();
                comboBox.Items.AddRange(getCtrlKeyItems());
                if (text._notEmpty()) comboBox_selectCtrlKeyItem(comboBox, text);
            }
        }

        private void comboBox_selectCtrlKeyItem(ComboBox combo, string key)
        {
            for (int idx = 0; idx < ctrlKeyItems.Length; ++idx) {
                var item = ctrlKeyItems[idx];
                if (item.StartsWith(key) && (item.Length == key.Length || item[key.Length] == ' ')) {
                    if (combo.Items.Count > idx) {
                        combo.SelectedIndex = idx;
                    } else {
                        combo.Items.Add(item);
                        combo.SelectedIndex = 0;
                    }
                }
            }
        }

        private void button_showPaddingsDesc_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            if (frmVkb != null) {
                var dlg = new DlgPaddingsDesc(frmVkb.GetPaddingsDesc());
                dlg.ShowDialog();
                dlg.Dispose();
            }
        }

        private void initializeCtrlKeyConversionComboBox()
        {
            comboBox_backSpaceKey.Enabled = checkBox_backSpaceKey.Checked;
            comboBox_deleteKey.Enabled = checkBox_deleteKey.Checked;
            comboBox_leftArrowKey.Enabled = checkBox_leftArrowKey.Checked;
            comboBox_rightArrowKey.Enabled = checkBox_rightArrowKey.Checked;
            comboBox_upArrowKey.Enabled = checkBox_upArrowKey.Checked;
            comboBox_downArrowKey.Enabled = checkBox_downArrowKey.Checked;
            comboBox_escKey.Enabled = checkBox_escKey.Checked;
            comboBox_tabKey.Enabled = checkBox_tabKey.Checked;
            comboBox_enterKey.Enabled = checkBox_enterKey.Checked;
            comboBox_homeKey.Enabled = checkBox_homeKey.Checked;
            comboBox_endKey.Enabled = checkBox_endKey.Checked;
            comboBox_dateStringKey.Enabled = checkBox_dateStringKey.Checked;
        }

        private void checkBox_backSpaceKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_backSpaceKey.Enabled = checkBox_backSpaceKey.Checked;
        }

        private void checkBox_deleteKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_deleteKey.Enabled = checkBox_deleteKey.Checked;
        }

        private void checkBox_leftArrowKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_leftArrowKey.Enabled = checkBox_leftArrowKey.Checked;
        }

        private void checkBox_rightArrowKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_rightArrowKey.Enabled = checkBox_rightArrowKey.Checked;
        }

        private void checkBox_upArrowKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_upArrowKey.Enabled = checkBox_upArrowKey.Checked;
        }

        private void checkBox_downArrowKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_downArrowKey.Enabled = checkBox_downArrowKey.Checked;
        }

        private void checkBox_escKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_escKey.Enabled = checkBox_escKey.Checked;
        }

        private void checkBox_tabKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_tabKey.Enabled = checkBox_tabKey.Checked;
        }

        private void checkBox_enterKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_enterKey.Enabled = checkBox_enterKey.Checked;
        }

        private void checkBox_homeKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_homeKey.Enabled = checkBox_homeKey.Checked;
        }

        private void checkBox_endKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_endKey.Enabled = checkBox_endKey.Checked;
        }

        private void checkBox_dateStringKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_dateStringKey.Enabled = checkBox_dateStringKey.Checked;
        }

        private void comboBox_backSpaceKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_backSpaceKey);
        }

        private void comboBox_deleteKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_deleteKey);
        }

        private void comboBox_leftArrowKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_leftArrowKey);
        }

        private void comboBox_rightArrowKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_rightArrowKey);
        }

        private void comboBox_upArrowKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_upArrowKey);
        }

        private void comboBox_downArrowKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_downArrowKey);
        }

        private void comboBox_homeKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_escKey);
        }

        private void comboBox_endKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_tabKey);
        }

        private void comboBox_enterKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_enterKey);
        }

        private void comboBox_homeKey_DropDown_1(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_homeKey);
        }

        private void comboBox_endKey_DropDown_1(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_endKey);
        }

        private void comboBox_fullEscapeKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_fullEscapeKey);
        }

        private void comboBox_strokeHelpRotationKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_strokeHelpRotationKey);
        }

        private void comboBox_historySearchKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_historySearchKey);
        }

        private void checkBox_historySearchKey_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_historySearchKey.Enabled = checkBox_historySearchKey.Checked;
        }

        private void comboBox_dateStringKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_dateStringKey);
        }

        private void button_openKeyboardFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            var filename = getKeyboardFileName(comboBox_keyboardFile.Text);
            if (filename._notEmpty() && filename._toLower() != "jp" && filename._toLower() != "us") {
                openFileByTxtAssociatedProgram(Settings.KeyboardFileDir._joinPath(filename));
            }
        }

        private void button_openTableFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openFileByTxtAssociatedProgram(getTableFileName(comboBox_tableFile.Text));
        }

        private void button_openTableFile2_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openFileByTxtAssociatedProgram(getTableFileName(comboBox_tableFile2.Text));
        }

        private void button_openKeyCharMapFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            var filename = getKeyboardFileName(comboBox_deckeyCharsFile.Text);
            if (filename._notEmpty()) {
                openFileByTxtAssociatedProgram(Settings.KeyboardFileDir._joinPath(filename));
            }
        }

        private void button_openEasyCharsFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openFileByTxtAssociatedProgram(Settings.EasyCharsFile);
        }

        private void button_openStrokeHelpFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openFileByTxtAssociatedProgram(Settings.StrokeHelpFile);
        }

        private void button_openBushuCompFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openFileByTxtAssociatedProgram(Settings.BushuFile);
            openFileByTxtAssociatedProgram(Settings.AutoBushuFile);
        }

        private void button_bushuAssocFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openFileByTxtAssociatedProgram(Settings.BushuAssocFile);
        }

        private void button_openMazeFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openFileByTxtAssociatedProgram(Settings.MazegakiFile._safeReplace("*", "user"));
        }

        private void button_openHistoryFile_Click(object sender, EventArgs e)
        {
            logger.InfoH("CALLED");
            openFileByTxtAssociatedProgram(Settings.HistoryFile._safeReplace("*", "entry"));
        }

        private void button_setModConversion_Click(object sender, EventArgs e)
        {
            int nameColWidth = Settings.AssignedKeyOrFuncNameColWidth;
            int descColWidth = Settings.AssignedKeyOrFuncDescColWidth;
            using (var dlg = new DlgModConversion()) {
                if (dlg.ShowDialog() == DialogResult.OK) {
                    // 設定内容を mod-conversion.txt に書き出す
                    writeModConversionSettings();
                    // SandSのシフト面のコンボボックスを置き換える
                    comboBox_SandSAssignedPlane.SelectedIndex = Settings.SandSAssignedPlane._lowLimit(0)._highLimit(7);
                }
                int width = dlg.Width;
                logger.InfoH(() => $"DlgModConversionWidth={width}");
                if (Settings.DlgModConversionWidth != width) {
                    Settings.SetUserIni("dlgModConversionWidth", width);
                    Settings.DlgModConversionWidth = width;
                }
                int height = dlg.Height;
                logger.InfoH(() => $"DlgModConversionHeight={height}");
                if (Settings.DlgModConversionHeight != height) {
                    Settings.SetUserIni("dlgModConversionHeight", height);
                    Settings.DlgModConversionHeight = height;
                }
                logger.InfoH(() => $"AssignedKeyOrFuncColNameWidth={nameColWidth}");
                if (Settings.AssignedKeyOrFuncNameColWidth != nameColWidth) {
                    Settings.SetUserIni("assignedKeyOrFuncNameColWidth", Settings.AssignedKeyOrFuncNameColWidth);
                }
                logger.InfoH(() => $"AssignedKeyOrFuncColDescWidth={descColWidth}");
                if (Settings.AssignedKeyOrFuncDescColWidth != descColWidth) {
                    Settings.SetUserIni("assignedKeyOrFuncDescColWidth", Settings.AssignedKeyOrFuncDescColWidth);
                }
            }
        }

        // 拡張修飾キー設定をファイルに書き出す
        private void writeModConversionSettings()
        {
            var path = KanchokuIni.Singleton.KanchokuDir._joinPath(textBox_modConversionFile.Text);
            logger.InfoH($"ENTER: path={path}");
            try {
                using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite)) {
                    using (var sw = new System.IO.StreamWriter(fs, Encoding.UTF8)) {
                        sw.Write(ExtraModifiers.MakeModConversionContents());
                    }
                }
            } catch (Exception e) {
                logger.Error(e._getErrorMsg());
            }
            logger.InfoH($"LEAVE");
        }

        // 打鍵ログ表示
        private void button_showDlgStrokeLog_Click(object sender, EventArgs e)
        {
            frmMain?.ShowDlgStrokeLog(this, Right - 10, Top);
        }

        private void comboBox_vkbShowHideTemporaryKey_DropDown(object sender, EventArgs e)
        {
            comboBox_ctrlKey_setItems(comboBox_vkbShowHideTemporaryKey);
        }

    }
}

