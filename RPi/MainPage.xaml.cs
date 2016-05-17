using System;
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
        CSensor bigSensor1 = new CSensor(ESensorType.FlexiForceA201, 0);
        CSensor bigSensor2 = new CSensor(ESensorType.FlexiForceA201, 1);

        CSensor smallSensor1 = new CSensor(ESensorType.SquareForceResistor, 2);
        CSensor smallSensor2 = new CSensor(ESensorType.SquareForceResistor, 3);

        public MainPage()
        {
            // initial setup
            CChair myChair = new CChair();



            myChair.Seat.Add(EChairPartArea.LeftMid, bigSensor1);
            myChair.Seat.Add(EChairPartArea.RightMid, bigSensor2);

            myChair.Back.Add(EChairPartArea.LeftMid, smallSensor1);
            myChair.Back.Add(EChairPartArea.RightMid, smallSensor2);
            

            this.InitializeComponent();
        }

        private void buttonRead1_Click(object sender, RoutedEventArgs e)
        {
            double w = Math.Round(double.Parse(textBoxWeight1.Text), 2);
            double v = Math.Round(bigSensor1.Read(), 2);
            double c = Math.Round(w/v, 2);
            textRead1.Text = "- W:" + w + " mV: " + v + " C: " + c;
        }

        private void buttonRead2_Click(object sender, RoutedEventArgs e)
        {
            double w = Math.Round(double.Parse(textBoxWeight2.Text), 2);
            double v = Math.Round(bigSensor1.Read(), 2);
            double c = Math.Round(w / v, 2);
            textRead2.Text = "- W:" + w + " mV: " + v + " C: " + c;
        }

        private void buttonRead3_Click(object sender, RoutedEventArgs e)
        {
            double w = Math.Round(double.Parse(textBoxWeight3.Text), 2);
            double v = Math.Round(bigSensor1.Read(), 2);
            double c = Math.Round(w / v, 2);
            textRead3.Text = "- W:" + w + " mV: " + v + " C: " + c;
        }

        private void buttonRead4_Click(object sender, RoutedEventArgs e)
        {
            double w = Math.Round(double.Parse(textBoxWeight4.Text), 2);
            double v = Math.Round(bigSensor1.Read(), 2);
            double c = Math.Round(w / v, 2);
            textRead4.Text = "- W:" + w + " mV: " + v + " C: " + c;
        }
    }
}
