using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Threading;
using IPS7Lnk.Advanced;
using Utilities;
using Globals;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using OPC.Objects;

namespace OPC
{
    public class DBRead
    {
        private static OPCClient _client;

        private string _sConnection = string.Empty,
                       _sProcessType = string.Empty;
        
        private OracleConnection _oraConnection;

        private OracleCommand _cmdSel = null,
        #region Init
 _cmdInitLOST = null,
                              _cmdInitPKID_RT = null,
                              _cmdInitPKID_CY = null,
        #endregion

        #region OPC
 _cmdTriggerWO = null,
                              _cmdTrackingMap = null,
                              _cmdConveyor = null,
                              _cmdInterlockConfirm = null,
                              _cmdStatusInformation = null,
        #endregion

        #region LTEC
                              _cmdScarfOutput = null;
        #endregion

        private int _iTimerTM = 0,
                    _iTimerTOP = 0,
                    _iTimerLOST = 0,
                    _iTimerLTEC = 0,
                    _iTimerWatchdog = 0,
                    _iValueWatchdogOld = 0,
                    _iValueWatchdogNew = 0;

        private bool bDBConnected = false,
                     bPLCReconnect = false;

        private int[] Place_PKID_RT,
                      Place_PKID_CY;

        private bool[] Place_Interlock,
                       Place_Status;

        private Timer _checkDBOPCTM, //timer for tracking map
                      _checkDBOPCTOP, //timer for take over point
                      _checkDBOPCLOST, //timer for interlock and status - has nothing todo with LOST
                      _checkDBLTEC,
                      _checkDBTimer,
                      _checkWatchdogTimer;
        
        public static Dictionary<int, TOPLoc> _dic_TOPLocation_actual = new Dictionary<int, TOPLoc>();
        public static Dictionary<int, TOPLoc> _dic_TOPLocation_old = new Dictionary<int, TOPLoc>();
        public static Dictionary<int, RTData> _dic_Lock_Status_actual = new Dictionary<int, RTData>();
        public static Dictionary<int, RTData> _dic_Lock_Status_old = new Dictionary<int, RTData>();
        public static Dictionary<int, RTData> _dic_Lock_Status_update = new Dictionary<int, RTData>();
        public static Dictionary<int, RTData> _dic_Slablist_RT_actual = new Dictionary<int, RTData>();
        public static Dictionary<int, RTData> _dic_Slablist_RT_old = new Dictionary<int, RTData>();
        public static Dictionary<int, Slablist> _dic_Slablist_Con_actual = new Dictionary<int, Slablist>();
        public static Dictionary<int, Slablist> _dic_Slablist_Con_old = new Dictionary<int, Slablist>();

