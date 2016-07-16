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
    ClientServer_ConnectDevice,
    ClientServer_GetLogs,

    Length

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
public struct SMessage<T>
{
    #region Fields

    public EMessageId messageid;
    public T data;

    #endregion

    #region Constructors
    public SMessage(EMessageId messageid, T data)
    {
        this.messageid = messageid;
        this.data = data;
    }
    #endregion
}

// Basic struct for each datapoint received from hardware
public class CDataPoint
{
    #region Fields

    public string deviceId;
    public DateTime datetime;
    public int[] pressure;

    #endregion

    #region Constructors
    public CDataPoint()
    {
        this.deviceId = "";
        this.datetime = DateTime.Now;
        this.pressure = new int[0];
    }

    public CDataPoint(int numOfSensors)
    {
        this.deviceId = "";
        this.datetime = DateTime.Now;
        this.pressure = new int[numOfSensors];
    }

    public CDataPoint(int numOfSensors, int initValue)
        :this(numOfSensors)
    {
        for (int i = 0; i < numOfSensors; i++)
            pressure[i] = initValue;
    }

    public CDataPoint(string deviceId, DateTime datetime, int[] pressure)
    {
        this.deviceId = deviceId;
        this.datetime = datetime;
        this.pressure = pressure;
    }
    #endregion
}

// Stores clientId and bool whether to send data realtime
public class CClient
{
    #region Fields

    public string clientId;
    public string deviceId;
    public bool bReceiveRealTime;

    #endregion

    #region Constructors
    public CClient()
    {
        this.clientId = "";
        this.deviceId = "";
        this.bReceiveRealTime = false;
    }
    public CClient(string clientId, string deviceId)
    {
        this.clientId = clientId;
        this.deviceId = deviceId;
        this.bReceiveRealTime = false;
    }

    public CClient(string clientId, string deviceId, bool sendRealTime)
    {
        this.clientId = clientId;
        this.deviceId = deviceId;
        this.bReceiveRealTime = sendRealTime;
    }
    #endregion
}

// Stores clientId and bool whether to send data realtime
public class CLogLimits
{
    #region Fields

    public DateTime startdate;
    public DateTime enddate;
    public string clientId;

    #endregion

    #region Constructors
    public CLogLimits()
    {
        this.startdate = DateTime.Today;
        this.enddate = DateTime.Today;
        this.clientId = "";
    }
    public CLogLimits(DateTime startdate, DateTime enddate, string clientId)
    {
        this.startdate = startdate;
        this.enddate = enddate;
        this.clientId = clientId;
    }
    #endregion
}

public class CMessageConvert
{
    #region Fields
    private static CMessageConvert instance;
    private static Type[] messageIdToStructMap;
    #endregion

    #region Constructors
    private CMessageConvert()
    {
        messageIdToStructMap = new Type[(int)EMessageId.Length];
        MapMessageIdToStruct();
    }
    #endregion

    #region Properties
    public static CMessageConvert Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new CMessageConvert();
            }
            return instance;
        }
    }
    #endregion

    #region Methods
    private void MapMessageIdToStruct()
    {
        messageIdToStructMap[(int)EMessageId.RpiServer_Datapoint] = typeof(CDataPoint);
        messageIdToStructMap[(int)EMessageId.ServerClient_Datapoint] = typeof(CDataPoint);
        messageIdToStructMap[(int)EMessageId.ServerClient_StopInit] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ServerClient_DayData] = typeof(List<List<object>>);
        messageIdToStructMap[(int)EMessageId.ServerClient_fixPosture] = typeof(CDataPoint);
        messageIdToStructMap[(int)EMessageId.ClientServer_StartRealtime] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_StopRealtime] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_StartInit] = typeof(string);
        messageIdToStructMap[(int)EMessageId.ClientServer_ConnectDevice] = typeof(CClient);
        messageIdToStructMap[(int)EMessageId.ClientServer_GetLogs] = typeof(CLogLimits);
    }

    public Type getTypeByMessageId(EMessageId messageId)
    {
        return messageIdToStructMap[(int)messageId];
    }

    public SMessage<object> decode(string messageString)
    {
        SMessage<object> messageStruct;
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

    private SMessage<object> deserializeMessageTry(string messageString)
    {
        SMessage<object> messageStruct = JsonConvert.DeserializeObject<SMessage<object>>(messageString);
        Type typeToDeserialize = getTypeByMessageId(messageStruct.messageid);
        try
        {
            messageStruct.data = JsonConvert.DeserializeObject(messageStruct.data.ToString(), typeToDeserialize);
        }
        catch (JsonReaderException)
        {
            //Console.WriteLine("Error parsing message data: {0}. If this is a string there is no error.", messageStruct.data.ToString());
        }

        return messageStruct;
    }

    private SMessage<object> deserializeMessageError(string messageString)
    {
        SMessage<object> messageStruct = new SMessage<object>();
        //Console.WriteLine("Error parsing message: {0}", messageString);
        return messageStruct;
    }

    public string encode(EMessageId messageId, object data)
    {
        Type typeToSerialize = getTypeByMessageId(messageId);
        Type typeOfData = typeof(SMessage<>).MakeGenericType(typeToSerialize);
        object messageStruct = Activator.CreateInstance(typeOfData, messageId, data);
        string messageString = JsonConvert.SerializeObject(messageStruct);

        return messageString;
    }
    #endregion
}