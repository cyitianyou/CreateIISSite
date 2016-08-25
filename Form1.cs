using IronPython.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreateIISSite
{
    public partial class Form1 : Form
    {
        List<KeyValuePair<int, string>> list;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int port = Convert.ToInt32(this.txt_Port.Text);
                string path = this.txt_Path.Text;
                IIS7Helper help = new IIS7Helper(port, path);
                help.CreateSite();
                MessageBox.Show("创建成功");
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            try
            {
                string path = this.txt_Path.Text;
                string oldSite = this.textBox2.Text;
                if (!Directory.Exists(Path.Combine(path, oldSite))) throw new Exception("原网站不存在");
                foreach (var item in list)
                {
                    WriteLine(string.Format("{0}:", item.Key));
                    WriteLine(string.Format("网站[{0}]开始复制文件。。。", item.Value));
                    CopyDir(Path.Combine(path, oldSite), Path.Combine(path, item.Value));
                    WriteLine(string.Format("网站[{0}]复制文件完成。", item.Value));
                    IIS7Helper help = new IIS7Helper(item.Key, Path.Combine(path, item.Value));
                    WriteLine(string.Format("网站[{0}]开始创建。。。", item.Value));
                    help.CreateSite();
                    WriteLine(string.Format("网站[{0}]创建成功", item.Value));
                }

            }
            catch (Exception error)
            {

                this.textBox1.Text += error.Message + System.Environment.NewLine;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string path = this.txt_Path.Text;
                foreach (var item in list)
                {
                    IIS7Helper help = new IIS7Helper(item.Key, Path.Combine(path, item.Value));
                    WriteLine(string.Format("网站[{0}]开始修改配置文件。。。", item.Value));
                    help.EditServiceInformation(string.Format(":{0}", this.txt_Port.Text), item.Key);
                    WriteLine(string.Format("网站[{0}]修改配置文件完成", item.Value));
                }

            }
            catch (Exception error)
            {

                this.textBox1.Text += error.Message + System.Environment.NewLine;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DownFile df = new DownFile();
            df.userName = "avauser";
            df.password = "Aa123456";
            df.domain = "avatech";
            df.uriString = "http://ibas-dev.avatech.com.cn:8866/ibas/modules/";
            df.Analysis();
            this.textBox1.Text = "";
            foreach (var item in df.fileList)
            {
                this.textBox1.Text += item + System.Environment.NewLine;
            }
            df.uriString = "http://ibas-dev.avatech.com.cn:8866/ibas/shell/";
            df.Analysis();
            foreach (var item in df.fileList)
            {
                this.textBox1.Text += item + System.Environment.NewLine;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            list = new List<KeyValuePair<int, string>>();
            ToolTip toolTip1 = new ToolTip();
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(this.button1, "需要提供网站根目录,端口");
            toolTip1.SetToolTip(this.button2, "根目录填网站的父级目录,原网站填网站文件夹名称");
            toolTip1.SetToolTip(this.button2, "根目录填网站的父级目录,端口填原网站端口,原网站填网站文件夹名称");
        }

        /// <summary>
        /// 将整个文件夹复制到目标文件夹中。
        /// </summary>
        /// <param name="srcPath">源文件夹</param>
        /// <param name="aimPath">目标文件夹</param>
        public void CopyDir(string srcPath, string aimPath)
        {
            try
            {
                // 检查目标目录是否以目录分割字符结束如果不是则添加之
                if (aimPath[aimPath.Length - 1] != Path.DirectorySeparatorChar)
                    aimPath += Path.DirectorySeparatorChar;
                // 判断目标目录是否存在如果不存在则新建之
                if (!Directory.Exists(aimPath))
                    Directory.CreateDirectory(aimPath);
                // 得到源目录的文件列表，该里面是包含文件以及目录路径的一个数组
                // 如果你指向copy目标文件下面的文件而不包含目录请使用下面的方法
                // string[] fileList = Directory.GetFiles(srcPath);
                string[] fileList = Directory.GetFileSystemEntries(srcPath);
                // 遍历所有的文件和目录
                foreach (string file in fileList)
                {
                    // 先当作目录处理如果存在这个目录就递归Copy该目录下面的文件
                    if (Directory.Exists(file))
                        CopyDir(file, aimPath + Path.GetFileName(file));
                    // 否则直接Copy文件
                    else
                        File.Copy(file, aimPath + Path.GetFileName(file), true);
                }

            }
            catch
            {
                throw;
            }
        }

        public void WriteLine(string msg)
        {
            this.textBox1.Text += msg + System.Environment.NewLine;
            Application.DoEvents();
        }

        private void button4_Click_2(object sender, EventArgs e)
        {
            try
            {
                var txt = this.textBox1.Text.Replace("\r", "");
                var webs = txt.Split('\n');
                var tmp = new List<KeyValuePair<int, string>>();
                foreach (var item in webs)
                {
                    var web = item.Split(',');
                    if (web.Length != 2) web = item.Split('，');
                    if (web.Length != 2) continue;
                    int port;
                    if (!Int32.TryParse(web[0], out port)) continue;
                    tmp.Add(new KeyValuePair<int, string>(port, web[1]));
                }
                list = tmp;
                this.textBox1.Text = "网站列表:" + System.Environment.NewLine;
                foreach (var item in list)
                {
                    this.textBox1.Text += string.Format("{0},{1}", item.Key, item.Value) + System.Environment.NewLine;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(string.Format("获取网站列表出错,错误信息:{0}", error.Message));
            }

        }




    }

    public class SiteInfo
    {
        public int Port;
        public string Path;
    }

}
