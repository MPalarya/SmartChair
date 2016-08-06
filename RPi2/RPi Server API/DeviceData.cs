using System;
using System.Collections.Generic;
using Windows.UI.Core;

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

        private MessageConverter messageConvert;
        private DeviceMessagesSendReceive deviceMessagesSendReceive;

        private static readonly string deviceId = "SmartChair01";
        private static readonly string deviceKey = "Sgerz/a7KV2M8/kJ+As5XH5u/o9fJtIIuDsQZYpLsGU=";

        internal Windows.UI.Xaml.Controls.TextBlock guiDebugging = null;

        #endregion

        #region Contructors

        private CDeviceData()
        {
            messageConvert = MessageConverter.Instance;
            deviceMessagesSendReceive = new DeviceMessagesSendReceive(deviceId, deviceKey);
        }

        #endregion

        #region Properties

        /// <summary>
        /// time to wait in seconds before sending next data-set to Server
        /// </summary>
        public static int frequencyToReport { get; set; } = 5;

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

        /// <summary>
        /// <para>access example: int particular_measurement = Data[Seat][RightMid];                                 </para>
        /// <para>or: foreach (var chairPart in Data.Keys) { foreach (var chairPartArea in Data[chairPart]) { .. } } </para>
        /// </summary>
        public Dictionary<EChairPart, Dictionary<EChairPartArea, int>> Data { get; set; }
            = new Dictionary<EChairPart, Dictionary<EChairPartArea, int>>();

        #endregion Properties

        #region Methods

        /// <summary>
        /// RPi sends data to Azure server, including device id, a list of normalized measurements and an end of sample timestamp
        /// keeps sending until returned success
        /// </summary>
        public void RPiServer_newDataSample(System.DateTime timestamp)
        {
            string messageString = createMessageStringFromData(timestamp);
            deviceMessagesSendReceive.sendMessageToServerAsync(messageString);
            reportMessageSent(messageString);
        }

        private string createMessageStringFromData(DateTime timestamp)
        {
            int[] pressureArr = aggregateDataToArray();
            Datapoint dataPoint = new Datapoint(deviceId, timestamp, pressureArr);
            string messageString = messageConvert.encode(EMessageId.RpiServer_Datapoint, dataPoint);

            return messageString;
        }

        private int[] aggregateDataToArray()
        {
            List<int> sensorDataList = new List<int>();
            foreach (var chairPart in Data)
            {
                foreach (var partArea in chairPart.Value)
                {
                    sensorDataList.Add(partArea.Value);
                }
            }

            return sensorDataList.ToArray();
        }

        private async void reportMessageSent(string messageString)
        {
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
