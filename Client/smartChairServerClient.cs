using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.Devices.Client;


namespace Client
{
    public class smartChairServerClient 
    {
        private static string deviceKey = "Pz5l6+AVMj877mvG3/qqRThVutch4XrdnOjugMh5i+g=";
        private static string deviceId = "00326-10000-00000-AA340";
        private static string RPiId = "00326-10000-00000-AA800"; //"SmartChair01"; ////orr's computer
        private MessageConverter messageConvert;
        private DeviceMessagesSendReceive deviceMessagesSendReceive;

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public delegate void PostureErrorEventHandler(object sender, postureErrorTypeEventArgs e);
        public delegate void DayDataEventHandler(object sender, dayDataEventArgs e);
        public event ChangedEventHandler HandleFinish;
        public event PostureErrorEventHandler postureError;
        public event DayDataEventHandler dayData;

        public bool isInitialize{ get; set; }
        private static smartChairServerClient m_smartChairServerClient;

        public static smartChairServerClient Instance
        {
            get
            {
                if (m_smartChairServerClient == null)
                    m_smartChairServerClient = new smartChairServerClient();

                return m_smartChairServerClient;
                
            }
        }

        public smartChairServerClient()
        {
            messageConvert = MessageConverter.Instance;
            deviceMessagesSendReceive = new DeviceMessagesSendReceive(deviceId, deviceKey);
            deviceMessagesSendReceive.receiveMessages(handleMessagesReceivedFromServer);

            isInitialize = true;

            pairWithOrrsDeviceTest();
            startCollectingInitData();
            //startCommunicationWithServer(); gives raw data- no need in client
        }

        private void handleMessagesReceivedFromServer(string messageString)
        {
            Message<object> messageStruct = messageConvert.decode(messageString);
            switch (messageStruct.messageid)
            {
                case EMessageId.ServerClient_Datapoint:
                    if (!isInitialize) return;
                    Datapoint datapoint = (Datapoint)messageStruct.data;
                    // not needed for client right now
                    break;

                case EMessageId.ServerClient_DayData:
                    if (!isInitialize) return;
                    List<List<object>> rawLogs = (List<List<object>>)messageStruct.data;
                    handleReceiveDataLogs(rawLogs);
                    break;

                case EMessageId.ServerClient_fixPosture:
                    if (!isInitialize) return;
                    EPostureErrorType postureErrorType = (EPostureErrorType)messageStruct.data;
                    handlePostureError(postureErrorType);
                    break;

                case EMessageId.ServerClient_StopInit:
                    handleFinishedInit();
                    break;
            }
        }
        
        private void onDayData(dayDataEventArgs e)
        {
            if (dayData != null)
                dayData(this, e);
        }

        private void handleReceiveDataLogs(List<List<object>> rawLogs)
        {
            List<toClientDataPoint> logs = messageConvert.convertRawLogsToDatapointsList(rawLogs);

            onDayData(new dayDataEventArgs(logs));
            
        }

        private void handlePostureError(EPostureErrorType postureErrorType)
        {
            OnPostureError(new postureErrorTypeEventArgs(postureErrorType));

        }

        protected virtual void OnPostureError(postureErrorTypeEventArgs e)
        {
            if (postureError != null)
                postureError(this, e);
        }

        protected virtual void OnHandleFinish(EventArgs e)
        {
            if (HandleFinish != null)
                HandleFinish(this, e);
        }

        private void handleFinishedInit()
        {
            // TODO Sivan: notify user we have finished collecting initializing data
            OnHandleFinish(EventArgs.Empty);
            isInitialize = true;
        }

        public void getLogsByDateTimeBounds(DateTime startdate, DateTime enddate)
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_GetLogs, new LogBounds(startdate, enddate, deviceId)));
        }

        public void pairWithDevice(string deviceIdtoPairWith)
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_PairDevice, new ClientProperties(deviceId, deviceIdtoPairWith)));
        }

        public void pairWithOrrsDeviceTest()
        {
            deviceMessagesSendReceive.sendMessageToServerAsync(messageConvert.encode(EMessageId.ClientServer_PairDevice, new ClientProperties(deviceId, RPiId)));
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

    public class postureErrorTypeEventArgs : EventArgs
    {
        private EPostureErrorType m_errorType;
        public postureErrorTypeEventArgs(EPostureErrorType errorType)
        {
            m_errorType = errorType;
        }

        public EPostureErrorType ErrorType {get { return m_errorType; }}
    }

    public class dayDataEventArgs: EventArgs
    {
        private List<toClientDataPoint> m_dataPoints;
        public dayDataEventArgs(List<toClientDataPoint> dataPoints)
        {
            m_dataPoints = dataPoints;
        }

        public List<toClientDataPoint> DayDataPoints { get { return m_dataPoints; } }
    }
}
