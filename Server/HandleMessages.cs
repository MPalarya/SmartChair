﻿using System;
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
    public static class ServerMessagesHandler
    {
        #region Fields

        private static ConcurrentDictionary<string, DatapointsBuffer> dataPointsBufferDict;
        private static IDbProxy dbProxy;
        private static MessageConverter messageConvert;
        private static ServerMessagesSendReceive serverMessagesSendReceive;

        #endregion

        #region Main
        static void Main(string[] args)
        {
            Console.WriteLine("Receiving and Handling messages:\n");

            dataPointsBufferDict = new ConcurrentDictionary<string, DatapointsBuffer>();
            dbProxy = CDbProxy.Instance;
            messageConvert = MessageConverter.Instance;
            serverMessagesSendReceive = new ServerMessagesSendReceive(scheduleMessageStringHandling);
            serverMessagesSendReceive.receiveMessages();
        }
        #endregion

        #region Methods

        private static void scheduleMessageStringHandling(string messageString)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleMessage), messageString);
        }

        private static void handleMessage(Object stateInfo)
        {
            var messageStruct = decodeMessage(stateInfo);
            processMessageByMessageId(messageStruct);
        }

        private static dynamic decodeMessage(Object stateInfo)
        {
            string messageString = (string)stateInfo;
            Message<object> messageStruct = messageConvert.decode(messageString);

            return messageStruct;
        }

        private static void processMessageByMessageId(Message<object> messageStruct)
        {
            switch (messageStruct.messageid)
            {
                case EMessageId.RpiServer_Datapoint:
                    handleMessageNewDatapoint(messageStruct);
                    break;

                case EMessageId.ClientServer_PairDevice:
                    handleMessagePairDevice(messageStruct);
                    break;

                case EMessageId.ClientServer_StartRealtime:
                    handleMessageStartRealtime(messageStruct);
                    break;

                case EMessageId.ClientServer_StopRealtime:
                    handleMessageStopRealtime(messageStruct);
                    break;

                case EMessageId.ClientServer_StartInit:
                    handleMessageStartCollectingInitData(messageStruct);
                    break;

                case EMessageId.ClientServer_GetLogs:
                    handleMessageGetLogs(messageStruct);
                    break;
            }
        }

        private static void handleMessageNewDatapoint(Message<object> messageStruct)
        {
            Datapoint datapoint = (Datapoint)messageStruct.data;
            ClientProperties client = dbProxy.getClientByDevice(datapoint.deviceId);
            addDatapointToBufferByDeviceId(datapoint);

            if (shouldSendRealtimeMessageToClient(client))
            {
                string messageString = messageConvert.encode(EMessageId.ServerClient_Datapoint, datapoint);
                ServerMessagesSendReceive.sendMessageToClient(client.clientId, messageString);
            }
        }

        private static bool shouldSendRealtimeMessageToClient(ClientProperties client)
        {
            return client != null && client.bReceiveRealTime;
        }

        private static void handleMessagePairDevice(Message<object> messageStruct)
        {
            ClientProperties client = (ClientProperties)messageStruct.data;
            dbProxy.setClient(client);
        }

        private static void handleMessageStartRealtime(Message<object> messageStruct)
        {
            setRealTime(messageStruct, true);
        }

        private static void handleMessageStopRealtime(Message<object> messageStruct)
        {
            setRealTime(messageStruct, false);
        }

        private static void setRealTime(Message<object> messageStruct, bool valueToSet)
        {
            string clientId = (string)messageStruct.data;
            string deviceId = dbProxy.getDeviceByClient(clientId);
            if (deviceId != null)
            {
                ClientProperties client = dbProxy.getClientByDevice(deviceId);
                if (client != null)
                {
                    client.bReceiveRealTime = valueToSet;
                    dbProxy.setClient(client);
                }
            }
        }

        private static void handleMessageStartCollectingInitData(Message<object> messageStruct)
        {
            string clientId = (string)messageStruct.data;
            string deviceId = dbProxy.getDeviceByClient(clientId);
            if (deviceId != null)
            {
                startCollectingInitDatapoints(deviceId);
            }
            else
            {
                // TODO: let client know no device is connected
            }
        }

        private static void handleMessageGetLogs(Message<object> messageStruct)
        {
            LogBounds logLimits = (LogBounds)messageStruct.data;
            string clientId = logLimits.clientId;
            string deviceId = dbProxy.getDeviceByClient(clientId);
            if (deviceId != null)
            {
                var logs = getDeviceLogsInJson(deviceId, logLimits.startdate, logLimits.enddate);
                ServerMessagesSendReceive.sendMessageToClient(clientId, messageConvert.encode(EMessageId.ServerClient_DayData, logs));
            }
        }

        private static void addDatapointToBufferByDeviceId(Datapoint datapoint)
        {
            DatapointsBuffer dataPointsBuffer;

            if (dataPointsBufferDict.TryGetValue(datapoint.deviceId, out dataPointsBuffer))
                dataPointsBuffer.addDataPoint(datapoint);
            else
                dataPointsBufferDict.TryAdd(datapoint.deviceId, new DatapointsBuffer(datapoint.pressure.Length, datapoint));
        }

        private static void startCollectingInitDatapoints(string deviceId)
        {
            DatapointsBuffer dataPointsBuffer;

            if (dataPointsBufferDict.TryGetValue(deviceId, out dataPointsBuffer))
                dataPointsBuffer.startCollectingInitDatapoints();
            else
                return;// TODO: return device not getting data
        }

        private static object getDeviceLogsInJson(string deviceId, DateTime startdate, DateTime enddate)
        {
            if (startdate > enddate)
                return null;

            string allLogs = dbProxy.getLog(deviceId);
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
        #endregion

        private class ServerMessagesSendReceive
        {
            #region Fields

            private static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";
            private static string iotHubD2cEndpoint = "messages/events";
            private static EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);
            private static ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            private Action<string> callbackOnReceiveMessage;

            #endregion

            #region Constructors

            public ServerMessagesSendReceive(Action<string> callbackOnReceiveMessage)
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
                try
                {
                    Message message = new Message(Encoding.ASCII.GetBytes(messageString));
                    await serviceClient.SendAsync(clientId, message);
                    printMessageSentToClient(clientId, messageString);
                }
                catch (Exception)
                {
                    printErrorSendingToClient(clientId, messageString);
                }
            }

            private static void printMessageSentToClient(string clientId, string messageString)
            {
                Console.WriteLine("Sent message {0} to client {1}", messageString, clientId);
            }

            private static void printErrorSendingToClient(string clientId, string messageString)
            {
                Console.WriteLine("ERROR SENDING message {0} to client {1}", messageString, clientId);
            }
            #endregion
        }

        private class DatapointsBuffer
        {
            #region Fields

            private static int CAPACITY = 5;
            private static ClassifySitting classifySitting;
            private static MessageConverter messageConvert;
            private int[] currPressureSum;
            private int[] initPressure;
            private int[] averagePressure;
            private int numOfSensors;
            private int size;
            private int count;
            private string deviceId;
            private bool bCollectingInitDatapoints;
            private Datapoint oldest;
            private Queue<Datapoint> queue;
            private IDbProxy dbProxy;
            private object syncLock;
            #endregion

            #region Constructors
            public DatapointsBuffer(int numOfSensors, Datapoint datapoint)
                : this(numOfSensors)
            {
                this.deviceId = datapoint.deviceId;
                int[] initPressure = dbProxy.getInit(deviceId);
                classifySitting = new ClassifySitting(initPressure);
                this.addDataPoint(datapoint);
            }

            private DatapointsBuffer(int numOfSensors)
            {
                messageConvert = MessageConverter.Instance;
                this.count = 0;
                this.size = 0;
                this.numOfSensors = numOfSensors;
                this.currPressureSum = new int[numOfSensors];
                this.averagePressure = new int[numOfSensors];
                this.bCollectingInitDatapoints = false;
                this.oldest = new Datapoint(numOfSensors, 0);
                this.queue = new Queue<Datapoint>(CAPACITY);
                this.dbProxy = CDbProxy.Instance;
                this.syncLock = new Object();
            }
            #endregion

            #region Methods
            // Enqueues a datapoint. If the queue is full a datapoint is dequeued and 
            // the average of the past CAPACITY datapoints is added to the database
            public void addDataPoint(Datapoint datapoint)
            {
                lock (syncLock)
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
            }

            private void enqueueDatapoint(Datapoint datapoint)
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

            private void updatePressureSumArray(Datapoint datapoint)
            {
                for (int i = 0; i < currPressureSum.Length; i++)
                {
                    currPressureSum[i] += datapoint.pressure[i];
                    currPressureSum[i] -= oldest.pressure[i];
                }
            }

            private void notifyClientAboutSittingCorrectness()
            {
                EPostureErrorType postureErrorType = classifySitting.isSittingCorrectly(averagePressure);
                if (postureErrorType != EPostureErrorType.Correct)
                {
                    ClientProperties client = dbProxy.getClientByDevice(deviceId);
                    ServerMessagesSendReceive.sendMessageToClient(client.clientId, messageConvert.encode(EMessageId.ServerClient_fixPosture, postureErrorType));
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
                Datapoint datapoint = new Datapoint(deviceId, datetime, averagePressure);
                saveAverageToDb(datapoint);
                if (bCollectingInitDatapoints)
                    saveInitToDb(datapoint);
            }

            private void saveAverageToDb(Datapoint datapoint)
            {
                dbProxy.updateLog(datapoint);
            }

            private void saveInitToDb(Datapoint datapoint)
            {
                bCollectingInitDatapoints = false;
                dbProxy.setInit(datapoint);
                ClientProperties client = dbProxy.getClientByDevice(datapoint.deviceId);
                classifySitting.updateInitData(datapoint.pressure);
                ServerMessagesSendReceive.sendMessageToClient(client.clientId, messageConvert.encode(EMessageId.ServerClient_StopInit, ""));
            }

            public void startCollectingInitDatapoints()
            {
                count = 0;
                bCollectingInitDatapoints = true;
            }
            #endregion
        }
    }
}
