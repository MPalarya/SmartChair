using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;

namespace RPiSimulator
{
    class RPiSimulator
    {
        static DeviceClient deviceClient;
        static string iotHubUri = "smartchair-iothub.azure-devices.net";
        static string deviceKey;
        static string deviceId;

        static void Main(string[] args)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "..\\..\\..\\GetDeviceIdentity\\bin\\Release\\GetDeviceIdentity.exe";
            p.Start();

            string[] deviceData = p.StandardOutput.ReadLine().Split();
            p.Close();
            deviceKey = deviceData[0];
            deviceId = deviceData[1];

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Simulated device on id = {0} ; key = {1}\n", deviceId, deviceKey);
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));

            sendDatapointToServer();
            Console.ReadLine();
        }

        private static async void sendDatapointToServer()
        {
            int[] currPressure = new int[7];
            Random rand = new Random();

            while (true)
            {
                for (int i = 0; i < currPressure.Length; i++)
                {
                    currPressure[i] = Math.Max(0, currPressure[i] + rand.Next(-3, 3));
                    currPressure[i] = Math.Min(currPressure[i], 100);
                }

                DataPoint datapoint = new DataPoint(deviceId, DateTime.Now, currPressure);
                MessageStruct<DataPoint> messagestruct = new MessageStruct<DataPoint>(messageId.RpiServer_Datapoint, datapoint);
                string messageString = JsonConvert.SerializeObject(messagestruct);
                Message message = new Message(Encoding.ASCII.GetBytes(messageString));
                Console.WriteLine("Sending message: {0}", messageString);
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("Completed");

                Thread.Sleep(1000);
            }
        }
    }
}
