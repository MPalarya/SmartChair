using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Globals
{
    #region Properties
    public static int NUMBER_OF_DATAPOINTS_TO_AGGREGATE { get; private set; } = 5;
    public static int MAX_SITTING_TIME_IN_SECONDS { get; private set; } = 120;
    public static int MAX_DIFFERENCE_BETWEEN_CONTINUAL_SITTING { get; private set; } = 30;
    public static double CORRECT_SITTING_RATIO_THRESHOLD { get; private set; } = 0.4; // = 40%

    #endregion
}

// Sets what type of message is being sent through IOT hub
public enum EMessageId
{
    #region Fields

    RpiServer_Datapoint,
    ServerClient_Datapoint,
    ServerClient_StopInit,
    ServerClient_resultLogsError,
    ServerClient_resultLogs,
    ServerClient_fixPosture,
    ServerClient_noDeviceConnected,
    ClientServer_StartRealtime,
    ClientServer_StopRealtime,
    ClientServer_StartInit,
    ClientServer_PairDevice,
    ClientServer_GetLogsError,
    ClientServer_GetLogs,

    Length

    #endregion
}

public enum EPostureErrorType
{
    #region Fields
    Correct,
    HighPressureLeftSeat,
    HighPressureRightSeat,
    HighPressureLeftBack,
    HighPressureRightBack,
    ContinuallySittingTooLong,
    HighPressureUpperBackToLowerBack,
    HighPressureLowerBackToUpperBack,
    CannotAnalyzeData,
    #endregion
}

/// <summary>
/// represents the part of chair that populates sensors.
/// computations logic is different depending on selected part.
/// </summary>
public enum EChairPart
{
    #region Fields

    Seat,
    Back,
    Handles,

    #endregion
}

/// <summary>
/// roughly divided into 3 rows and 2 coloumns to indicate the specific area onto part's surface.
/// </summary>
public enum EChairPartArea
{
    #region Fields

    LeftFront,
    LeftMid,
    LeftRear,
    LeftTop = LeftFront,
    LeftBottom = LeftRear,

    RightFront,
    RightMid,
    RightRear,
    RightTop = RightFront,
    RightBottom = RightRear,

    #endregion
}

public struct Message<T>
{
    #region Fields

    public EMessageId messageid;
    public T data;

    #endregion

    #region Constructors
    public Message(EMessageId messageid, T data)
    {
        this.messageid = messageid;
        this.data = data;
    }
    #endregion
}

public class Datapoint
{
    #region Fields

    public string deviceId;
    public DateTime datetime;
    public int[] pressure;

    #endregion

    #region Constructors
    public Datapoint()
    {
        this.deviceId = "";
        this.datetime = DateTime.Now;
        this.pressure = new int[0];
    }

    public Datapoint(int numOfSensors)
    {
        this.deviceId = "";
        this.datetime = DateTime.Now;
        this.pressure = new int[numOfSensors];
    }

    public Datapoint(int numOfSensors, int initValue)
        : this(numOfSensors)
    {
        for (int i = 0; i < numOfSensors; i++)
            pressure[i] = initValue;
    }

    public Datapoint(string deviceId, DateTime datetime, int[] pressure)
    {
        this.deviceId = deviceId;
        this.datetime = datetime;
        this.pressure = pressure;
    }

    public Datapoint(List<object> rawDataPoint)
    {
        this.deviceId = "";
        this.datetime = DateTime.Parse((string)rawDataPoint[0]);
        this.pressure = (int[])rawDataPoint[1];
    }
    #endregion
}

public class toClientDataPoint
{
    #region Fields
    public DateTime datetime { get; set; }
    public long pressure { get; set; }
    #endregion

    #region Constructors
    public toClientDataPoint(List<object> rawDataPoint)
    {
        //this.datetime = DateTime.Parse((string)rawDataPoint[0]);
        this.datetime = DateTime.ParseExact((string)rawDataPoint[0], "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
        this.pressure = (long)rawDataPoint[1];
    }
    #endregion
}

public class ClientProperties
{
    #region Fields

    public string clientId;
    public string deviceId;
    public bool bReceiveRealTime;

    #endregion

    #region Constructors
    public ClientProperties()
    {
        this.clientId = "";
        this.deviceId = "";
        this.bReceiveRealTime = false;
    }
    public ClientProperties(string clientId, string deviceId)
    {
        this.clientId = clientId;
        this.deviceId = deviceId;
        this.bReceiveRealTime = false;
    }

