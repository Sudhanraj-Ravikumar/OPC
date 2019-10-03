using System;
using System.Configuration;
using System.Text;
using Utilities;
using Globals;
using System.Reflection;


namespace OPC.Telegram
{
    // this enum contains all the possible telegram types used for further processing
    // not actually used in this program
    #region Telegram Types
    public enum TelegramType
    {
        Undef = 0,
        #region OPC
        Lock_Request = 1,
        CTS_Lift_Drop = 2,
        New_Target = 3,
        #endregion

        #region LTEC
        New_Slab_Scarfing
        #endregion
    }
    #endregion

    //conclusion:
    // TelegramHeader class - this class is used for creating a telegram header (lifesignals and acknowledge messages)
    //                        and for parsing the headers of received PES telegrams to get the contained informations about type and length etc.
    #region TelegramHeader
    public class TelegramHeader
    {
        public int _iTelLength = 0;

        public string _sMessageID = string.Empty,
                      _sSendDC = string.Empty,
                      _sRecDC = string.Empty,
                      _sFuncC = string.Empty;

        public UInt16 Day,
                      Month,
                      Year,
                      Hour,
                      Minute,
                      Second;

        private string _sTelegram;

        public string Telegram
        {
            set { _sTelegram = value; ParseTelegram(_sTelegram); }
            get { return (_sTelegram); }
        }

        public TelegramHeader Shallowcopy()
        {
            TelegramHeader th = (TelegramHeader)this.MemberwiseClone();
            return th;
        }

        //this method parses the containing informations of the received PES telegrams
        public void ParseTelegram(string sTelegram)
        {
            string sTeleHeader = sTelegram.Substring(0, GlobalParameters._iHeaderLength);

            this._iTelLength = Int32.Parse(sTeleHeader.Substring(0, 4));
            this._sMessageID = sTeleHeader.Substring(4, 6);
            this._sSendDC = sTeleHeader.Substring(24, 2);
            this._sRecDC = sTeleHeader.Substring(26, 2);
            this._sFuncC = sTeleHeader.Substring(28, 1);
        }

        //this method creates a DateTime once a new telegram was received, used by database as history informations
        public string HeaderDateTime(ref TelegramHeader th)
        {
            DateTime dt = DateTime.Now;
            th.Day = (ushort)dt.Day;
            th.Month = (ushort)dt.Month;
            th.Year = (ushort)dt.Year;
            th.Hour = (ushort)dt.Hour;
            th.Minute = (ushort)dt.Minute;
            th.Second = (ushort)dt.Second;

            return (string.Format("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", th.Year, th.Month, th.Day, th.Hour, th.Minute, th.Second));
        }

        //this method checks the incoming buffer for telegrams - as long as the buffer can contain a whole telegram
        //character oriented method
        public static int getNextTelegram(ref string sTeleBuffer, string LineFeed)
        {
            //search for next telegram and return length
            int iTeleIndex = sTeleBuffer.IndexOf(LineFeed);

            string sTeleBuffer_temp = string.Empty;

            if (iTeleIndex > 0)
                sTeleBuffer_temp = sTeleBuffer.Substring(0, iTeleIndex);

            int iLengthTeleBuffer = sTeleBuffer.Length;

            //check telegram length
            int iTeleLength = 0;

            //check if telegram length is an integer
            bool bInt = Int32.TryParse(sTeleBuffer_temp.Substring(0, 4), out iTeleLength);

            if (bInt == false)
                return -1;

            if (iLengthTeleBuffer >= iTeleLength)
                return iTeleLength;
            else
                return -1;
        }

