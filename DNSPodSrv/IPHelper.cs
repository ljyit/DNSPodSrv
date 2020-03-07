using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace DNSPodSrv
{
    public class IPHelper : Control
    {
        public event Action<string> OnNotifyMsg;
        public event Action<string> OnIPChanged;

        static bool NeedStop = false;
        static object locker = new object();

        public string LocalIP { get; set; }
        //配置信息
        public string ID { get; set; }
        public string Token { get; set; }
        public string Domain { get; set; }
        public string[] Hosts { get; set; }
        public int TimeLoop { get; set; }
        public string ServiceName { get; set; }

        public string ConfigFile { get; set; }
        public string LogFile { get; set; }
        public bool LogNotify { get; set; }//是否记录心跳包
        public bool LogIPChange { get; set; }//是否记录IP变化记录
        const  string ConfigSection = "DNSPod";

        public IPHelper()
        {
            //默认的配置文件
            string path = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            ConfigFile = path + "\\Setting.ini";
            LogFile = path + "\\DNSPodSrv.log";
            LocalIP = "";
            LogNotify = true;
            LogIPChange = true;
        }


        #region 加载设置 LoadConfig()
        public void LoadConfig()
        {

            ID = iniRead(ConfigSection, "ID", "", ConfigFile);
            Token = iniRead(ConfigSection, "Token", "", ConfigFile);
            Domain = iniRead(ConfigSection, "Domain", "", ConfigFile);
            ServiceName = iniRead(ConfigSection, "ServiceName", "DDNS", ConfigFile);
            LogFile = iniRead(ConfigSection, "LogFile", LogFile, ConfigFile);
            LogNotify = Convert.ToBoolean(iniRead(ConfigSection, "LogNotify", LogNotify.ToString(), ConfigFile));
            LogIPChange = Convert.ToBoolean(iniRead(ConfigSection, "LogIPChange", LogIPChange.ToString(), ConfigFile));

            TimeLoop = Convert.ToInt32(iniRead(ConfigSection, "TimeLoop", "30", ConfigFile));
            if (TimeLoop<5)
            {
                TimeLoop = 5;
            }

            string cHost = iniRead(ConfigSection, "Host", "", ConfigFile);
            Hosts = cHost.Split(',');
        }
        #endregion

        #region 写入配置
        public void SaveConfig()
        {
            iniWrite(ConfigSection, "ID", ID, ConfigFile);
            iniWrite(ConfigSection, "Token", Token, ConfigFile);
            iniWrite(ConfigSection, "Domain", Domain, ConfigFile);
            iniWrite(ConfigSection, "TimeLoop", TimeLoop.ToString(), ConfigFile);
            iniWrite(ConfigSection, "ServiceName", ServiceName, ConfigFile);
            iniWrite(ConfigSection, "LogFile", LogFile, ConfigFile);
            iniWrite(ConfigSection, "LogIPChange", LogIPChange.ToString(), ConfigFile);
            iniWrite(ConfigSection, "LogNotify", LogNotify.ToString(), ConfigFile);

            string subdomain = "";
            foreach (string cHost in Hosts)
            {
                if (cHost.Trim().Length>0)
                {
                    subdomain += cHost + ",";
                }
            }
            iniWrite(ConfigSection, "Host", subdomain.Trim(','), ConfigFile);

        }
        #endregion

        #region StopHooking() 停止
        public void StopHooking(bool isWait)
        {
            lock (locker)
            {
                NeedStop = true;
            }
            while (isWait)
            {
                lock (locker)
                {  
                    isWait = NeedStop;
                }

                Thread.Sleep(100);
            }
        }
        #endregion

        #region StartHooking() 开始监视IP变化
        
        public void StartHooking()
		{
            lock (locker)
            {
                NeedStop = false;
            }

            new Thread(new ParameterizedThreadStart(this.ThreadProcess))
            {
                IsBackground = true
            }.Start();
		}


        protected void NotifyMsg(string msg)
        {
            if (OnNotifyMsg == null) return;
            
            if (this.InvokeRequired)
            {
                Action<string> method = new Action<string>(NotifyMsg);
                base.Invoke(method, new object[] { msg });
            }
            else
            {
                OnNotifyMsg(msg);
            }
        }
        protected void IPChanged(string msg)
        {
             if (this.OnIPChanged == null) return;

            if (this.InvokeRequired)
            {
                Action<string> method = new Action<string>(IPChanged);
                base.Invoke(method, new object[] { msg });
            }
            else
            {
               this.LocalIP = msg;
               OnIPChanged(msg); 
            }
        }
        protected void ThreadProcess(object state)
        {
            int second = TimeLoop;

            while (!NeedStop)
            {
                if (second >0)
                {
                    second--;
                    Thread.Sleep(1000);
                    continue;
                }
                second = TimeLoop;


                try
                {
                    this.NotifyMsg("开始获取IP.");
                    string cIP = this.GetIP();
                    this.NotifyMsg("获取IP成功，当前IP：" + cIP);

                    //为了让第一次运行时，触发IP变化
                    //if (this.LocalIP != null && cIP != this.LocalIP)
                    if ( cIP != this.LocalIP)
                    {
                        this.IPChanged(cIP);
                    }
                    this.LocalIP = cIP;
                }
                catch (System.Exception ex)
                {
                    this.NotifyMsg("发生了错误：" + ex.Message);
                }
            }
            lock (locker)
            {
                NeedStop = false;
            }
            this.NotifyMsg("服务已停止.");
        }
        #endregion

        #region GetIP()  获取本地IP
        public string  GetIP()
        {
            string cIP = "";
            try { cIP = IPHelper.GetIPFromDNSPod(); return cIP; }
            catch { this.NotifyMsg("从DNSPOD获取IP失败"); }

            try { cIP = IPHelper.GetIpFromIp138(); return cIP; }
            catch { this.NotifyMsg("从IP138获取IP失败"); }

            try { cIP = IPHelper.GetIpFromGreak(); return cIP; }
            catch { this.NotifyMsg("从GreakIP失败"); }

            throw new Exception("用尽所有备用服务器");
        }
        #endregion

        
        #region GetIPFromDNSPod() 获取IP 从DNSPod
        static string GetIPFromDNSPod()
        {
            using (TcpClient tcpClient = new TcpClient())
            {

                string str="";
                try
                {
                    tcpClient.SendTimeout = 3000;
                    tcpClient.ReceiveTimeout = 3000;
                    
                    tcpClient.Connect("ns1.dnspod.net", 6666);
                    byte[] array = new byte[512];
                    int count = tcpClient.GetStream().Read(array, 0, 512);
                    str = Encoding.ASCII.GetString(array, 0, count);
                    tcpClient.Close();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return str;
            }

        }
        #endregion


        #region GetIpFromIp138() 获取IP 从IP138
        public static string GetIpFromIp138()
        {
            string url = "http://www.ip138.com/ip2city.asp";
            return IPHelper.GetIpByWeb(url, IPHelper._regIp138, "ip138");
        }
        private static string GetIpByWeb(string url, Regex reg, string domainName)
        {
            WebClient webClient = new WebClient();

            string input = webClient.DownloadString(url);
            MatchCollection matchCollection = reg.Matches(input);
            
            IEnumerator enumerator = matchCollection.GetEnumerator();
            if (enumerator.MoveNext())
            {
                Match match = (Match)enumerator.Current;
                return match.Groups[1].Value;
            }
            throw new ApplicationException(string.Format("get ip error from {0}", domainName));
        }
        private static Regex _regIp138 = new Regex("<center>.*?(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}).*<\\/center>", RegexOptions.IgnoreCase);
        #endregion

        #region GetIpFromGreak
        public static string GetIpFromGreak()
        {
            string url = "http://greak.net/ip";
            return IPHelper.GetIpByWeb(url, IPHelper._regGreak, "greak.net");
        }
        private static Regex _regGreak = new Regex("(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})", RegexOptions.IgnoreCase);
        #endregion

        #region API函数声明

        [DllImport("kernel32")]//返回0表示失败，非0为成功
        private static extern long WritePrivateProfileString(string section, string key,
            string val, string filePath);

        [DllImport("kernel32")]//返回取得字符串缓冲区的长度
        private static extern long GetPrivateProfileString(string section, string key,
            string def, StringBuilder retVal, int size, string filePath);


        #endregion

        #region 读Ini文件

        public static string iniRead(string Section, string Key, string NoText, string iniFilePath)
        {
            if (File.Exists(iniFilePath))
            {
                StringBuilder temp = new StringBuilder(1024);
                GetPrivateProfileString(Section, Key, NoText, temp, 1024, iniFilePath);
                return temp.ToString();
            }
            else
            {
                return NoText;
            }
        }

        #endregion

        #region 写Ini文件

        public static bool iniWrite(string Section, string Key, string Value, string iniFilePath)
        {
            long OpStation = WritePrivateProfileString(Section, Key, Value, iniFilePath);
            if (OpStation == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion
    }
}
