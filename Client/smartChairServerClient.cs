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
        private static string deviceKey = "Pz5l6+AVMj877mvG3/qqRThVutch4XrdnOjugMh5i+g=";
        private static string deviceId = "00326-10000-00000-AA340";
        private static string RPiId = "00326-10000-00000-AA800"; //orr's computer
        private static string iotHubUri = "smartchair-iothub.azure-devices.net";
        private CMessageConvert messageConvert;
        private CDeviceMessagesSendReceive deviceMessagesSendReceive;

        public smartChairServerClient()
        {
            messageConvert = CMessageConvert.Instance;
            deviceMessagesSendReceive = new CDeviceMessagesSendReceive(deviceId, deviceKey);
            deviceMessagesSendReceive.receiveMessages(handleMessagesReceivedFromServer);
        }

        private void handleMessagesReceivedFromServer(string messageString)
        {
            SMessage<object> messageStruct = messageConvert.decode(messageString);
            switch (messageStruct.messageid)
            {
                case EMessageId.ServerClient_Datapoint:
                    CDataPoint datapoint = (CDataPoint)messageStruct.data;
                    handleRealtimeDatapoint(datapoint);
                    break;

                case EMessageId.ServerClient_DayData:
                    List<List<object>> rawLogs = (List<List<object>>)messageStruct.data;
                    
                    break;

                case EMessageId.ServerClient_fixPosture:
                    EPostureErrorType postureErrorType = (EPostureErrorType)messageStruct.data;
                    handlePostureError(postureErrorType);
                    break;

                case EMessageId.ServerClient_StopInit:
                    handleFinishedInit();
                    break;
            }
        }

        private void handleRealtimeDatapoint(CDataPoint datapoint)
        {
            // TODO Sivan: display data point in real time screen
        }

        private void handleReceiveDataLogs(List<List<object>> rawLogs)
        {
            List<CDataPoint> logs = messageConvert.convertRawLogsToDatapointsList(rawLogs);
            // TODO Sivan: display logs received
        }

        private void handlePostureError(EPostureErrorType postureErrorType)
        {
            // TODO Sivan: notify user of posture error
        }

        private void handleFinishedInit()
        {
            // TODO Sivan: notify user we have finished collecting initializing data
        }

        public void getLogsByDateTimeBounds(DateTime startdate, DateTime enddate)
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_GetLogs, new CLogLimits(startdate, enddate, deviceId)));
        }

        public void pairWithDevice(string deviceIdtoPairWith)
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_ConnectDevice, new CClient(deviceId, deviceIdtoPairWith)));
        }

        public void pairWithOrrsDeviceTest()
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_ConnectDevice, new CClient(deviceId, RPiId)));
        }

        public void startCollectingInitData()
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_StartInit, deviceId));
        }

        public void startCommunicationWithServer()
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_StartRealtime, deviceId));
        }

        public void stopCommunicationWithServer()
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_StopRealtime, deviceId));
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
        private Action<string> callbackOnReceiveMessage;
        #endregion Fields

        #region Constuctors

        public CDeviceMessagesSendReceive(string deviceId, string deviceKey)
        {
            this.deviceId = deviceId;
            this.deviceKey = deviceKey;
            connectToDeviceClient();
        }

        #endregion

        #region Methods

        private void connectToDeviceClient()
        {
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
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
                Message receivedMessage = await deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;
                messageString = Encoding.ASCII.GetString(receivedMessage.GetBytes()).ToString();
                callbackOnReceiveMessage(messageString);
                await deviceClient.CompleteAsync(receivedMessage);
            }
        }

        public async void sendMessageToServerAsync(string messageString)
        {
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            await deviceClient.SendEventAsync(message);
        }

        public string getDeviceId()
        {
            return deviceId;
        }

        #endregion
    }
}