        //this method checks the incoming buffer for telegrams - as long as the buffer can contain a whole telegram
        //byte oriented method
        public static int getNextTelegram(ref byte[] TlgContent, int iStartIndex, byte LineFeed)
        {
            //check telegram length
            int iTeleLength = 0;

            //check if telegram length is an integer
            bool bInt = Int32.TryParse(Encoding.UTF8.GetString(TlgContent, iStartIndex, 4), out iTeleLength);

            if (bInt == false)
                return -1;
            try
            {
                //check if telegram ending sign is correct
                if (TlgContent[iTeleLength - 1 + iStartIndex] == LineFeed)
                    return iTeleLength;
                else
                    return -1;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": Buffer: " + TlgContent.Length + " Tellength: " + iTeleLength + ", Expection: " + ex.Message);
                return -1;
            }
        }
        #region Telegram_Acknowledge
        //this method creates a telegramheader for the lifesignals/ack/nack messages being send by the commserver
        public string GetTeleAck(int iAck, string Exception, string sMessageID)
        {
            string sAck = string.Empty,
                   sAckLen = string.Empty,
                   sDoS = string.Empty,
                   sToS = string.Empty,
                   ex = string.Empty;

            sAckLen = "0110";
            _sMessageID = sMessageID;
            _sSendDC = ConfigurationManager.AppSettings["Sender.Name"];
            _sRecDC = ConfigurationManager.AppSettings["Receiver.Name"];
            sDoS = DateTime.Now.ToString("yyyyMMdd");
            sToS = DateTime.Now.ToString("HHmmss");

            if (iAck == 1)
            {
                _sFuncC = "A";
                sAck = sAckLen + _sMessageID + sDoS + sToS + _sSendDC + _sRecDC + _sFuncC + ex.PadRight(80, ' ') + "\n";
            }
            else if (iAck == 0)
            {
                _sFuncC = "B";
                ex = Exception;
                sAck = sAckLen + _sMessageID + sDoS + sToS + _sSendDC + _sRecDC + _sFuncC + ex.PadRight(80, ' ') + "\n";
            }
            else
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": wrong Parameter for the sFuncC: {0}", iAck);

