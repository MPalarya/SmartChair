using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.Devices.Client;


namespace Client
{
    public class smartChairServerClient : ISmartChairServerClient
    {
        private string deviceKey = "Pz5l6 + AVMj877mvG3 / qqRThVutch4XrdnOjugMh5i + g =";
        private string deviceId = "00326-10000-00000-AA340";
        string RPiId = "00326-10000-00000-AA800"; //orr's computer
        static string iotHubUri = "smartchair-iothub.azure-devices.net";
        static DeviceClient deviceClient;
        CMessageConvert messageConvert;

        public smartChairServerClient()
        {
            messageConvert = CMessageConvert.Instance;

            //Process p = new Process();
            //p.StartInfo.UseShellExecute = false;
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.FileName = "..\\..\\..\\GetDeviceIdentity\\bin\\Release\\GetDeviceIdentity.exe";
            //p.Start();

            //string[] deviceData = p.StandardOutput.ReadLine().Split();
            //p.Close();
            //deviceKey = deviceData[0];
            //deviceId = deviceData[1];

            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
        }
        public Dictionary<DateTime, int> getLastWeekData(string email)
        {
            throw new NotImplementedException();
        }

        public bool initialize(string email)
        {
            sendMessagesToServer(messageConvert.encode(EMessageId.ClientServer_ConnectDevice, new CClient(deviceId, RPiId)));
            sendMessagesToServer(messageConvert.encode(EMessageId.ClientServer_StartInit, deviceId));
            return true;
        }

        //public bool login(string email, string deviceId, string deviceKey)
        //{
            
            
        //    return true;
        //}

        public void startCommunicationWithServer()
        {
            sendMessagesToServer(messageConvert.encode(EMessageId.ClientServer_StartRealtime, deviceId));
        }
        public void stopCommunicationWithServer()
        {
            sendMessagesToServer(messageConvert.encode(EMessageId.ClientServer_StopRealtime, deviceId));
        }
        
        public static async void sendMessagesToServer(string messageString)
        {
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            //Console.WriteLine("Sending message: {0}", messageString);
            await deviceClient.SendEventAsync(message);
            //Console.WriteLine("Completed");
        }

        public bool login(string email, string deviceId, string deviceKey)
        {
            throw new NotImplementedException();
        }
    }

    public class CDeviceMessagesSendReceive
    {
        #region Fields
        private static string connectionString = "HostName=smartchair-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1LHpY6zkPYMuj1pa9rBYYAz9EK3a4rNyOIbW8VYn1sk=";
        private static string iotHubUri = "smartchair-iothub.azure-devices.net";
        private string deviceKey = "Pz5l6+AVMj877mvG3/qqRThVutch4XrdnOjugMh5i+g=";
        private string deviceId = "00326-10000-00000-AA340";
        private DeviceClient deviceClient;
        //private RegistryManager registryManager;
        private Action<string> callbackOnReceiveMessage;
        #endregion Fields

        #region Constuctors

        public CDeviceMessagesSendReceive(Action<string> callbackOnReceiveMessage)
        {
            this.callbackOnReceiveMessage = callbackOnReceiveMessage;
            //registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            //addOrGetDeviceAsync().Wait();
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
        }

        #endregion

        #region Methods


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
            //Console.WriteLine("Sending message: {0}", messageString);
            await deviceClient.SendEventAsync(message);
            //Console.WriteLine("Completed");
        }

        public string getDeviceId()
        {
            return deviceId;
        }

        #endregion
    }
}
