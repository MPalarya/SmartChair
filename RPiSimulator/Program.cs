﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Diagnostics;

namespace RPiSimulator
{
    class Program
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

            Console.WriteLine("Simulated device on key {0}\n", deviceId);
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));

            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }

        private static async void SendDeviceToCloudMessagesAsync()
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

                var telemetryDataPoint = new
                {
                    deviceId = deviceId,
                    datetime = DateTime.Now,
                    pressure = currPressure
                };

                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                await deviceClient.SendEventAsync(message);
                Console.WriteLine("Sending message: {0}\n", messageString);

                Task.Delay(1000).Wait();
            }
        }
    }
}