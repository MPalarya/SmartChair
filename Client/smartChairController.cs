using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client
{
    public class smartChairController
    {
        public smartChairController()
        {
            this.InitializeCommand = new DelegateCommand<object>(this.onInitialize, this.canExecute);
            this.ViewWeeklySummaryCommand = new DelegateCommand<object>(this.onViewWeeklySummary, this.canExecute);
            this.LoginCommand = new DelegateCommand<object>(this.onLogin);
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
    }
}
