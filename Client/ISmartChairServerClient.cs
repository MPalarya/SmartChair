using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public interface ISmartChairServerClient
    {
        // pairs client with device by deviceId
        // this only needs to be done once (ever)
        void pairWithDevice(string deviceIdtoPairWith);

        // sends message to server to start collecting data for initial correct sitting
        void startCollectingInitData();

        // sends message to server to get logs for time between startdate and enddate
        // ISmartChairServerClient implementation handles logs recieved
        void getLogsByDateTimeBounds(DateTime startdate, DateTime enddate);

        // sends message to server to start getting realtime data. 
        // ISmartChairServerClient implementation handles datapoint recieved
        void startCommunicationWithServer();

        // sends message to server to stop getting realtime data. 
        void stopCommunicationWithServer();
    }
}
