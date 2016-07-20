using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public interface ISmartChairServerClient
    {
        /// <summary>
        /// Sends the email to server. If email does not exist in the server- add it
        /// </summary>
        /// <param name="email"></param>
        /// <returns>returns true if this user have logged in before, otherwise false</returns>
        bool login(string email, string deviceId, string deviceKey);

        /// <summary>
        /// Calibarates smartchair according to the person. Occurs only on the first time, when we
        /// don't have the user's email in the DB
        /// </summary>
        /// <param name="email">to link data to user</param>
        /// <returns>returns true if initialization succeeded, otherwise false</returns>
        bool initialize(string email);

        /// <summary>
        /// Gets the data saved from the last week. 3 times for every day 11AM, 14PM, 18PM
        /// </summary>
        /// <param name="email"></param>
        /// <returns>A list of the the int value that was measured and the time it was measured in</returns>
        Dictionary<DateTime, int> getLastWeekData(string email);

        void startCommunicationWithServer();
        void stopCommunicationWithServer();
        

    }
}
