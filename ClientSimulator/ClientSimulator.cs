using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Management;

// id = 00326-10000-00000-AA800

namespace ClientSimulator
{
    class ClientSimulator
    {
        static private CMessageConvert messageConvert;
        static private CDeviceMessagesSendReceive deviceMessagesSendReceive;

        static void Main(string[] args)
        {
            messageConvert = CMessageConvert.Instance;
            deviceMessagesSendReceive = new CDeviceMessagesSendReceive(printMessage);
            string deviceId = deviceMessagesSendReceive.getDeviceId();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Receiving cloud to device messages from service\n");

            deviceMessagesSendReceive.receiveMessages();

            string line;
            while(true)
            {
                line = Console.ReadLine();
                if (line[0] == '!')
                    continue;

                if (line == "start")
                {
                    deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_StartRealtime, deviceId));
                }
                else if (line == "stop")
                {
                    deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_StopRealtime, deviceId));
                }
                else if (line == "init")
                {
                    deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_StartInit, deviceId));
                }
                else if (line[0] == '%')
                {
                    deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_ConnectDevice, new CClient(deviceId, line.Substring(1, line.Length - 1))));
                }
                else if (line[0] == '<')
                {
                    deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_GetLogs, new CLogLimits(new DateTime(2016, 07, 11), new DateTime(2016, 07, 12), deviceId)));
                }
            }
        }

        public static void printMessage(string messageString)
        {
            SMessage<object> messageStruct;
            messageStruct = JsonConvert.DeserializeObject<SMessage<object>>(messageString);
            Console.WriteLine("!Received message {0}, data = {1}", messageStruct.messageid, messageStruct.data);
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

        public CDeviceMessagesSendReceive(Action<string> callbackOnReceiveMessage)
        {
            this.callbackOnReceiveMessage = callbackOnReceiveMessage;
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

        public void receiveMessages()
        {
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
            Console.WriteLine("Sending message: {0}", messageString);
            await deviceClient.SendEventAsync(message);
            Console.WriteLine("Completed");
        }

        public string getDeviceId()
        {
            return deviceId;
        }

        #endregion
    }
}
