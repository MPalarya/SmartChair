using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Microsoft.Azure.Devices;

// TODO: predict if sitting incorrectly and alert client

namespace Server
{
    // Runs the main code.
    // Connects to IOT hub to receive messages and handle.
    // Holds Hash Table of key = deviceId value = CDataPointsBuffer. Therefore adding a datapoint to its correct queue can happen in O(1).
    // Adds every received message to a ThreadPool so a thread can process it. 
    class CMessagesHandler
    {
        #region Fields

        static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;
        static ServiceClient serviceClient;
        static ConcurrentDictionary<string, CDataPointsBuffer> dataPointsBufferDict;
        static CDbInterface dbInterface;
        static CMessageConvert messageConvert;

        #endregion

        #region Constructors
        private CMessagesHandler(){}
        #endregion

        #region Main
        static void Main(string[] args)
        {
            Console.WriteLine("Receiving and Handling messages:\n");
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            var deviceToCloudPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;
            dataPointsBufferDict = new ConcurrentDictionary<string, CDataPointsBuffer>();
            dbInterface = CDbInterface.Instance;
            messageConvert = CMessageConvert.Instance;

            // make sure to stop when we exit
            CancellationTokenSource cts = new CancellationTokenSource();
            System.Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            // receive messages
            var receiveMessagesTask = new List<Task>();
            foreach (string partition in deviceToCloudPartitions)
            {
                receiveMessagesTask.Add(receiveMessagesFromDeviceAsync(partition, cts.Token));
            }
            Task.WaitAll(receiveMessagesTask.ToArray());
        }
        #endregion

