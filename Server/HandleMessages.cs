using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Microsoft.Azure.Devices;

// TODO: parse message from client to send client data
// TODO: receive log on clients side
// TODO: predict if sitting incorrectly and alert client
// TODO: save cliets initial data for prediction

namespace Server
{
    // Runs the main code.
    // Connects to IOT hub to receive messages and handle.
    // Holds Hash Table of key = deviceId value = DataPointsQueue. Therefore adding a datapoint to its correct queue can happen in O(1).
    // Adds every received message to a ThreadPool so a thread can process it. 
    class HandleMessages
    {
        #region Fields

        static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;
        static ServiceClient serviceClient;
        static ConcurrentDictionary<string, DataPointsQueue> dict;
        
        #endregion

        #region Main
        static void Main(string[] args)
        {
            Console.WriteLine("Receiving and Handling messages:\n");
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;
            dict = new ConcurrentDictionary<string, DataPointsQueue>();

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
            messageStruct<object> messagestruct = JsonConvert.DeserializeObject<messageStruct<object>>(data);
            
            switch (messagestruct.messageid)
            {
                case messageId.RpiServer_Datapoint:
                    dataPoint datapoint = JsonConvert.DeserializeObject<dataPoint>(messagestruct.data.ToString());
                    addDatapointToQueue(datapoint);
                    break;
            }
        }

        // Thread adds the message data to its correct DataPointsQueue by using the Hash Table. 
        // If there is no queue for the key then it is added.
        static void addDatapointToQueue(dataPoint datapoint)
        {
            DataPointsQueue dpqueue;
            SendCloudToDeviceMessageAsync(datapoint);

            if (dict.TryGetValue(datapoint.deviceId, out dpqueue))
            {
                dpqueue.addDataPoint(datapoint);
            }
            else
            {
                dict.TryAdd(datapoint.deviceId, new DataPointsQueue(datapoint));
            }
        }

        private async static void SendCloudToDeviceMessageAsync(dataPoint datapoint)
        {
            messageStruct<dataPoint> messagestruct = new messageStruct<dataPoint>(messageId.ServerClient_Datapoint, datapoint);
            string messageString = JsonConvert.SerializeObject(messagestruct);
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            Console.WriteLine("Sending message: {0}", messageString);
            await serviceClient.SendAsync("00326-10000-00000-AA800", message);
            Console.WriteLine("Completed");
        }
        #endregion
    }

    // Provides a Queue to save the last CAPACITY datapoints and perform calculations on them
    class DataPointsQueue
    {
        #region Fields

        dbInterface dbi;
        int[] currSum;
        int size = 0;
        int count;
        Queue<dataPoint> queue;
        
        #endregion

        #region Properties
        static int CAPACITY = 3;
        #endregion

        #region Constructors
        public DataPointsQueue()
        {
            currSum = new int[7];
            count = 0;
            queue = new Queue<dataPoint>(CAPACITY);
            dbi = new dbInterface();
        }

        public DataPointsQueue(dataPoint datapoint)
            : this()
        {
            addDataPoint(datapoint);
        }
        #endregion

        #region Methods
        // Enqueues a datapoint. If the queue is full a datapoint is dequeued and 
        // the average of the past CAPACITY datapoints is added to the database
        public void addDataPoint(dataPoint datapoint)
        {
            int[] averagePressure = new int[7];
            dataPoint oldest = new dataPoint(0);

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

                addToDatabase(new dataPoint(datapoint.deviceId, datapoint.datetime, averagePressure));
            }
        }

        // Adds a datapoint to the database
        private void addToDatabase(dataPoint datapoint)
        {
            dbi.updateKey(datapoint);
        }
        #endregion
    }
}
