using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Sets what type of message is being sent through IOT hub
public enum messageId
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
public struct MessageStruct<T>
{
    #region Fields

    public messageId messageid;
    public T data;

    #endregion

    #region Constructors
    public MessageStruct(messageId messageid, T data)
    {
        this.messageid = messageid;
        this.data = data;
    }
    #endregion
}

// Basic struct for each datapoint received from hardware
public class DataPoint
{
    #region Fields

    public string deviceId;
    public DateTime datetime;
    public int[] pressure;

    #endregion

    #region Constructors
    public DataPoint()
    {
        this.deviceId = "";
        this.datetime = DateTime.Now;
        this.pressure = new int[7];
    }

    public DataPoint(int init)
        :this()
    {
        for (int i = 0; i < 7; i++)
            pressure[i] = init;
    }

    public DataPoint(string deviceId, DateTime datetime, int[] pressure)
    {
        this.deviceId = deviceId;
        this.datetime = datetime;
        this.pressure = pressure;
    }
    #endregion
}

// Stores clientId and bool whether to send data realtime
public class Client
{
    #region Fields

    public string clientId;
    public string deviceId;
    public bool sendRealTime;

    #endregion

    #region Constructors
    public Client()
    {
        this.clientId = "";
        this.deviceId = "";
        this.sendRealTime = false;
    }
    public Client(string clientId, string deviceId)
    {
        this.clientId = clientId;
        this.deviceId = deviceId;
        this.sendRealTime = false;
    }

    public Client(string clientId, string deviceId, bool sendRealTime)
    {
        this.clientId = clientId;
        this.deviceId = deviceId;
        this.sendRealTime = sendRealTime;
    }
    #endregion
}

// Stores clientId and bool whether to send data realtime
public class LogsDate
{
    #region Fields

    public DateTime startdate;
    public DateTime enddate;
    public string clientId;

    #endregion

    #region Constructors
    public LogsDate()
    {
        this.startdate = DateTime.Today;
        this.enddate = DateTime.Today;
        this.clientId = "";
    }
    public LogsDate(DateTime startdate, DateTime enddate, string clientId)
    {
        this.startdate = startdate;
        this.enddate = enddate;
        this.clientId = clientId;
    }
    #endregion
}