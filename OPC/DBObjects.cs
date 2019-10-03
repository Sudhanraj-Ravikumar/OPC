using System;
using System.Reflection;
using System.Text;
using IPS7Lnk.Advanced;
using Utilities;
using OPC.Telegram;

namespace OPC.Objects
{
    public class DBObjects : PlcObject
    {
        //flag used to check if the LTEC output got updated
        public static PlcBoolean plcbScarf_Output = new PlcBoolean("DB572.DBX 2.0");

        private char[] ConvertChar(string sString, int iLaenge)
        {
            byte[] byteArray = new byte[iLaenge],
                   byteTmpArray;
            char[] charConvert = new char[iLaenge];

            try
            {
                byteTmpArray = Encoding.ASCII.GetBytes(sString);
                byteTmpArray.CopyTo(byteArray, 0);
                charConvert = Encoding.ASCII.GetChars(byteArray, 0, iLaenge);
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return null;
            }

            return Encoding.ASCII.GetChars(byteArray, 0, iLaenge);
        }

        private string ConvertString(char[] sChar, int iLaenge)
        {
            byte[] byteArray = new byte[iLaenge],
                   byteTmpArray;
            string sConvert = string.Empty;

            try
            {
                byteTmpArray = Encoding.ASCII.GetBytes(sChar);
                byteTmpArray.CopyTo(byteArray, 0);
                sConvert = Encoding.ASCII.GetString(byteArray, 0, iLaenge - 1).Trim();
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return null;
            }

            return Encoding.ASCII.GetString(byteArray, 0, iLaenge - 1).Trim();
        }

        #region OPC Input
        #region Lock Request
        public class Lock_Request : DBObjects
        {
            [PlcMember("DB51.DBX 28.0")]
            private bool LR1 = false;

            [PlcMember("DB51.DBX 28.1")]
            private bool R1_1 = false;

            [PlcMember("DB51.DBX 28.2")]
            private bool R1_2 = false;

            [PlcMember("DB51.DBX 28.3")]
            private bool CLT1_1 = false;

            [PlcMember("DB51.DBX 28.4")]
            private bool R2_1 = false;

            [PlcMember("DB51.DBX 28.5")]
            private bool R2_2 = false;

            [PlcMember("DB51.DBX 28.6")]
            private bool R2_3 = false;

            [PlcMember("DB51.DBX 28.7")]
            private bool SM = false;

            [PlcMember("DB51.DBX 29.0")]
            private bool R3_1 = false;

            [PlcMember("DB51.DBX 29.1")]
            private bool R3_2 = false;

            [PlcMember("DB51.DBX 29.2")]
            private bool CLT1_2 = false;

            [PlcMember("DB51.DBX 29.3")]
            private bool R4_1 = false;

            [PlcMember("DB51.DBX 29.4")]
            private bool R4_2 = false;

            [PlcMember("DB51.DBX 29.5")]
            private bool R5_1 = false;

            [PlcMember("DB51.DBX 29.6")]
            private bool R5_2 = false;

            [PlcMember("DB51.DBX 29.7")]
            private bool R5_3 = false;

            [PlcMember("DB51.DBX 30.0")]
            private bool R5_4 = false;

            [PlcMember("DB51.DBX 30.1")]
            private bool SKD1_1 = false;

            [PlcMember("DB51.DBX 30.2")]
            private bool SKD1_2 = false;

            [PlcMember("DB51.DBX 30.3")]
            private bool SKD2_1 = false;

            [PlcMember("DB51.DBX 30.4")]
            private bool SKD2_2 = false;

            [PlcMember("DB51.DBX 30.5")]
            private bool CLT2_1 = false;

            [PlcMember("DB51.DBX 30.6")]
            private bool CLT2_2 = false;

            [PlcMember("DB51.DBX 30.7")]
            private bool CY1 = false;

            [PlcMember("DB51.DBX 31.0")]
            private bool CY2 = false;

            [PlcMember("DB51.DBX 31.1")]
            private bool TN = false;

            [PlcMember("DB51.DBX 31.2")]
            private bool R6_1 = false;

            [PlcMember("DB51.DBX 31.3")]
            private bool R6_2 = false;

            [PlcMember("DB51.DBX 31.4")]
            private bool LTR1 = false;

