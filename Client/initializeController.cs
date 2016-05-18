using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class initializeController
    {
        public initializeController()
        {
            showPage = true;
            CloseWindowCommand = new DelegateCommand<object>(this.closeWindow);
            message = "Please sit stright while we calliberate your chair";
            if ((isApproved = getApprovalFromServer()))
            {
                message = "You are ready to go!";
            }
            else
            {
                message = "Please try again";
            }
        }


        private void closeWindow(object arg)
        {
            showPage = false;
        }
        private bool getApprovalFromServer()
        {
            return true;
        }
        //get approval/reject from server

        public DelegateCommand<object> CloseWindowCommand { get; private set; }
        public bool isApproved { get; set; }
        public string message { get; set; }
        public bool showPage { get; set; }
    }

    
}
