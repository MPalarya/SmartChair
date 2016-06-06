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
        public smartChairController()
        {
            this.InitializeCommand = new DelegateCommand<object>(this.onInitialize, this.canExecute);
            this.ViewWeeklySummaryCommand = new DelegateCommand<object>(this.onViewWeeklySummary, this.canExecute);
            this.LoginCommand = new DelegateCommand<object>(this.onLogin);
            
            createNotification();
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

        private void createNotification()
        {
            //// In a real app, these would be initialized with actual data
            //string from = "smartchair App";
            //string subject = "Sitting alatam";
            //string body = "You are sitting way too long. Take a walk";


          

            var xmlToastTemplate = "<toast launch=\"app-defined-string\">" +
                         "<visual>" +
                           "<binding template =\"ToastGeneric\">" +
                             "<text>smartchair App - sitting alaram</text>" +
                             "<text>" +
                               "You are sitting way too long. Take a walk" +
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
