namespace Server
{
    public interface DbInterface
    {
        CClient getClientByDevice(string deviceId);
        string getDeviceByClient(string clientId);
        int[] getInit(string deviceId);
        string getLog(string deviceId);
        void removeClient(string deviceId);
        void removeDevice(string clientId);
        void removeInit(string deviceId);
        void removeLog(string deviceId);
        void setClient(CClient client);
        void setClient(string deviceId, string clientId);
        void setClient(string deviceId, string clientId, bool sendRealTime);
        void setDevice(string clientId, string deviceId);
        void setInit(CDataPoint datapoint);
        void updateLog(CDataPoint datapoint);
    }
}