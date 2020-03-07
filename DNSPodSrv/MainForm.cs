using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; 
using System.Reflection;
using DNSPod;


namespace DNSPodSrv
{
    public partial class MainForm : Form
    {
        SRV_Controller srvControl = new SRV_Controller();
        IPHelper iphelper = new IPHelper();
        public MainForm()
        {
            InitializeComponent();
            
            //CheckForIllegalCrossThreadCalls = false; 
        }

        private void MainForm_Load(object sender, EventArgs e)
        { 
            iphelper.LoadConfig();

            txtID.Text = iphelper.ID;
            txtDomain.Text = iphelper.Domain;
            txtToken.Text = iphelper.Token;
            numSeconds.Value =  iphelper.TimeLoop;
            txtHost.Lines = iphelper.Hosts;
            txtLogFile.Text = iphelper.LogFile;
            chkLogIPChange.Checked  = iphelper.LogIPChange;
            chkLogNotify.Checked = iphelper.LogNotify;

            timer.Enabled = true;;

            timer_Tick(sender, e);
        }

        private void btnWriteConfig_Click(object sender, EventArgs e)
        { 
            iphelper.ID = txtID.Text.Trim();
            iphelper.Domain = txtDomain.Text.Trim();
            iphelper.Token = txtToken.Text.Trim();
            iphelper.TimeLoop = Convert.ToInt32(numSeconds.Value);
            iphelper.Hosts = txtHost.Lines;
            iphelper.LogFile = txtLogFile.Text;
            iphelper.LogIPChange = chkLogIPChange.Checked;
            iphelper.LogNotify = chkLogNotify.Checked;

            iphelper.SaveConfig();
            MessageBox.Show("保存完成!","提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            if (!srvControl.Start(iphelper.ServiceName))
            {
                MessageBox.Show("启动服务失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            timer_Tick(sender, e);
        }
        private void cmdStop_Click(object sender, EventArgs e)
        {
            if (!srvControl.Stop(iphelper.ServiceName))
            {
                MessageBox.Show("停止服务失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            timer_Tick(sender, e);
        }
        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (!srvControl.Delete(iphelper.ServiceName))
            {
                MessageBox.Show("删除服务失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            timer_Tick(sender, e);
        }
        private void cmdCreate_Click(object sender, EventArgs e)
        {
            SRV_Controller.SrvParam parm = new SRV_Controller.SrvParam();
            parm.Name = iphelper.ServiceName;
            parm.exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            parm.DisplayName= "DNSPod Client";
            parm.Description= "动态DNS服务程序, by 木瓜";
            parm.Depend="tcpip";
            srvControl.Create(parm);

            timer_Tick(sender, e);
        }

        private void cmdSelectFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.RestoreDirectory = true;
            dlg.DefaultExt = "txt";
            dlg.Filter = "日志文件(*.log)|*.log";
            dlg.FileName = iphelper.LogFile;

            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtLogFile.Text = dlg.FileName;
        }



        private void timer_Tick(object sender, EventArgs e)
        {
            SRV_Controller.SRVStatus status = srvControl.QueryStatus(iphelper.ServiceName);
            switch (status)
            {
                case SRV_Controller.SRVStatus.STATUS_ERROR:
                    lblStatus.Text = "服务未安装";
                    cmdCreate.Enabled = true;
                    cmdStart.Enabled = false;
                    cmdStop.Enabled = false;
                    cmdDelete.Enabled = false;
                    break;
                case SRV_Controller.SRVStatus.STOPPED:
                    lblStatus.Text = "服务已停止";
                    cmdCreate.Enabled = false;
                    cmdStart.Enabled = true;
                    cmdStop.Enabled = false;
                    cmdDelete.Enabled = true;
                    break;
                case SRV_Controller.SRVStatus.START_PENDING:
                    lblStatus.Text = "服务正在启动中";
                    cmdCreate.Enabled = false;
                    cmdStart.Enabled = false;
                    cmdStop.Enabled = false;
                    cmdDelete.Enabled = false;
                    break;
                case SRV_Controller.SRVStatus.STOP_PENDING:
                    lblStatus.Text = "服务正在停止中";
                    cmdCreate.Enabled = false;
                    cmdStart.Enabled = false;
                    cmdStop.Enabled = false;
                    cmdDelete.Enabled = false;
                    break;
                case SRV_Controller.SRVStatus.RUNNING:
                    lblStatus.Text = "服务运行中";
                    cmdCreate.Enabled = false;
                    cmdStart.Enabled = false;
                    cmdStop.Enabled = true;
                    cmdDelete.Enabled = false;
                    break;
                case SRV_Controller.SRVStatus.CONTINUE_PENDING:
                    lblStatus.Text = "服务正在恢复中";
                    cmdCreate.Enabled = false;
                    cmdStart.Enabled = false;
                    cmdStop.Enabled = false;
                    cmdDelete.Enabled = false;
                    break;
                case SRV_Controller.SRVStatus.PAUSE_PENDING:
                    lblStatus.Text = "服务正在暂停中";
                    cmdCreate.Enabled = false;
                    cmdStart.Enabled = false;
                    cmdStop.Enabled = false;
                    cmdDelete.Enabled = false;
                    break;
                case SRV_Controller.SRVStatus.PAUSED:
                    lblStatus.Text = "服务已暂停";
                    cmdCreate.Enabled = false;
                    cmdStart.Enabled = true;
                    cmdStop.Enabled = true;
                    cmdDelete.Enabled = false;
                    break;
                default:
                    lblStatus.Text = "未知状态";
                    cmdCreate.Enabled = false;
                    cmdStart.Enabled = false;
                    cmdStop.Enabled = false;
                    cmdDelete.Enabled = false;
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            iphelper.OnIPChanged += new Action<string>(OnIpChange);
            iphelper.StartHooking();

        }

        void OnIpChange(string obj)
        {
            Log("检测到IP发生变化，新的IP为" + obj);
            DNSPod_Record rs = new DNSPod_Record(iphelper.ID,iphelper.Token);
            foreach (string host in iphelper.Hosts)
            {
                DNSPod_Record.L_Response ls = rs.List(iphelper.Domain, host);
                if (ls.records[0].value != obj)
                {
                    Log(host + "." + iphelper.Domain + "当前IP为:" + ls.records[0].value + "正准备更新");
                    rs.Update(iphelper.Domain, host, obj);
                }
                else
                {
                    Log("与DNSPOD登记的IP相同，未更新");
                }

            }
        }
        protected void Log(string msg)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter("C:\\log.txt", true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + msg);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FileInfo info = new FileInfo(iphelper.LogFile);
            if (info.Exists)
            {
                //if (info.Length > 1024 * 1024)
                {

                    //超过1M的文件改名处理

                    string newFile = info.DirectoryName + "\\" + Path.GetFileNameWithoutExtension(info.Name) + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + info.Extension;
                    MessageBox.Show(newFile);
                    //info.MoveTo(iphelper.LogFile)
                }
            }
        }

         



       
 

    }
}
