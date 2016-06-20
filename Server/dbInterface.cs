using System;
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
    class dbInterface
    {
        #region Fields

        ConnectionMultiplexer conn;
        IDatabase cache;

        #endregion

        #region Constructors
        public dbInterface()
        {
            // connect to database
            conn = ConnectionMultiplexer.Connect("smartchair.redis.cache.windows.net:6380,password=EcwNyqsaIKcM8JcnWbkP8FHy/xs/YHf6omQ2UaHvnlw=,ssl=True,abortConnect=False");
            cache = conn.GetDatabase();
        }
        #endregion

        #region Methods
        // Appends a datapoint encoded in JSON to the key
        // If key does not exist it is added with the datapoint
        public void updateKey(dataPoint datapoint)
        {
            object[] data = { datapoint.datetime.ToString(), datapoint.pressure };
            var jsondata = JsonConvert.SerializeObject(data);
            cache.StringAppend(datapoint.deviceId, jsondata.ToString() + ",");
            Console.WriteLine("Appended key = " + datapoint.deviceId + "; value = " + jsondata.ToString() + ",");
        }

        // Gets value for key in encoded JSON array
        // Returns string [[string datetime, int pressure[7]], [string datetime, int pressure[7]], ... ]
        public string getKey(string deviceId)
        {
            string value = cache.StringGet(deviceId).ToString();
            Console.WriteLine("Got key = " + deviceId);
            return "[" + value.TrimEnd(',') + "]";
        }

        // Deletes key from database
        public void removeKey(string deviceId)
        {
            cache.KeyDelete(deviceId);
            Console.WriteLine("Removed key = " + deviceId);
        }
        #endregion
    }
}
