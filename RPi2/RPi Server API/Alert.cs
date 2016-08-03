using System;

namespace RPi2.RPi_Server_API
{
    public enum EAlertMsg
    {
        Success,
        Fail,

        // TODO: define
    }

    public class CAlert
    {
        #region Properties

        public UInt32 DeviceID { get; private set; }

        public EAlertMsg Message { get; set; }

        #endregion

        #region Methods

        #endregion

    }
}
