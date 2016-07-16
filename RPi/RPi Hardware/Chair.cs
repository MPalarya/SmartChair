using RPi.RPi_Server_API;
using System;
using System.Collections.Generic;

namespace RPi.RPi_Hardware
{
    /// <summary>
    /// the chair holds record of sensors distribution on the surface of its different parts
    /// </summary>
    internal sealed class CChair
    {
        #region Fields

        private static volatile CChair m_instance;
        private static object syncRoot = new object();
        private static DateTime lastReported = DateTime.Now;

        #endregion

        #region Constructors

        private CChair()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// CChair Singleton class uses double lock methodology,
        /// recommended on MSDN for multithreaded access to a Singleton instance.
        /// </summary>
        public static CChair Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new CChair();
                        }
                    }
                }
                return m_instance;
            }
        }


        public Dictionary<EChairPartArea, CSensor> Back { get; set; }
            = new Dictionary<EChairPartArea, CSensor>();

        public Dictionary<EChairPartArea, CSensor> Seat { get; set; }
            = new Dictionary<EChairPartArea, CSensor>();

        public Dictionary<EChairPartArea, CSensor> Handles { get; set; }
            = new Dictionary<EChairPartArea, CSensor>();

        public Dictionary<EChairPart, Dictionary<EChairPartArea, CSensor>> Sensors { get; set; }
            = new Dictionary<EChairPart, Dictionary<EChairPartArea, CSensor>>();

        #endregion

        #region Methods

        /// <summary>
        /// reads measurements from all sensors and sends data to Server
        /// </summary>
        public void ReadAndReport()
        {
            DateTime curTime = DateTime.Now;

            if (curTime.Subtract(lastReported).TotalMinutes < CDeviceData.frequencyToReport)
                return;

            lastReported = curTime;

            CDeviceData deviceData = CDeviceData.Instance;
            deviceData.Data.Clear();
            foreach (var chairPart in Sensors)
            {
                foreach (var partArea in chairPart.Value)
                {
                    deviceData.Data[chairPart.Key][partArea.Key] = partArea.Value.ReadKG();
                }
            }
            deviceData.RPiServer_newDataSample(DateTime.Now);
        }

        #endregion
    }
}
