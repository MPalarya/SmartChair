using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace RPiConsole
{
    public class DeviceDataAggregator
    {
        #region Fields

        private static DeviceDataAggregator instance;
        private static IRPiServerProxy rpiServerProxy;
        private static DateTime lastReportedDatetime;
        private static string deviceId;
        #endregion

        #region Contructors

        private DeviceDataAggregator()
        {
            lastReportedDatetime = DateTime.Now;
            rpiServerProxy = CRPiServerProxy.Instance;

            setupSensorParts();
        }

        #endregion

        #region Properties
        public static int frequencyToReport { get; } = 60;

        public static DeviceDataAggregator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceDataAggregator();
                }
                return instance;
            }
        }

        public Dictionary<EChairPartArea, Sensor> Back { get; set; }
            = new Dictionary<EChairPartArea, Sensor>();

        public Dictionary<EChairPartArea, Sensor> Seat { get; set; }
            = new Dictionary<EChairPartArea, Sensor>();

        public Dictionary<EChairPartArea, Sensor> Handles { get; set; }
            = new Dictionary<EChairPartArea, Sensor>();

        private Dictionary<EChairPart, Dictionary<EChairPartArea, Sensor>> Sensors { get; set; }
            = new Dictionary<EChairPart, Dictionary<EChairPartArea, Sensor>>();

        #endregion Properties

        #region Methods
        private void setupSensorParts()
        {
            Sensors[EChairPart.Seat] = Seat;
            Sensors[EChairPart.Back] = Back;
            Sensors[EChairPart.Handles] = Handles;
        }

        public void aggregateAndReportData()
        {
            DateTime currDatetime = DateTime.Now;

            if (shouldReportNow(currDatetime))
            {
                int[] sensorDataArr = aggregateData();
                rpiServerProxy.RPiServer_newDataSample(currDatetime, sensorDataArr);

                lastReportedDatetime = currDatetime;
            }
        }

        private bool shouldReportNow(DateTime currDatetime)
        {
            return currDatetime.Subtract(lastReportedDatetime).TotalMinutes >= frequencyToReport;
        }

        private int[] aggregateData()
        {
            List<int> sensorDataList = new List<int>();
            foreach (var chairPart in Sensors)
            {
                foreach (var partArea in chairPart.Value)
                {
                    sensorDataList.Add(partArea.Value.readKG());
                }
            }

            return sensorDataList.ToArray();
        }

        #endregion Methods
    }
}
