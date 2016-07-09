using RPi.RPi_Hardware;
using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Diagnostics;

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
        static DeviceClient deviceClient;
        static string iotHubUri = "smartchair-iothub.azure-devices.net";
        static string deviceKey;
        static string deviceId;

        #endregion

        #region Contructors

        private CDeviceData()
        {
            // TODO Michael: the code in this function needs to run in main and pass the deviceKey and deviceId to this class
            /*
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "..\\..\\..\\GetDeviceIdentity\\bin\\Release\\GetDeviceIdentity.exe";
            p.Start();

            string[] deviceData = p.StandardOutput.ReadLine().Split();
            p.Close();
            deviceKey = deviceData[0];
            deviceId = deviceData[1];

            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
            */
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
        public async void RPiServer_newDataSample(System.DateTime timestamp)
        {
            List<int> pressureList = new List<int>();
            foreach (KeyValuePair<EChairPart, Dictionary<EChairPartArea, int>> part in Data)
            {
                foreach (KeyValuePair<EChairPartArea, int> partarea in part.Value)
                {
                    pressureList.Add(partarea.Value);
                }
            }

            DataPoint datapoint = new DataPoint(deviceId, timestamp, pressureList.ToArray());
            MessageStruct<DataPoint> messagestruct = new MessageStruct<DataPoint>(messageId.RpiServer_Datapoint, datapoint);
            string messageString = JsonConvert.SerializeObject(messagestruct);
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            await deviceClient.SendEventAsync(message);

            //TODO Michael: for debugging purposes output 'messageString' to app
        }

        public void Clear()
        {
            Data.Clear();
        }

        #endregion Methods
    }
}
