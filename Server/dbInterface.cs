﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Newtonsoft.Json;

/* REDIS
We chose a Redis databse for the project.
- There are many writes because each device writes to the database once every 60 seconds. Each write appends a value to the key.
- There are few reads. Only when client asks for log. Each reads gets all of the value.
Both these operations are very efficiant in Redis, performing at O(1) complexity.
*/

namespace Server
{   
    // Provides an interface to interact with the redis database
    // Interface methods can remain the same even if converting data to another database
    // Table 0: key = deviceId, value = device datapoint log
    // Table 1: key = deviceId, value = init dataset for device
    // Table 2: key = deviceId, value = Client class
    // Table 3: key = clientId, value = deviceId
    class DbInterface
    {
        #region Fields

        ConnectionMultiplexer conn;
        IDatabase redisLogs, redisInit, redicClients, redisDevicesByClient;

        #endregion

        #region Constructors
        public DbInterface()
        {
            // connect to database
            conn = ConnectionMultiplexer.Connect("smartchair.redis.cache.windows.net:6380,password=EcwNyqsaIKcM8JcnWbkP8FHy/xs/YHf6omQ2UaHvnlw=,ssl=True,abortConnect=False");
            redisLogs = conn.GetDatabase(0);
            redisInit = conn.GetDatabase(1);
            redicClients = conn.GetDatabase(2);
            redisDevicesByClient = conn.GetDatabase(3);
        }
        #endregion

        #region Methods
        /* ----------------------------------------*
         * LOGS Table                               *
         * ----------------------------------------*/
        // Appends a datapoint encoded in JSON to the deviceId key in Logs table
        // If key does not exist it is added with the datapoint
        public void updateLog(DataPoint datapoint)
        {
            object[] data = { datapoint.datetime.ToString(), datapoint.pressure };
            string jsondata = JsonConvert.SerializeObject(data);
            redisLogs.StringAppend(datapoint.deviceId, jsondata + ",");
            Console.WriteLine("Appended key = " + datapoint.deviceId + "; value = " + jsondata + "," + " to redisLogs");
        }

        // Gets Log value for deviceId key from Logs table in encoded JSON array
        // Returns string [[string datetime, int pressure[7]], [string datetime, int pressure[7]], ... ]
        public string getLog(string deviceId)
        {
            string value = redisLogs.StringGet(deviceId);
            Console.WriteLine("Got key = " + deviceId + " from redisLogs");
            return "[" + value.TrimEnd(',') + "]";
        }

        // Deletes key from Logs database
        public void removeLog(string deviceId)
        {
            redisLogs.KeyDelete(deviceId);
            Console.WriteLine("Removed key = " + deviceId + " from redisLogs");
        }


        /* ----------------------------------------*
         * INIT Table                               *
         * ----------------------------------------*/
        // Sets init datapoint value for deviceId key in Init table
        public void setInit(DataPoint datapoint)
        {
            string jsondata = JsonConvert.SerializeObject(datapoint.pressure);
            redisInit.StringSet(datapoint.deviceId, jsondata);
            Console.WriteLine("Set key = " + datapoint.deviceId + "; value = " + jsondata.ToString() + " to redisInit");
        }

        // Gets init data value for deviceId key
        public int[] getInit(string deviceId)
        {
            string value = redisInit.StringGet(deviceId);
            Console.WriteLine("Got key = " + deviceId + " from redisInit");
            if (value == null)
                return null;

            return JsonConvert.DeserializeObject<int[]>(value);
        }

        // Deletes deviceId key from Init table
        public void removeInit(string deviceId)
        {
            redisInit.KeyDelete(deviceId);
            Console.WriteLine("Removed key = " + deviceId + " from redisInit");
        }


        /* ----------------------------------------*
         * CLIENTS Table                            *
         * ----------------------------------------*/
        // Sets clientId value for deviceId key in Client table
        public void setClient(Client client)
        {
            string jsonClient = JsonConvert.SerializeObject(client);
            redicClients.StringSet(client.deviceId, jsonClient);
            setDevice(client.clientId, client.deviceId);
            Console.WriteLine("Set key = " + client.deviceId + "; value = " + client.clientId + " to redisClients");
        }
        public void setClient(string deviceId, string clientId)
        {
            Client client = new Client(clientId, deviceId);
            setClient(client);
        }
        public void setClient(string deviceId, string clientId, bool sendRealTime)
        {
            Client client = new Client(clientId, deviceId, sendRealTime);
            setClient(client);
        }

        // Gets clientId value for deviceId key from Clients table
        public Client getClient(string deviceId)
        {
            string value = redicClients.StringGet(deviceId);
            Console.WriteLine("Got key = " + deviceId + " from redisClients");
            if (value != null)
                return JsonConvert.DeserializeObject<Client>(value);
            else
                return null;
        }

        // Deletes deviceId key from Clients table
        public void removeClient(string deviceId)
        {
            Client client = getClient(deviceId);
            redicClients.KeyDelete(deviceId);
            if (client != null)
                removeDevice(client.clientId);
            Console.WriteLine("Removed key = " + deviceId + " from redisClients");
        }


        /* ----------------------------------------*
         * DEVICES Table                            *
         * ----------------------------------------*/
        // Sets deviceId value for clientId key in Devices table
        public void setDevice(string clientId, string deviceId)
        {
            redisDevicesByClient.StringSet(clientId, deviceId);
            Console.WriteLine("Set key = " + clientId + "; value = " + deviceId + " to redisDevicesByClient");
        }

        // Gets deviceId data value for clientId key
        public string getDevice(string clientId)
        {
            string value = redisDevicesByClient.StringGet(clientId);
            Console.WriteLine("Got key = " + clientId.ToString() + " from redisDevicesByClient");
            return value;
        }

        // Deletes clientId key from Devices table
        public void removeDevice(string clientId)
        {
            redisDevicesByClient.KeyDelete(clientId);
            Console.WriteLine("Removed key = " + clientId + " from redisDevicesByClient");
        }
        #endregion
    }
}
