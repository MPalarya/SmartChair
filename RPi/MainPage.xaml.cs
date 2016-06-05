﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using RPi.RPi_Hardware;
using RPi.RPi_Server_API;

namespace RPi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CSensor m_bigSensor1 = new CSensor(ESensorType.FlexiForceA201, 0);
        private CSensor m_bigSensor2 = new CSensor(ESensorType.FlexiForceA201, 1);

        private List<double> m_weightsBigSensor1 = new List<double>();
        private List<double> m_voltagesBigSensor1 = new List<double>();

        private List<double> m_weightsBigSensor2 = new List<double>();
        private List<double> m_voltagesBigSensor2 = new List<double>();

        private CDeviceData m_deviceData = CDeviceData.Instance;
        private Dictionary<EChairPart, Dictionary<EChairPartArea, CSensor>> m_sensors;

        DateTime lastReported = DateTime.Now;

        public MainPage()
        {
            InitialSetup();

            this.Loaded += (sender, args) =>
            {
                CSensor.ConnectAdcDeviceAsync();
            };

            this.InitializeComponent();
        }

        /// <summary>
        /// Initial System Setup.
        /// </summary>
        private void InitialSetup()
        {
            CChair myChair = new CChair();
            myChair.Seat.Add(EChairPartArea.LeftMid, m_bigSensor1);
            myChair.Seat.Add(EChairPartArea.RightMid, m_bigSensor2);

            m_sensors = new Dictionary<EChairPart, Dictionary<EChairPartArea, CSensor>>()
            {
                [EChairPart.Seat] = myChair.Seat,
                [EChairPart.Back] = myChair.Back,
                [EChairPart.Handles] = myChair.Handles,
            };
        }

        private void buttonRead1_Click(object sender, RoutedEventArgs e)
        {
            double v = Math.Round(m_bigSensor1.Read(), 2);
            textRead1.Text = v.ToString();
        }

        private void buttonRead2_Click(object sender, RoutedEventArgs e)
        {
            double v = Math.Round(m_bigSensor2.Read(), 2);
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
            double r1 = Math.Round(m_bigSensor1.ReadSingle() * m_bigSensor1.Coefficient / 1000, 2);
            double r2 = Math.Round(m_bigSensor2.ReadSingle() * m_bigSensor2.Coefficient / 1000, 2);

            double a1 = Math.Round(m_bigSensor1.Read() * m_bigSensor1.Coefficient / 1000, 2);
            double a2 = Math.Round(m_bigSensor2.Read() * m_bigSensor2.Coefficient / 1000, 2);

            textReadAll.Text = "single read: \n" + r1 + " kg\n " + r2 + " kg\n average of 3 reads: \n" + a1 + "kg \n " + a2 + " kg";

            ReadAndReport();
        }

        private void ReadAndReport()
        {
            DateTime curTime = DateTime.Now;

            if (curTime.Subtract(lastReported).TotalMinutes < CDeviceData.frequencyToReport)
                return;

            lastReported = curTime;
            
            m_deviceData.Data.Clear();
            foreach (var chairPart in m_sensors)
            {
                foreach (var partArea in chairPart.Value)
                {
                    m_deviceData.Data[chairPart.Key][partArea.Key] = partArea.Value.ReadKG();
                }
            }
            m_deviceData.RPiServer_newDataSample(DateTime.Now);
        }

        private void buttonSave1_Click(object sender, RoutedEventArgs e)
        {
            m_weightsBigSensor1.Add(double.Parse(textBoxWeight1.Text));
            m_voltagesBigSensor1.Add(double.Parse(textRead1.Text));
        }

        private void buttonSave2_Click(object sender, RoutedEventArgs e)
        {
            m_weightsBigSensor2.Add(double.Parse(textBoxWeight2.Text));
            m_voltagesBigSensor2.Add(double.Parse(textRead2.Text));
        }

        private void buttonCalibrate1_Click(object sender, RoutedEventArgs e)
        {
            m_bigSensor1.Calibrate(m_weightsBigSensor1, m_voltagesBigSensor1);
            textCal1.Text = Math.Round(m_bigSensor1.Coefficient, 2).ToString();
        }

        private void buttonCalibrate2_Click(object sender, RoutedEventArgs e)
        {
            m_bigSensor2.Calibrate(m_weightsBigSensor2, m_voltagesBigSensor2);
            textCal2.Text = Math.Round(m_bigSensor2.Coefficient, 2).ToString();
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
