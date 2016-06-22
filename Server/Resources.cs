using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Sets what type of message is being sent through IOT hub
public enum messageId
{
    RpiServer_Datapoint,
    ServerClient_Datapoint,
    ServerClient_StopInit,
    ClientServer_StartRealtime,
    ClientServer_StopRealtime,
    ClientServer_StartInit,
    ServerClient_DayData,
}

// Basic struct for each datapoint received from hardware
public struct dataPoint
{
    #region Fields

    public string deviceId;
    public DateTime datetime;
    public int[] pressure;

    #endregion

    #region Constructors
    public dataPoint(int init)
    {
        this.deviceId = "";
        this.datetime = DateTime.Now;
        this.pressure = new int[7];

        for (int i = 0; i < 7; i++)
            pressure[i] = init;
    }

    public dataPoint(string deviceId, DateTime datetime, int[] pressure)
    {
        this.deviceId = deviceId;
        this.datetime = datetime;
        this.pressure = pressure;
    }
    #endregion
}

public struct messageStruct<T>
{
    #region Fields

    public messageId messageid;
    public T data;

    #endregion

    #region Constructors
    public messageStruct(messageId messageid, T data)
    {
        this.messageid = messageid;
        this.data = data;
    }
    #endregion
}