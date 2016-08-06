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
        static private MessageConverter messageConvert;
        static private DeviceMessagesSendReceive deviceMessagesSendReceive;
        static private CreateDevice createDevice;

        static void Main(string[] args)
        {
            messageConvert = MessageConverter.Instance;
            createDevice = new CreateDevice();
            string deviceId = createDevice.getDeviceId();
            string deviceKey = createDevice.getDeviceKey();
            deviceMessagesSendReceive = new DeviceMessagesSendReceive(deviceId, deviceKey);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Receiving cloud to device messages from service\n");

            deviceMessagesSendReceive.receiveMessages(printMessage);

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
                    deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_PairDevice, new ClientProperties(deviceId, line.Substring(1, line.Length - 1).Trim())));
                }
                else if (line[0] == '<')
                {
                    deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_GetLogs, new LogBounds(new DateTime(2016, 08, 6), new DateTime(2016, 08, 7), deviceId)));
                }
            }
        }

        public static void printMessage(string messageString)
        {
            Message<object> messageStruct;
            messageStruct = JsonConvert.DeserializeObject<Message<object>>(messageString);
            Console.WriteLine("!Received message {0}, data = {1}", messageStruct.messageid, messageStruct.data);
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
