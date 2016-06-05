using RPi.RPi_Hardware;
using System;
using System.Collections.Generic;

namespace RPi.RPi_Server_API
{
    /// <summary>
    /// CDeviceData is a Singleton. use CDeviceData.Instace.
    /// </summary>
    public sealed class CDeviceData
    {
        #region Fields

        private static volatile CDeviceData m_instance;
        private static object syncRoot = new object();

        #endregion

        #region Contructors

        private CDeviceData()
        {
            Id = Convert.ToUInt32((new Random().Next()));
        }

        #endregion

        #region Properties

        /// <summary>
        /// time to wait in minutes before sending next data-set to Server
        /// </summary>
        public static double frequencyToReport { get; } = 1;

        /// <summary>
        /// CDeviceData Singleton class uses double lock methodology,
        /// recommended on MSDN for multithreaded access to a Singleton instance.
        /// </summary>
        public static CDeviceData Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new CDeviceData();
                        }
                    }
                }
                return m_instance;
            }
        }

        public UInt32 Id { get; private set; }

        /// <summary>
        /// <para>access example: int particular_measurement = Data[Seat][RightMid];                                 </para>
        /// <para>or: foreach (var chairPart in Data.Keys) { foreach (var chairPartArea in Data[chairPart]) { .. } } </para>
        /// </summary>
        public Dictionary<EChairPart, Dictionary<EChairPartArea, int>> Data { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// RPi sends data to Azure server, including device id, a list of normalized measurements and an end of sample timestamp
        /// keeps sending until returned success
        /// </summary>
        public bool RPiServer_newDataSample(System.DateTime timestamp)
        {
            // CDeviceData data == this.
            // TODO: to be implemented by Orr

            return true;
        }

        public void Clear()
        {
            Data.Clear();
        }

        #endregion Methods
    }
}
