using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Diagnostics;

// id = 00326-10000-00000-AA800
 
namespace ClientSimulator
{
    class ClientSimulator
    {
        static DeviceClient deviceClient;
        static string iotHubUri = "smartchair-iothub.azure-devices.net";
        static string deviceKey;
        static string deviceId;
        static CMessageConvert messageConvert;

        static void Main(string[] args)
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

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Receiving cloud to device messages from service\n");
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));

            ReceiveC2dAsync();
            string line;
            while(true)
            {
                line = Console.ReadLine();
                if (line[0] == '!')
                    continue;

                if (line == "start")
                {
                    sendMessageToServer(messageConvert.encode(EMessageId.ClientServer_StartRealtime, deviceId));
                }
                else if (line == "stop")
                {
                    sendMessageToServer(messageConvert.encode(EMessageId.ClientServer_StopRealtime, deviceId));
                }
                else if (line == "init")
                {
                    sendMessageToServer(messageConvert.encode(EMessageId.ClientServer_StartInit, deviceId));
                }
                else if (line[0] == '%')
                {
                    sendMessageToServer(messageConvert.encode(EMessageId.ClientServer_ConnectDevice, new CClient(deviceId, line.Substring(1, line.Length - 1))));
                }
                else if (line[0] == '<')
                {
                    sendMessageToServer(messageConvert.encode(EMessageId.ClientServer_GetLogs, new CLogLimits(new DateTime(2016, 07, 11), new DateTime(2016, 07, 12), deviceId)));
                }
            }
        }

        private static async void sendMessageToServer(string messageString)
        {
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            Console.WriteLine("Sending message: {0}", messageString);
            await deviceClient.SendEventAsync(message);
            Console.WriteLine("Completed");
        }

        private static async void ReceiveC2dAsync()
        {
            SMessage<object> messagestruct;

            while (true)
            {
                Message receivedMessage = await deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;
                messagestruct = JsonConvert.DeserializeObject<SMessage<object>>(Encoding.ASCII.GetString(receivedMessage.GetBytes()).ToString());
                Console.WriteLine("!Received message {0}, data = {1}", messagestruct.messageid, messagestruct.data);
                await deviceClient.CompleteAsync(receivedMessage);
            }
        }
    }
}
