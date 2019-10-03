using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Configuration;
using IPS7Lnk.Advanced;
using Utilities;
using Globals;

namespace OPC
{
    #region Service TCP
    [System.ComponentModel.DesignerCategory("code")]

    public partial class ServiceTCP : ServiceBaseX
    {
        //separate worker threads
        private DBRead _DBRead;
        private DBWrite _DBWrite;
        private Thread DBReadThread = null,
                       DBWriteThead = null;

        public ServiceTCP()
        {
            _logger = new Logger();
        }

        protected override void OnStart(string[] args)
        {
            if (!bIsService)
                Console.Title = ConfigurationManager.AppSettings["AppName"];

            _logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + ".OnStart");

            Thread.CurrentThread.Name = GetType().Name;

            IntPtr handle = ServiceHandle;
            if (bIsService)
            {
                _svcStatus.currentState = (int)ServiceState.SERVICE_START_PENDING;
                SetServiceStatus(handle, ref _svcStatus);
            }

            ++_syncEvts.ThreadsToRun;
            _DBRead = new DBRead();

            ++_syncEvts.ThreadsToRun;
            _DBWrite = new DBWrite();

            _logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": waiting for {0} threads ...", _syncEvts.ThreadsToRun);

            if (_syncEvts.AllThreadsRunning.WaitOne(GlobalParameters._iThreadStartTimeout))
            {
                if (bIsService)
                {
                    _svcStatus.currentState = (int)ServiceState.SERVICE_RUNNING;
                    SetServiceStatus(handle, ref _svcStatus);
                }
            }
            else
                _logger.Log(Category.SysError, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": waiting for SyncEvts.AllThreadsRunning failed");
        }

        protected override void OnStop()
        {
            _logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ".OnStop()");

            //signal the worker threads to exit
            _syncEvts.GlobalExitEvent.Set();

            if (_DBRead != null)
                _DBRead = null;

            if (_DBWrite != null)
                _DBWrite = null;

            //join all the threads and stop them
            Thread[] thrArr = new Thread[] { DBReadThread, DBWriteThead };
            foreach (Thread t in thrArr)
            {
                if (t != null && t.IsAlive)
                {
                    //gives the threat 5s time to finish otherwise it will be aborted
                    bool b = t.Join(5000);
                    if (!b)
                        t.Abort();
                }
            }

            DBReadThread = null;
            DBWriteThead = null;

            //wait additional time to close application
            Thread.Sleep(200);
            //indicate a successful exit
            ExitCode = 0;
        }
    }
    #endregion
}


