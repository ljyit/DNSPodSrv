using System;
using System.Runtime.InteropServices;

namespace DNSPodSrv
{
    class SRV_Controller
    {
        #region DLL Declare

        [DllImport("advapi32.dll")]
        public static extern IntPtr OpenSCManager(string lpMachineName, string lpSCDB, SRVSCManage scParameter);
        [DllImport("Advapi32.dll")]
        public static extern IntPtr CreateService(IntPtr SC_HANDLE, string lpSvcName, string lpDisplayName,
        SRVAccess dwDesiredAccess, int dwServiceType, int dwStartType, int dwErrorControl, string lpPathName,
        string lpLoadOrderGroup, int lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);
        [DllImport("advapi32.dll")]
        public static extern void CloseServiceHandle(IntPtr SCHANDLE);
        [DllImport("advapi32.dll")]
        public static extern bool StartService(IntPtr SVHANDLE, int dwNumServiceArgs, string lpServiceArgVectors);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr OpenService(IntPtr SCHANDLE, string lpSvcName, int dwNumServiceArgs);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool QueryServiceStatus(IntPtr hService, ref SRV_Win32_Status lpServiceStatus);
        [DllImport("advapi32.dll")]
        public static extern bool DeleteService(IntPtr SVHANDLE);
        [DllImport("advapi32.dll",SetLastError = true)]
        public static extern bool ControlService(IntPtr hService, SRVControl dwControl, ref SRV_Win32_Status lpServiceStatus);
        [DllImport("advapi32.dll",SetLastError = true )]
        public static extern bool ChangeServiceConfig2(IntPtr serviceHandle, int infoLevel, [MarshalAs(UnmanagedType.Struct)] ref SRVDesc serviceDesc);
        [DllImport("kernel32.dll")]
        public static extern int GetLastError();



        public class SrvParam
        {
            public string Name;
            public string exePath;
            public string DisplayName;
            public string Description = "";
            public int StartMode = 0x00000002;  //SERVICE_AUTO_START
            public bool useDesktop = false;
            public string Depend = "";
        }

        public enum SRVSCManage
        {
            CONNECT = 0x0001,
            CREATE_SERVICE = 0x0002,
            ENUMERATE_SERVICE = 0x0004,
            LOCK = 0x0008,
            QUERY_LOCK_STATUS = 0x0010,
            MODIFY_BOOT_CONFIG = 0x0020,
            ALL_ACCESS = 0xF003F
        }

