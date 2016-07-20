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
        private string deviceKey;
        private string deviceId;
        string RPiId;
        static string iotHubUri = "smartchair-iothub.azure-devices.net";
        static DeviceClient deviceClient;
        CMessageConvert messageConvert;

        public smartChairServerClient()
        {
            messageConvert = CMessageConvert.Instance;

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "..\\..\\..\\GetDeviceIdentity\\bin\\Release\\GetDeviceIdentity.exe";
            p.Start();

            string[] deviceData = p.StandardOutput.ReadLine().Split();
            p.Close();
            deviceKey = deviceData[0];
            deviceId = deviceData[1];

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
    }
}
