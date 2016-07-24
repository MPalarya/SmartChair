using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Sets what type of message is being sent through IOT hub
public enum EMessageId
{
    #region Fields

    RpiServer_Datapoint,
    ServerClient_Datapoint,
    ServerClient_StopInit,
    ServerClient_DayData,
    ServerClient_fixPosture,
    ClientServer_StartRealtime,
    ClientServer_StopRealtime,
    ClientServer_StartInit,
    ClientServer_PairDevice,
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
    HighPressureLeftHandle,
    HighPressureRightHandle,
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

// Struct to communicate through IOT hub with
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

// Basic struct for each datapoint received from hardware
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
        :this(numOfSensors)
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

// Stores clientId and bool whether to send data realtime
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

// Stores clientId and bool whether to send data realtime
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

public class MessageConverter
{
    #region Fields
    private static MessageConverter instance;
    private static Type[] messageIdToStructMap;
    #endregion

    #region Constructors
    private MessageConverter()
    {
        messageIdToStructMap = new Type[(int)EMessageId.Length];
        MapMessageIdToStruct();
    }
    #endregion

    #region Properties
    public static MessageConverter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new MessageConverter();
            }
            return instance;
        }
    }
    #endregion

    #region Methods
    private void MapMessageIdToStruct()
    {
        messageIdToStructMap[(int)EMessageId.RpiServer_Datapoint] = typeof(Datapoint);
        messageIdToStructMap[(int)EMessageId.ServerClient_Datapoint] = typeof(Datapoint);
        messageIdToStructMap[(int)EMessageId.ServerClient_StopInit] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ServerClient_DayData] = typeof(List<List<object>>);
        messageIdToStructMap[(int)EMessageId.ServerClient_fixPosture] = typeof(EPostureErrorType);
        messageIdToStructMap[(int)EMessageId.ClientServer_StartRealtime] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_StopRealtime] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_StartInit] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_PairDevice] = typeof(ClientProperties);
        messageIdToStructMap[(int)EMessageId.ClientServer_GetLogs] = typeof(LogBounds);
    }

    public Type getTypeByMessageId(EMessageId messageId)
    {
        return messageIdToStructMap[(int)messageId];
    }

    public Message<object> decode(string messageString)
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

    private Message<object> deserializeMessageTry(string messageString)
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

    private Message<object> deserializeMessageError(string messageString)
    {
        Message<object> messageStruct = new Message<object>();
        return messageStruct;
    }

    public string encode(EMessageId messageId, object data)
    {
        Type typeToSerialize = getTypeByMessageId(messageId);
        Type typeOfData = typeof(Message<>).MakeGenericType(typeToSerialize);
        object messageStruct = Activator.CreateInstance(typeOfData, messageId, data);
        string messageString = JsonConvert.SerializeObject(messageStruct);

        return messageString;
    }

    public List<Datapoint> convertRawLogsToDatapointsList(List<List<object>> logs)
    {
        List<Datapoint> retList = new List<Datapoint>();
        foreach (var rawDatapoint in logs)
        {
            retList.Add(new Datapoint(rawDatapoint));
        }

        return retList;
    }

    #endregion
}