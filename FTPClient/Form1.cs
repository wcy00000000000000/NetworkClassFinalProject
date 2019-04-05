using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FTPClient
{
    public partial class Form1 : Form
    {
        private FtpWebRequest ftpWebRequest;
        private FtpWebResponse ftpWebResponse;
        private String localPath = null;
        private String ip;
        private String user;
        private String pwd;
        private String ftpPath;
        private String filePath;
        private Stream readStream = null;
        private FileStream writeStream = null;
        private long totalByte = 0;
        private bool isUpload = false;


        public Form1()
        {
            InitializeComponent();
        }

        private void 帮助ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请先输入要连接的FTP服务器的ip地址及连接使用的用户名和密码连接至服务器\n" +
                "左边是本地的文件，右边是服务器的文件\n可以将文件上传至服务器或从服务器下载所需文件\n" +
                "选择本地目录时请不要选择系统文件夹\n本FTP客户端支持断点续传，可以随时暂停与继续", "帮助");
        }

        private void 上传ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Upload();
        }

        private void 下载ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Download();
        }

        private void 暂停ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void 继续ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Resume();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text.Equals("连接"))
            {
                ip = textBox1.Text;
                user = textBox2.Text;
                pwd = textBox3.Text;
                ftpPath = "ftp://" + ip;
                上传ToolStripMenuItem.Enabled = true;
                下载ToolStripMenuItem.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button1.Text = "断开";
                TreeNode root = new TreeNode("/");
                root.Tag = ftpPath + "/";
                root.Text = "/";
                treeView2.Nodes.Add(root);
                FillFTPTree(root, ftpPath);
            }
            else if (button1.Text.Equals("断开"))
            {
                上传ToolStripMenuItem.Enabled = false;
                下载ToolStripMenuItem.Enabled = false;
                暂停ToolStripMenuItem.Enabled = false;
                继续ToolStripMenuItem.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                treeView2.Nodes.Clear();
                button1.Text = "连接";
                if (ftpWebRequest != null)
                {
                    ftpWebRequest.Abort();
                    ftpWebRequest = null;
                }
                if (ftpWebResponse != null)
                {
                    ftpWebResponse.Close();
                    ftpWebResponse = null;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Upload();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Download();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Resume();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                localPath = folderBrowserDialog.SelectedPath;
                FillTreeView(treeView1, folderBrowserDialog.SelectedPath);
            }
        }

        private void FillTreeView(TreeView tree, String path)
        {
            try
            {
                tree.Nodes.Clear();
                TreeNode root = new TreeNode(path);
                root.Tag = path;
                root.Text = path;
                tree.Nodes.Add(root);
                FillTreeViewNode(root, path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        private void FillTreeViewNode(TreeNode treeNode, String path)
        {
            if (Directory.Exists(path) == false)
            {
                return;
            }
            DirectoryInfo directory = new DirectoryInfo(path);
            FileInfo[] fileInfos = directory.GetFiles();
            DirectoryInfo[] dirs = directory.GetDirectories();
            int i = 0;
            foreach (DirectoryInfo d in dirs)
            {
                TreeNode node = new TreeNode();
                node.Text = d.Name;
                node.Tag = path + "\\" + d.Name;
                treeNode.Nodes.Add(node);
                string subPath = path + "\\" + d.Name;
                FillTreeViewNode(treeNode.Nodes[i], subPath);
                i++;
            }
            foreach (FileInfo f in fileInfos)
            {
                TreeNode node = new TreeNode();
                node.Text = f.Name;
                node.Tag = path + "\\" + f.Name;
                treeNode.Nodes.Add(node);
            }
        }

        private void Upload()
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0)
            {
                MessageBox.Show("请选择需要上传的文件", "提示");
                return;
            }
            isUpload = true;
            filePath = treeView1.SelectedNode.Tag.ToString();
            暂停ToolStripMenuItem.Enabled = true;
            上传ToolStripMenuItem.Enabled = false;
            下载ToolStripMenuItem.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = true;
            FileInfo fileInfo = new FileInfo(filePath);
            totalByte = (long)fileInfo.Length;
            string file = fileInfo.Name;
            long startPosition = 0;
            long startByte = startPosition;
            int percent = (int)(startByte * 100 / totalByte);
            label4.Text = percent + "%";
            Application.DoEvents();
            ftpWebRequest = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + user + "@" + ip + "/" + file));
            ftpWebRequest.Credentials = new NetworkCredential(user, pwd);
            ftpWebRequest.KeepAlive = false;
            ftpWebRequest.Method = WebRequestMethods.Ftp.AppendFile;
            ftpWebRequest.UseBinary = true;
            ftpWebRequest.ContentLength = fileInfo.Length;
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            writeStream = fileInfo.OpenRead();
            try
            {
                readStream = ftpWebRequest.GetRequestStream();
                writeStream.Seek(startPosition, 0);
                int contentLen = writeStream.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    readStream.Write(buff, 0, contentLen);
                    contentLen = writeStream.Read(buff, 0, buffLength);
                    startByte += contentLen;
                    percent = (int)(startByte * 100 / totalByte);
                    label4.Text = percent + "%";
                    Application.DoEvents();
                }
                readStream.Close();
                writeStream.Close();
                暂停ToolStripMenuItem.Enabled = false;
                上传ToolStripMenuItem.Enabled = true;
                下载ToolStripMenuItem.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = false;
                isUpload = false;
                label4.Text = "100%";
                Application.DoEvents();
            }
            catch
            {

            }
            finally
            {
                if (writeStream != null)
                {
                    writeStream.Close();
                }
                if (readStream != null)
                {
                    readStream.Close();
                }
            }
        }

        private void Download()
        {
            if (treeView2.SelectedNode == null || treeView2.SelectedNode.Nodes.Count > 0)
            {
                MessageBox.Show("请选择需要下载的文件", "提示");
                return;
            }
            if (localPath == null || localPath.Length == 0)
            {
                MessageBox.Show("请选择需要下载到的目录", "提示");
                return;
            }
            filePath = treeView2.SelectedNode.Tag.ToString();
            暂停ToolStripMenuItem.Enabled = true;
            上传ToolStripMenuItem.Enabled = false;
            下载ToolStripMenuItem.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = true;
            button5.Enabled = false;
            FtpWebRequest remoteFileLenReq;
            try
            {
                writeStream = new FileStream(localPath + "\\" + treeView2.SelectedNode.Text, FileMode.Append);
                remoteFileLenReq = (FtpWebRequest)FtpWebRequest.Create(filePath);
                remoteFileLenReq.Credentials = new NetworkCredential(user, pwd);
                remoteFileLenReq.UseBinary = true;
                remoteFileLenReq.ContentOffset = 0;
                remoteFileLenReq.Method = WebRequestMethods.Ftp.GetFileSize;
                FtpWebResponse response = (FtpWebResponse)remoteFileLenReq.GetResponse();
                totalByte = response.ContentLength;
                response.Close();
                ftpWebRequest = (FtpWebRequest)FtpWebRequest.Create(filePath);
                ftpWebRequest.Credentials = new NetworkCredential(user, pwd);
                ftpWebRequest.UseBinary = true;
                ftpWebRequest.KeepAlive = false;
                ftpWebRequest.ContentOffset = 0;
                ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
                readStream = ftpWebResponse.GetResponseStream();
                long downloadedByte = 0;
                int bufferSize = 512;
                byte[] btArray = new byte[bufferSize];
                int contentSize = readStream.Read(btArray, 0, btArray.Length);
                while (contentSize > 0)
                {
                    downloadedByte += contentSize;
                    int percent = (int)(downloadedByte * 100 / totalByte);
                    label4.Text = percent + "%";
                    Application.DoEvents();
                    writeStream.Write(btArray, 0, contentSize);
                    contentSize = readStream.Read(btArray, 0, btArray.Length);
                }
                暂停ToolStripMenuItem.Enabled = false;
                上传ToolStripMenuItem.Enabled = true;
                下载ToolStripMenuItem.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = false;
                button5.Enabled = false;
                readStream.Close();
                writeStream.Close();
                ftpWebResponse.Close();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                if (readStream != null)
                {
                    readStream.Close();
                }
                if (writeStream != null)
                {
                    writeStream.Close();
                }
            }
        }

        private void Stop()
        {
            button4.Enabled = false;
            button5.Enabled = true;
            暂停ToolStripMenuItem.Enabled = false;
            继续ToolStripMenuItem.Enabled = true;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = true;
            if (readStream != null)
            {
                readStream.Close();
            }
            if (writeStream != null)
            {
                writeStream.Close();
            }
            if (ftpWebResponse != null)
            {
                ftpWebResponse.Close();
            }
        }

        private void Resume()
        {
            button5.Enabled = false;
            button4.Enabled = true;
            继续ToolStripMenuItem.Enabled = false;
            暂停ToolStripMenuItem.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = false;
            if (isUpload)
            {
                if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0)
                {
                    MessageBox.Show("请选择需要上传的文件", "提示");
                    return;
                }
                isUpload = true;
                filePath = treeView1.SelectedNode.Tag.ToString();
                暂停ToolStripMenuItem.Enabled = true;
                上传ToolStripMenuItem.Enabled = false;
                下载ToolStripMenuItem.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = true;
                FileInfo fileInfo = new FileInfo(filePath);
                string file = fileInfo.Name;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(ftpPath + "/" + file);
                reqFTP.KeepAlive = false;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(user, pwd);
                reqFTP.Method = WebRequestMethods.Ftp.GetFileSize;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long startPosition = response.ContentLength;
                long startByte = startPosition;
                int percent = (int)(startByte * 100 / totalByte);
                label4.Text = percent + "%";
                Application.DoEvents();
                ftpWebRequest = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpPath + "/" + file));
                ftpWebRequest.Credentials = new NetworkCredential(user, pwd);
                ftpWebRequest.KeepAlive = false;
                ftpWebRequest.Method = WebRequestMethods.Ftp.AppendFile;
                ftpWebRequest.UseBinary = true;
                ftpWebRequest.ContentLength = fileInfo.Length;
                int buffLength = 2048;
                byte[] buff = new byte[buffLength];
                writeStream = fileInfo.OpenRead();
                try
                {
                    readStream = ftpWebRequest.GetRequestStream();
                    writeStream.Seek(startPosition, 0);
                    int contentLen = writeStream.Read(buff, 0, buffLength);
                    while (contentLen != 0)
                    {
                        readStream.Write(buff, 0, contentLen);
                        contentLen = writeStream.Read(buff, 0, buffLength);
                        startByte += contentLen;
                        percent = (int)(startByte * 100 / totalByte);
                        label4.Text = percent + "%";
                        Application.DoEvents();
                    }
                    readStream.Close();
                    writeStream.Close();
                    暂停ToolStripMenuItem.Enabled = false;
                    上传ToolStripMenuItem.Enabled = true;
                    下载ToolStripMenuItem.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = false;
                    isUpload = false;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (writeStream != null)
                    {
                        writeStream.Close();
                    }
                    if (readStream != null)
                    {
                        readStream.Close();
                    }
                }
            }
            else
            {
                try
                {
                    writeStream = new FileStream(localPath + "\\" + treeView2.SelectedNode.Text, FileMode.Append);
                    long startPosition = writeStream.Length;
                    if (startPosition >= totalByte)
                    {
                        writeStream.Close();
                        return;
                    }
                    ftpWebRequest = (FtpWebRequest)FtpWebRequest.Create(filePath);
                    ftpWebRequest.Credentials = new NetworkCredential(user, pwd);
                    ftpWebRequest.UseBinary = true;
                    ftpWebRequest.KeepAlive = false;
                    ftpWebRequest.ContentOffset = startPosition;
                    ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                    ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
                    readStream = ftpWebResponse.GetResponseStream();
                    long downloadedByte = startPosition;
                    int bufferSize = 512;
                    byte[] btArray = new byte[bufferSize];
                    int contentSize = readStream.Read(btArray, 0, btArray.Length);
                    while (contentSize > 0)
                    {
                        downloadedByte += contentSize;
                        int percent = (int)(downloadedByte * 100 / totalByte);
                        label4.Text = percent + "%";
                        Application.DoEvents();
                        writeStream.Write(btArray, 0, contentSize);
                        contentSize = readStream.Read(btArray, 0, btArray.Length);
                    }
                    暂停ToolStripMenuItem.Enabled = false;
                    上传ToolStripMenuItem.Enabled = true;
                    下载ToolStripMenuItem.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = false;
                    button5.Enabled = false;
                    readStream.Close();
                    writeStream.Close();
                    ftpWebResponse.Close();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
                finally
                {
                    if (readStream != null)
                    {
                        readStream.Close();
                    }
                    if (writeStream != null)
                    {
                        writeStream.Close();
                    }
                }
            }
        }

        private void FillFTPTree(TreeNode treeNode, String path)
        {
            int i = 0;
            foreach (String d in GetDirctory(path))
            {
                TreeNode node = new TreeNode();
                node.Text = d;
                node.Tag = path + "/" + d;
                treeNode.Nodes.Add(node);
                string subPath = path + "/" + d;
                FillFTPTree(treeNode.Nodes[i], subPath);
                i++;
            }
            foreach (string f in GetFile(path))
            {
                TreeNode node = new TreeNode();
                node.Text = f;
                node.Tag = path + "/" + f;
                treeNode.Nodes.Add(node);
            }
        }

        private List<string> GetDirctory(string path)
        {
            List<string> strs = new List<string>();
            try
            {
                ftpWebRequest = (FtpWebRequest)FtpWebRequest.Create(path + "/");
                ftpWebRequest.Credentials = new NetworkCredential(user, pwd);
                ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = ftpWebRequest.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (line[0] >= '0' && line[0] <= '9' && line.Contains("<DIR>"))
                    {
                        //Windows
                        string msg = line.Substring(line.LastIndexOf("<DIR>") + 5).Trim();
                        strs.Add(msg);
                    }
                    else if (line[0] == 'd')
                    {
                        //Linux
                        string msg = line.Substring(53).Trim();
                        strs.Add(msg);
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
                response.Close();
                return strs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            return strs;
        }

        private List<string> GetFile(String path)
        {
            List<string> strs = new List<string>();
            try
            {
                ftpWebRequest = (FtpWebRequest)FtpWebRequest.Create(path + "/");
                ftpWebRequest.Credentials = new NetworkCredential(user, pwd);
                ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = ftpWebRequest.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (line[0] >= '0' && line[0] <= '9' && !line.Contains("<DIR>") && line.Length > 39)
                    {
                        string msg = line.Substring(39).Trim();
                        strs.Add(msg);
                    }
                    else if (line[0] != 'd' && line.Length > 53)
                    {
                        //Linux
                        string msg = line.Substring(53).Trim();
                        strs.Add(msg);
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
                response.Close();
                return strs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            return strs;
        }
    }
}
