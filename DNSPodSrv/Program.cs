using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace DNSPodSrv
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                Application.EnableVisualStyles();
                Application.Run(new MainForm());
            }
            else
            {
                ServiceBase.Run(new DNSPod_Service());
            }
        }
    }
}
