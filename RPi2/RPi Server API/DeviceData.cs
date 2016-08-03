using RPi2.RPi_Hardware;
using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Windows.UI.Core;
//using Windows.System.

namespace RPi2.RPi_Server_API
{
    /// <summary>
    /// CDeviceData is a Singleton. use CDeviceData.Instace.
    /// </summary>
    public sealed class CDeviceData
    {
        #region Fields

        private static volatile CDeviceData m_instance;
        private static object syncRoot = new object();
        private static DeviceClient m_client = null;

        private static readonly string iotHubUri = "smartchair-iothub.azure-devices.net";
        private static readonly string deviceId = "SmartChair01";
        private static readonly string deviceKey = "Sgerz/a7KV2M8/kJ+As5XH5u/o9fJtIIuDsQZYpLsGU=";

        public static DeviceClient Client
        {
            get
            {
                if (m_client == null)
                {
                    m_client = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
                }
                return m_client;
            }
        }

        internal Windows.UI.Xaml.Controls.TextBlock guiDebugging = null;

        #endregion

        #region Contructors

        private CDeviceData()
        {
            
        }

        #endregion

        #region Properties

        /// <summary>
        /// time to wait in seconds before sending next data-set to Server
        /// </summary>
        public static int frequencyToReport { get; } = 60;

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
                foreach (KeyValuePair<EChairPartArea, int> partArea in part.Value)
                {
                    pressureList.Add(partArea.Value);
                }
            }

            Datapoint dataPoint = new Datapoint(deviceId, timestamp, pressureList.ToArray());
            Message<Datapoint> messageStruct = new Message<Datapoint>(EMessageId.RpiServer_Datapoint, dataPoint);
            string messageString = JsonConvert.SerializeObject(messageStruct);
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            await Client.SendEventAsync(message);

            if (guiDebugging != null)
            {
                // call dispacher to update gui from another thread:
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        guiDebugging.Text += "\n\n" + messageString;
                    });
            }
        }

        public void Clear()
        {
            Data.Clear();
        }

        #endregion Methods
    }
}