            return (sAck);
        }
        #endregion
    }
    #endregion

    #region OPC Telegrams
    #region New Lock Request
    public class OPC_New_Lock_Request
    {
        public string[] Lock = new string[34];
        private int Index = 0;

        public void ParseTelegraminformation(string sTeleBuffer)
        {
            try
            {
                string sTeleBody = sTeleBuffer.Remove(0, GlobalParameters._iHeaderLength);

                byte[] byteTeleBody = Encoding.UTF8.GetBytes(sTeleBody);

                for (int i = 0; i < 34; i++)
                {
                    int check = 0;
                    int laenge = byteTeleBody.Length - Index;
                    string sLock = string.Empty;

                    if (laenge > 1)
                    {
                        sLock = Encoding.UTF8.GetString(byteTeleBody, Index, 1);
                    
                        Index = Index + 1;
                        check = sLock.Length;

                        if (check > 0)
                            Lock[i] = sLock;

                    }
                    else
                        break;
                }
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on parsing telegraminformations: " + ex.Message);
            }
        }
    }
	#endregion

    #region CTS Lift/Drop
    public class OPC_CTS_Lift_Drop
    {
        public string OperateFlag = string.Empty,
                      Mat_ID = string.Empty,
                      Place = string.Empty,
                      Target = string.Empty;

        public void ParseTelegraminformation(string sTeleBuffer)
        {
            try
            {
                string sTeleBody = sTeleBuffer.Remove(0, GlobalParameters._iHeaderLength);

                byte[] byteTeleBody = Encoding.UTF8.GetBytes(sTeleBody);

                this.OperateFlag = Encoding.UTF8.GetString(byteTeleBody, 0, 1);
                this.Mat_ID = Encoding.UTF8.GetString(byteTeleBody, 1, 12);
                this.Place = Encoding.UTF8.GetString(byteTeleBody, 21,6);
                this.Target = Encoding.UTF8.GetString(byteTeleBody, 41, 6);
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on parsing telegraminformations: " + ex.Message);
            }
        }
    }
	#endregion

    #region New Target
    public class OPC_New_Target
    {
        public string Mat_ID = string.Empty,
                      Target = string.Empty;

        public void ParseTelegraminformation(string sTeleBuffer)
        {
            try
            {
                string sTeleBody = sTeleBuffer.Remove(0, GlobalParameters._iHeaderLength);

                byte[] byteTeleBody = Encoding.UTF8.GetBytes(sTeleBody);

                this.Mat_ID = Encoding.UTF8.GetString(byteTeleBody, 0, 12);
                this.Target = Encoding.UTF8.GetString(byteTeleBody, 20, 6);
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on parsing telegraminformations: " + ex.Message);
            }
        }
    }
	#endregion
	#endregion

    #region LTEC Telegram
    public class LTEC_Input
    {
        public string Mat_ID = string.Empty,
                      Mat_Grade = string.Empty,
                      Steel_Group_ID = string.Empty;

        public short Mat_Length = 0,
                     Mat_Thickness = 0,
                     Mat_Width_Head = 0,
                     Mat_Width_Tail = 0,
                     Preheat_Pos = 0,
                     Oper_Mode_Code = 0,
                     Pattern_Code = 0,
                     Preheat_Time = 0;

        public float Pattern_Depth_Base = 0,
                     Pattern_Depth_Heavy = 0,
                     Depth_Top = 0,
                     Depth_Bottom = 0,
                     Depth_Top_Fixed = 0,
                     Depth_Top_Float = 0,
                     Depth_Bottom_Fixed = 0,
                     Depth_Bottom_Float = 0,
                     Depth_Top_Edge = 0,
                     Depth_Bottom_Edge = 0,
                     Scarf_Speed = 0,
                     OST = 0,
                     OSB = 0,
                     OSTFX = 0,
                     OSTFL = 0,
                     OSBFX = 0,
                     OSBFL = 0,
                     OSTE = 0,
                     OSBE = 0;

        public void ParseTelegramBody(string sTeleBuffer)
        {
            try
            {
                string sTeleBody = sTeleBuffer.Remove(0, GlobalParameters._iHeaderLength);

                byte[] byteTeleBody = Encoding.UTF8.GetBytes(sTeleBody);

                this.Mat_ID = Encoding.UTF8.GetString(byteTeleBody, 0, 14);
                this.Mat_Grade = Encoding.UTF8.GetString(byteTeleBody, 20, 22);
                this.Steel_Group_ID = Encoding.UTF8.GetString(byteTeleBody, 42, 2);
                this.Mat_Length = Int16.Parse(Encoding.UTF8.GetString(byteTeleBody, 44, 5));
                this.Mat_Thickness = Int16.Parse(Encoding.UTF8.GetString(byteTeleBody, 49, 5));
                this.Mat_Width_Head = Int16.Parse(Encoding.UTF8.GetString(byteTeleBody, 54, 5));
                this.Mat_Width_Tail = Int16.Parse(Encoding.UTF8.GetString(byteTeleBody, 59, 5));
                this.Preheat_Pos = Int16.Parse(Encoding.UTF8.GetString(byteTeleBody, 64, 5));
                this.Oper_Mode_Code = Int16.Parse(Encoding.UTF8.GetString(byteTeleBody, 69, 5));
                this.Pattern_Code = Int16.Parse(Encoding.UTF8.GetString(byteTeleBody, 74, 5));
                this.Pattern_Depth_Base = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 79, 10));
                this.Pattern_Depth_Heavy = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 89, 10));
                //this.Depth_Top = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 99, 10));
                //this.Depth_Bottom = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 109, 10));
                //this.Depth_Top_Fixed = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 119, 10));
                //this.Depth_Top_Float = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 129, 10));
                //this.Depth_Bottom_Fixed = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 139, 10));
                //this.Depth_Bottom_Float = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 149, 10));
                //this.Depth_Top_Edge = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 159, 10));
                //this.Depth_Bottom_Edge = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 169, 10));
                //this.Preheat_Time = Int16.Parse(Encoding.UTF8.GetString(byteTeleBody, 179, 5));
                //this.Scarf_Speed = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 184, 10));
                //this.OST = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 194, 10));
                //this.OSB = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 204, 10));
                //this.OSTFX = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 214, 10));
                //this.OSTFL = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 224, 10));
                //this.OSBFX = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 234, 10));
                //this.OSBFL = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 244, 10));
                //this.OSTE = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 254, 10));
                //this.OSBE = float.Parse(Encoding.UTF8.GetString(byteTeleBody, 264, 10));
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on parsing telegraminformation: " + ex.Message);
            }
        }

    }
    #endregion
}