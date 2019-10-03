using System;
using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection;
using System.Configuration;
using System.Collections.Specialized;
using System.Security.Permissions;
using OPC;
using Utilities;

namespace CommServer
{
    static class Program
    {
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static int Main(params string[] args)
        {
            ServiceBaseX sbx = null;
            ServiceBaseX.dtStartProcess = DateTime.Now;

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ProcessHandler);

            try
            {
                //check if process restarts
                bool bRestart = false;

                //check on two arguments
                if (args.Length == 2)
                {
                    args[1].ToLower().EndsWith("restart");
                }

                if (SingleInstance.AlreadyRunning("Global\\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName) && bRestart == false)
                    return (1);

                sbx = new ServiceTCP();

                if (sbx != null)
                {
                    if (args.Length > 0 && args[0].ToLower().StartsWith("console"))
                        sbx.RunConsole(args);
                    else
                        System.ServiceProcess.ServiceBase.Run(sbx);
                }
            }
            catch (Exception ex)
            {
                if (ServiceBaseX._logger != null)
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
            return (0);
        }

        static void ProcessHandler(object sender, UnhandledExceptionEventArgs ea)
        {
            Exception ex = (Exception)ea.ExceptionObject;
            ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": uncaught exception: " + ex.Message);

            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": trying to restart service ... ");

            DateTime dtEndProcess = DateTime.Now;

            TimeSpan tsProcessTime = dtEndProcess - ServiceBaseX.dtStartProcess;

            //restart only if process lifetime over 20 seconds (otherwise theres a bigger problem)
            if (tsProcessTime.Milliseconds > 20000)
            {
                //close file
                Logger.Close();

                string sArgument = string.Empty;

                if (ServiceBaseX.bIsService == false)
                    sArgument = Environment.GetCommandLineArgs()[1] + "restart";

                //restart process
                System.Diagnostics.Process.Start(Environment.GetCommandLineArgs()[0], sArgument);

                Environment.Exit(0);
            }
            else
            {
                ServiceBaseX._logger.Log(Category.Warning, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": restart process aborted: process lifetime to short.");
                Environment.Exit(0);
            }
        }
    }
}