    public ClientProperties(string clientId, string deviceId, bool sendRealTime)
    {
        this.clientId = clientId;
        this.deviceId = deviceId;
        this.bReceiveRealTime = sendRealTime;
    }
    #endregion
}

public class LogBounds
{
    #region Fields

    public DateTime startdate;
    public DateTime enddate;
    public string clientId;

    #endregion

    #region Constructors
    public LogBounds()
    {
        this.startdate = DateTime.Today;
        this.enddate = DateTime.Today;
        this.clientId = "";
    }
    public LogBounds(DateTime startdate, DateTime enddate, string clientId)
    {
        this.startdate = startdate;
        this.enddate = enddate;
        this.clientId = clientId;
    }
    #endregion
}

public static class MessageConverter
{
    #region Fields
    private static Type[] messageIdToStructMap;
    #endregion

    #region Constructors
    static MessageConverter()
    {
        messageIdToStructMap = new Type[(int)EMessageId.Length];
        MapMessageIdToStruct();
    }
    #endregion

    #region Methods
    private static void MapMessageIdToStruct()
    {
        messageIdToStructMap[(int)EMessageId.RpiServer_Datapoint] = typeof(Datapoint);
        messageIdToStructMap[(int)EMessageId.ServerClient_Datapoint] = typeof(Datapoint);
        messageIdToStructMap[(int)EMessageId.ServerClient_StopInit] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ServerClient_resultLogsError] = typeof(List<List<object>>);
        messageIdToStructMap[(int)EMessageId.ServerClient_resultLogs] = typeof(List<List<object>>);
        messageIdToStructMap[(int)EMessageId.ServerClient_fixPosture] = typeof(EPostureErrorType);
        messageIdToStructMap[(int)EMessageId.ServerClient_noDeviceConnected] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_StartRealtime] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_StopRealtime] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_StartInit] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_PairDevice] = typeof(ClientProperties);
        messageIdToStructMap[(int)EMessageId.ClientServer_GetLogsError] = typeof(LogBounds);
        messageIdToStructMap[(int)EMessageId.ClientServer_GetLogs] = typeof(LogBounds);
    }

    public static Type getTypeByMessageId(EMessageId messageId)
    {
        return messageIdToStructMap[(int)messageId];
    }

    public static Message<object> decode(string messageString)
    {
        Message<object> messageStruct;
        try
        {
            messageStruct = deserializeMessageTry(messageString);
        }
        catch (JsonReaderException)
        {
            messageStruct = deserializeMessageError(messageString);
        }

        return messageStruct;
    }

    private static Message<object> deserializeMessageTry(string messageString)
    {
        Message<object> messageStruct = JsonConvert.DeserializeObject<Message<object>>(messageString);
        Type typeToDeserialize = getTypeByMessageId(messageStruct.messageid);
        try
        {
            messageStruct.data = JsonConvert.DeserializeObject(messageStruct.data.ToString(), typeToDeserialize);
        }
        catch (JsonReaderException)
        {
        }

        return messageStruct;
    }

    private static Message<object> deserializeMessageError(string messageString)
    {
        Message<object> messageStruct = new Message<object>();
        return messageStruct;
    }

    public static string encode(EMessageId messageId, object data)
    {
        Type typeToSerialize = getTypeByMessageId(messageId);
        Type typeOfData = typeof(Message<>).MakeGenericType(typeToSerialize);
        object messageStruct = Activator.CreateInstance(typeOfData, messageId, data);
        string messageString = JsonConvert.SerializeObject(messageStruct);

        return messageString;
    }

    public static List<toClientDataPoint> convertRawLogsToDatapointsList(List<List<object>> logs)
    {
        List<toClientDataPoint> retList = new List<toClientDataPoint>();
        int count = 0;
        foreach (var rawDatapoint in logs)
        {
            try
            {
                retList.Add(new toClientDataPoint(rawDatapoint));
                count++;
            }
            catch (Exception e)
            {

                //throw;
            }
            
           
        }

        return retList;
    }

    #endregion
}

public static class ChairPartConverter
{
    #region Fields
    private static Dictionary<EChairPart, Dictionary<EChairPartArea, int>> mapChairPartToIndex;
    private static Tuple<EChairPart, EChairPartArea>[] mapIndexToChairPart;
    private static int maxIndex;
    #endregion