            [PlcMember("DB51.DBX 31.5")]
            private bool R7_1 = false;

            [PlcMember("DB51.DBX 31.6")]
            private bool R7_2 = false;

            [PlcMember("DB51.DBX 31.7")]
            private bool R7_3 = false;

            [PlcMember("DB51.DBX 32.0")]
            private bool R7_4 = false;

            [PlcMember("DB51.DBX 32.1")]
            private bool R7_5 = false;
            
            //parse the string value to a bool value
            private bool[] ParseStringToBool(string[] Values)
            {
                bool[] bValues_new = new bool[34];

                try
                {
                    for (int i = 0; i < 34; i++)
                    {
                        if (Values[i] == "Y")
                            bValues_new[i] = true;
                        else if (Values[i] == "N")
                            bValues_new[i] = false;
                    }

                    return bValues_new;
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on parsing String to Bool: " + ex.Message);
                    return null;
                }
            }

            //parse the bool value to the specific datasets
            public void ParseDBInformation(string[] Values)
            {
                try
                {
                    int i = 0;
                    bool[] bValues = this.ParseStringToBool(Values);

                    this.LR1 = bValues[i];
                    //ServiceBaseX._logger.Log(Category.Info, "Interlock of Place LR1: " + bValues[i]);
                    this.R1_1 = bValues[++i];
                    this.R1_2 = bValues[++i];
                    this.CLT1_1 = bValues[++i];
                    this.R2_1 = bValues[++i];
                    this.R2_2 = bValues[++i];
                    this.R2_3 = bValues[++i];
                    this.SM = bValues[++i];
                    this.R3_1 = bValues[++i];
                    this.R3_2 = bValues[++i];
                    this.CLT1_2 = bValues[++i];
                    this.R4_1 = bValues[++i];
                    this.R4_2 = bValues[++i];
                    this.R5_1 = bValues[++i];
                    this.R5_2 = bValues[++i];
                    this.R5_3 = bValues[++i];
                    this.R5_4 = bValues[++i];
                    this.SKD1_1 = bValues[++i];
                    //ServiceBaseX._logger.Log(Category.Info, "Interlock of Place SKD1_1: " + bValues[i]);
                    this.SKD1_2 = bValues[++i];
                    //ServiceBaseX._logger.Log(Category.Info, "Interlock of Place SKD2_1: " + bValues[i]);
                    this.SKD2_1 = bValues[++i];
                    //ServiceBaseX._logger.Log(Category.Info, "Interlock of Place SKD1_2: " + bValues[i]);
                    this.SKD2_2 = bValues[++i];
                    //ServiceBaseX._logger.Log(Category.Info, "Interlock of Place SKD2_2: " + bValues[i]);
                    this.CLT2_1 = bValues[++i];
                    this.CLT2_2 = bValues[++i];
                    this.CY1 = bValues[++i];
                    this.CY2 = bValues[++i];
                    this.TN = bValues[++i];
                    this.R6_1 = bValues[++i];
                    this.R6_2 = bValues[++i];
                    this.LTR1 = bValues[++i];
                    this.R7_1 = bValues[++i];
                    this.R7_2 = bValues[++i];
                    this.R7_3 = bValues[++i];
                    this.R7_4 = bValues[++i];
                    this.R7_5 = bValues[++i];
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }
        #endregion
        
        #region Lift Drop
        public class Lift_Drop : DBObjects
        {
            [PlcMember("DB51.DBX 1.1")]
            public bool Lift = false;

            [PlcMember("DB51.DBX 1.2")]
            public bool Drop = false;

            [PlcMember("DB51.DBB 2", Length = 12)]
            private char[] Mat_ID = new char[12];

            [PlcMember("DB51.DBB 14", Length = 6)]
            private char[] Place = new char[6];

            [PlcMember("DB51.DBB 20", Length = 6)]
            private char[] Target = new char[6];

            public void ParseDBInformation(OPC_CTS_Lift_Drop obj)
            {
                try
                {
                    if (obj.OperateFlag == "L")
                        this.Lift = true;
                    else if (obj.OperateFlag == "D")
                        this.Drop = true;

                    this.Mat_ID = obj.Mat_ID.ToCharArray();
                    this.Place = obj.Place.ToCharArray();
                    this.Target = obj.Target.ToCharArray();
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }
        #endregion

        #region New Target
        public class New_Target : DBObjects
        {
            [PlcMember("DB51.DBB 2", Length = 12)]
            private char[] Mat_ID = new char[12];

            [PlcMember("DB51.DBB 20", Length = 6)]
            private char[] Target = new char[12];

            public void ParseDBInformation(OPC_New_Target obj)
            {
                try
                {
                    this.Mat_ID = obj.Mat_ID.ToCharArray();
                    this.Target = obj.Target.ToCharArray();
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }
        #endregion
        #endregion

        #region OPC Output
        #region Take Out Confirm
        public class Take_Out_Confirm : DBObjects
        {
            [PlcMember("DB50.DBX 1.0")]
            private bool R7_4 = false;

            [PlcMember("DB50.DBX 1.1")]
            private bool R7_5 = false;

            [PlcMember("DB50.DBX 1.2")]
            private bool SKD2_1 = false;

            [PlcMember("DB50.DBX 1.3")]
            private bool SKD2_2 = false;

            [PlcMember("DB50.DBX 1.4")]
            private bool LR1 = false;

            [PlcMember("DB50.DBX 1.5")]
            private bool SDK1_1 = false;

            [PlcMember("DB50.DBX 1.6")]
            private bool SDK1_2 = false;

            public bool[] ParseDBInformation()
            {
                bool[] bValues = new bool[7];
                try
                {
                    int i = 0;
                    foreach (var TakeOut in typeof(DBObjects.Take_Out_Confirm).GetFields())
                    {
                        bValues[i] = bool.Parse(TakeOut.GetValue(this).ToString());
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    return null;
                }

                return bValues;
            }
        }
        #endregion

        #region Interlock Confirm
        public class Interlock_Confirm : DBObjects
        {
            [PlcMember("DB50.DBX 20.6")]
            public bool LR1 = false;

            [PlcMember("DB50.DBX 38.6")]
            public bool R1_1 = false;

            [PlcMember("DB50.DBX 56.6")]
            public bool R1_2 = false;

            [PlcMember("DB50.DBX 74.6")]
            public bool CLT1_1 = false;

            [PlcMember("DB50.DBX 92.6")]
            public bool R2_1 = false;

            [PlcMember("DB50.DBX 110.6")]
            public bool R2_2 = false;

            [PlcMember("DB50.DBX 128.6")]
            public bool R2_3 = false;

            [PlcMember("DB50.DBX 146.6")]
            public bool SM = false;

            [PlcMember("DB50.DBX 164.6")]
            public bool R3_1 = false;

            [PlcMember("DB50.DBX 182.6")]
            public bool R3_2 = false;

            [PlcMember("DB50.DBX 200.6")]
            public bool CLT1_2 = false;

            [PlcMember("DB50.DBX 218.6")]
            public bool R4_1 = false;

            [PlcMember("DB50.DBX 236.6")]
            public bool R4_2 = false;

            [PlcMember("DB50.DBX 254.6")]
            public bool R5_1 = false;

            [PlcMember("DB50.DBX 272.6")]
            public bool R5_2 = false;

            [PlcMember("DB50.DBX 290.6")]
            public bool R5_3 = false;

            [PlcMember("DB50.DBX 308.6")]
            public bool R5_4 = false;

            [PlcMember("DB50.DBX 326.6")]
            public bool SKD1_1 = false;

            [PlcMember("DB50.DBX 344.6")]
            public bool SKD1_2 = false;

            [PlcMember("DB50.DBX 362.6")]
            public bool SKD2_1 = false;

            [PlcMember("DB50.DBX 380.6")]
            public bool SKD2_2 = false;

            [PlcMember("DB50.DBX 398.6")]
            public bool CLT2_1 = false;

            [PlcMember("DB50.DBX 416.6")]
            public bool CLT2_2 = false;

            [PlcMember("DB50.DBX 434.6")]
            public bool CY1_1 = false;

            [PlcMember("DB50.DBX 452.6")]
            public bool CY1_X = false;

            [PlcMember("DB50.DBX 470.6")]
            public bool TN = false;

            [PlcMember("DB50.DBX 488.6")]
            public bool R6_1 = false;

            [PlcMember("DB50.DBX 506.6")]
            public bool R6_2 = false;

            [PlcMember("DB50.DBX 524.6")]
            public bool LTR1 = false;

            [PlcMember("DB50.DBX 542.6")]
            public bool CY2_1 = false;

            [PlcMember("DB50.DBX 560.6")]
            public bool CY2_X = false;

            [PlcMember("DB50.DBX 578.6")]
            public bool R7_1 = false;

            [PlcMember("DB50.DBX 596.6")]
            public bool R7_2 = false;

            [PlcMember("DB50.DBX 614.6")]
            public bool R7_3 = false;

            [PlcMember("DB50.DBX 848.6")]
            public bool R7_4 = false;

            [PlcMember("DB50.DBX 866.6")]
            public bool R7_5 = false;

            public bool[] ParseDBInformation()
            {
                bool[] bInterlock = new bool[36];
                try
                {
                    int i = 0;

                    foreach (var Interlock in typeof(Interlock_Confirm).GetFields())
                    {
                        bInterlock[i] = bool.Parse(Interlock.GetValue(this).ToString());
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    return null;
                }

                return bInterlock;
            }
        }
        #endregion

        #region Status Information
        public class Status_Information : DBObjects
        {
            [PlcMember("DB50.DBX 20.5")]
            public bool LR1 = false;

            [PlcMember("DB50.DBX 38.5")]
            public bool R1_1 = false;

            [PlcMember("DB50.DBX 56.5")]
            public bool R1_2 = false;

            [PlcMember("DB50.DBX 74.5")]
            public bool CLT1_1 = false;

            [PlcMember("DB50.DBX 92.5")]
            public bool R2_1 = false;

            [PlcMember("DB50.DBX 110.5")]
            public bool R2_2 = false;

            [PlcMember("DB50.DBX 128.5")]
            public bool R2_3 = false;

            [PlcMember("DB50.DBX 146.5")]
            public bool SM = false;

            [PlcMember("DB50.DBX 164.5")]
            public bool R3_1 = false;

            [PlcMember("DB50.DBX 182.5")]
            public bool R3_2 = false;

            [PlcMember("DB50.DBX 200.5")]
            public bool CLT1_2 = false;

            [PlcMember("DB50.DBX 218.5")]
            public bool R4_1 = false;

            [PlcMember("DB50.DBX 236.5")]
            public bool R4_2 = false;

            [PlcMember("DB50.DBX 254.5")]
            public bool R5_1 = false;

            [PlcMember("DB50.DBX 272.5")]
            public bool R5_2 = false;

            [PlcMember("DB50.DBX 290.5")]
            public bool R5_3 = false;

            [PlcMember("DB50.DBX 308.5")]
            public bool R5_4 = false;

            [PlcMember("DB50.DBX 326.5")]
            public bool SKD1_1 = false;

            [PlcMember("DB50.DBX 344.5")]
            public bool SKD1_2 = false;

            [PlcMember("DB50.DBX 362.5")]
            public bool SKD2_1 = false;

            [PlcMember("DB50.DBX 380.5")]
            public bool SKD2_2 = false;

            [PlcMember("DB50.DBX 398.5")]
            public bool CLT2_1 = false;

            [PlcMember("DB50.DBX 416.5")]
            public bool CLT2_2 = false;

            [PlcMember("DB50.DBX 434.5")]
            public bool CY1_1 = false;

            [PlcMember("DB50.DBX 452.5")]
            public bool CY1_X = false;

            [PlcMember("DB50.DBX 470.5")]
            public bool TN = false;

            [PlcMember("DB50.DBX 488.5")]
            public bool R6_1 = false;

            [PlcMember("DB50.DBX 506.5")]
            public bool R6_2 = false;

            [PlcMember("DB50.DBX 524.5")]
            public bool LTR1 = false;

            [PlcMember("DB50.DBX 542.5")]
            public bool CY2_1 = false;

            [PlcMember("DB50.DBX 560.5")]
            public bool CY2_X = false;

            [PlcMember("DB50.DBX 578.5")]
            public bool R7_1 = false;

            [PlcMember("DB50.DBX 596.5")]
            public bool PT1_1 = false;

            [PlcMember("DB50.DBX 614.5")]
            public bool PT1_2 = false;

            public bool[] ParseDBInformation()
            {
                bool[] bStatus = new bool[34];
                try
                {
                    int i = 0;

                    foreach (var Status in typeof(DBObjects.Status_Information).GetFields())
                    {
                        bStatus[i] = bool.Parse(Status.GetValue(this).ToString());
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    return null;
                }

                return bStatus;
            }
        }
        #endregion

        #region TrackingMap Rollertable
        public class TrackingMap_Rollertable : DBObjects
        {
            [PlcMember("DB50.DBB 4.0", Length = 12)]
            public char[] LR1 = new char[12];

            [PlcMember("DB50.DBB 22.0", Length = 12)]
            public char[] R1_1 = new char[12];

            [PlcMember("DB50.DBB 40.0", Length = 12)]
            public char[] R1_2 = new char[12];

            [PlcMember("DB50.DBB 58.0", Length = 12)]
            public char[] CLT1_1 = new char[12];

            [PlcMember("DB50.DBB 76.0", Length = 12)]
            public char[] R2_1 = new char[12];

            [PlcMember("DB50.DBB 94.0", Length = 12)]
            public char[] R2_2 = new char[12];

            [PlcMember("DB50.DBB 112.0", Length = 12)]
            public char[] R2_3 = new char[12];

            [PlcMember("DB50.DBB 130.0", Length = 12)]
            public char[] SM = new char[12];

            [PlcMember("DB50.DBB 148.0", Length = 12)]
            public char[] R3_1 = new char[12];

            [PlcMember("DB50.DBB 166.0", Length = 12)]
            public char[] R3_2 = new char[12];

            [PlcMember("DB50.DBB 184.0", Length = 12)]
            public char[] CLT1_2 = new char[12];

            [PlcMember("DB50.DBB 202.0", Length = 12)]
            public char[] R4_1 = new char[12];

            [PlcMember("DB50.DBB 220.0", Length = 12)]
            public char[] R4_2 = new char[12];

            [PlcMember("DB50.DBB 238.0", Length = 12)]
            public char[] R5_1 = new char[12];

            [PlcMember("DB50.DBB 256.0", Length = 12)]
            public char[] R5_2 = new char[12];

            [PlcMember("DB50.DBB 274.0", Length = 12)]
            public char[] R5_3 = new char[12];

            [PlcMember("DB50.DBB 292.0", Length = 12)]
            public char[] R5_4 = new char[12];

            [PlcMember("DB50.DBB 310.0", Length = 12)]
            public char[] SKD1_1 = new char[12];

            [PlcMember("DB50.DBB 328.0", Length = 12)]
            public char[] SKD1_2 = new char[12];

            [PlcMember("DB50.DBB 346.0", Length = 12)]
            public char[] SKD2_1 = new char[12];

            [PlcMember("DB50.DBB 364.0", Length = 12)]
            public char[] SKD2_2 = new char[12];

            [PlcMember("DB50.DBB 382.0", Length = 12)]
            public char[] CLT2_1 = new char[12];

            [PlcMember("DB50.DBB 400.0", Length = 12)]
            public char[] CLT2_2 = new char[12];

            [PlcMember("DB50.DBB 418.0", Length = 12)]
            public char[] CY1_1 = new char[12];

            [PlcMember("DB50.DBB 436.0", Length = 12)]
            public char[] CY1_X = new char[12];

            [PlcMember("DB50.DBB 454.0", Length = 12)]
            public char[] TN = new char[12];

            [PlcMember("DB50.DBB 472.0", Length = 12)]
            public char[] R6_1 = new char[12];

            [PlcMember("DB50.DBB 490.0", Length = 12)]
            public char[] R6_2 = new char[12];

            [PlcMember("DB50.DBB 508.0", Length = 12)]
            public char[] LRT1 = new char[12];

            [PlcMember("DB50.DBB 526.0", Length = 12)]
            public char[] CY2_1 = new char[12];

            [PlcMember("DB50.DBB 544.0", Length = 12)]
            public char[] CY2_2 = new char[12];

            [PlcMember("DB50.DBB 562.0", Length = 12)]
            public char[] R7_1 = new char[12];

            [PlcMember("DB50.DBB 580.0", Length = 12)]
            public char[] R7_2 = new char[12];

            [PlcMember("DB50.DBB 598.0", Length = 12)]
            public char[] R7_3 = new char[12];

            [PlcMember("DB50.DBB 832.0", Length = 12)]
            public char[] R7_4 = new char[12];

            [PlcMember("DB50.DBB 850.0", Length = 12)]
            public char[] R7_5 = new char[12];

            public string[] ParseDBInformation()
            {
                string[] Mat_ID = new string[36];

                try
                {
                    int i = 0;
                    foreach (var Material in typeof(TrackingMap_Rollertable).GetFields())
                    {
                        string sConvert = new string((char[])Material.GetValue(this)).Trim();
                        Mat_ID[i] = sConvert;
                        i++;

                        sConvert = null;
                    }
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    return null;
                }

                return Mat_ID;
            }
        }
        #endregion

        #region TrackingMap Conveyor
        public class TrackingMap_Conveyor : DBObjects
        {
            [PlcMember("DB50.DBB 616.0", Length = 12)]
            public char[] CY1_2 = new char[12];

            [PlcMember("DB50.DBB 628.0", Length = 12)]
            public char[] CY1_3 = new char[12];

            [PlcMember("DB50.DBB 640.0", Length = 12)]
            public char[] CY1_4 = new char[12];

            [PlcMember("DB50.DBB 652.0", Length = 12)]
            public char[] CY1_5 = new char[12];

            [PlcMember("DB50.DBB 664.0", Length = 12)]
            public char[] CY1_6 = new char[12];

            [PlcMember("DB50.DBB 676.0", Length = 12)]
            public char[] CY1_7 = new char[12];

            [PlcMember("DB50.DBB 688.0", Length = 12)]
            public char[] CY1_8 = new char[12];

            [PlcMember("DB50.DBB 700.0", Length = 12)]
            public char[] CY1_9 = new char[12];

            [PlcMember("DB50.DBB 712.0", Length = 12)]
            public char[] CY1_10 = new char[12];

            [PlcMember("DB50.DBB 724.0", Length = 12)]
            public char[] CY2_2 = new char[12];

            [PlcMember("DB50.DBB 736.0", Length = 12)]
            public char[] CY2_3 = new char[12];

            [PlcMember("DB50.DBB 748.0", Length = 12)]
            public char[] CY2_4 = new char[12];

            [PlcMember("DB50.DBB 760.0", Length = 12)]
            public char[] CY2_5 = new char[12];

            [PlcMember("DB50.DBB 772.0", Length = 12)]
            public char[] CY2_6 = new char[12];

            [PlcMember("DB50.DBB 784.0", Length = 12)]
            public char[] CY2_7 = new char[12];

            [PlcMember("DB50.DBB 796.0", Length = 12)]
            public char[] CY2_8 = new char[12];

            [PlcMember("DB50.DBB 808.0", Length = 12)]
            public char[] CY2_9 = new char[12];

            [PlcMember("DB50.DBB 816.0", Length = 12)]
            public char[] CY2_10 = new char[12];

            public string[] ParseDBInformation()
            {
                string[] Mat_ID = new string[18];

                try
                {
                    int i = 0;
                    foreach (var Material in typeof(DBObjects.TrackingMap_Conveyor).GetFields())
                    {
                        string sConvert = new string((char[])Material.GetValue(this));
                        Mat_ID[i] = sConvert;
                        i++;

                        sConvert = null;
                    }
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    return null;
                }

                return Mat_ID;
            }
        }
        #endregion
        #endregion

        #region LTEC
        #region Scarf Input
        public class Scarf_Input : DBObjects
        {
            [PlcMember("DB562.DBB 6.0", Length = 14)]
            public char[] Mat_ID = new char[14];

            [PlcMember("DB562.DBB 20.0", Length = 22)]
            public char[] Mat_Grade = new char[22];

            [PlcMember("DB562.DBB 42.0", Length = 2)]
            public char[] Steel_Group_ID = new char[2];

            [PlcMember("DB562.DBW 46.0")]
            public short Mat_Length = 0;

            [PlcMember("DB562.DBW 48.0")]
            public short Mat_Thickness = 0;

            [PlcMember("DB562.DBW 50.0")]
            public short Mat_Width_Head = 0;

            [PlcMember("DB562.DBW 52.0")]
            public short Mat_Width_Tail = 0;

            [PlcMember("DB562.DBW 54.0")]
            public short Preheat_Pos = 0;

            [PlcMember("DB562.DBW 56.0")]
            public short Oper_Mode_Code = 0;

            [PlcMember("DB562.DBW 60.0")]
            public short Pattern_Code = 0;

            [PlcMember("DB562.DBD 62.0")]
            public float Pattern_Depth_Base = 0;

            [PlcMember("DB562.DBD 66.0")]
            public float Pattern_Depth_Heavy = 0;

            //[PlcMember("DB562.DBD 74.0")]
            //public float Depth_Top = 0;

            //[PlcMember("DB562.DBD 78.0")]
            //public float Depth_Bottom = 0;

            //[PlcMember("DB562.DBD 82.0")]
            //public float Depth_Top_Fixed = 0;

            //[PlcMember("DB562.DBD 86.0")]
            //public float Depth_Top_Float = 0;

            //[PlcMember("DB562.DBD 90.0")]
            //public float Depth_Bottom_Fixed = 0;

            //[PlcMember("DB562.DBD 94.0")]
            //public float Depth_Bottom_Float = 0;

            //[PlcMember("DB562.DBD 98.0")]
            //public float Depth_Top_Edge = 0;

            //[PlcMember("DB562.DBD 102.0")]
            //public float Depth_Bottom_Edge = 0;

            //[PlcMember("DB562.DBW 110.0")]
            //public short Preheat_Time = 0;

            //[PlcMember("DB562.DBD 112.0")]
            //public float Scarf_Speed = 0;

            //[PlcMember("DB562.DBD 116.0")]
            //public float OST = 0;

            //[PlcMember("DB562.DBD 120.0")]
            //public float OSB = 0;

            //[PlcMember("DB562.DBD 124.0")]
            //public float OSTFX = 0;

            //[PlcMember("DB562.DBD 128.0")]
            //public float OSTFL = 0;

            //[PlcMember("DB562.DBD 132.0")]
            //public float OSBFX = 0;

            //[PlcMember("DB562.DBD 136.0")]
            //public float OSBFL = 0;

            //[PlcMember("DB562.DBD 140.0")]
            //public float OSTE = 0;

            //[PlcMember("DB562.DBD 144.0")]
            //public float OSBE = 0;

            public void ParseDBInformation(LTEC_Input obj)
            {
                try
                {
                    this.Mat_ID = obj.Mat_ID.ToCharArray();
                    this.Mat_Grade = obj.Mat_Grade.ToCharArray();
                    this.Steel_Group_ID = obj.Steel_Group_ID.ToCharArray();
                    this.Mat_Length = obj.Mat_Length;
                    this.Mat_Thickness = obj.Mat_Thickness;
                    this.Mat_Width_Head = obj.Mat_Width_Head;
                    this.Mat_Width_Tail = obj.Mat_Width_Tail;
                    this.Preheat_Pos = obj.Preheat_Pos;
                    this.Oper_Mode_Code = obj.Oper_Mode_Code;
                    this.Pattern_Code = obj.Pattern_Code;
                    this.Pattern_Depth_Base = obj.Pattern_Depth_Base;
                    this.Pattern_Depth_Heavy = obj.Pattern_Depth_Heavy;
                    //this.Depth_Top = obj.Depth_Top;
                    //this.Depth_Bottom = obj.Depth_Bottom;
                    //this.Depth_Top_Fixed = obj.Depth_Top_Fixed;
                    //this.Depth_Top_Float = obj.Depth_Top_Float;
                    //this.Depth_Bottom_Fixed = obj.Depth_Bottom_Fixed;
                    //this.Depth_Bottom_Float = obj.Depth_Bottom_Float;
                    //this.Depth_Top_Edge = obj.Depth_Top_Edge;
                    //this.Depth_Bottom_Edge = obj.Depth_Bottom_Edge;
                    //this.Preheat_Time = obj.Preheat_Time;
                    //this.Scarf_Speed = obj.Scarf_Speed;
                    //this.OST = obj.OST;
                    //this.OSB = obj.OSB;
                    //this.OSTFX = obj.OSTFX;
                    //this.OSTFL = obj.OSTFL;
                    //this.OSBFX = obj.OSBFX;
                    //this.OSBFL = obj.OSBFL;
                    //this.OSTE = obj.OSTE;
                    //this.OSBE = obj.OSBE;
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }
        #endregion

        #region Scarf Output
        public class Scarf_Output : DBObjects
        {
            [PlcMember("DB572.DBB 10.0", Length = 14)]
            private char[] cMat_ID = new char[14];

            [PlcMember("DB572.DBW 24.0")]
            public short Oper_Mode_Code = 0;

            [PlcMember("DB572.DBB 26.0", Length = 2)]
            public char[] Steel_Group_ID = new char[2];

            [PlcMember("DB572.DBX 28.0")]
            public char Shift_Time;

            [PlcMember("DB572.DBX 29.0")]
            public char Shift_Group_ID;

            [PlcMember("DB572.DBW 34.0")]
            public short Stop_Code = 0;

            [PlcMember("DB572.DBW 36.0")]
            public short Mat_Temp = 0;

            [PlcMember("DB572.DBW 38.0")]
            private short dt_Year = 0;

            [PlcMember("DB572.DBW 40.0")]
            private short dt_Month = 0;

            [PlcMember("DB572.DBW 42.0")]
            private short dt_Day = 0;

            [PlcMember("DB572.DBW 44.0")]
            private short dt_Hour = 0;

            [PlcMember("DB572.DBW 46.0")]
            private short dt_Min = 0;

            [PlcMember("DB572.DBW 48.0")]
            private short dt_Sec = 0;

            [PlcMember("DB572.DBW 50.0")]
            public short Cycle_Time = 0;

            [PlcMember("DB572.DBW 52.0")]
            public short Preheat_Pos = 0;

            [PlcMember("DB572.DBW 54.0")]
            public short Preheat_Time = 0;

            [PlcMember("DB572.DBD 56.0")]
            public float Scarf_Speed = 0;

            [PlcMember("DB572.DBD 60.0")]
            public float SO_SP_OST = 0;

            [PlcMember("DB572.DBD 64.0")]
            public float SO_SP_OSB = 0;

            [PlcMember("DB572.DBD 68.0")]
            public float SO_SP_OSTFX = 0;

            [PlcMember("DB572.DBD 72.0")]
            public float SO_SP_OSTFL = 0;

            [PlcMember("DB572.DBD 76.0")]
            public float SO_SP_OSBFX = 0;

            [PlcMember("DB572.DBD 80.0")]
            public float SO_SP_OSBFL = 0;

            [PlcMember("DB572.DBD 84.0")]
            public float SO_SP_OSTE = 0;

            [PlcMember("DB572.DBD 88.0")]
            public float SO_SP_OSBE = 0;

            [PlcMember("DB572.DBD 96.0")]
            public float Pattern_Code = 0;

            [PlcMember("DB572.DBD 100.0")]
            public float Mat_Width = 0;

            [PlcMember("DB572.DBD 104.0")]
            public float Mat_Thickness = 0;

            [PlcMember("DB572.DBD 108.0")]
            public float Flow_Total_O2_Cycle = 0;

            [PlcMember("DB572.DBD 112.0")]
            public float Flow_Total_FG_Cycle = 0;

            [PlcMember("DB572.DBD 120.0")]
            public float Scarf_Depth_Top = 0;

            [PlcMember("DB572.DBD 124.0")]
            public float Scarf_Depth_Bottom = 0;

            [PlcMember("DB572.DBD 128.0")]
            public float Scarf_Depth_Top_Edge = 0;

            [PlcMember("DB572.DBD 132.0")]
            public float Scarf_Depth_Bottom_Edge = 0;
            
            [PlcMember("DB572.DBD 140.0")]
            public float Scarf_Removed_Weight = 0;

            public string Mat_ID = string.Empty,
                          StartTime = string.Empty;
            
            public void ReworkValues()
            {
                try
                {
                    this.Mat_ID = this.ConvertString(this.cMat_ID, 14);
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error while reworking 'Mat_ID'-Value: " + ex.Message);
                }

                try
                {
                    this.StartTime = this.dt_Year.ToString().PadLeft(4, '0') + this.dt_Month.ToString().PadLeft(2, '0') + this.dt_Day.ToString().PadLeft(2, '0') + this.dt_Hour.ToString().PadLeft(2, '0') + this.dt_Min.ToString().PadLeft(2, '0') + this.dt_Sec.ToString().PadLeft(2, '0');
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error while reworking 'StartTime'-Value: " + ex.Message);
                }
            }
        }
        #endregion
        #endregion
    }
}
