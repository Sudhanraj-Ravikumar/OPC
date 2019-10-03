using System;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading;
using IPS7Lnk.Advanced;
using Utilities;
using Globals;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using OPC.Telegram;
using OPC.Objects;

namespace OPC
{
    public class DBWrite
    {
        private OPCClient _client;

        private string _sConnection = string.Empty,
                       _sDBMode = string.Empty,
                       _sSelectedTable = string.Empty,
                       _sProcessType = string.Empty;

        private int _iDependecyPort = 0,
                    _iTimer = 0,
                    _iWatchdogTimer = 0,
                    iWatchdogCnt = 0,
                    iWatchdogMaxCnt = 0;

        private OracleConnection _oraConnection;

        private OracleCommand _cmdSel = null,
                              _cmdNotification = null,
                              _cmdReInitTQS = null,
                              _cmdUpdTQSStatus = null;
        
        private OracleDependency _dependency = null;

        private bool bDBConnected = false;

        private Timer _checkDBTimer,
                      _updateWatchdog;

        private readonly ManualResetEventSlim mre_dependency = new ManualResetEventSlim(true);
        private readonly ManualResetEventSlim mre_dependency_backup = new ManualResetEventSlim(true);
        private readonly ManualResetEvent mre_dbaccess = new ManualResetEvent(false);

