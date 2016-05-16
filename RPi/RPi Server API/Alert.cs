using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPi.RPi_Server_API
{
    enum EAlertMsg
    {
        [Description("Success")]
        Success,

        [Description("Fail")]
        Fail,

        // TODO: define
    }

    class CAlert
    {
        #region Fields

        private UInt32 _deviceId;
        private EAlertMsg _message;

        #endregion

        #region Contructors


        #endregion

        #region Properties

        public UInt32 DeviceID
        {
            get { return _deviceId; }
        }

        public EAlertMsg Message
        {
            get { return _message; }
            set { _message = value; }
        }

        #endregion


        #region Methods

        #endregion

    }
}
