using System;
using System.Data;
using System.Reflection;
using Utilities;
using Oracle.ManagedDataAccess.Client;

namespace OPC
{
    public class DBCommand
    {
        public static void CheckOracleConnection(string sException, OracleConnection connection, ref bool bDBConnected)
        {
            try
            {
                if ((sException.Contains("ORA") && sException.Contains("03113")) || sException.Contains("03114") || sException.Contains("03135") || sException.Contains("01036") || sException.Contains("12560") ||
                   sException.Contains("12571") || sException.Contains("12535") || sException.Contains("12543") || sException.Contains("12152") || sException.Contains("12170") || sException.Contains("NET:"))
                {
                    //close databank connection immediate
                    connection.Close();
                    bDBConnected = false;
                }
            }
            catch (Exception ex)
            {
                ServiceTCP._logger.Log(Category.Error, MethodBase.GetCurrentMethod().DeclaringType.Name + "_" + MethodBase.GetCurrentMethod().DeclaringType.Name + ": " + ex.Message);
                bDBConnected = false;
            }
        }
    }

    public class DBCommand_Write
    {
        public static OracleCommand SelTQS(OracleConnection con)
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            return cmd;
        }

        public static OracleCommand ReInitTQS_Status(OracleConnection con)
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            return cmd;
        }

        public static OracleCommand UpdTQS_Status(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_COMMSVR.SPR_UPD_TQ_S_STATUS", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new OracleParameter("PI_PKID", OracleDbType.Int32, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_STATUS", OracleDbType.Int32, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_TABLE", OracleDbType.Varchar2, ParameterDirection.Input));
            return cmd;
        }
    }

    public class DBCommand_Init
    {
        public static OracleCommand PKID_RT_Init(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_OPC.SPR_INIT_PLACE_RT", con);
            cmd.CommandType = CommandType.StoredProcedure;

            OracleParameter par1 = new OracleParameter("PO_STATUS", OracleDbType.Int32, ParameterDirection.Output);
            par1.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par1);

            return cmd;
        }

        public static OracleCommand PKID_CY_Init(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_OPC.SPR_INIT_PLACE_CY", con);
            cmd.CommandType = CommandType.StoredProcedure;

            OracleParameter par1 = new OracleParameter("PO_STATUS", OracleDbType.Int32, ParameterDirection.Output);
            par1.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par1);

            return cmd;
        }

        public static OracleCommand LOST_Init(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_OPC.SPR_INIT_LOST", con);
            cmd.CommandType = CommandType.StoredProcedure;

            OracleParameter par1 = new OracleParameter("PO_STATUS", OracleDbType.Varchar2, ParameterDirection.Output);
            par1.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par1);

            OracleParameter par2 = new OracleParameter("PO_INTERLOCK", OracleDbType.Varchar2, ParameterDirection.Output);
            par2.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par2);

            return cmd;
        }
    }

    public class DBCommand_Read
    {
        #region OPC Commands
        public static OracleCommand Trigger_Workorder(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_OPC.SPR_TRIGGER_WO", con);
            cmd.CommandType = CommandType.StoredProcedure;

            OracleParameter par1 = new OracleParameter("PI_PLACE_ID", OracleDbType.Varchar2, ParameterDirection.Input);
            par1.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par1);

            OracleParameter par2 = new OracleParameter("PI_FLAG", OracleDbType.Int32, ParameterDirection.Input);
            par2.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par2);

            cmd.Parameters.Add(new OracleParameter("PO_RESULT", OracleDbType.Int32, ParameterDirection.Output));
            return cmd;
        }

        public static OracleCommand TrackingMap(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_OPC.SPR_TRACKING_MAP", con);
            cmd.CommandType = CommandType.StoredProcedure;
            
            OracleParameter par1 = new OracleParameter("PI_PLACE_PKID", OracleDbType.Int32, ParameterDirection.Input);
            par1.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par1);

            OracleParameter par2 = new OracleParameter("PI_MAT_ID", OracleDbType.Varchar2, ParameterDirection.Input);
            par2.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par2);

            cmd.Parameters.Add(new OracleParameter("PO_RESULT", OracleDbType.Int32, ParameterDirection.Output));
            return cmd;
        }

        public static OracleCommand Conveyor(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_OPC.SPR_CONVEYOR", con);
            cmd.CommandType = CommandType.StoredProcedure;

            OracleParameter par1 = new OracleParameter("PI_MAT_ID", OracleDbType.Varchar2, ParameterDirection.Input);
            par1.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par1);

            OracleParameter par2 = new OracleParameter("PI_PLACE_PKID", OracleDbType.Int32, ParameterDirection.Input);
            par2.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par2);

            cmd.Parameters.Add(new OracleParameter("PO_RESULT", OracleDbType.Int32, ParameterDirection.Output));
            return cmd;
        }

        public static OracleCommand InterlockConfirm(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_OPC.SPR_INTERLOCK_CONFIRM", con);
            cmd.CommandType = CommandType.StoredProcedure;

            OracleParameter par1 = new OracleParameter("PI_PLACE_PKID", OracleDbType.Int32, ParameterDirection.Input);
            par1.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par1);

            OracleParameter par2 = new OracleParameter("PI_INTERLOCK", OracleDbType.Varchar2, ParameterDirection.Input);
            par2.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par2);

            cmd.Parameters.Add(new OracleParameter("PO_RESULT", OracleDbType.Int32, ParameterDirection.Output));
            return cmd;
        }

        public static OracleCommand StatusInformation(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_OPC.SPR_CONVEYOR", con);
            cmd.CommandType = CommandType.StoredProcedure;

            OracleParameter par1 = new OracleParameter("PI_PLACE_PKID", OracleDbType.Int32, ParameterDirection.Input);
            par1.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par1);

            OracleParameter par2 = new OracleParameter("PI_STATUS", OracleDbType.Varchar2, ParameterDirection.Input);
            par2.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            cmd.Parameters.Add(par2);

            cmd.Parameters.Add(new OracleParameter("PO_RESULT", OracleDbType.Int32, ParameterDirection.Output));
            return cmd;
        }
        #endregion

        public static OracleCommand Scarf_Output(OracleConnection con)
        {
            OracleCommand cmd = new OracleCommand("LOGOTEK_LTEC.SPR_SCARF_OUTPUT", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new OracleParameter("PI_MAT_ID", OracleDbType.Varchar2, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_OPER_MODE_CODE", OracleDbType.Int32, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_STEEL_GROUP_ID", OracleDbType.Varchar2, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SHIFT_TIME", OracleDbType.Varchar2, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SHIFT_GROUP_ID", OracleDbType.Varchar2, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_STOP_CODE", OracleDbType.Int32, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_MAT_TEMP", OracleDbType.Int32, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_START_DATETIME", OracleDbType.Varchar2, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_CYCLE_TIME", OracleDbType.Int32, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_PREHEAT_POS", OracleDbType.Int32, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_PREHEAT_TIME", OracleDbType.Int32, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SCARF_SPEED", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SP_OST", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SP_OSB", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SP_OSTFX", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SP_OSTFL", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SP_OSBFX", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SP_OSBFL", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SP_OSTE", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SP_OSBE", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_PATTERN_CODE", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_MAT_WIDTH", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_MAT_THICKNESS", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_O2_CYCLE", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_FG_CYCLE", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SCARF_DEPTH_TOP", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SCARF_DEPTH_BOTTOM", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SCARF_DEPTH_TOP_EDGE", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SCARF_DEPTH_BOTTOM_EDGE", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PI_SCARF_REMOVED_WEIGHT", OracleDbType.Single, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("PO_RESULT", OracleDbType.Int32, ParameterDirection.Output));
            return cmd;
        }
    }
}
