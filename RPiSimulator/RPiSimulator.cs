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
        static private int[] currPressure = new int[6];
        static private string deviceId;

        static void Main(string[] args)
        {
            createDevice = new CreateDevice();
            deviceId = createDevice.getDeviceId();
            string deviceKey = createDevice.getDeviceKey();
            deviceMessagesSendReceive = new DeviceMessagesSendReceive(deviceId, deviceKey);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Simulated device on id = {0}\n", deviceId);

            createDemoData();
            Console.ReadLine();
        }

        private static void createDemoData()
        {
            Random rand = new Random();
            string line;

            for (int i = 0; i < currPressure.Length; i++)
            {
                currPressure[i] = 20;
            }

            SendDemoData();

            do
            {
                line = Console.ReadLine();
                switch (line)
                {
                    case "normal":
                        currPressure[0] = 20;
                        currPressure[1] = 20;
                        currPressure[2] = 20;
                        currPressure[3] = 20;
                        currPressure[4] = 20;
                        currPressure[5] = 20;
                        break;
                    case "left":
                        currPressure[0] =50;
                        currPressure[1] =20;
                        currPressure[2] =20;
                        currPressure[3] =20;
                        currPressure[4] =20;
                        currPressure[5] =20;
                        break;
                    case "right":
                        currPressure[0] =20;
                        currPressure[1] =50;
                        currPressure[2] =20;
                        currPressure[3] =20;
                        currPressure[4] =20;
                        currPressure[5] =20;
                        break;
                    case "back":
                        currPressure[0] =20;
                        currPressure[1] =20;
                        currPressure[2] =50;
                        currPressure[3] =20;
                        currPressure[4] =20;
                        currPressure[5] =20;
                        break;
                    case "random":
                        for (int i = 0; i < currPressure.Length; i++)
                        {
                            currPressure[i] = Math.Max(1, 30 + rand.Next(-20, 21));
                        }
                        break;

                    default:
                        break;
                }
            }
            while (line != null);
        }

        private static async void SendDemoData()
        {
            while(true)
            {
                Datapoint datapoint = new Datapoint(deviceId, DateTime.Now, currPressure);
                Message<Datapoint> messagestruct = new Message<Datapoint>(EMessageId.RpiServer_Datapoint, datapoint);
                string messageString = JsonConvert.SerializeObject(messagestruct);

                Console.WriteLine("Sending message: {0}", messageString);
                deviceMessagesSendReceive.sendMessageToServerAsync(messageString);

                await Task.Delay(1000);
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