        public DBWrite()
        {
            try
            {
                _sConnection = ConfigurationManager.ConnectionStrings["oracle"].ConnectionString;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [oracle]: " + ex.Message);
                return;
            }

            try
            {
                _iDependecyPort = Int32.Parse(ConfigurationManager.AppSettings["OracleDependency.Port"]);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [OracleDependency.Port]: " + ex.Message);
                return;
            }

            try
            {
                _sDBMode = ConfigurationManager.AppSettings["DB.Mode"];

                if (_sDBMode != "DEP")
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [DB.Mode] - accepted value: DEP");
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [DB.Mode]: " + ex.Message);
                return;
            }

            try
            {
                _sSelectedTable = ConfigurationManager.AppSettings["SelectedTable.Dependency"];
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [SelectedTable.Dependency]: " + ex.Message);
                return;
            }

            try
            {
                _sProcessType  = ConfigurationManager.AppSettings["Process.Type"];

                if (_sProcessType != "OPC" && _sProcessType != "LTEC")
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": ProcessType has to be 'OPC' or 'LTEC'!");
                    return;
                }
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Process.Type]: " + ex.Message);
                return;
            }

            try
            {
                _iTimer = Int32.Parse(ConfigurationManager.AppSettings["SleepTimer.Thread"]);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [SleepTime.Thread]: " + ex.Message);
                return;
            }

            try
            {
                _iWatchdogTimer = Int32.Parse(ConfigurationManager.AppSettings["Timer.Watchdog"]);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Timer.Watchdog]: " + ex.Message);
                return;
            }
            
            try
            {
                iWatchdogMaxCnt = Int32.Parse(ConfigurationManager.AppSettings["Max.Value.Watchdog"]);
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Max.Value.Watchdog]: " + ex.Message);
                return;
            }

            //initialize the opc object providing the plc connection
            _client = new OPCClient();

            if (_client.bConnectionOK == true)
                ServiceBaseX._logger.Log(Category.HLite, MethodBase.GetCurrentMethod().DeclaringType.Name + " connection to the PLC established.");

            InitDBConnection(_sConnection);
            bDBConnected = OpenDB();

            if (bDBConnected == true)
            {
                InitDBObjects(_oraConnection);
                //reinit fuction for possible reasons
                ReInitTQS();
            }
            
            _checkDBTimer = new Timer(OnDBCheck, this, GlobalParameters._iTimerPeriod, GlobalParameters._iTimerPeriod);
            _updateWatchdog = new Timer(OnWatchdog, this, GlobalParameters._iTimerPeriod, GlobalParameters._iTimerPeriod);
        }

        private void InitDBConnection(string sConnection)
        {
            try
            {
                _oraConnection = new OracleConnection(sConnection);
                _oraConnection.StateChange += new StateChangeEventHandler(DBStateChange);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private bool OpenDB()
        {
            try
            {
                if (_oraConnection != null)
                    _oraConnection.Open();
                return true;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }
        }

        private void DBStateChange(object sender, StateChangeEventArgs ea)
        {
            try
            {
                string sMessage = ": " + ea.OriginalState + " => " + ea.CurrentState;

                ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + sMessage);

                if (ea.CurrentState != ConnectionState.Open /*&& bDisposed == false*/)
                {
                    bDBConnected = false;

                    //restart timer to check connection
                    if (_checkDBTimer != null)
                    {
                        _checkDBTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        _checkDBTimer.Change(GlobalParameters._iTimerPeriod, Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        public void ReconnectDB()
        {
            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": try to reconnect to database.");

            TermDBObjects();
            bDBConnected = OpenDB();

            if (bDBConnected == true)
                InitDBObjects(_oraConnection);
            else
                ServiceBaseX._logger.Log(Category.Warning, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": try to reconnect to database failed.");
        }

        public void InitDBObjects(OracleConnection connection)
        {
            string sSelect = string.Empty;

            try
            {
                //PL SQL commands for updating TQS status
                _cmdReInitTQS = DBCommand_Write.ReInitTQS_Status(_oraConnection);
                _cmdUpdTQSStatus = DBCommand_Write.UpdTQS_Status(_oraConnection);

                //select command TQS
                _cmdSel = DBCommand_Write.SelTQS(_oraConnection);
                sSelect = "SELECT * FROM " + _sSelectedTable + " WHERE STATUS = 0";
                _cmdSel.CommandText = sSelect;

                //check if oracle dependency is active
                if (sSelect != string.Empty && _sDBMode == "DEP" && _dependency == null)
                {
                    _cmdNotification = new OracleCommand(sSelect, _oraConnection);
                    _cmdNotification.AddRowid = true;
                    _cmdNotification.NotificationAutoEnlist = true;
                    _cmdNotification.CommandTimeout = 2000;

                    _dependency = new OracleDependency(_cmdNotification, false, 0, false);

                    OracleDependency.Port = _iDependecyPort;
                    ServiceBaseX._logger.Log(Category.HLite, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": OracleDependency.Port = {0}", OracleDependency.Port);

                    _dependency.OnChange += new OnChangeEventHandler(dependency_OnChangeOracle);
                    _cmdNotification.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void ReInitTQS()
        {
            try
            {
                string sSelectSend = "UPDATE " + _sSelectedTable + " SET STATUS = 1 WHERE STATUS = -9";
                _cmdReInitTQS.CommandText = sSelectSend;

                _cmdReInitTQS.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
            }
        }

        private void TermDBObjects()
        {
            try
            {
                if (_cmdSel != null)
                {
                    _cmdSel.Dispose();
                    _cmdSel = null;
                }

                if (_cmdNotification != null)
                {
                    _cmdNotification.Dispose();
                    _cmdNotification = null;
                }

                if (_cmdReInitTQS != null)
                {
                    _cmdReInitTQS.Dispose();
                    _cmdReInitTQS = null;
                }

                if (_cmdUpdTQSStatus != null)
                {
                    _cmdUpdTQSStatus.Dispose();
                    _cmdUpdTQSStatus = null;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void TermDB()
        {
            try
            {
                TermDBObjects();

                if (_oraConnection != null)
                {
                    _oraConnection.Close();
                    _oraConnection.Dispose();
                    _oraConnection = null;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void dependency_OnChangeOracle(object sender, OracleNotificationEventArgs ea)
        {
            if (_client.bConnectionOK == true)
            {
                try
                {
                    if (!mre_dependency_backup.IsSet)
                        return;

                    mre_dependency_backup.Reset();
                    DateTime dtStartProcessDB = DateTime.Now;
                    ProcessDB();

                    DateTime dtEndProcessDB = DateTime.Now;

                    TimeSpan tsProcessDB = dtStartProcessDB - dtEndProcessDB;

                    if (tsProcessDB.TotalMilliseconds > 500)
                        ServiceBaseX._logger.Log(Category.Warning, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": time to process DB over 500ms. Time in ms {0}.", tsProcessDB);

                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
                finally
                {
                    mre_dependency.Set();
                }
            }
        }

        private void ProcessDB()
        {
            try
            {
                //check reference of DB object _cmdSel
                if (bDBConnected == true && _cmdSel == null)
                    bDBConnected = false;

                //if not connected return
                if (bDBConnected == false)
                    return;

                if (!mre_dependency.IsSet)
                {
                    mre_dependency.Wait();
                }

                mre_dependency.Reset();
                mre_dependency_backup.Set();

                //read TQS dataset with status between X and 0
                using (OracleDataReader dr = _cmdSel.ExecuteReader(CommandBehavior.Default))
                {
                    do
                    {
                        while (dr.Read())
                        {
                            int iPKID = dr.GetOracleDecimal(0).ToInt32(),
                                iStatus = dr.GetOracleDecimal(8).ToInt32();
                            string sMessageID = dr.GetOracleString(4).ToString();
                            
                            UpdateTQS(iPKID, 2, _sSelectedTable);

                            string sMessage = string.Empty;
                            OracleBlob blob = null;

                            //get the actual message to send
                            blob = dr.GetOracleBlob(9);
                            sMessage = Encoding.UTF8.GetString(blob.Value);

                            //parse the included informations
                            switch (sMessageID)
                            {
                                case "OPC_01":
                                    OPC_New_Lock_Request New_Lock_Request = new OPC_New_Lock_Request();
                                    New_Lock_Request.ParseTelegraminformation(sMessage);
                                    if (!CheckFlagDB(TelegramType.Lock_Request))
                                    {
                                        WriteDataToPLC(New_Lock_Request);
                                        Thread.Sleep(_iTimer);
                                        if (CheckFlagDB(TelegramType.Lock_Request))
                                        {
                                            UpdateTQS(iPKID, -9, _sSelectedTable);
                                            ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": DB not processed after being written.");
                                        }
                                        else
                                        {
                                            UpdateTQS(iPKID, 1, _sSelectedTable);
                                            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": OPC_01 Information successfully updated to DB.");
                                        }
                                    }
                                    else
                                    {
                                        UpdateTQS(iPKID, -9, _sSelectedTable);
                                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": DB not ready to be written at all. (OPC_01)");
                                    }
                                    break;

                                case "OPC_02":
                                    OPC_CTS_Lift_Drop CTS_Lift_Drop = new OPC_CTS_Lift_Drop();
                                    CTS_Lift_Drop.ParseTelegraminformation(sMessage);
                                    if (!CheckFlagDB(TelegramType.CTS_Lift_Drop))
                                    {
                                        WriteDataToPLC(CTS_Lift_Drop);
                                        Thread.Sleep(_iTimer);
                                        if (CheckFlagDB(TelegramType.CTS_Lift_Drop))
                                        {
                                            UpdateTQS(iPKID, -9, _sSelectedTable);
                                            ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": DB not processed after being written.");
                                        }
                                        else
                                        {
                                            UpdateTQS(iPKID, 1, _sSelectedTable);
                                            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": OPC_02 Information successfully updated to DB.");
                                        }
                                    }
                                    else
                                    {
                                        UpdateTQS(iPKID, -9, _sSelectedTable);
                                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": DB not ready to be written at all. (OPC_02)");
                                    }
                                    break;

                                case "OPC_03":
                                    OPC_New_Target New_Target = new OPC_New_Target();
                                    New_Target.ParseTelegraminformation(sMessage);
                                    if (!CheckFlagDB(TelegramType.New_Target))
                                    {
                                        WriteDataToPLC(New_Target);
                                        Thread.Sleep(_iTimer);
                                        if (CheckFlagDB(TelegramType.New_Target))
                                        {
                                            UpdateTQS(iPKID, -9, _sSelectedTable);
                                            ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": DB not processed after being written.");
                                        }
                                        else
                                        {
                                            UpdateTQS(iPKID, 1, _sSelectedTable);
                                            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": OPC_03 Information successfully updated to DB.");
                                        }
                                    }
                                    else
                                    {
                                        UpdateTQS(iPKID, -9, _sSelectedTable);
                                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": DB not ready to be written at all. (OPC_03)");
                                    }
                                    break;

                                case "LTEC01":
                                    LTEC_Input LTEC_Input = new LTEC_Input();
                                    LTEC_Input.ParseTelegramBody(sMessage);
                                    if (!CheckFlagDB(TelegramType.New_Slab_Scarfing))
                                    {
                                        WriteDataToPLC(LTEC_Input);
                                        Thread.Sleep(_iTimer);
                                        if (CheckFlagDB(TelegramType.New_Slab_Scarfing))
                                        {
                                            UpdateTQS(iPKID, -9, _sSelectedTable);
                                                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": DB not processed after being written.");
                                        }
                                        else
                                        {
                                            UpdateTQS(iPKID, 1, _sSelectedTable);
                                            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": LTEC Information successfully updated to DB.");
                                        }
                                    }
                                    else
                                    {
                                        UpdateTQS(iPKID, -9, _sSelectedTable);
                                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": DB not ready to be written at all. (LTEC)");
                                    }
                                    break;

                                default:
                                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": unknown MessageID.");
                                    break;
                            }
                        }
                    } while (dr.NextResult());
                }
                mre_dependency.Set();
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void UpdateTQS(int iPKID, int iStatus, string sTable)
        {
            try
            {
                _cmdUpdTQSStatus.Parameters[0].Value = iPKID;
                _cmdUpdTQSStatus.Parameters[1].Value = iStatus;
                _cmdUpdTQSStatus.Parameters[2].Value = sTable;

                try
                {
                    _cmdUpdTQSStatus.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void WriteDataToPLC(OPC_New_Lock_Request obj)
        {
            try
            {
                DBObjects.Lock_Request Lock_Request = new DBObjects.Lock_Request();
                Lock_Request.ParseDBInformation(obj.Lock);

                _client._plcConnection.Write(Lock_Request);
                _client._plcConnection.WriteBoolean("DB51.DBX 1", true);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void WriteDataToPLC(OPC_CTS_Lift_Drop obj)
        {
            try
            {
                DBObjects.Lift_Drop Lift_Drop = new DBObjects.Lift_Drop();
                Lift_Drop.ParseDBInformation(obj);

                _client._plcConnection.Write(Lift_Drop);
                if (Lift_Drop.Lift == true)
                    _client._plcConnection.WriteBoolean("DB51.DBX 1.1", true);
                else if (Lift_Drop.Drop == true)
                    _client._plcConnection.WriteBoolean("DB51.DBX 1.2", true);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void WriteDataToPLC(OPC_New_Target obj)
        {
            try
            {
                DBObjects.New_Target New_Target = new DBObjects.New_Target();
                New_Target.ParseDBInformation(obj);

                _client._plcConnection.Write(New_Target);
                _client._plcConnection.WriteBoolean("DB51.DBX 1.3", true);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void WriteDataToPLC(LTEC_Input obj)
        {
            try
            {
                DBObjects.Scarf_Input Scarf_Input = new DBObjects.Scarf_Input();
                Scarf_Input.ParseDBInformation(obj);

                _client._plcConnection.Write(Scarf_Input);
                _client._plcConnection.WriteBoolean("DB562.DBX 2.0", true);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        //check if the DB can be written again or not
        private bool CheckFlagDB(TelegramType teltype)
        {
            bool bOK = false,
                //for CTS_Lift_Drop / New Target
                 bDrop = false,
                 bLift = false,
                 bTarget = false;

            switch (teltype)
            {
                case TelegramType.Lock_Request:
                    bOK = _client._plcConnection.ReadBoolean("DB51.DBX 1.0");
                    //in case the DB was blocked, sleep and try again
                    if (bOK == true)
                    {
                        Thread.Sleep(_iTimer);
                        bOK = _client._plcConnection.ReadBoolean("DB51.DBX 1.0");
                    }
                    break;

                case TelegramType.CTS_Lift_Drop:
                    bLift = _client._plcConnection.ReadBoolean("DB51.DBX 1.1");
                    bDrop = _client._plcConnection.ReadBoolean("DB51.DBX 1.2");
                    bTarget = _client._plcConnection.ReadBoolean("DB51.DBX 1.3");

                    bOK = bLift || bDrop || bTarget;
                    //in case the DB was blocked, sleep and try again
                    if (bOK == true)
                    {
                        Thread.Sleep(_iTimer);
                        bLift = _client._plcConnection.ReadBoolean("DB51.DBX 1.1");
                        bDrop = _client._plcConnection.ReadBoolean("DB51.DBX 1.2");
                        bTarget = _client._plcConnection.ReadBoolean("DB51.DBX 1.3");

                        bOK = bLift || bDrop || bTarget;
                    }
                    break;

                case TelegramType.New_Target:
                    bLift = _client._plcConnection.ReadBoolean("DB51.DBX 1.1");
                    bDrop = _client._plcConnection.ReadBoolean("DB51.DBX 1.2");
                    bTarget = _client._plcConnection.ReadBoolean("DB51.DBX 1.3");

                    bOK = bTarget || bLift || bDrop;
                    //in case the DB was blocked, sleep and try again
                    if (bOK == true)
                    {
                        Thread.Sleep(_iTimer);
                        bLift = _client._plcConnection.ReadBoolean("DB51.DBX 1.1");
                        bDrop = _client._plcConnection.ReadBoolean("DB51.DBX 1.2");
                        bTarget = _client._plcConnection.ReadBoolean("DB51.DBX 1.3");

                        bOK = bTarget || bLift || bDrop;
                    }
                    break;

                case TelegramType.New_Slab_Scarfing:
                    bOK = _client._plcConnection.ReadBoolean("DB562.DBX 2.0");
                    if (bOK == true)
                    {
                        Thread.Sleep(_iTimer);
                        bOK = _client._plcConnection.ReadBoolean("DB562.DBX 2.0");
                    }
                    break;

                default:
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": wrong Telegramtype.");
                    return false;
            }
            return bOK;
        }

        private void OnDBCheck(object obj)
        {
            try
            {
                //disable timer
                _checkDBTimer.Change(Timeout.Infinite, Timeout.Infinite);

                //try to reconnect to databse
                if (bDBConnected == false)
                {
                    ReconnectDB();
                }

                //enable timer to check database connection
                _checkDBTimer.Change(GlobalParameters._iTimerPeriod, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }
        private void UpdateWatchdog(short iCounter, PlcDeviceConnection connection)
        {
            if (_sProcessType == "OPC")
            {
                byte bCounter = (byte)iCounter;
                connection.WriteByte("DB51.DBB 0", bCounter);
            }
            else if (_sProcessType == "LTEC")
                connection.WriteInt16("DB562.DBW 4", iCounter);
        }

        private void OnWatchdog(object obj)
        {
            try
            {
                _updateWatchdog.Change(Timeout.Infinite, Timeout.Infinite);

                if (iWatchdogCnt < iWatchdogMaxCnt)
                {
                    UpdateWatchdog((short)iWatchdogCnt, _client._plcConnection);
                    iWatchdogCnt++;
                }
                else
                {
                    iWatchdogCnt = 0;
                    UpdateWatchdog((short)iWatchdogCnt, _client._plcConnection);
                    iWatchdogCnt++;
                }

                _updateWatchdog.Change(_iWatchdogTimer, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }
    }
}
