using System;
using System.IO;
using System.Diagnostics;
using System.Configuration;

namespace Utilities
{
    #region Category
    public enum Category { Undef = 0, Info, Error, Warning, SysInfo, SysError, SysWarning, HLite }
    #endregion

    #region Extension
    public static class Extension
    {
        public static bool IsError(this Category eCat) { return (eCat == Category.Error || eCat == Category.SysError); }
        public static bool IsWarning(this Category eCat) { return (eCat == Category.Warning || eCat == Category.SysWarning); }
    }
    #endregion

    #region Logger
    public class Logger
    {
        static readonly object locker = new object();
        private static StreamWriter n_sw;
        private string _sDirectory, _sLogDir;
        private DateTime n_dt = DateTime.Now;
        private EventLog n_ev;

        public Logger()
        {
            _sLogDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            _sDirectory = _sLogDir.Trim();
            if (_sDirectory[_sDirectory.Length - 1] != '\\')
                _sDirectory += '\\';

            _sDirectory = _sDirectory + "Log\\";

            if (!Directory.Exists(_sDirectory))
                Directory.CreateDirectory(_sDirectory);

            n_sw = CreateFileStreamWriter();

            if (EventLog.Exists(_sDirectory))
                EventLog.CreateEventSource(_sDirectory, null);

            n_ev = new EventLog();
        }

        private StreamWriter CreateFileStreamWriter()
        {
            string sExe = ConfigurationManager.AppSettings["Process.Name"];
            StreamWriter sw = File.AppendText(_sDirectory + sExe + "_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            return (sw);
        }

        public void Log(Category eCat, string sMessage)
        {
            try
            {
                lock (locker)
                {
                    DateTime dt = DateTime.Now;

                    if (Math.Abs(dt.DayOfYear - n_dt.DayOfYear) > 0) // Neuer Tag -> neues File
                    {
                        if (n_sw != null)
                        {
                            n_sw.Close();
                            n_sw.Dispose();
                        }
                        n_sw = CreateFileStreamWriter();
                    }

                    if (n_sw != null)
                    {
                        n_dt = dt;

                        string s = dt.ToString("yyyy-MM-dd HH:mm:ss:fff") + " " + eCat + ": > " + sMessage;

                        if (eCat.IsWarning())
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        else if (eCat.IsError())
                            Console.ForegroundColor = ConsoleColor.Red;
                        else if (eCat == Category.HLite)
                            Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(s);
                        Console.ResetColor();

                        n_sw.BaseStream.Seek(0, SeekOrigin.End);
                        n_sw.WriteLine(s);
                        n_sw.Flush();
                    }
                }
            }
            catch (Exception) { }
        }

        public void Log(Category eCat, string sFmt, object arg0)
        {
            Log(eCat, string.Format(sFmt, arg0));
        }
        public void Log(Category eCat, string sFmt, object arg0, object arg1)
        {
            Log(eCat, string.Format(sFmt, arg0, arg1));
        }
        public void Log(Category eCat, string sFmt, object arg0, object arg1, object arg2)
        {
            Log(eCat, string.Format(sFmt, arg0, arg1, arg2));
        }
        public void Log(Category eCat, string sFmt, object arg0, object arg1, object arg2, object arg3)
        {
            Log(eCat, string.Format(sFmt, arg0, arg1, arg2, arg3));
        }

        public static void Close()
        {
            if (n_sw != null)
            {
                n_sw.Close();
                n_sw.Dispose();
                n_sw = null;
            }
        }
    }
    #endregion
}
