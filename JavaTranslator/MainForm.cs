using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JavaTranslator
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        private string appPath { get; set; } = Path.GetDirectoryName(Application.ExecutablePath);
        private string JarPath { get; set; }
        private IList<DataTable> ListDt = new List<DataTable>();

        private bool IsModify = false;

        public Action<string> OnListFile;


        private void MainForm_Load(object sender, EventArgs e)
        {
        }
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;

        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var str = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            FileInfo info = new FileInfo(str);
            if ((info.Attributes & FileAttributes.Directory) != 0)
            {
                ListFlie(str);
            }
            //若为文件，则获取文件名  
            else if (File.Exists(str))
            {
                JarPath = str;
                if (Path.GetExtension(JarPath) ==".jar")
                {
                    GetFlieList();

                }
            }

        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsModify)
            {
                DialogResult TS = MessageBox.Show("修改尚未保存，是否退出？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (TS == DialogResult.Yes)
                    e.Cancel = false;
                else
                    e.Cancel = true;
                return;
            }
        }

        private void OpenBtn_Click(object sender, EventArgs e)
        {
            if (OfdJar.ShowDialog() == DialogResult.OK)
            {
                JarPath = OfdJar.FileName;

                GetFlieList();

            }
        }


        private void LoadDicBtn_Click(object sender, EventArgs e)
        {
            if (OfdJar.ShowDialog() == DialogResult.OK)
            {
                var index = listBox1.SelectedIndex;
                RunAsync(() =>
                {
                    LoadDic(OfdJar.FileName, index);
                });
            }
            
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {

            IsModify = false;
        }
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var s = listBox1.SelectedIndex;
            dataGridView1.DataSource = ListDt[s];
            dataGridView1.Columns["字段"].ReadOnly = true;
            dataGridView1.Columns["原文"].ReadOnly = true;
            dataGridView1.Columns["字段"].Width = 160;
            dataGridView1.Columns["原文"].Width = 160;
            dataGridView1.Columns["译文"].Width = 160;
            
        }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow.Index >= 0)
            {
                textBox1.Text = dataGridView1.CurrentRow.Cells[1].Value.ToString();
                textBox2.Text = dataGridView1.CurrentRow.Cells[2].Value.ToString();
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            var str = textBox2.Text;
            ListDt[listBox1.SelectedIndex].Rows[dataGridView1.CurrentRow.Index][2] = textBox2.Text;
            IsModify = true;
        }

        private void GetFlieList()
        {
            string unzipPath = Path.Combine(appPath, @"unzip.exe");
            if (!File.Exists(unzipPath))
            {
                return;
            }

            var o = Path.GetDirectoryName(JarPath);
            var startInfo = new ProcessStartInfo(unzipPath);
            string args = string.Format(" \"{0}\" \"messages/*.*\" -d {1} -o", JarPath, o);
            startInfo.Arguments = args;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit(2000);
            }
            //var o = Path.GetDirectoryName(JarPath);
            var path = Path.Combine(o, @"messages");

            ListFlie(path);

        }

        private void ListFlie(string path)
        {
            var files = Directory.GetFiles(path, "*.properties");

            OnListFile = (s) => {
                RunInMainthread(() => {
                    listBox1.Items.Add(s);
                });
            };
            RunAsync(() => {
                foreach (var file in files)
                {
                    var s = Path.GetFileName(file);
                    OnListFile?.Invoke(s);
                    //listBox1.Items.Add(s);
                    DataTable dt = new DataTable();
                    dt.Columns.Add(new DataColumn("字段"));
                    dt.Columns.Add(new DataColumn("原文"));
                    dt.Columns.Add(new DataColumn("译文"));

                    ListDt.Add(dt);
                    using (var sr = new StreamReader(file, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (GetValue(line) != null)
                            {
                                var s1 = GetValue(line)[0];
                                var s2 = GetValue(line)[1];
                                dt.Rows.Add(new object[] { s1, s2, "" });
                            }
                        }
                        sr.Close();
                    }
                }
            });

        }

        private void LoadDic(string path,int index)
        {
            using (var sr = new StreamReader(path, Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (GetValue(line) != null)
                    {
                        var s1 = GetValue(line)[0];
                        var s2 = GetValue(line)[1];
                        foreach (DataRow item in ListDt[index].Rows)
                        {
                            if (item[0].ToString()== s1)
                            {
                                item[2] = s2;
                            }
                        }
                    }
                }
                sr.Close();
            }
        }


        private string[] GetValue(string Str)
        {
            string[] words=null;
            if (Str.Contains('='))
            {
                words = Str.Split('=');               
            }
            return words;
        }

        private void SaveFile()
        {

        }

        void RunAsync(Action action)
        {
            ((Action)(delegate () {
                action?.Invoke();
            })).BeginInvoke(null, null);
        }

        void RunInMainthread(Action action)
        {
            this.BeginInvoke((Action)(delegate () {
                action?.Invoke();
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
           textBox2.Text= gb2312_Unicode(textBox1.Text );
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.Text = Unicode_gb2312(textBox1.Text);        

        }
        public static string gb2312_Unicode(string text)
        {
            string str = text;
            string outStr = "";
            if (!string.IsNullOrEmpty(str))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    //将中文字符转为10进制整数，然后转为16进制unicode字符  
                    outStr += "\\u" + ((int)str[i]).ToString("x");
                }
            }
            return outStr;
        }

        public static string Unicode_gb2312(string text)
        {
            string str = text;
            string outStr = "";

            if (!string.IsNullOrEmpty(str))
            {
                string[] strlist = str.Replace("\\", "").Split('u');
                try
                {
                    for (int i = 1; i < strlist.Length; i++)
                    {
                        //将unicode字符转为10进制整数，然后转为char中文字符  
                        outStr += (char)int.Parse(strlist[i], System.Globalization.NumberStyles.HexNumber);
                    }
                }
                catch (FormatException ex)
                {
                    outStr = ex.Message;
                }
            }
            return outStr;
        }


    }
}