        #region Methods
        // Receives a message and adds it to the ThreadPool to be processed
        private static async Task receiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string messageString = Encoding.UTF8.GetString(eventData.GetBytes());
                ThreadPool.QueueUserWorkItem(new WaitCallback(processIncomingMessageByMessageId), messageString);
            }
        }

        // Thread deserializes message and handles each message by messageId
        static void processIncomingMessageByMessageId(Object stateInfo)
        {
            string messageString = (string)stateInfo;
            CClient client;
            string clientId, deviceId;
            SMessage<object> messageStruct = messageConvert.decode(messageString);

            switch (messageStruct.messageid)
            {
                case EMessageId.RpiServer_Datapoint:
                    CDataPoint datapoint = (CDataPoint)messageStruct.data;
                    addDatapointToBufferByDeviceId(datapoint);
                    client = dbInterface.getClientByDevice(datapoint.deviceId);
                    // TODO: can be more efficiant by saving client.sendRealTime in correct queue
                    if (client != null && client.bReceiveRealTime)
                    {
                        sendMessageToClient(client.clientId, messageConvert.encode(EMessageId.ServerClient_Datapoint, datapoint));
                    }
                    break;

                case EMessageId.ClientServer_ConnectDevice:
                    client = (CClient)messageStruct.data;
                    dbInterface.setClient(client);
                    break;

                case EMessageId.ClientServer_StartRealtime:
                    clientId = (string)messageStruct.data;
                    deviceId = dbInterface.getDeviceByClient(clientId);
                    if (deviceId != null)
                    {
                        client = dbInterface.getClientByDevice(deviceId);
                        if (client != null)
                        {
                            client.bReceiveRealTime = true;
                            dbInterface.setClient(client);
                        }
                    }
                    break;

                case EMessageId.ClientServer_StopRealtime:
                    clientId = (string)messageStruct.data;
                    deviceId = dbInterface.getDeviceByClient(clientId);
                    if (deviceId != null)
                    {
                        client = dbInterface.getClientByDevice(deviceId);
                        if (client != null)
                        {
                            client.bReceiveRealTime = false;
                            dbInterface.setClient(client);
                        }
                    }
                    break;

                case EMessageId.ClientServer_StartInit:
                    clientId = (string)messageStruct.data;
                    deviceId = dbInterface.getDeviceByClient(clientId);
                    if (deviceId != null)
                    {
                        startCollectingInitDatapoints(deviceId);
                    }
                    else
                    {
                        // TODO: let client know no device is connected
                    }
                    break;

                case EMessageId.ClientServer_GetLogs:
                    CLogLimits logLimits = (CLogLimits)messageStruct.data;
                    clientId = logLimits.clientId;
                    deviceId = dbInterface.getDeviceByClient(clientId);
                    if (deviceId != null)
                    {
                        var logs = getDeviceLogsAsJson(deviceId, logLimits.startdate, logLimits.enddate);
                        sendMessageToClient(clientId, messageConvert.encode(EMessageId.ServerClient_DayData, logs));
                    }
                    break;
            }
        }

        // Thread adds the message data to its correct DataPointsQueue by using the Hash Table. 
        // If there is no queue for the key then it is added.
        private static void addDatapointToBufferByDeviceId(CDataPoint datapoint)
        {
            CDataPointsBuffer dataPointsBuffer;

            if (dataPointsBufferDict.TryGetValue(datapoint.deviceId, out dataPointsBuffer))
                dataPointsBuffer.addDataPoint(datapoint);
            else
                dataPointsBufferDict.TryAdd(datapoint.deviceId, new CDataPointsBuffer(datapoint.pressure.Length, datapoint));
        }

        // Tells correct devices queue know to start collecting init data
        private static void startCollectingInitDatapoints(string deviceId)
        {
            CDataPointsBuffer dataPointsBuffer;

            if (dataPointsBufferDict.TryGetValue(deviceId, out dataPointsBuffer))
                dataPointsBuffer.startCollectingInitDatapoints();
            else
                return;// TODO: return device not getting data
        }

        // Returns logs as JSON string for date
        private static object getDeviceLogsAsJson(string deviceId, DateTime startdate, DateTime enddate)
        {
            if (startdate > enddate)
                return null;

            string allLogs = dbInterface.getLog(deviceId);
            var allLogsList = JsonConvert.DeserializeObject<List<List<object>>>(allLogs);
            List<List<object>> retLogsList = new List<List<object>>();
            int i = 0;

            while (DateTime.Parse((string)allLogsList[i][0]) < startdate)
            {
                i++;
                if (i == allLogsList.Count)
                    return null;
            }

            while (DateTime.Parse((string)allLogsList[i][0]) <= enddate)
            {
                retLogsList.Add(allLogsList[i]);
                i++;
                if (i == allLogsList.Count)
                    break;
            }

            return retLogsList;
        }

        // Sends async message to client
        public static async void sendMessageToClient(string clientId, string messageString)
        {
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            Console.WriteLine("Sending message {0} to client {1}", messageString, clientId);
            await serviceClient.SendAsync(clientId, message);
            Console.WriteLine("Completed");
        }
        #endregion
    }

    // Provides a Buffer to save the last CAPACITY datapoints and perform calculations on them
    class CDataPointsBuffer
    {
        #region Fields

        int[] currPressureSum;
        int[] initPressure;
        int numOfSensors;
        int size;
        int count;
        bool bCollectingInitDatapoints;
        CDataPoint oldest;
        Queue<CDataPoint> queue;
        CDbInterface dbInterface;
        CMessageConvert messageConvert;

        #endregion

        #region Properties
        static int CAPACITY = 3;
        #endregion

        #region Constructors
        public CDataPointsBuffer(int numOfSensors)
        {
            count = 0;
            size = 0;
            this.numOfSensors = numOfSensors;
            currPressureSum = new int[numOfSensors];
            bCollectingInitDatapoints = false;
            oldest = new CDataPoint(numOfSensors, 0);
            queue = new Queue<CDataPoint>(CAPACITY);
            dbInterface = CDbInterface.Instance;
            messageConvert = CMessageConvert.Instance;
        }

        public CDataPointsBuffer(int numOfSensors, CDataPoint datapoint)
            : this(numOfSensors)
        {
            addDataPoint(datapoint);
        }
        #endregion

        #region Methods
        // Enqueues a datapoint. If the queue is full a datapoint is dequeued and 
        // the average of the past CAPACITY datapoints is added to the database
        public void addDataPoint(CDataPoint datapoint)
        {
            storeDatapointAndUpdateSum(datapoint);
            if (count == CAPACITY)
                saveAndClassifyDatapointsAverage(datapoint);
        }

        private void storeDatapointAndUpdateSum(CDataPoint datapoint)
        {
            // enqueue and dequeue if full
            count++;
            size++;
            queue.Enqueue(datapoint);
            if (size > CAPACITY)
            {
                oldest = queue.Dequeue();
                size--;
            }

            // update sum array
            for (int i = 0; i < currPressureSum.Length; i++)
            {
                currPressureSum[i] += datapoint.pressure[i];
                currPressureSum[i] -= oldest.pressure[i];
            }
        }

        private void saveAndClassifyDatapointsAverage(CDataPoint datapoint)
        {
            int[] averagePressure = new int[numOfSensors];
            ClassifySitting classifySitting = ClassifySitting.Instance;

            count = 0;
            for (int i = 0; i < currPressureSum.Length; i++)
            {
                averagePressure[i] = currPressureSum[i] / CAPACITY;
            }

            saveToDb(new CDataPoint(datapoint.deviceId, datapoint.datetime, averagePressure));

            if (initPressure == null)
                initPressure = dbInterface.getInit(datapoint.deviceId);

            if (!classifySitting.isSittingCorrectly(averagePressure, initPressure))
            {
                CClient client = dbInterface.getClientByDevice(datapoint.deviceId);
                CMessagesHandler.sendMessageToClient(client.clientId, messageConvert.encode(EMessageId.ServerClient_fixPosture, ""));
            }
        }

        private void saveToDb(CDataPoint datapoint)
        {
            saveAverageToDb(datapoint);
            if (bCollectingInitDatapoints)
                saveInitToDb(datapoint);
        }

        private void saveAverageToDb(CDataPoint datapoint)
        {
            dbInterface.updateLog(datapoint);
        }

        private void saveInitToDb(CDataPoint datapoint)
        {
            bCollectingInitDatapoints = false;
            dbInterface.setInit(datapoint);
            CClient client = dbInterface.getClientByDevice(datapoint.deviceId);
            CMessagesHandler.sendMessageToClient(client.clientId, messageConvert.encode(EMessageId.ServerClient_StopInit, ""));
        }

        // Starts the calculations of device init data
        public void startCollectingInitDatapoints()
        {
            count = 0;
            bCollectingInitDatapoints = true;
        }
        #endregion
    }
}
