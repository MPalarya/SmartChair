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
//using System.Xml;

namespace Client
{
    public class smartChairController
    {
        smartChairServerClient smartClientServerClient = new smartChairServerClient(); 
        public smartChairController()
        {
            this.InitializeCommand = new DelegateCommand<object>(this.onInitialize, this.canExecute);
            this.ViewWeeklySummaryCommand = new DelegateCommand<object>(this.onViewWeeklySummary, this.canExecute);
            this.LoginCommand = new DelegateCommand<object>(this.onLogin);

            smartClientServerClient.HandleFinish += SmartClientServerClient_HandleFinish;
            smartClientServerClient.postureError += SmartClientServerClient_postureError;
            
            //createNotification("Welcome!");
        }

        private void SmartClientServerClient_postureError(object sender, postureErrorTypeEventArgs e)
        {
            string message;
            switch (e.ErrorType)
            {
                case EPostureErrorType.Correct:
                    break;
                case EPostureErrorType.HighPressureLeftSeat:
                    message = "You are linning on your left side";
                    createNotification("Posture Error " + message);
                    break;
                case EPostureErrorType.HighPressureRightSeat:
                    message = "You are linning on your left side";
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
                case EPostureErrorType.HighPressureLeftHandle:
                    message = "You are linning on your left side";
                    createNotification("Posture Error " + message);
                    break;
                case EPostureErrorType.HighPressureRightHandle:
                    message = "You are linning on your right side";
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
        public DelegateCommand<object> InitializeCommand { get; private set; }
        public DelegateCommand<object> ViewWeeklySummaryCommand { get; private set; }
        public DelegateCommand<object> LoginCommand { get; private set; }
        //private void OnSubmit(object arg) {...}

        private bool canExecute(object arg)
        {
            return isLogined;
        }
        
        private void onLogin(object arg)
        {
            //login to server
            isLogined = true;
        }

        private void onInitialize(object arg)
        {

            //new screen- asks to sit straight and gets approval/error from server 
        }

        private void onViewWeeklySummary(object arg)
        {
            //new screen- asks the data from the server and presents it
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
