using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Configuration;
using Utilities;
using Globals;
using OPC;

namespace Utilities
{
    #region Interface IRunConsole
    public interface IRunConsole
    {
        void RunConsole(string[] args);
    }
    #endregion

    #region ServiceBaseX
    [System.ComponentModel.DesignerCategory("code")]

    public partial class ServiceBaseX : System.ServiceProcess.ServiceBase, IRunConsole
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS
        {
            public int serviceType,
                       currentState,
                       controlsAccepted,
                       win32ExitCode,
                       serviceSpecificExitCode,
                       checkPoint,
                       waitHint;
        }

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007
        }

        protected SERVICE_STATUS _svcStatus;
        [DllImport("advapi32.DLL", EntryPoint = "SetServiceStatus", SetLastError = true)]
        public static extern bool SetServiceStatus(IntPtr hServiceStatus, ref SERVICE_STATUS rServiceStatus);

        public static bool bIsService = true;

        public static Logger _logger;
        public static DateTime dtStartProcess;
        public static SyncEvents _syncEvts = new SyncEvents();

        public ServiceBaseX() { InitializeComponent(); }

        public void RunConsole(string[] args)
        {
            try
            {
                bIsService = false;

                Console.CancelKeyPress += (sender, eventsArgs) =>
                {
                    eventsArgs.Cancel = true;
                    _syncEvts.GlobalExitEvent.Set();
                };

                OnStart(args);

                if (ServiceBaseX._syncEvts.AllThreadsRunning.WaitOne(GlobalParameters._iThreadStartTimeout))
                {
                    _logger.Log(Category.HLite, "Up and running! Press Ctrl-C to stop.");

                    //threads running; wait for the exit event
                    _syncEvts.GlobalExitEvent.WaitOne();
                }
                else
                    _logger.Log(Category.SysError, "Threads not starting in time, stopping ...");

                OnStop();
            }
            catch (Exception ex)
            {
                _logger.Log(Category.SysError, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }
    }
    #endregion

    #region SyncEvents
    public class SyncEvents
    {
        private EventWaitHandle _evtAllThreadsRunning, _evtGlobalExit;
        private int _iThreadsToRun, _iThreadsRunning;

        // Constructor
        public SyncEvents()
        {
            _evtAllThreadsRunning = new ManualResetEvent(false);
            _evtGlobalExit = new ManualResetEvent(false);

            _iThreadsToRun = 0;
            _iThreadsRunning = 0;
        }
        // counting the threads to start
        public int ThreadsToRun
        {
            get { return (_iThreadsToRun); }
            set { _iThreadsToRun = value; }
        }

        // counting the threads running
        public void IncrementThreadsRunning()
        {
            // for all threads useable
            Interlocked.Increment(ref _iThreadsRunning);

            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": ToRun = {0}, Running = {1}", _iThreadsToRun, _iThreadsRunning);

            if (_iThreadsRunning >= _iThreadsToRun)
                _evtAllThreadsRunning.Set();
        }

        //not used anymore
        public void DecrementThreadsRunning()
        {
            // for all threads useable
            while (_iThreadsRunning > 1)
            {
                Interlocked.Decrement(ref _iThreadsRunning);
            }

            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": ToRun = {0}, Running = {1}", _iThreadsToRun, _iThreadsRunning);
        }

        public EventWaitHandle AllThreadsRunning { get { return (_evtAllThreadsRunning); } }
        public EventWaitHandle GlobalExitEvent { get { return (_evtGlobalExit); } }
    }
    #endregion

    #region Single Instance
    static public class SingleInstance
    {
        private static Mutex _mutex = null;

        public static bool AlreadyRunning(string sName)
        {
            Mutex m = null;
            bool bCreated = false,
                 bNotExisting = false,
                 bNotAuthorized = false;

            try
            {
                m = Mutex.OpenExisting(sName, System.Security.AccessControl.MutexRights.ReadPermissions);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                bNotExisting = true;
            }
            catch (UnauthorizedAccessException)
            {
                bNotAuthorized = true;
            }

            if (m == null || bNotExisting || bNotAuthorized)
            {
                _mutex = new Mutex(true, sName, out bCreated);

                return (!bCreated);
            }

            return (true);
        }
    }
    #endregion
}