        public enum SRVStatus
        {
            STATUS_ERROR =              -1,            //查询状态失败
            STOPPED=                     0x00000001,    //服务已停止
            START_PENDING=               0x00000002,    //服务正在启动
            STOP_PENDING=                0x00000003,    //服务正在停止
            RUNNING=                     0x00000004,    //服务已运行
            CONTINUE_PENDING=            0x00000005,    //服务正在恢复
            PAUSE_PENDING=               0x00000006,    //服务正在暂停
            PAUSED=                      0x00000007,    //服务已暂停 
        }
        public enum SRVAccess
        {
            QUERY_CONFIG=           0x0001,
            CHANGE_CONFIG=          0x0002,
            QUERY_STATUS=           0x0004,
            ENUMERATE_DEPENDENTS=   0x0008,
            START=                  0x0010,
            STOP=                   0x0020,
            PAUSE_CONTINUE=         0x0040,
            INTERROGATE=            0x0080,
            USER_DEFINED_CONTROL=   0x0100,
            ALL_ACCESS =            0x001FF,
            DELETE=                 0x10000
        }
        //服务控制结构
        public enum SRVControl
        {
            STOP=0x00000001,
            PAUSE = 0x00000002,
            CONTINUE = 0x00000003,
            INTERROGATE = 0x00000004,
            SHUTDOWN = 0x00000005,
            PARAMCHANGE = 0x00000006,
            NETBINDADD = 0x00000007,
            NETBINDREMOVE = 0x00000008,
            NETBINDENABLE = 0x00000009,
            NETBINDDISABLE = 0x0000000A,

        }
        /// <summary>
        /// 服务状态结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SRV_Win32_Status 
        {
            public int dwServiceType;
            public SRVStatus CurrentStatus;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SRVDesc 
        {
            public string Description;
        }

        #endregion DLLImport


        #region 查询服务状态 QueryStatus()
        public SRVStatus QueryStatus(string srvName)
        {
            IntPtr hScm = OpenSCManager(null, null, SRVSCManage.QUERY_LOCK_STATUS);
            if (hScm.ToInt32() == 0) return SRVStatus.STATUS_ERROR;

            IntPtr hSrv=OpenService(hScm,srvName,(int) SRVAccess.QUERY_STATUS);
            if (hSrv.ToInt32()==0)
            {
                CloseServiceHandle(hScm);
                return SRVStatus.STATUS_ERROR;
            }
            SRV_Win32_Status status=new SRV_Win32_Status();

            bool isOK=QueryServiceStatus(hSrv,ref status);
            CloseServiceHandle(hSrv);
            CloseServiceHandle(hScm);

            if (isOK)
            {
                return status.CurrentStatus;
            }
            else
            {
                return SRVStatus.RUNNING;
            }
        }
        #endregion

        #region 启动服务 Start()
        public bool Start(string srvName)
        {
            IntPtr hScm = OpenSCManager(null, null, SRVSCManage.ALL_ACCESS);
            if (hScm.ToInt32() == 0) return false;

            IntPtr hSrv = OpenService(hScm, srvName, (int)SRVAccess.ALL_ACCESS);
            if (hSrv.ToInt32() == 0)
            {
                CloseServiceHandle(hScm);
                return false;
            }
            bool ret = StartService(hSrv, 0, null);
            CloseServiceHandle(hSrv);
            CloseServiceHandle(hScm);
            return ret;
            
        }
        #endregion

        #region 停止服务 Stop()
        public bool Stop(string srvName)
        {
            IntPtr hScm = OpenSCManager(null, null, SRVSCManage.ALL_ACCESS);
            if (hScm.ToInt32() == 0) return false;

            IntPtr hSrv = OpenService(hScm, srvName, (int)SRVAccess.ALL_ACCESS);
            if (hSrv.ToInt32() == 0)
            {
                CloseServiceHandle(hScm);
                return false;
            }
            SRV_Win32_Status stat = new SRV_Win32_Status();
            bool ret = ControlService(hSrv, SRVControl.STOP, ref stat);
            CloseServiceHandle(hSrv);
            CloseServiceHandle(hScm);
            return ret;

        }
        #endregion
        
        #region 删除服务 Stop()
        public bool Delete(string srvName)
        {
            IntPtr hScm = OpenSCManager(null, null, SRVSCManage.ALL_ACCESS);
            if (hScm.ToInt32() == 0) return false;

            IntPtr hSrv = OpenService(hScm, srvName, (int)SRVAccess.DELETE);
            if (hSrv.ToInt32() == 0)
            {
                CloseServiceHandle(hScm);
                return false;
            }
            SRV_Win32_Status stat = new SRV_Win32_Status();
            bool ret = DeleteService(hSrv);
            CloseServiceHandle(hSrv);
            CloseServiceHandle(hScm);
            return ret;

        }
        #endregion

        #region 创建服务 Create()
        public bool Create(SrvParam parm)
        { 
            try
            {
                IntPtr hScm = OpenSCManager(null, null, SRVSCManage.CREATE_SERVICE);
                if (hScm.ToInt32() == 0) return false;

                IntPtr hSrv = CreateService(hScm
                                            , parm.Name
                                            , parm.DisplayName
                                            , SRVAccess.ALL_ACCESS
                                            , 0x10//SERVICE_WIN32_OWN_PROCESS
                                            , 0x02//SERVICE_AUTO_START
                                            , 0x01//SERVICE_ERROR_NORMAL
                                            , parm.exePath
                                            , null, 0, null, null, null);
                

                if (hSrv.ToInt32() == 0)
                {
                    CloseServiceHandle(hScm);
                    return false;
                }
                SRVDesc desc;
                desc.Description=parm.Description;
                ChangeServiceConfig2(hSrv, 1, ref desc);

                CloseServiceHandle(hSrv);
                CloseServiceHandle(hScm);
                return true;

                
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #endregion
    }
}
