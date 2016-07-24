using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace RPiConsole
{
    public class CRPiServerProxy : IRPiServerProxy
    {
        #region Fields
        private static CRPiServerProxy instance;
        private static DeviceMessagesSendReceive deviceMessagesSendReceive;
        private static MessageConverter messageConvert;
        #endregion

        #region Constructors
        private CRPiServerProxy()
        {
            messageConvert = MessageConverter.Instance;
            deviceMessagesSendReceive = new DeviceMessagesSendReceive();
        }
        #endregion

        #region Properties
        public static CRPiServerProxy Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CRPiServerProxy();
                }
                return instance;
            }
        }
        #endregion

        #region Methods
        public void RPiServer_newDataSample(DateTime datetime, int[] pressure)
        {
            Datapoint datapoint = new Datapoint(deviceMessagesSendReceive.getDeviceId(), datetime, pressure);
            Message<Datapoint> messagestruct = new Message<Datapoint>(EMessageId.RpiServer_Datapoint, datapoint);
            string messageString = JsonConvert.SerializeObject(messagestruct);

            Console.WriteLine("Sending message: {0}", messageString);
            deviceMessagesSendReceive.sendMessageToServerAsync(messageString);
        }
        #endregion
    }

    public class DeviceMessagesSendReceive
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

        public DeviceMessagesSendReceive()
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
