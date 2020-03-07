using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.IO;
using DNSPod;

namespace DNSPodSrv
{
    public partial class DNSPod_Service : ServiceBase
    {
        IPHelper iphelper = new IPHelper();

        public DNSPod_Service()
        {
            InitializeComponent();
            
        }

        protected override void OnStart(string[] args)
        {
            Log("服务开始载入配置");
            string path = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            iphelper.ConfigFile = path + "\\Setting.ini";
            iphelper.LoadConfig();
            Log("服务开始启动");

            if (iphelper.LogNotify)
            {
                iphelper.OnNotifyMsg += new Action<string>(Log);
            }
            if (iphelper.LogIPChange)
            {
                iphelper.OnIPChanged += new Action<string>(OnIpChange);
            }
            
            iphelper.StartHooking();
        }

        void OnIpChange(string obj)
        {
            Log("检测到IP发生变化，新的IP为" + obj);
            DNSPod_Record rs = new DNSPod_Record(iphelper.ID,iphelper.Token);
            foreach (string host in iphelper.Hosts)
            {
                DNSPod_Record.L_Response ls= rs.List(iphelper.Domain, host);
                if (ls.records[0].value != obj)
                {
                    Log(host+"." +iphelper.Domain+"当前IP为:" + ls.records[0].value + "正准备更新");
                    rs.Update(iphelper.Domain, host, obj);
                }
                else
                {
                    Log("与DNSPOD登记的IP相同，未更新");
                }
                
            }
            
        }

        protected override void OnStop()
        {
            Log("服务停止中……");
            iphelper.StopHooking(true);
            iphelper.OnNotifyMsg -= new Action<string>(Log);
            iphelper.OnIPChanged -= new Action<string>(OnIpChange);

        }

        protected void Log(string msg)
        {
           
            try
            {
                FileInfo info = new FileInfo(iphelper.LogFile);
                if (info.Exists)
                {
                    if (info.Length>1024*1024)
                    {                        
                        //超过1M的文件改名处理
                        string newFile = info.DirectoryName + "\\" + Path.GetFileNameWithoutExtension(info.Name) + DateTime.Now.ToString("yyyyMMdd_hhmmss") + info.Extension;
                        info.MoveTo(newFile);
                    }
                }


            }
            catch (System.Exception ex)
            {
            	    
            }


            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(iphelper.LogFile, true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + msg);
            }
        }
    }
}
