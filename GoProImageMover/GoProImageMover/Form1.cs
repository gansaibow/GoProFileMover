using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using GoProImageMover.Data;

namespace GoProImageMover
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string savefile = "app.xml";

        //コントロールの値を変更するためのデリゲート
        private delegate void SetProgressValueDelegate(int num);
        //バックグラウンド処理が終わった時にコントロールの値を変更するためのデリゲート
        private delegate void ThreadCompletedDelegate();
        //処理がキャンセルされた時にコントロールの値を変更するためのデリゲート
        private delegate void ThreadCanceledDelegate();

        //キャンセルボタンがクリックされたかを示すフラッグ
        private volatile bool canceled = false;
        //別処理をするためのスレッド
        private System.Threading.Thread fileMoveThread;

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(savefile))
            {
                DataAppInfo info = CustomXMLSerializer.LoadXmlData<DataAppInfo>(savefile);

                cmbDrive1.Text = info.Driver1;
                cmbDrive2.Text = info.Driver2;
                cmbDrive3.Text = info.Driver3;
                cmbDrive4.Text = info.Driver4;
                cmbDrive5.Text = info.Driver5;
                cmbDrive6.Text = info.Driver6;

                txtGoProPath.Text = info.GoProPath;
                txtPCPath.Text = info.PCPath;
                numTake.Value = info.take;
                txtFileFomat.Text = info.format;
            }

            // 論理ドライブ名をすべて取得する
            string[] stDrives = System.IO.Directory.GetLogicalDrives();

            ReloadDriveNames();
        }

        private void ReloadDriveNames()
        {
            cmbDrive1.Items.Clear();
            cmbDrive2.Items.Clear();
            cmbDrive3.Items.Clear();
            cmbDrive4.Items.Clear();
            cmbDrive5.Items.Clear();
            cmbDrive6.Items.Clear();
            // 論理ドライブ名をすべて取得する
            string[] stDrives = System.IO.Directory.GetLogicalDrives();

            foreach (string stDrive in stDrives)
            {
                string name = CustomString.Left(stDrive, 1);
                cmbDrive1.Items.Add(name);
                cmbDrive2.Items.Add(name);
                cmbDrive3.Items.Add(name);
                cmbDrive4.Items.Add(name);
                cmbDrive5.Items.Add(name);
                cmbDrive6.Items.Add(name);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnSave.Enabled = false;
            //grpFileInfo.Enabled = false;
            //grpDriveInfo.Enabled = false;
            btnStop.Enabled = true;

            lblMessage.Text = "";

            if (txtGoProPath.Text.Equals(string.Empty))
            {
                MessageBox.Show("GoPro Pathを入力してください。",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtGoProPath.BackColor = Color.Red;
                return;
            }

            txtGoProPath.BackColor = Color.FromKnownColor(KnownColor.Window);

            if (txtPCPath.Text.Equals(string.Empty))
            {
                MessageBox.Show("PC Pathを入力してください。",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtPCPath.BackColor = Color.Red;
                return;
            }

            if (!Directory.Exists(txtPCPath.Text))
            {
                MessageBox.Show("PC Pathのフォルダがありません",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtPCPath.BackColor = Color.Red;
                return;
            }
            txtPCPath.BackColor = Color.FromKnownColor(KnownColor.Window);

            if (txtFileFomat.Text.Equals(string.Empty))
            {
                MessageBox.Show("ファイル名フォーマットを入力してください。",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtFileFomat.BackColor = Color.Red;
                return;
            }
            txtFileFomat.BackColor = Color.FromKnownColor(KnownColor.Window);

            List<string> drives = new List<string>();
            drives.Add(cmbDrive1.Text);
            drives.Add(cmbDrive2.Text);
            drives.Add(cmbDrive3.Text);
            drives.Add(cmbDrive4.Text);
            drives.Add(cmbDrive5.Text);
            drives.Add(cmbDrive6.Text);

            //DrivesFileMoveメソッドを別スレッドで実行する
            fileMoveThread = new System.Threading.Thread(
                new System.Threading.ParameterizedThreadStart(DrivesFileMove));
            fileMoveThread.IsBackground = true;
            fileMoveThread.Start((object)drives);
        }

        private void DrivesFileMove(object args)
        {
            //デリゲートの作成
            SetProgressValueDelegate progressDlg =
                new SetProgressValueDelegate(SetProgressValue);
            ThreadCompletedDelegate completeDlg =
                new ThreadCompletedDelegate(ThreadCompleted);
            ThreadCanceledDelegate canceledDlg =
                new ThreadCanceledDelegate(ThreadCanceled);

            
            string takePath = txtPCPath.Text + "/" + numTake.Value.ToString();
            // Takeフォルダの存在確認
            if (!Directory.Exists(takePath))
            {
                // Takeフォルダを作成する
                Directory.CreateDirectory(takePath);
            }

            List<string> drives = (List<string>)args;
            int i = 1;

            foreach (string drive in drives)
            {
                //コントロールの表示を変更する
                this.Invoke(progressDlg, new object[] { i });
                //キャンセルボタンがクリックされたか調べる
                if (canceled)
                {
                    //キャンセルされたときにコントロールの値を変更する
                    this.Invoke(canceledDlg);
                    canceled = false;
                    //処理を終了させる
                    return;
                }

                // 入力確認
                if (!drive.Equals(string.Empty))
                {
                    // ドライブの存在確認
                    if (Directory.Exists(drive + ":/"))
                    {
                        Console.WriteLine(string.Format("{0}ドライブが存在する：{1}", drive, i));

                        string goproSrcDirPath = string.Format("{0}:/{1}", drive, txtGoProPath.Text);
                        // GoProの指定Pathの存在確認
                        if (Directory.Exists(goproSrcDirPath))
                        {
                            Console.WriteLine(string.Format("GoPro側のPathが存在する　：{0}", goproSrcDirPath));
                            FileMove(goproSrcDirPath, takePath, txtFileFomat.Text, i);
                        }
                        else
                        {
                            Console.WriteLine(string.Format("GoPro側のPathが存在しない：{0}", goproSrcDirPath));
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0}ドライブが存在しない：{1}", drive, i));
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("ドライブ名入力なし：{0}", i));
                }

                i++;
            }
            //完了したときにコントロールの値を変更する
            this.Invoke(completeDlg);
        }

        private void FileMove(string src, string dest, string format, int index)
        {

            string[] files = System.IO.Directory.GetFiles(
                src, "*", System.IO.SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                //キャンセルボタンがクリックされたか調べる
                if (canceled)
                {
                    //処理を終了させる
                    return;
                }

                string filename = Path.GetFileName(file);
                string copyfile = string.Format(dest + "/" + format + filename, index);

                System.IO.File.Copy(file, copyfile, true);
                System.IO.File.Delete(file);
            }
        }


        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;

            //キャンセルのフラッグを立てる
            canceled = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveAppInfo();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAppInfo();
        }

        private void SaveAppInfo()
        {
            DataAppInfo info = new DataAppInfo();
            info.Driver1 = cmbDrive1.Text;
            info.Driver2 = cmbDrive2.Text;
            info.Driver3 = cmbDrive3.Text;
            info.Driver4 = cmbDrive4.Text;
            info.Driver5 = cmbDrive5.Text;
            info.Driver6 = cmbDrive6.Text;

            info.GoProPath = txtGoProPath.Text;
            info.PCPath = txtPCPath.Text;
            info.take = (int)numTake.Value;
            info.format = txtFileFomat.Text;

            CustomXMLSerializer.SaveXmlData<DataAppInfo>(info, savefile);
        }

        //コントロールの値を変更する
        private void SetProgressValue(int num)
        {
            //ProgressBar1の値を変更する
            //ProgressBar1.Value = num;
            //Label1のテキストを変更する
            lblMessage.Text = string.Format("処理中：{0}/6", num);
        }

        //処理が完了した時にコントロールの値を変更する
        private void ThreadCompleted()
        {
            numTake.Value++;
            lblMessage.Text = "完了しました";
            canceled = false;
            btnStop.Enabled = false;
            btnSave.Enabled = true;
            btnStart.Enabled = true;
            grpFileInfo.Enabled = true;
            //grpDriveInfo.Enabled = true;
        }

        //処理がキャンセルされた時にコントロールの値を変更する
        private void ThreadCanceled()
        {
            lblMessage.Text = "キャンセルされました。";
            btnStop.Enabled = false;
            btnSave.Enabled = true;
            btnStart.Enabled = true;
            grpFileInfo.Enabled = true;
            //grpDriveInfo.Enabled = true;
        }

        private void btnRef_Click(object sender, EventArgs e)
        {
            // FolderBrowserDialog の新しいインスタンスを生成する (デザイナから追加している場合は必要ない)
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();

            // ダイアログの説明を設定する
            folderBrowserDialog1.Description = "PCの保存先を選択してください。";

            // ルートになる特殊フォルダを設定する (初期値 SpecialFolder.Desktop)
            folderBrowserDialog1.RootFolder = System.Environment.SpecialFolder.MyComputer;

            // 初期選択するパスを設定する
            folderBrowserDialog1.SelectedPath = @"C:\Program Files\";

            // [新しいフォルダ] ボタンを表示する (初期値 true)
            //folderBrowserDialog1.ShowNewFolderButton = true;

            // ダイアログを表示し、戻り値が [OK] の場合は、選択したディレクトリを表示する
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtPCPath.Text = folderBrowserDialog1.SelectedPath;
            }

            // 不要になった時点で破棄する (正しくは オブジェクトの破棄を保証する を参照)
            folderBrowserDialog1.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ReloadDriveNames();
        }
    }
}
