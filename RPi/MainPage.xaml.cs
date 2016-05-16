using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            CChair myChair = new CChair();

            CSensor bigSensor1 = new CSensor(ESensorType.FlexiForceA201, 0);
            CSensor bigSensor2 = new CSensor(ESensorType.FlexiForceA201, 1);

            CSensor smallSensor1 = new CSensor(ESensorType.SquareForceResistor, 2);
            CSensor smallSensor2 = new CSensor(ESensorType.SquareForceResistor, 3);

            myChair.Seat.Add(EChairPartArea.LeftMid, bigSensor1);
            myChair.Seat.Add(EChairPartArea.RightMid, bigSensor2);

            myChair.Back.Add(EChairPartArea.LeftMid, smallSensor1);
            myChair.Back.Add(EChairPartArea.RightMid, smallSensor2);


            this.InitializeComponent();
        }

    }
}
