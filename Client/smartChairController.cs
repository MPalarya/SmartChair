using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NotificationsExtensions.Tiles;
using NotificationsExtensions;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Threading;

namespace Client
{
    public class smartChairController
    {
        smartChairServerClient smartClientServerClient = smartChairServerClient.Instance; 
        public smartChairController()
        {
            isLogined = false;
            smartClientServerClient.HandleFinish += SmartClientServerClient_HandleFinish;
            smartClientServerClient.postureError += SmartClientServerClient_postureError;
        }
        
        private void SmartClientServerClient_postureError(object sender, postureErrorTypeEventArgs e)
        {
            string message;
            switch (e.ErrorType)
            {
                case EPostureErrorType.Correct:
                    break;
                case EPostureErrorType.HighPressureLeftSeat:
                    message = "You are leaning on your left side";
                    createNotification("Posture Error " + message);
                    break;
                case EPostureErrorType.HighPressureRightSeat:
                    message = "You are leaning on your right side";
                    createNotification("Posture Error " + message);
                    break;
                case EPostureErrorType.HighPressureLeftBack:
                    message = "Your back is not straight";
                    createNotification("Posture Error " + message);
                    break;
                case EPostureErrorType.HighPressureRightBack:
                    message = "Your back is not straight";
                    createNotification("Posture Error " + message);
                    break;
                default:
                    break;
            }  
        }

        private void SmartClientServerClient_HandleFinish(object sender, EventArgs e)
        {
            isLogined = true;
            createNotification("You are all set");
        }

        public bool isLogined { get; set; }

        private bool canExecute(object arg)
        {
            return isLogined;
        }
        
        public void onInitialize()
        {
            createNotification("Stay stright while we collect your posture data");
            smartClientServerClient.startCollectingInitData();

            //System.Threading.Tasks.Task.Delay(5000).Wait();
            //createNotification("You are all set");
           // demo();
        }

        private void demo()
        {
            createNotification("Posture Error You are leaning on your left side");
            System.Threading.Tasks.Task.Delay(5000).Wait();
            createNotification("Posture Error You are leaning on your right side");
            System.Threading.Tasks.Task.Delay(5000).Wait();
            createNotification("Posture Error Your back is not stright");
        }
        
        private void createNotification(string message)
        {
            var xmlToastTemplate = "<toast launch=\"app-defined-string\">" +
                         "<visual>" +
                           "<binding template =\"ToastGeneric\">" +
                             "<text>smartchair App - sitting alaram</text>" +
                             "<text>" +
                               message +
                             "</text>" +
                           "</binding>" +
                         "</visual>" +
                       "</toast>";

            // load the template as XML document
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlToastTemplate);

            // create the toast notification and show to user
            var toastNotification = new ToastNotification(xmlDocument);
            var notification = ToastNotificationManager.CreateToastNotifier();
            notification.Show(toastNotification);
        }
    }
}
