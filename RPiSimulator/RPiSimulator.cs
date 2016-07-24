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
        private static CDeviceMessagesSendReceive deviceMessagesSendReceive;
        private static MessageConverter messageConvert;

        static private string deviceId;

        static void Main(string[] args)
        {
            messageConvert = MessageConverter.Instance;
            deviceMessagesSendReceive = new CDeviceMessagesSendReceive();
            deviceId = deviceMessagesSendReceive.getDeviceId();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Simulated device on id = {0}\n", deviceId);

            createAndSendTelemetryDatapointToServer();
            Console.ReadLine();
        }

        private static void createAndSendTelemetryDatapointToServer()
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

                Datapoint datapoint = new Datapoint(deviceId, DateTime.Now, currPressure);
                Message<Datapoint> messagestruct = new Message<Datapoint>(EMessageId.RpiServer_Datapoint, datapoint);
                string messageString = JsonConvert.SerializeObject(messagestruct);

                Console.WriteLine("Sending message: {0}", messageString);
                deviceMessagesSendReceive.sendMessageToServerAsync(messageString);

                Thread.Sleep(1000);
            }
        }
    }

    public class CDeviceMessagesSendReceive
    {
        #region Fields
        private static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";
        private static string iotHubUri = "smartchair-iothub.azure-devices.net";
        private string deviceKey;
        private string deviceId;
        private DeviceClient deviceClient;
        private RegistryManager registryManager;
        private Action<string> callbackOnReceiveMessage;
        #endregion Fields

        #region Constuctors

        public CDeviceMessagesSendReceive()
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            addOrGetDeviceAsync().Wait();
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
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

        public void receiveMessages(Action<string> callbackOnReceiveMessage)
        {
            this.callbackOnReceiveMessage = callbackOnReceiveMessage;
            receiveMessagesAsync();
        }

        private async void receiveMessagesAsync()
        {
            string messageString;

            while (true)
            {
                Microsoft.Azure.Devices.Client.Message receivedMessage = await deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;
                messageString = Encoding.ASCII.GetString(receivedMessage.GetBytes()).ToString();
                callbackOnReceiveMessage(messageString);
                await deviceClient.CompleteAsync(receivedMessage);
            }
        }

        public async void sendMessageToServerAsync(string messageString)
        {
            Microsoft.Azure.Devices.Client.Message message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
            await deviceClient.SendEventAsync(message);
        }

        public string getDeviceId()
        {
            return deviceId;
        }

        #endregion
    }
}
