using System;
using System.Collections.Generic;

namespace RPi.RPi_Server_API
{
    public class CDeviceData
    {
        #region Contructors

        public CDeviceData()
        {
            Id = Convert.ToUInt32((new Random().Next()));
        }

        public CDeviceData(UInt32 id)
        {
            Id = id;
        }

        #endregion

        #region Properties

        public UInt32 Id { get; private set; }

        public List<int> Data { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// RPi sends data to Azure server, including device id, a list of normalized measurements and an end of sample timestamp
        /// keeps sending until returned success
        /// </summary>
        public bool RPiServer_newDataSample(CDeviceData data, System.DateTime timestamp)
        {
            // TODO: to be implemented by Orr
            return true;
        }

        #endregion
    }
}
