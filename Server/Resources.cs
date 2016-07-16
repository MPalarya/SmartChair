using System;
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
        this.pressure = new int[7];
    }

    public CDataPoint(int init)
        :this()
    {
        for (int i = 0; i < 7; i++)
            pressure[i] = init;
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
    public bool sendRealTime;

    #endregion

    #region Constructors
    public CClient()
    {
        this.clientId = "";
        this.deviceId = "";
        this.sendRealTime = false;
    }
    public CClient(string clientId, string deviceId)
    {
        this.clientId = clientId;
        this.deviceId = deviceId;
        this.sendRealTime = false;
    }

    public CClient(string clientId, string deviceId, bool sendRealTime)
    {
        this.clientId = clientId;
        this.deviceId = deviceId;
        this.sendRealTime = sendRealTime;
    }
    #endregion
}

// Stores clientId and bool whether to send data realtime
public class CLogsDate
{
    #region Fields

    public DateTime startdate;
    public DateTime enddate;
    public string clientId;

    #endregion

    #region Constructors
    public CLogsDate()
    {
        this.startdate = DateTime.Today;
        this.enddate = DateTime.Today;
        this.clientId = "";
    }
    public CLogsDate(DateTime startdate, DateTime enddate, string clientId)
    {
        this.startdate = startdate;
        this.enddate = enddate;
        this.clientId = clientId;
    }
    #endregion
}