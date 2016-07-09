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
    // Holds Hash Table of key = deviceId value = DataPointsQueue. Therefore adding a datapoint to its correct queue can happen in O(1).
    // Adds every received message to a ThreadPool so a thread can process it. 
    class CHandleMessages
    {
        #region Fields

        static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;
        static ServiceClient serviceClient;
        static ConcurrentDictionary<string, CDataPointsQueue> dict;
        static CDbInterface dbi;

        #endregion

        #region Constructors
        private CHandleMessages(){}
        #endregion

        #region Main
        static void Main(string[] args)
        {
            Console.WriteLine("Receiving and Handling messages:\n");
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;
            dict = new ConcurrentDictionary<string, CDataPointsQueue>();
            dbi = CDbInterface.Instance;

            // make sure to stop when we exit
            CancellationTokenSource cts = new CancellationTokenSource();
            System.Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            // receive messages
            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }
            Task.WaitAll(tasks.ToArray());
        }
        #endregion

        #region Methods
        // Receives a message and adds it to the ThreadPool to be processed
        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                ThreadPool.QueueUserWorkItem(new WaitCallback(processIncomingMessage), data);
            }
        }

        // Thread deserializes message and handles each message by messageId
        static void processIncomingMessage(Object stateInfo)
        {
            string data = (string)stateInfo;
            CClient client;
            string clientId, deviceId;
            SMessage<object> messagestruct = JsonConvert.DeserializeObject<SMessage<object>>(data);

            switch (messagestruct.messageid)
            {
                case EMessageId.RpiServer_Datapoint:
                    CDataPoint datapoint = JsonConvert.DeserializeObject<CDataPoint>(messagestruct.data.ToString());
                    addDatapointToQueue(datapoint);
                    client = dbi.getClient(datapoint.deviceId);
                    // TODO: can be more efficiant by saving client.sendRealTime in correct queue
                    if (client != null && client.sendRealTime)
                    {
                        sendMessageToClient(client.clientId, datapoint, EMessageId.ServerClient_Datapoint);
                    }
                    break;

                case EMessageId.ClientServer_ConnectDevice:
                    client = JsonConvert.DeserializeObject<CClient>(messagestruct.data.ToString());
                    dbi.setClient(client);
                    break;

                case EMessageId.ClientServer_StartRealtime:
                    clientId = (string)messagestruct.data;
                    deviceId = dbi.getDevice(clientId);
                    if (deviceId != null)
                    {
                        client = dbi.getClient(deviceId);
                        if (client != null)
                        {
                            client.sendRealTime = true;
                            dbi.setClient(client);
                        }
                    }
                    break;

                case EMessageId.ClientServer_StopRealtime:
                    clientId = (string)messagestruct.data;
                    deviceId = dbi.getDevice(clientId);
                    if (deviceId != null)
                    {
                        client = dbi.getClient(deviceId);
                        if (client != null)
                        {
                            client.sendRealTime = false;
                            dbi.setClient(client);
                        }
                    }
                    break;

                case EMessageId.ClientServer_StartInit:
                    clientId = (string)messagestruct.data;
                    deviceId = dbi.getDevice(clientId);
                    if (deviceId != null)
                    {
                        startInit(deviceId);
                    }
                    else
                    {
                        // TODO: let client know no device is connected
                    }
                    break;

                case EMessageId.ClientServer_GetLogs:
                    CLogsDate logsdate = JsonConvert.DeserializeObject<CLogsDate>(messagestruct.data.ToString());
                    clientId = logsdate.clientId;
                    deviceId = dbi.getDevice(clientId);
                    if (deviceId != null)
                    {
                        var logs = getDeviceLogs(deviceId, logsdate.startdate, logsdate.enddate);
                        sendMessageToClient(clientId, logs, EMessageId.ServerClient_DayData);
                    }
                    break;
            }
        }

        // Thread adds the message data to its correct DataPointsQueue by using the Hash Table. 
        // If there is no queue for the key then it is added.
        private static void addDatapointToQueue(CDataPoint datapoint)
        {
            CDataPointsQueue dpqueue;
            if (dict.TryGetValue(datapoint.deviceId, out dpqueue))
            {
                dpqueue.addDataPoint(datapoint);
            }
            else
            {
                dict.TryAdd(datapoint.deviceId, new CDataPointsQueue(datapoint));
            }
        }

        // Tells correct devices queue know to start collecting init data
        private static void startInit(string deviceId)
        {
            CDataPointsQueue dpqueue;
            if (dict.TryGetValue(deviceId, out dpqueue))
            {
                dpqueue.startInit();
            }
            else
            {
                // TODO: return device not getting data
            }
        }

        // Returns logs as JSON string for date
        private static object getDeviceLogs(string deviceId, DateTime startdate, DateTime enddate)
        {
            if (startdate > enddate)
                return null;

            string alllogs = dbi.getLog(deviceId);
            var alllogslist = JsonConvert.DeserializeObject<List<List<object>>>(alllogs);
            List<List<object>> retlogs = new List<List<object>>();
            int i = 0;

            while (DateTime.Parse((string)alllogslist[i][0]) < startdate)
            {
                i++;
                if (i == alllogslist.Count)
                    return null;
            }

            while (DateTime.Parse((string)alllogslist[i][0]) <= enddate)
            {
                retlogs.Add(alllogslist[i]);
                i++;
                if (i == alllogslist.Count)
                    break;
            }

            return retlogs;
        }

        // Sends async message to client
        public static async void sendMessageToClient<T>(string clientId, T data, EMessageId messageid)
        {
            SMessage<T> messagestruct = new SMessage<T>(messageid, data);
            string messageString = JsonConvert.SerializeObject(messagestruct);
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            Console.WriteLine("Sending message {0} to client {1}", messageid.ToString(), clientId);
            await serviceClient.SendAsync(clientId, message);
            Console.WriteLine("Completed");
        }
        #endregion
    }

    // Provides a Queue to save the last CAPACITY datapoints and perform calculations on them
    class CDataPointsQueue
    {
        #region Fields

        int[] currSum;
        int[] initPressure;
        int size = 0;
        int count;
        Queue<CDataPoint> queue;
        CDbInterface dbi;
        bool saveInit;

        #endregion

        #region Properties
        static int CAPACITY = 3;
        #endregion

        #region Constructors
        public CDataPointsQueue()
        {
            currSum = new int[7];
            count = 0;
            queue = new Queue<CDataPoint>(CAPACITY);
            dbi = CDbInterface.Instance;
            saveInit = false;
        }

        public CDataPointsQueue(CDataPoint datapoint)
            : this()
        {
            addDataPoint(datapoint);
        }
        #endregion

        #region Methods
        // Enqueues a datapoint. If the queue is full a datapoint is dequeued and 
        // the average of the past CAPACITY datapoints is added to the database
        public void addDataPoint(CDataPoint datapoint)
        {
            int[] averagePressure = new int[7];
            CDataPoint oldest = new CDataPoint(0);
            ClassifySitting cs = ClassifySitting.Instance;

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
            for (int i = 0; i < currSum.Length; i++)
            {
                currSum[i] += datapoint.pressure[i];
                currSum[i] -= oldest.pressure[i];
            }

            // every CAPACITY seconds add average of last CAPACITY datapoints to database
            if (count == CAPACITY)
            {
                count = 0;
                for (int i = 0; i < currSum.Length; i++)
                {
                    averagePressure[i] = currSum[i] / CAPACITY;
                }

                addToDatabase(new CDataPoint(datapoint.deviceId, datapoint.datetime, averagePressure));

                if (initPressure == null)
                    initPressure = dbi.getInit(datapoint.deviceId);

                if(!cs.testSitting(averagePressure, initPressure))
                {
                    CClient client = dbi.getClient(datapoint.deviceId);
                    CHandleMessages.sendMessageToClient(client.clientId, "", EMessageId.ServerClient_fixPosture);
                }
            }
        }

        // Adds a datapoint to the database
        private void addToDatabase(CDataPoint datapoint)
        {
            dbi.updateLog(datapoint);
            if(saveInit)
            {
                saveInit = false;
                dbi.setInit(datapoint);
                CClient client = dbi.getClient(datapoint.deviceId);
                CHandleMessages.sendMessageToClient(client.clientId, "", EMessageId.ServerClient_StopInit);
            }
        }

        // Starts the calculations of device init data
        public void startInit()
        {
            count = 0;
            saveInit = true;
        }
        #endregion
    }
}
