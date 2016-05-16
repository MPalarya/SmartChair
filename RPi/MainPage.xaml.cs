using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RPi.RPi_Hardware;

namespace RPi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            // initial setup
            //CChair myChair = new CChair();
            
            //CSensor bigSensor1 = new CSensor(ESensorType.FlexiForceA201);
            //CSensor bigSensor2 = new CSensor(ESensorType.FlexiForceA201);

            //CSensor smallSensor1 = new CSensor(ESensorType.SquareForceResistor);
            //CSensor smallSensor2 = new CSensor(ESensorType.SquareForceResistor);

            //myChair.Seat.Add(ESensorPosition.SeatLeftMid, bigSensor1);
            //myChair.Seat.Add(ESensorPosition.SeatRightMid, bigSensor2);

            //myChair.Back.Add(ESensorPosition.BackLeftMid, smallSensor1);
            //myChair.Back.Add(ESensorPosition.BackRightMid, smallSensor2);


            this.InitializeComponent();
        }

        private void ClickMe_Click(object sender, RoutedEventArgs e)
        {
            this.HelloMessage.Text = "Hello, Windows IoT Core!";
        }
    }
}
