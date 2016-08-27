using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.Management;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace RPiSimulator
{
    class RPiSimulator
    {
        static private DeviceMessagesSendReceive deviceMessagesSendReceive;
        static private CreateDevice createDevice;

        static private string deviceId;

        static void Main(string[] args)
        {
            createDevice = new CreateDevice();
            deviceId = createDevice.getDeviceId();
            string deviceKey = createDevice.getDeviceKey();
            deviceMessagesSendReceive = new DeviceMessagesSendReceive(deviceId, deviceKey);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Simulated device on id = {0}\n", deviceId);

            //createAndSendTelemetryDatapointToServer();
            createDemoData();
            Console.ReadLine();
        }

        private static void createDemoData()
        {
            int[] currPressure = new int[6];
            string s;

            do
            {
                s = Console.ReadLine();
                switch (s)
                {
                    case "left":
                        currPressure[0] =50;
                        currPressure[1] =2;
                        currPressure[2] =2;
                        currPressure[3] =2;
                        currPressure[4] =2;
                        currPressure[5] =2;
                        break;
                    case "right":
                        currPressure[0] =2;
                        currPressure[1] =50;
                        currPressure[2] =2;
                        currPressure[3] =2;
                        currPressure[4] =2;
                        currPressure[5] =2;
                        break;
                    case "back":
                        currPressure[0] =2;
                        currPressure[1] =2;
                        currPressure[2] =50;
                        currPressure[3] =2;
                        currPressure[4] =2;
                        currPressure[5] =2;
                        break;
                    default:
                        break;
                }
                Datapoint datapoint = new Datapoint(deviceId, DateTime.Now, currPressure);
                Message<Datapoint> messagestruct = new Message<Datapoint>(EMessageId.RpiServer_Datapoint, datapoint);
                string messageString = JsonConvert.SerializeObject(messagestruct);

                Console.WriteLine("Sending message: {0}", messageString);
                deviceMessagesSendReceive.sendMessageToServerAsync(messageString);
            }
            while (s != null);
            
        }

        private static void createAndSendTelemetryDatapointToServer()
        {
            int[] currPressure = new int[6];
            Random rand = new Random();

            for(int i = 0; i < currPressure.Length; i++)
            {
                currPressure[i] = 30;
            }

            while (true)
            {
                for (int i = 0; i < currPressure.Length; i++)
                {
                    currPressure[i] = Math.Max(1, currPressure[i] + rand.Next(-1, 2));
                    currPressure[i] = Math.Min(currPressure[i], 100);
                }

                Datapoint datapoint = new Datapoint(deviceId, DateTime.Now, currPressure);
                Message<Datapoint> messagestruct = new Message<Datapoint>(EMessageId.RpiServer_Datapoint, datapoint);
                string messageString = JsonConvert.SerializeObject(messagestruct);

                Console.WriteLine("Sending message: {0}", messageString);
                deviceMessagesSendReceive.sendMessageToServerAsync(messageString);

                Thread.Sleep(1000);
            }
        }
    }

    public class CreateDevice
    {
        #region Fields
        private static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";
        private string deviceKey;
        private string deviceId;
        private RegistryManager registryManager;
        #endregion Fields

        #region Constuctors

        public CreateDevice()
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            addOrGetDeviceAsync().Wait();
        }

        #endregion

        #region Methods

        private async Task addOrGetDeviceAsync()
        {
            deviceId = getOsSerialNumber();
            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }

            deviceKey = device.Authentication.SymmetricKey.PrimaryKey;
        }

        private string getOsSerialNumber()
        {
            ManagementObject o = new ManagementObject("Win32_OperatingSystem=@");
            string serial = (string)o["SerialNumber"];

            return serial;
        }

        public string getDeviceId()
        {
            return deviceId;
        }

        public string getDeviceKey()
        {
            return deviceKey;
        }

        #endregion
    }
}
