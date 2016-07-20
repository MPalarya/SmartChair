using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Microsoft.Azure.Devices;

namespace Server
{
    // Runs the main code.
    // Connects to IOT hub to receive messages and handle.
    // Holds Hash Table of key = deviceId value = CDataPointsBuffer. Therefore adding a datapoint to its correct queue can happen in O(1).
    // Adds every received message to a ThreadPool so a thread can process it. 
    internal static class CServerMessagesHandler
    {
        #region Fields

        private static ConcurrentDictionary<string, CDataPointsBuffer> dataPointsBufferDict;
        private static DbInterface dbInterface;
        private static CMessageConvert messageConvert;
        public static CServerMessagesSendReceive serverMessagesSendReceive;

        #endregion

        #region Main
        static void Main(string[] args)
        {
            Console.WriteLine("Receiving and Handling messages:\n");

            dataPointsBufferDict = new ConcurrentDictionary<string, CDataPointsBuffer>();
            dbInterface = CDbInterface.Instance;
            messageConvert = CMessageConvert.Instance;
            serverMessagesSendReceive = new CServerMessagesSendReceive(distributeMessageStringsToThreads);

            serverMessagesSendReceive.receiveMessages();
        }
        #endregion

        #region Methods

        public static void distributeMessageStringsToThreads(string messageString)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(decodeMessageAndProcess), messageString);
        }

        public static void decodeMessageAndProcess(Object stateInfo)
        {
            string messageString = (string)stateInfo;
            SMessage<object> messageStruct = messageConvert.decode(messageString);

            processIncomingMessageByMessageId(messageStruct);
        }

        private static void processIncomingMessageByMessageId(SMessage<object> messageStruct)
        {
            CClient client;
            string clientId, deviceId;

            switch (messageStruct.messageid)
            {
                case EMessageId.RpiServer_Datapoint:
                    CDataPoint datapoint = (CDataPoint)messageStruct.data;
                    addDatapointToBufferByDeviceId(datapoint);
                    client = dbInterface.getClientByDevice(datapoint.deviceId);
                    // TODO: can be more efficiant by saving client.sendRealTime in correct queue
                    if (client != null && client.bReceiveRealTime)
                    {
                        CServerMessagesSendReceive.sendMessageToClient(client.clientId, messageConvert.encode(EMessageId.ServerClient_Datapoint, datapoint));
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
                        var logs = getDeviceLogsInJson(deviceId, logLimits.startdate, logLimits.enddate);
                        CServerMessagesSendReceive.sendMessageToClient(clientId, messageConvert.encode(EMessageId.ServerClient_DayData, logs));
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
        private static object getDeviceLogsInJson(string deviceId, DateTime startdate, DateTime enddate)
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

        #endregion
    }

    internal class CServerMessagesSendReceive
    {
        #region Fields

        private static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";
        private static string iotHubD2cEndpoint = "messages/events";
        private static EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);
        private static ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        private Action<string> callbackOnReceiveMessage;

        #endregion

        #region Constructors

        public CServerMessagesSendReceive(Action<string> callbackOnReceiveMessage)
        {
            this.callbackOnReceiveMessage = callbackOnReceiveMessage;
        }
        #endregion

        #region Methods

        public void receiveMessages()
        {
            string[] deviceToCloudPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;
            var receiveMessagesTask = new List<Task>();
            foreach (string partition in deviceToCloudPartitions)
            {
                receiveMessagesTask.Add(receiveMessagesFromDeviceAsync(partition));
            }
            Task.WaitAll(receiveMessagesTask.ToArray());
        }

        private async Task receiveMessagesFromDeviceAsync(string partition)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string messageString = Encoding.UTF8.GetString(eventData.GetBytes());
                callbackOnReceiveMessage(messageString);
            }
        }

        public static async void sendMessageToClient(string clientId, string messageString)
        {
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            Console.WriteLine("Sending message {0} to client {1}", messageString, clientId);
            await serviceClient.SendAsync(clientId, message);
            Console.WriteLine("Completed");
        }
        #endregion
    }


    // Provides a buffer to save the last CAPACITY datapoints, calculates average pressure and saves to database
    internal class CDataPointsBuffer
    {
        #region Fields

        private static int CAPACITY = 3;
        private int[] currPressureSum;
        private int[] initPressure;
        private int[] averagePressure;
        private int numOfSensors;
        private int size;
        private int count;
        private string deviceId;
        private bool bCollectingInitDatapoints;
        private CDataPoint oldest;
        private Queue<CDataPoint> queue;
        private DbInterface dbInterface;
        private CMessageConvert messageConvert;
        private ClassifySitting classifySitting;
        #endregion

        #region Constructors
        public CDataPointsBuffer(int numOfSensors, CDataPoint datapoint)
            : this(numOfSensors)
        {
            this.deviceId = datapoint.deviceId;
            this.initPressure = dbInterface.getInit(deviceId);
            this.addDataPoint(datapoint);
        }

        private CDataPointsBuffer(int numOfSensors)
        {
            this.count = 0;
            this.size = 0;
            this.numOfSensors = numOfSensors;
            this.currPressureSum = new int[numOfSensors];
            this.averagePressure = new int[numOfSensors];
            this.bCollectingInitDatapoints = false;
            this.oldest = new CDataPoint(numOfSensors, 0);
            this.queue = new Queue<CDataPoint>(CAPACITY);
            this.dbInterface = CDbInterface.Instance;
            this.messageConvert = CMessageConvert.Instance;
            this.classifySitting = ClassifySitting.Instance;
        }
        #endregion

        #region Methods
        // Enqueues a datapoint. If the queue is full a datapoint is dequeued and 
        // the average of the past CAPACITY datapoints is added to the database
        public void addDataPoint(CDataPoint datapoint)
        {
            enqueueDatapoint(datapoint);
            updatePressureSumArray(datapoint);
            if (count == CAPACITY)
            {
                calculateAveragePressure();
                saveAverageAndInitPresseureToDb(datapoint.datetime);
                notifyClientAboutSittingCorrectness();
            }
        }

        private void enqueueDatapoint(CDataPoint datapoint)
        {
            count++;
            size++;
            queue.Enqueue(datapoint);
            if (size > CAPACITY)
            {
                oldest = queue.Dequeue();
                size--;
            }   
        }

        private void updatePressureSumArray(CDataPoint datapoint)
        {
            for (int i = 0; i < currPressureSum.Length; i++)
            {
                currPressureSum[i] += datapoint.pressure[i];
                currPressureSum[i] -= oldest.pressure[i];
            }
        }

        private void notifyClientAboutSittingCorrectness()
        {
            if (!classifySitting.isSittingCorrectly(averagePressure, initPressure))
            {
                CClient client = dbInterface.getClientByDevice(deviceId);
                CServerMessagesSendReceive.sendMessageToClient(client.clientId, messageConvert.encode(EMessageId.ServerClient_fixPosture, ""));
            }
        }

        private void calculateAveragePressure()
        {
            count = 0;
            for (int i = 0; i < currPressureSum.Length; i++)
            {
                averagePressure[i] = currPressureSum[i] / CAPACITY;
            }
        }

        private void saveAverageAndInitPresseureToDb(DateTime datetime)
        {
            CDataPoint datapoint = new CDataPoint(deviceId, datetime, averagePressure);
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
            CServerMessagesSendReceive.sendMessageToClient(client.clientId, messageConvert.encode(EMessageId.ServerClient_StopInit, ""));
        }

        public void startCollectingInitDatapoints()
        {
            count = 0;
            bCollectingInitDatapoints = true;
        }
        #endregion
    }
}
