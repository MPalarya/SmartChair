using System;
using System.Collections.Generic;
using System.Linq;
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
        private CSensor bigSensor1 = new CSensor(ESensorType.FlexiForceA201, 0);
        private CSensor bigSensor2 = new CSensor(ESensorType.FlexiForceA201, 1);

        private List<double> weightsBigSensor1 = new List<double>();
        private List<double> voltagesBigSensor1 = new List<double>();

        private List<double> weightsBigSensor2 = new List<double>();
        private List<double> voltagesBigSensor2 = new List<double>();

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


            this.Loaded += (sender, args) =>
            {
                CSensor.ConnectAdcDeviceAsync();
            };

            this.InitializeComponent();
        }

        private void buttonRead1_Click(object sender, RoutedEventArgs e)
        {
            double v = Math.Round(bigSensor1.Read(), 2);
            textRead1.Text = v.ToString();
        }

        private void buttonRead2_Click(object sender, RoutedEventArgs e)
        {
            double v = Math.Round(bigSensor2.Read(), 2);
            textRead2.Text = v.ToString();
        }

        private void buttonReadAll_Click(object sender, RoutedEventArgs e)
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();

            dispatcherTimer.Tick += ReadAllTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

        }
        private void ReadAllTick(object sender, object e)
        {
            // from miliVolts to Volts
            double r1 = Math.Round(bigSensor1.ReadSingle() * bigSensor1.Coefficient / 1000, 2);
            double r2 = Math.Round(bigSensor2.ReadSingle() * bigSensor2.Coefficient / 1000, 2);

            double a1 = Math.Round(bigSensor1.Read() * bigSensor1.Coefficient / 1000, 2);
            double a2 = Math.Round(bigSensor2.Read() * bigSensor2.Coefficient / 1000, 2);

            textReadAll.Text = "single read: \n" + r1 + " kg\n " + r2 + " kg\n average of 3 reads: \n" + a1 + "kg \n " + a2 + " kg";

        }
        private void buttonSave1_Click(object sender, RoutedEventArgs e)
        {
            weightsBigSensor1.Add(double.Parse(textBoxWeight1.Text));
            voltagesBigSensor1.Add(double.Parse(textRead1.Text));
        }

        private void buttonSave2_Click(object sender, RoutedEventArgs e)
        {
            weightsBigSensor2.Add(double.Parse(textBoxWeight2.Text));
            voltagesBigSensor2.Add(double.Parse(textRead2.Text));
        }

        private void buttonCalibrate1_Click(object sender, RoutedEventArgs e)
        {
            bigSensor1.Calibrate(weightsBigSensor1, voltagesBigSensor1);
            textCal1.Text = Math.Round(bigSensor1.Coefficient, 2).ToString();
        }

        private void buttonCalibrate2_Click(object sender, RoutedEventArgs e)
        {
            bigSensor2.Calibrate(weightsBigSensor2, voltagesBigSensor2);
            textCal2.Text = Math.Round(bigSensor2.Coefficient, 2).ToString();
        }

        #region keyboard

        private void button_Click(object sender, RoutedEventArgs e)
        {
            textBoxWeight1.Text = "1250";
            textBoxWeight2.Text = "1250";
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            textBoxWeight1.Text = "2500";
            textBoxWeight2.Text = "2500";
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            textBoxWeight1.Text = "3750";
            textBoxWeight2.Text = "3750";
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            textBoxWeight1.Text = "5000";
            textBoxWeight2.Text = "5000";
        }

        #endregion
    }
}
