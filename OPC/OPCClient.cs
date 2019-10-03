using System;
using System.Configuration;
using System.Net;
using System.Reflection;
using IPS7Lnk.Advanced;
using Utilities;

namespace OPC
{
    public sealed class OPCClient
    {
        private SiemensDevice client;

        private string _sConnection = string.Empty,
                        _sEndPoint = string.Empty;

        private int _iRack = 0,
                    _iSlot = 0;

        private IPDeviceEndPoint _EndPoint;
        public PlcDeviceConnection _plcConnection = null;

        private bool bFirstConnect = true;
        public bool bConnectionOK = false;

        public OPCClient()
        {
            try
            {
                try
                {
                    _iRack = Int32.Parse(ConfigurationManager.AppSettings["Client.Rack"]);
                }
                catch(Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Client.Rack]: " + ex.Message);
                    return;
                }

                try
                {
                    _iSlot = Int32.Parse(ConfigurationManager.AppSettings["Client.Slot"]);
                }
                catch(Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Client.Slot]: " + ex.Message);
                    return;
                }

                try
                {
                    _sEndPoint = ConfigurationManager.AppSettings["Client.AddressPort"];
                    _EndPoint = new IPDeviceEndPoint((IPAddress.Parse(_sEndPoint)), _iRack, _iSlot);
                }
                catch (Exception ex)
                {
                    ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": error on reading configuration [Client.AddressPort]: " + ex.Message);
                    return;
                }

                //connect to the PLC
                bConnectionOK = Connect();
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private bool Connect()
        {
            if (bFirstConnect == true)
                ServiceBaseX._syncEvts.IncrementThreadsRunning();

            try
            {
                client = new SiemensDevice(_EndPoint, SiemensDeviceType.S7300_400);

                IPS7Lnk.Advanced.Licenser.LicenseKey = "lgAAAA29d9Q/xtEBlgFDb21wYW55TmFtZT1Mb2dvVGVrICBHbWJIIEdlc2VsbHNjaGFmdCBmw7xyIEluZm9ybWF0aW9uc3RlY2hub2xvZ2llO0ZpcnN0TmFtZT1DaHJpc3RvcGhlcjtMYXN0TmFtZT1Lw7ZtcGVsO0VtYWlsPWNocmlzdG9waGVyLmtvZW1wZWxAbG9nb3Rlay1nbWJoLmRlO0NvdW50cnlOYW1lPUQ7Q2l0eU5hbWU9TWFya3RoZWlkZW5mZWxkO1ppcENvZGU9OTc4Mjg7U3RyZWV0TmFtZT1BbiBkZXIgS8O2aGxlcmVpIDc7U3RyZWV0TnVtYmVyPTtSZXRhaWxlck5hbWU9VHJhZWdlciBJbmR1c3RyeSBDb21wb25lbnRzO1ZvbHVtZT0xO1NlcmlhbE51bWJlcj0xMDAxO1N1cHBvcnRFeHBpcnlEYXRlPTA2LzE0LzIwMTcgMDA6MDA6MDA7VXNlTm9CcmFuZGluZz1GYWxzZTtDb250YWN0Rmlyc3ROYW1lPTtDb250YWN0TGFzdE5hbWU9GQwP4pqjgIkqQ3rkHBitUvrSkZA87Wf+QGXIW7F54n+Fnqh7gR8rfZy/oUnKKTGz";
                _plcConnection = client.CreateConnection();
                _plcConnection.Open();

                bFirstConnect = false;
                return true;
            }
            catch (Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }
        }

        public bool Reconnect()
        {
            try
            {
                //close plc connection
                _plcConnection.Close();
                ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": PLC connection closed.");
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }

            try
            {
                //open plc connection again
                _plcConnection.Open();
                ServiceBaseX._logger.Log(Category.Info, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": PLC connection re-established.");
                return true;
            }
            catch(Exception ex)
            {
                ServiceBaseX._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