        public DBRead()
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
                _sProcessType = ConfigurationManager.AppSettings["Process.Type"];
                if (_sProcessType != "OPC" && _sProcessType != "LTEC")
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": ProcessType has to be 'OPC' or 'LTEC'!");
                    return;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Process.Type]: " + ex.Message);
                return;
            }

            try
            {
                _iTimerTM = Int32.Parse(ConfigurationManager.AppSettings["Timer.TrackingMap"]);
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Timer.TrackingMap]: " + ex.Message);
                return;
            }

            try
            {
                _iTimerTOP = Int32.Parse(ConfigurationManager.AppSettings["Timer.TakeOverPoint"]);
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Timer.TakeOverPoint]: " + ex.Message);
                return;
            }

            try
            {
                _iTimerLOST = Int32.Parse(ConfigurationManager.AppSettings["Timer.LockStatus"]);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Timer.LockStatus]: " + ex.Message);
                return;
            }

            try
            {
                _iTimerLTEC = Int32.Parse(ConfigurationManager.AppSettings["Timer.LTEC"]);
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Timer.LTEC]: " + ex.Message);
                return;
            }

            try
            {
                _iTimerWatchdog = Int32.Parse(ConfigurationManager.AppSettings["Timer.Watchdog"]);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Timer.Watchdog]: " + ex.Message);
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
                InitWatchdog(_sProcessType);

                if (_sProcessType == "OPC")
                {
                    //initialize the Place PKID object
                    InitPKID_RT();
                    InitPKID_CY();
                    //initialize the Interlock/Status object
                    InitOldLOST();
                    //initialize the take over point dictionary
                    InitTOPLoc();
                    //initialize the lock status dictioncary
                    InitLOST();
                    //initialize the rollertable dictionary
                    InitRT();
                    //initialize the conveyor dictionary
                    InitCon();
                }
            }

            if (_sProcessType == "OPC")
            {
                _checkDBOPCTM = new Timer(OnDBOPCTMCheck, this, _iTimerTM, GlobalParameters._iTimerPeriod);
                _checkDBOPCTOP = new Timer(OnDBOPCTOP, this, _iTimerTOP, GlobalParameters._iTimerPeriod);
                _checkDBOPCLOST = new Timer(OnDBOPCLOST, this, _iTimerLOST, GlobalParameters._iTimerPeriod);
            }
            else
            {
                DBObjects.plcbScarf_Output.Changed += ScarfOutputHandler;
                _checkDBLTEC = new Timer(OnDBLTEC, this, _iTimerLTEC, GlobalParameters._iTimerPeriod);
            }
                
            _checkDBTimer = new Timer(OnDBCheck, this, GlobalParameters._iTimerPeriod, GlobalParameters._iTimerPeriod);
            _checkWatchdogTimer = new Timer(OnWatchdog, this, _iTimerWatchdog, GlobalParameters._iTimerPeriod);
        }

        private void InitDBConnection(string sConnection)
        {
            try
            {
                _oraConnection = new OracleConnection(sConnection);
                _oraConnection.StateChange += new StateChangeEventHandler(DBStateChange);
            }
            catch(Exception ex)
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

        private void ReconnectDB()
        {
            ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": try to reconnect to database.");

            TermDBObjects();
            bDBConnected = OpenDB();

            if (bDBConnected == true)
                InitDBObjects(_oraConnection);
            else
                ServiceBaseX._logger.Log(Category.Warning, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": try to reconnect to database failed.");
        }

        private void InitDBObjects(OracleConnection connection)
        {
            try
            {
                _cmdSel = DBCommand_Write.SelTQS(_oraConnection);
                //Init db commands
                _cmdInitLOST = DBCommand_Init.LOST_Init(_oraConnection);
                _cmdInitPKID_RT = DBCommand_Init.PKID_RT_Init(_oraConnection);
                _cmdInitPKID_CY = DBCommand_Init.PKID_CY_Init(_oraConnection);
                //OPC db commands
                _cmdTriggerWO = DBCommand_Read.Trigger_Workorder(_oraConnection);
                _cmdTrackingMap = DBCommand_Read.TrackingMap(_oraConnection);
                _cmdConveyor = DBCommand_Read.Conveyor(_oraConnection);
                _cmdInterlockConfirm = DBCommand_Read.InterlockConfirm(_oraConnection);
                _cmdStatusInformation = DBCommand_Read.StatusInformation(_oraConnection);
                //LTEC db command
                _cmdScarfOutput = DBCommand_Read.Scarf_Output(_oraConnection);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
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

                if (_cmdInitLOST != null)
                {
                    _cmdInitLOST.Dispose();
                    _cmdInitLOST = null;
                }

                if (_cmdInitPKID_RT != null)
                {
                    _cmdInitPKID_RT.Dispose();
                    _cmdInitPKID_RT = null;
                }

                if (_cmdInitPKID_CY != null)
                {
                    _cmdInitPKID_CY.Dispose();
                    _cmdInitPKID_CY = null;
                }

                if (_cmdTriggerWO != null)
                {
                    _cmdTriggerWO.Dispose();
                    _cmdTriggerWO = null;
                }

                if (_cmdTrackingMap != null)
                {
                    _cmdTrackingMap.Dispose();
                    _cmdTrackingMap = null;
                }

                if (_cmdConveyor != null)
                {
                    _cmdConveyor.Dispose();
                    _cmdConveyor = null;
                }

                if (_cmdInterlockConfirm != null)
                {
                    _cmdInterlockConfirm.Dispose();
                    _cmdInterlockConfirm = null;
                }

                if (_cmdStatusInformation != null)
                {
                    _cmdStatusInformation.Dispose();
                    _cmdStatusInformation = null;
                }

                if (_cmdScarfOutput != null)
                {
                    _cmdScarfOutput.Dispose();
                    _cmdScarfOutput = null;
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

        private void InitWatchdog(string sProcessType)
        {
            short iCounter = 0;
            byte bCounter = 0;

            try
            {
                if (_sProcessType == "OPC")
                {
                    bCounter = _client._plcConnection.ReadByte("DB50.DBB 0");
                    _iValueWatchdogOld = (int)bCounter;
                }
                else if (_sProcessType == "LTEC")
                {
                    iCounter = _client._plcConnection.ReadInt16("DB572.DBW 4.0");
                    _iValueWatchdogOld = iCounter;
                    //ServiceBaseX._logger.Log(Category.Info, _iValueWatchdogOld.ToString());
                }
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void InitPKID_RT()
        {
            try
            {
                if (_cmdInitPKID_RT != null)
                {
                    OracleDecimal[] PKIDs;
                    try
                    {
                        _cmdInitPKID_RT.Parameters[0].Size = 36;
                        _cmdInitPKID_RT.ExecuteNonQuery();
                        PKIDs = (OracleDecimal[])_cmdInitPKID_RT.Parameters[0].Value;

                        Place_PKID_RT = ParseOracleToInt(PKIDs);
                    }
                    catch (Exception ex)
                    {
                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                        DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void InitPKID_CY()
        {
            try
            {
                if (_cmdInitPKID_CY != null)
                {
                    OracleDecimal[] PKIDs;
                    try
                    {
                        _cmdInitPKID_CY.Parameters[0].Size = 18;
                        _cmdInitPKID_CY.ExecuteNonQuery();
                        PKIDs = (OracleDecimal[])_cmdInitPKID_CY.Parameters[0].Value;

                        Place_PKID_CY = ParseOracleToInt(PKIDs);
                    }
                    catch (Exception ex)
                    {
                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                        DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private int[] ParseOracleToInt(OracleDecimal[] values)
        {
            int[] PKIDs = new int[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                PKIDs[i] = (int)values[i];
            }

            return PKIDs;
        }

        private void InitOldLOST()
        {
            try
            {
                if (_cmdInitLOST != null)
                {
                    OracleString[] Interlock,
                                   Status;

                    int[] ArraySize = new int[36];
                    InitValues(ArraySize, 1);

                    try
                    {
                        _cmdInitLOST.Parameters[0].Size = 36;
                        _cmdInitLOST.Parameters[0].ArrayBindSize = ArraySize;
                        _cmdInitLOST.Parameters[1].Size = 36;
                        _cmdInitLOST.Parameters[1].ArrayBindSize = ArraySize;
                        _cmdInitLOST.ExecuteNonQuery();
                        Status = (OracleString[])_cmdInitLOST.Parameters[0].Value;
                        Interlock = (OracleString[])_cmdInitLOST.Parameters[1].Value;

                        Place_Status = ParseOracleToBool(Status);
                        Place_Interlock = ParseOracleToBool(Interlock);
                    }
                    catch (Exception ex)
                    {
                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                        DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void InitValues(int[] array, int Init)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Init;
            }
        }

        private bool[] ParseOracleToBool(OracleString[] values)
        {
            bool[] OldValues = new bool[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                if ((string)values[i].Value == "Y")
                    OldValues[i] = true;
                else if ((string)values[i].Value == "N")
                    OldValues[i] = false;
                else
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error while parsing Interlock/Status informationen.");
            }

            return OldValues;
        }

        private void InitTOPLoc()
        {
            try
            {
                string[] TOP_Locations = { "R7_4", "R7_5", "SKD2_1", "SDK2_2", "LR1", "SDK1_1", "SDK1_2" };
                int l = TOP_Locations.Length;

                for (int i = 0; i < l; i++)
                {
                    TOPLoc TL_act = new TOPLoc(),
                           TL_old = new TOPLoc();

                    TL_act.sPlace_ID = TOP_Locations[i];
                    TL_old.sPlace_ID = TOP_Locations[i];

                    _dic_TOPLocation_old.Add(i, TL_act);
                    _dic_TOPLocation_actual.Add(i, TL_old);

                    TL_act = null;
                    TL_old = null;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void InitRT()
        {
            try
            {
                //fill the rtdata objects with the specific place ids
                string[] RT_Locations = { "LR1", "R1_1", "R1_2", "CLT1_1", "R2_1", "R2_2", "R2_3", "SM", "R3_1", "R3_2", "CLT1_2", "R4_1", "R4_2", "R5_1", "R5_2", "R5_3", "R5_4", "SKD1_1", "SKD1_2", "SKD2_1", "SKD2_2", "CLT2_1", "CLT2_2", "CY1_1", "CY1_X", "TN", "R6_1", "R6_2", "LTR1", "CY2_1", "CY2_X", "R7_1", "R7_2", "R7_3", "R7_4", "R7_5" };
                int l = RT_Locations.Length;

                for (int i = 0; i < l; i++)
                {
                    RTData RTData_act = new RTData(),
                           RTData_old = new RTData();

                    RTData_act.sPlace_ID = RT_Locations[i];
                    RTData_old.sPlace_ID = RT_Locations[i];

                    _dic_Slablist_RT_old.Add(i, RTData_act);
                    _dic_Slablist_RT_actual.Add(i, RTData_old);

                    RTData_act = null;
                    RTData_old = null;
                }

                //fill the rtdata objects with the pkid of the specific places
                CompleteInitRT(Place_PKID_RT);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void InitLOST()
        {
            try
            {
                //fill the rtdata objects with the specific place ids
                string[] RT_Locations = { "LR1", "R1_1", "R1_2", "CLT1_1", "R2_1", "R2_2", "R2_3", "SM", "R3_1", "R3_2", "CLT1_2", "R4_1", "R4_2", "R5_1", "R5_2", "R5_3", "R5_4", "SKD1_1", "SKD1_2", "SKD2_1", "SKD2_2", "CLT2_1", "CLT2_2", "CY1_1", "CY1_X", "TN", "R6_1", "R6_2", "LTR1", "CY2_1", "CY2_X", "R7_1", "R7_2", "R7_3", "R7_4", "R7_5" };
                int l = RT_Locations.Length;

                for (int i = 0; i < l; i++)
                {
                    RTData RTData_act = new RTData(),
                           RTData_old = new RTData();

                    RTData_act.sPlace_ID = RT_Locations[i];
                    RTData_old.sPlace_ID = RT_Locations[i];

                    _dic_Lock_Status_old.Add(i, RTData_act);
                    _dic_Lock_Status_actual.Add(i, RTData_old);

                    RTData_act = null;
                    RTData_old = null;
                }

                //fill the rtdata objects with the pkid of the specific places
                CompleteInitLOST(Place_PKID_RT, Place_Interlock, Place_Status);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void CompleteInitRT(int[] PKIDs)
        {
            //string sPlace = String.Empty;
            try
            {
                foreach (KeyValuePair<int, RTData> rtd in _dic_Slablist_RT_actual)
                {
                    rtd.Value.iPKID = PKIDs[rtd.Key];
                    //sPlace = rtd.Value.sPlace_ID;
                    //string sSelect = "SELECT PKID FROM PLACE WHERE PLACE_ID = '" + sPlace + "'";
                    //_cmdSel.CommandText = sSelect;

                    //using (OracleDataReader dr = _cmdSel.ExecuteReader(CommandBehavior.Default))
                    //{
                    //    do
                    //    {
                    //        while (dr.Read())
                    //        {
                    //            int iPKID = dr.GetOracleDecimal(0).ToInt32();
                    //            rtd.Value.iPKID = iPKID;
                    //        }
                    //    } while (dr.NextResult());
                    //}
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on selecting Place PKID for RT Data: " + ex.Message);
            }
        }

        private void CompleteInitLOST(int[] PKIDs, bool[] Interlock, bool[] Status)
        {
            //string sPlace = String.Empty;
            try
            {
                foreach (KeyValuePair<int, RTData> rtd in _dic_Lock_Status_actual)
                {
                    rtd.Value.iPKID = PKIDs[rtd.Key];
                    //sPlace = rtd.Value.sPlace_ID;
                    //string sSelect = "SELECT PKID FROM PLACE WHERE PLACE_ID = '" + sPlace + "'";
                    //_cmdSel.CommandText = sSelect;

                    //using (OracleDataReader dr = _cmdSel.ExecuteReader(CommandBehavior.Default))
                    //{
                    //    do
                    //    {
                    //        while (dr.Read())
                    //        {
                    //            int iPKID = dr.GetOracleDecimal(0).ToInt32();
                    //            rtd.Value.iPKID = iPKID;
                    //        }
                    //    } while (dr.NextResult());
                    //}
                }

                foreach (KeyValuePair<int, RTData> rtd in _dic_Lock_Status_old)
                {
                    rtd.Value.bLock = Interlock[rtd.Key];
                    rtd.Value.bError = Status[rtd.Key];
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on selecting Place PKID for RT Data: " + ex.Message);
            }
        }

        private void InitCon()
        {
            try
            {
                string[] ConveyorPlaces = { "CY1_2", "CY1_3", "CY1_4", "CY1_5", "CY1_6", "CY1_7", "CY1_8", "CY1_9", "CY1_10", "CY2_2", "CY2_3", "CY2_4", "CY2_5", "CY2_6", "CY2_7", "CY2_8", "CY2_9", "CY2_10" };
                for (int i = 0; i < 18; i++)
                {
                    Slablist SL = new Slablist();

                    SL.sPlace_ID = ConveyorPlaces[i];

                    _dic_Slablist_Con_old.Add(i, SL);
                    _dic_Slablist_Con_actual.Add(i, SL);

                    SL = null;
                }
            
                //fill the rtdata objects with the pkid of the specific places
                CompleteInitCY(Place_PKID_CY);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void CompleteInitCY(int[] PKIDs)
        {
            //string sPlace = String.Empty;
            try
            {
                foreach (KeyValuePair<int, Slablist> sl in _dic_Slablist_Con_actual)
                {
                    sl.Value.iPKID = PKIDs[sl.Key];
                    //sPlace = sl.Value.sPlace_ID;
                    //string sSelect = "SELECT PKID FROM PLACE WHERE PLACE_ID = '" + sPlace + "'";
                    //_cmdSel.CommandText = sSelect;

                    //using (OracleDataReader dr = _cmdSel.ExecuteReader(CommandBehavior.Default))
                    //{
                    //    do
                    //    {
                    //        while (dr.Read())
                    //        {
                    //            int iPKID = dr.GetOracleDecimal(0).ToInt32();
                    //            sl.Value.iPKID = iPKID;
                    //        }
                    //    } while (dr.NextResult());
                    //}
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on selecting Place PKID for RT Data: " + ex.Message);
            }
        }

        private void ReadDBOPCValues(PlcDeviceConnection connection)
        {
            try
            {
                //read the included information of the OPC plc
                ReadTrackingMap(connection);
                ReadConveyor(connection);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void ReadDBLTECValues(PlcDeviceConnection connection)
        {
            try
            {
                //check if plc can be read
                if (CheckFlagDB())
                    //read the included information of the LTEC plc
                    ReadScarfOutput(connection);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        //procedure used to get the informations about take over point flags
        private void ReadTOPLoc(PlcDeviceConnection connection)
        {
            try
            {
                //ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + " starts reading.");
                DBObjects.Take_Out_Confirm Take_Out_Confirm = connection.Read<DBObjects.Take_Out_Confirm>();
                bool[] TOP_Loc = Take_Out_Confirm.ParseDBInformation();
                int iTOP = TOP_Loc.Length;

                for (int i = 0; i < iTOP; i++)
                {
                    _dic_TOPLocation_actual[i].bTOP = TOP_Loc[i];
                }

                if (CompareValues(_dic_TOPLocation_actual, _dic_TOPLocation_old))
                {
                    ChangeDictionary(_dic_TOPLocation_actual, _dic_TOPLocation_old);
                    UpdateDatabase(_dic_TOPLocation_actual);
                }

                Take_Out_Confirm = null;
            }
            catch (PlcException ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        //procedure used to check if there are differences between the old and the new values
        private bool CompareValues(Dictionary<int, TOPLoc> dictionary_act, Dictionary<int, TOPLoc> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    if (dictionary_act[i].bTOP != dictionary_old[i].bTOP)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }
        }

        //procedure used to swap the actual values of the db into the dictionary_old for further comparison
        private void ChangeDictionary(Dictionary<int, TOPLoc> dictionary_act, Dictionary<int, TOPLoc> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    dictionary_old[i].bTOP = dictionary_act[i].bTOP;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void UpdateDatabase(Dictionary<int, TOPLoc> dictionary_act)
        {
            try
            {
                string[] sPlace_ID = new string[7];
                bool[] bTOP = new bool[7];

                for (int i = 0; i < sPlace_ID.Length; i++)
                {
                    sPlace_ID[i] = dictionary_act[i].sPlace_ID;
                    bTOP[i] = dictionary_act[i].bTOP;
                }

                if (_cmdTriggerWO != null)
                {
                    OracleDecimal dec = -1;

                    try
                    {
                        _cmdTriggerWO.Parameters[0].Value = sPlace_ID;
                        _cmdTriggerWO.Parameters[1].Value = bTOP;

                        _cmdTriggerWO.ExecuteNonQuery();

                        dec = (OracleDecimal)_cmdTriggerWO.Parameters[2].Value;
                    }
                    catch (Exception ex)
                    {
                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                        DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void ReadLockStatus(PlcDeviceConnection connection)
        {
            try
            {
                //read the interlock and status information
                ReadInterlock(connection);
                ReadStatus(connection);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void ReadInterlock(PlcDeviceConnection connection)
        {
            try
            {
                //ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + " starts reading.");
                DBObjects.Interlock_Confirm Interlock_Confirm = connection.Read<DBObjects.Interlock_Confirm>();
                bool[] Interlock = Interlock_Confirm.ParseDBInformation();
                int iInterlock = Interlock.Length;

                for (int i = 0; i < iInterlock; i++)
                {
                    _dic_Lock_Status_actual[i].bLock = Interlock[i];
                }

                if (CompareValuesLock(_dic_Lock_Status_actual, _dic_Lock_Status_old))
                {
                    _dic_Lock_Status_update = FindChangedInterlock(_dic_Lock_Status_actual, _dic_Lock_Status_old);
                    ChangeDictionaryLock(_dic_Lock_Status_actual, _dic_Lock_Status_old);
                    UpdateDatabaseLock(_dic_Lock_Status_update);
                }

                Interlock_Confirm = null;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private bool CompareValuesLock(Dictionary<int, RTData> dictionary_act, Dictionary<int, RTData> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    if (dictionary_act[i].bLock != dictionary_old[i].bLock)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }
        }

        private Dictionary<int, RTData> FindChangedInterlock(Dictionary<int, RTData> dictionary_act, Dictionary<int, RTData> dictionary_old)
        {
            ServiceBaseX._logger.Log(Category.HLite, MethodBase.GetCurrentMethod().Name + " started.");
            _dic_Lock_Status_update.Clear();

            RTData RTData_update = new RTData();
            int cnt = 0;

            for (int i = 0; i < dictionary_act.Count; i++)
            {
                if (dictionary_act[i].bLock != dictionary_old[i].bLock)
                {
                    RTData_update = dictionary_act[i].ShallowCopy();
                    _dic_Lock_Status_update.Add(cnt, RTData_update);
                    cnt++;

                    //console output with every CHANGED value between old - actual dictionary
                    ServiceBaseX._logger.Log(Category.Info, RTData_update.sPlace_ID + ", " + RTData_update.bLock);
                }
            }

            return _dic_Lock_Status_update;
        }

        private void ChangeDictionaryLock(Dictionary<int, RTData> dictionary_act, Dictionary<int, RTData> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    dictionary_old[i].bLock = dictionary_act[i].bLock;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void UpdateDatabaseLock(Dictionary<int, RTData> dictionary_update)
        {
            try
            {
                bool bInterlock = false;
                int[] iPKID = new int[dictionary_update.Count];
                string[] sInterlock = new string[dictionary_update.Count];

                for (int i = 0; i < dictionary_update.Count; i++)
                {
                    iPKID[i] = dictionary_update[i].iPKID;
                    bInterlock = dictionary_update[i].bLock;
                    if (bInterlock == true)
                        sInterlock[i] = "Y";
                    else
                        sInterlock[i] = "N";
                }

                if (_cmdInterlockConfirm != null)
                {
                    OracleDecimal dec = -1;

                    try
                    {
                        _cmdInterlockConfirm.Parameters[0].Value = iPKID;
                        _cmdInterlockConfirm.Parameters[1].Value = sInterlock;

                        _cmdInterlockConfirm.ExecuteNonQuery();

                        dec = (OracleDecimal)_cmdInterlockConfirm.Parameters[2].Value;
                    }
                    catch (Exception ex)
                    {
                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                        DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void ReadStatus(PlcDeviceConnection connection)
        {
            try
            {
                //ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + " starts reading.");
                DBObjects.Status_Information Status_Information = connection.Read<DBObjects.Status_Information>();
                bool[] Status = Status_Information.ParseDBInformation();
                int iStatus = Status.Length;

                for (int i = 0; i < iStatus; i++)
                {
                    _dic_Lock_Status_actual[i].bError = Status[i];
                }

                if (CompareValuesStatus(_dic_Lock_Status_actual, _dic_Lock_Status_old))
                {
                    _dic_Lock_Status_update = FindChangedStatus(_dic_Lock_Status_actual, _dic_Lock_Status_old);
                    ChangeDictionaryStatus(_dic_Lock_Status_actual, _dic_Lock_Status_old);
                    UpdateDatabaseStatus(_dic_Lock_Status_update);
                }

                Status_Information = null;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private bool CompareValuesStatus(Dictionary<int, RTData> dictionary_act, Dictionary<int, RTData> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    if (dictionary_act[i].bError != dictionary_old[i].bError)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }
        }

        private Dictionary<int, RTData> FindChangedStatus(Dictionary<int, RTData> dictionary_act, Dictionary<int, RTData> dictionary_old)
        {
            ServiceBaseX._logger.Log(Category.HLite, MethodBase.GetCurrentMethod().Name + " started.");
            _dic_Lock_Status_update.Clear();

                RTData RTData_update = new RTData();
                int cnt = 0;

                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    if (dictionary_act[i].bError != dictionary_old[i].bError)
                    {
                        RTData_update = dictionary_act[i].ShallowCopy();
                        _dic_Lock_Status_update.Add(cnt, RTData_update);
                        cnt++;

                        //console output with every CHANGED value between old - actual dictionary
                        ServiceBaseX._logger.Log(Category.Info, RTData_update.sPlace_ID + ", " + RTData_update.bError);
                    }
                }

                return _dic_Lock_Status_update;
        }

        private void ChangeDictionaryStatus(Dictionary<int, RTData> dictionary_act, Dictionary<int, RTData> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    dictionary_old[i].bError = dictionary_act[i].bError;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void UpdateDatabaseStatus(Dictionary<int, RTData> dictionary_act)
        {
            try
            {
                bool bStatus = false;
                int[] iPKID = new int[36];
                string[] sStatus = new string[36];

                for (int i = 0; i < iPKID.Length; i++)
                {
                    iPKID[i] = dictionary_act[i].iPKID;
                    bStatus = dictionary_act[i].bError;
                    if (bStatus == true)
                        sStatus[i] = "Y";
                    else
                        sStatus[i] = "N";
                }

                if (_cmdStatusInformation != null)
                {
                    OracleDecimal dec = -1;

                    try
                    {
                        _cmdStatusInformation.Parameters[0].Value = iPKID;
                        _cmdStatusInformation.Parameters[1].Value = sStatus;

                        _cmdStatusInformation.ExecuteNonQuery();

                        dec = (OracleDecimal)_cmdStatusInformation.Parameters[2].Value;
                    }
                    catch (Exception ex)
                    {
                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                        DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        //procedure used to get the informations about the tracking map movement
        private void ReadTrackingMap(PlcDeviceConnection connection)
        {
            try
            {
                ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + " starts reading.");
                DBObjects.TrackingMap_Rollertable TrackingMap_Rollertable = connection.Read<DBObjects.TrackingMap_Rollertable>();
                string[] Mat_ID = TrackingMap_Rollertable.ParseDBInformation();
                string sMat_ID = string.Empty;
                int iMat = Mat_ID.Length;

                for (int i = 0; i < iMat; i++)
                {
                    _dic_Slablist_RT_actual[i].sMat_ID = Mat_ID[i];
                }

                if (CompareValues(_dic_Slablist_RT_actual, _dic_Slablist_RT_old))
                {
                    ChangeDictionary(_dic_Slablist_RT_actual, _dic_Slablist_RT_old);
                    UpdateDatabase(_dic_Slablist_RT_actual);
                }

                TrackingMap_Rollertable = null;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private bool CompareValues(Dictionary<int, RTData> dictionary_act, Dictionary<int, RTData> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    if (dictionary_act[i].sMat_ID != dictionary_old[i].sMat_ID)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }
        }

        private void ChangeDictionary(Dictionary<int, RTData> dictionary_act, Dictionary<int, RTData> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    dictionary_old[i].sMat_ID = dictionary_act[i].sMat_ID;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void UpdateDatabase(Dictionary<int, RTData> dictionary_act)
        {
            try
            {
                string[] sMat_ID = new string[36];
                int[] iPKID = new int[36];

                for (int i = 0; i < iPKID.Length; i++)
                {
                    iPKID[i] = dictionary_act[i].iPKID;
                    sMat_ID[i] = dictionary_act[i].sMat_ID;
                    ServiceBaseX._logger.Log(Category.Info, "Mat_ID: " + dictionary_act[i].sMat_ID + " Place_ID: " + dictionary_act[i].sPlace_ID);
                }

                if (_cmdTrackingMap != null)
                {
                    OracleDecimal dec = -1;

                    try
                    {
                        _cmdTrackingMap.Parameters[0].Value = iPKID;
                        _cmdTrackingMap.Parameters[1].Value = sMat_ID;

                        _cmdTrackingMap.ExecuteNonQuery();

                        dec = (OracleDecimal)_cmdTrackingMap.Parameters[2].Value;
                    }
                    catch (Exception ex)
                    {
                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                        DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        //procedure used to get informations about the conveyor
        private void ReadConveyor(PlcDeviceConnection connection)
        {
            try
            {
                //ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + " starts reading.");
                DBObjects.TrackingMap_Conveyor TrackingMap_Conveyor = connection.Read<DBObjects.TrackingMap_Conveyor>();
                string[] Mat_ID = TrackingMap_Conveyor.ParseDBInformation();
                int iMat = 18;
                
                for (int i = 0; i < iMat; i++)
                {
                    _dic_Slablist_Con_actual[i].sMat_ID = Mat_ID[i];
                }

                if (CompareValues(_dic_Slablist_Con_actual, _dic_Slablist_Con_old))
                {
                    ChangeDictionary(_dic_Slablist_Con_actual, _dic_Slablist_Con_old);
                    UpdateDatabase(_dic_Slablist_Con_actual);
                }

                TrackingMap_Conveyor = null;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private bool CompareValues(Dictionary<int, Slablist> dictionary_act, Dictionary<int, Slablist> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    if (dictionary_act[i].sMat_ID != dictionary_old[i].sMat_ID)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }
        }

        private void ChangeDictionary(Dictionary<int, Slablist> dictionary_act, Dictionary<int, Slablist> dictionary_old)
        {
            try
            {
                for (int i = 0; i < dictionary_act.Count; i++)
                {
                    dictionary_old[i].sMat_ID = dictionary_act[i].sMat_ID;
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void UpdateDatabase(Dictionary<int, Slablist> dictionary_act)
        {
            try
            {
                string[] sMat_ID = new string[18];
                int[] iPlace_PKID = new int[18];

                for (int i = 0; i < sMat_ID.Length; i++)
                {
                    sMat_ID[i] = dictionary_act[i].sMat_ID;
                    iPlace_PKID[i] = dictionary_act[i].iPKID;
                }

                if (_cmdConveyor != null)
                {
                    OracleDecimal dec = -1;

                    try
                    {
                        _cmdConveyor.Parameters[0].Value = sMat_ID;
                        _cmdConveyor.Parameters[1].Value = iPlace_PKID;

                        _cmdConveyor.ExecuteNonQuery();

                        dec = (OracleDecimal)_cmdConveyor.Parameters[2].Value;
                    }
                    catch (Exception ex)
                    {
                        ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                        DBCommand.CheckOracleConnection(ex.Message, _oraConnection, ref bDBConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private bool CheckFlagDB()
        {
            bool bOK = false;

            try
            {
                bOK = _client._plcConnection.ReadBoolean("DB572.DBX 2.0");
                //in case the DB was blocked, sleep and try again
                if (bOK == true)
                {
                    Thread.Sleep(200);
                    bOK = _client._plcConnection.ReadBoolean("DB572.DBX 2.0");
                }
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }

            return bOK;
        }

        private void ScarfOutputHandler(object sender, ValueChangedEventArgs<bool> e)
        {
            if (e.NewValue == true)
                ReadScarfOutput(_client._plcConnection);
        }
   
        private void ReadScarfOutput(PlcDeviceConnection connection)
        {
            try
            {
                DBObjects.Scarf_Output Scarf_Output = connection.Read<DBObjects.Scarf_Output>();
                ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": Values from LTEC read.");
                //recalculate the starttime for the DB
                Scarf_Output.ReworkValues();
                UpdateDatabase(Scarf_Output);
                ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": Values from LTEC updated to database.");

                Scarf_Output = null;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void UpdateDatabase(DBObjects.Scarf_Output Scarf_Output)
        {
            if (_cmdScarfOutput != null)
            {
                OracleDecimal dec = -1;

                try
                {
                    _cmdScarfOutput.Parameters[0].Value = Scarf_Output.Mat_ID;
                    _cmdScarfOutput.Parameters[1].Value = Scarf_Output.Oper_Mode_Code;
                    _cmdScarfOutput.Parameters[2].Value = Scarf_Output.Steel_Group_ID;
                    _cmdScarfOutput.Parameters[3].Value = Scarf_Output.Shift_Time;
                    _cmdScarfOutput.Parameters[4].Value = Scarf_Output.Shift_Group_ID;
                    _cmdScarfOutput.Parameters[5].Value = Scarf_Output.Stop_Code;
                    _cmdScarfOutput.Parameters[6].Value = Scarf_Output.Mat_Temp;
                    _cmdScarfOutput.Parameters[7].Value = Scarf_Output.StartTime;
                    _cmdScarfOutput.Parameters[8].Value = Scarf_Output.Cycle_Time;
                    _cmdScarfOutput.Parameters[9].Value = Scarf_Output.Preheat_Pos;
                    _cmdScarfOutput.Parameters[10].Value = Scarf_Output.Preheat_Time;
                    _cmdScarfOutput.Parameters[11].Value = Scarf_Output.Scarf_Speed;
                    _cmdScarfOutput.Parameters[12].Value = Scarf_Output.SO_SP_OST;
                    _cmdScarfOutput.Parameters[13].Value = Scarf_Output.SO_SP_OSB;
                    _cmdScarfOutput.Parameters[14].Value = Scarf_Output.SO_SP_OSTFX;
                    _cmdScarfOutput.Parameters[15].Value = Scarf_Output.SO_SP_OSTFL;
                    _cmdScarfOutput.Parameters[16].Value = Scarf_Output.SO_SP_OSBFX;
                    _cmdScarfOutput.Parameters[17].Value = Scarf_Output.SO_SP_OSBFL;
                    _cmdScarfOutput.Parameters[18].Value = Scarf_Output.SO_SP_OSTE;
                    _cmdScarfOutput.Parameters[19].Value = Scarf_Output.SO_SP_OSBE;
                    _cmdScarfOutput.Parameters[20].Value = Scarf_Output.Pattern_Code;
                    _cmdScarfOutput.Parameters[21].Value = Scarf_Output.Mat_Width;
                    _cmdScarfOutput.Parameters[22].Value = Scarf_Output.Mat_Thickness;
                    _cmdScarfOutput.Parameters[23].Value = Scarf_Output.Flow_Total_O2_Cycle;
                    _cmdScarfOutput.Parameters[24].Value = Scarf_Output.Flow_Total_FG_Cycle;
                    _cmdScarfOutput.Parameters[25].Value = Scarf_Output.Scarf_Depth_Top;
                    _cmdScarfOutput.Parameters[26].Value = Scarf_Output.Scarf_Depth_Bottom;
                    _cmdScarfOutput.Parameters[27].Value = Scarf_Output.Scarf_Depth_Top_Edge;
                    _cmdScarfOutput.Parameters[28].Value = Scarf_Output.Scarf_Depth_Bottom_Edge;
                    _cmdScarfOutput.Parameters[29].Value = Scarf_Output.Scarf_Removed_Weight;

                    _cmdScarfOutput.ExecuteNonQuery();

                    dec = (OracleDecimal)_cmdScarfOutput.Parameters[30].Value;
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }

        private void OnDBOPCTMCheck(object obj)
        {
            try
            {
                _checkDBOPCTM.Change(Timeout.Infinite, Timeout.Infinite);

                if (_client.bConnectionOK == true)
                    ReadDBOPCValues(_client._plcConnection);

                _checkDBOPCTM.Change(_iTimerTM, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void OnDBOPCTOP(object obj)
        {
            try
            {
                _checkDBOPCTOP.Change(Timeout.Infinite, Timeout.Infinite);

                if (_client.bConnectionOK == true)
                    ReadTOPLoc(_client._plcConnection);

                _checkDBOPCTOP.Change(_iTimerTOP, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void OnDBOPCLOST(object obj)
        {
            try
            {
                _checkDBOPCLOST.Change(Timeout.Infinite, Timeout.Infinite);

                if (_client.bConnectionOK == true)
                    ReadLockStatus(_client._plcConnection);

                _checkDBOPCLOST.Change(_iTimerLOST, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void OnDBLTEC(object obj)
        {
            try
            {
                _checkDBLTEC.Change(Timeout.Infinite, Timeout.Infinite);

                if (_client.bConnectionOK == true)
                    _client._plcConnection.ReadValues(DBObjects.plcbScarf_Output);

                _checkDBLTEC.Change(_iTimerLTEC, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
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
                    //ReconnectDB();
                }

                //enable timer to check database connection
                _checkDBTimer.Change(GlobalParameters._iTimerPeriod, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void OnWatchdog(object obj)
        {
            byte bCounter = 0;
            short iCounter = 0;

            try
            {
                //disable timer
                _checkWatchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);

                if (_sProcessType == "OPC")
                {
                    bCounter = _client._plcConnection.ReadByte("DB50.DBB 0");
                    _iValueWatchdogNew = (int)bCounter;
                }
                else if (_sProcessType == "LTEC")
                {
                    iCounter = _client._plcConnection.ReadInt16("DB572.DBW 4.0");
                    _iValueWatchdogNew = iCounter;
                    //ServiceBaseX._logger.Log(Category.Info, iCounter.ToString());
                }

                if (_iValueWatchdogNew > _iValueWatchdogOld + 3)
                {
                    ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": trying to reconnect to PLC.");
                    bPLCReconnect = _client.Reconnect();
                    if (bPLCReconnect == true)
                        ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": trying to reconnect to PLC successful.");
                }

                _iValueWatchdogOld = _iValueWatchdogNew;
                //enable timer to check database connection
                _checkWatchdogTimer.Change(_iTimerWatchdog, Timeout.Infinite);
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }
    }

    public class TOPLoc
    {
        public string sPlace_ID = string.Empty;
        public bool bTOP = false;
    }

    public class RTData
    {
        public string sMat_ID = string.Empty,
                      sPlace_ID = string.Empty;

        public int iPKID = 0;
        public bool bError = false,
                    bLock = false;

        public RTData ShallowCopy()
        {
            return (RTData)this.MemberwiseClone();
        }
    }

    public class Slablist
    {
        public string sMat_ID = string.Empty,
                      sPlace_ID = string.Empty;
        public int iPKID = 0;
        public bool bDelFlag = false;
    }
}
