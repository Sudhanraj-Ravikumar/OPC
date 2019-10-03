using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Globals
{
    //this class contains all the static global parameters used for further processing
    #region Global Parameters
    static class GlobalParameters
    {
        public const int _iThreadStartTimeout = 30000,
                         _iTimerPeriod = 5000,
                         _iDBTimerPeriod = 200,
                         _iTimerTrigger = 50,
                         //headerlength
                         _iHeaderLength = 29,
                         //ackbodylength
                         _iBodyLength = 80;

        public const string _sPES = "PES";
    }
    #endregion
}