    #region Constructors
    static ChairPartConverter()
    {
        MapChairPartToIndex();
        MapIndexToChairPart();
    }
    #endregion

    #region Methods
    private static void MapChairPartToIndex()
    {
        mapChairPartToIndex = new Dictionary<EChairPart, Dictionary<EChairPartArea, int>>();

        mapChairPartToIndex.Add(EChairPart.Back, new Dictionary<EChairPartArea, int>());
        mapChairPartToIndex.Add(EChairPart.Handles, new Dictionary<EChairPartArea, int>());
        mapChairPartToIndex.Add(EChairPart.Seat, new Dictionary<EChairPartArea, int>());

        mapChairPartToIndex[EChairPart.Seat].Add(EChairPartArea.LeftMid, 0);
        mapChairPartToIndex[EChairPart.Seat].Add(EChairPartArea.RightMid, 1);
        mapChairPartToIndex[EChairPart.Back].Add(EChairPartArea.LeftBottom, 2);
        mapChairPartToIndex[EChairPart.Back].Add(EChairPartArea.RightBottom, 3);
        mapChairPartToIndex[EChairPart.Back].Add(EChairPartArea.LeftTop, 4);
        mapChairPartToIndex[EChairPart.Back].Add(EChairPartArea.RightTop, 5);

        maxIndex = 5;
    }

    private static void MapIndexToChairPart()
    {
        mapIndexToChairPart = new Tuple<EChairPart, EChairPartArea>[maxIndex + 1];
        foreach (var chairPart in mapChairPartToIndex)
        {
            foreach (var partArea in chairPart.Value)
            {
                mapIndexToChairPart[partArea.Value] = new Tuple<EChairPart, EChairPartArea>(chairPart.Key, partArea.Key);
            }
        }
    }

    public static int getIndexByChairPart(EChairPart chairPart, EChairPartArea chairPartArea)
    {
        Dictionary<EChairPartArea, int> chairPartAreaDict;
        if (mapChairPartToIndex.TryGetValue(chairPart, out chairPartAreaDict))
        {
            int index;
            if (chairPartAreaDict.TryGetValue(chairPartArea, out index))
            {
                return index;
            }
        }

        return -1;
    }

    public static Tuple<EChairPart, EChairPartArea> getChairPartByIndex(int index)
    {
        if (index >= mapIndexToChairPart.Length || index < 0)
            return null;

        return mapIndexToChairPart[index];
    }

    public static int[] dictionaryToArrayOrNullIfEmpty(Dictionary<EChairPart, Dictionary<EChairPartArea, int>> dataDictionary)
    {
        bool isDataEmpty = true;
        int[] dataArray = new int[maxIndex + 1];

        foreach (var chairPart in dataDictionary)
        {
            foreach (var partArea in chairPart.Value)
            {
                int index = ChairPartConverter.getIndexByChairPart(chairPart.Key, partArea.Key);
                dataArray[index] = partArea.Value;

                if (partArea.Value > 0)
                    isDataEmpty = false;
            }
        }

        if (isDataEmpty)
            return null;

        return dataArray;
    }
    #endregion
}

public class DeviceMessagesSendReceive
{
    #region Fields
    private static string iotHubUri = "smartchair-iothub.azure-devices.net";
    private string deviceKey;
    private string deviceId;
    private DeviceClient deviceClient;
    private Action<string> callbackOnReceiveMessage;
    #endregion Fields

    #region Constuctors

    public DeviceMessagesSendReceive(string deviceId, string deviceKey)
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
            try
            {
                Message receivedMessage = await deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;
                messageString = Encoding.ASCII.GetString(receivedMessage.GetBytes()).ToString();
                callbackOnReceiveMessage(messageString);
                await deviceClient.CompleteAsync(receivedMessage);
            }
            catch (Exception e)
            {

            }
        }
    }

    public async void sendMessageToServerAsync(string messageString)
    {
        try
        {
            Message message = new Message(Encoding.ASCII.GetBytes(messageString));
            await deviceClient.SendEventAsync(message);
        }
        catch (Exception e)
        {
            //Console.WriteLine(e);
        }

    }

    public string getDeviceId()
    {
        return deviceId;
    }

    #endregion
}