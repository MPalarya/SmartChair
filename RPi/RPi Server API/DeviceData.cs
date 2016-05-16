using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPi.RPi_Server_API
{
    class CDeviceData
    {
        #region Fields

        private UInt32 _deviceId;
        private List<int> _data;

        #endregion

        #region Contructors

        public CDeviceData()
        {
            _deviceId = Convert.ToUInt32((new Random().Next()));
        }

        public CDeviceData(UInt32 id)
        {
            _deviceId = id;
        }

        #endregion

        #region Properties

        public UInt32 ID
        {
            get { return _deviceId; }
        }

        public List<int> Data
        {
            get { return _data; }
            set { _data = value; }
        }

        #endregion


        #region Methods

        /// <summary>
        /// RPi sends data to Azure server, including device id, a list of normalized measurements and an end of sample timestamp
        /// keeps sending until returned success
        /// </summary>
        public bool RPiServer_newDataSample(CDeviceData data, System.DateTime timestamp)
        {
            // TODO: connect to Azure and send a msg
            return true;
        }

        #endregion
    }
}
