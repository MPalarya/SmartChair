using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Foundation.Metadata;

namespace RPi.RPi_Hardware
{
    internal enum ESensorType
    {
        FlexiForceA201,
        SquareForceResistor,
    }

    internal class CSensor
    {
        #region Fields

        private static ManualResetEvent _eventConnect = new ManualResetEvent(false);
        private static bool _isConnected = false;

        #endregion

        #region Constructors

        /// <summary>
        /// <param name="channel">number of channel the sensor uses to connect to AdcDevice</param>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="channel"></param>
        public CSensor(ESensorType type, byte channel)
        {
            Channel = channel;

            switch (type)
            {
                case ESensorType.FlexiForceA201:
                    MaxLoad = LbToKgMultiplier * 100;
                    break;

                case ESensorType.SquareForceResistor:
                    MaxLoad = 10; // kg
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// mcp3008 is the device used to convert analog input to digital output.
        /// all sensors are connected to this device and are read through it.
        /// </summary>
        private static SpiDevice AdcDevice { get; set; }

        /// <summary>
        /// to convert weight in lb to kg, multiply by this constant value.
        /// </summary>
        public double LbToKgMultiplier => 0.453592;

        /// <summary>
        /// channel number used to connect to AdcDevice
        /// </summary>
        private byte Channel { get; set; } = 0xFF;

        /// <summary>
        /// max load to be detected by the sensor, as provided by the manufacturer.
        /// value is in kilograms.
        /// </summary>
        public double MaxLoad { get; set; } = -1;

        /// <summary>
        /// min load is the effectice zero,
        /// the load detected by the sensor when no additional pressure is applied
        /// (besides the weight of the system and physical deviations).
        /// value is in kilograms.
        /// </summary>
        public double MinLoad { get; set; } = -1;

        /// <summary>
        /// conversation ratio between weight and voltage.
        /// satisfying: Weight(kilograms) = Coefficient * Vout(volt)
        /// </summary>
        public double Coefficient { get; set; } = 0;

        /// <summary>
        /// defines whether a sensor is in working condition.
        /// </summary>
        public bool IsWorking { get; set; } = false;

        #endregion

        #region Methods

        /// <summary>
        /// open a SPI connection between mcp3008 and RPi
        /// </summary>
        public static async void ConnectAdcDeviceAsync()
        {
            if (_isConnected)
                return;

            _eventConnect.Reset();

            try
            {
                //using SPI0 on the RPi
                var spiSettings = new SpiConnectionSettings(0)
                {
                    ClockFrequency = 3600000, // 3.6 MHz
                    Mode = SpiMode.Mode0
                };

                var spiQuery = SpiDevice.GetDeviceSelector("SPI0");


                DeviceInformationCollection deviceInfo = await DeviceInformation.FindAllAsync(spiQuery);
                

                if (deviceInfo != null && deviceInfo.Count > 0)
                {
                    AdcDevice = await SpiDevice.FromIdAsync(deviceInfo[0].Id, spiSettings);
                    
                    _isConnected = true;
                    
                }
            }

            finally
            {
                _eventConnect.Set();
            }
        }

        /// <summary>
        /// mcp3008 has 8 analog inputs, channel number is a number in range [0,7].
        /// return value is the Vin value in miliVolts.
        /// </summary>
        /// <param name="channel"></param>
        public double Read()
        {
            //ConnectAdcDeviceAsync();
            //_eventConnect.WaitOne();

            // from mcp3008 datasheet:

            byte[] transmitBuffer = new byte[3];
            byte[] receiveBuffer = new byte[3];

            // first byte: send start bit for SPI
            transmitBuffer[0] = 1;

            // second byte: configuration byte
            // lower 4 bits are "don't care", and higher 4 bits:
            // first bit set to indicate single measure
            // following 3 bits select channel number
            transmitBuffer[1] = (byte)((8 + Channel) << 4);

            // third byte: "don't care"
            transmitBuffer[2] = 0;

            AdcDevice.TransferFullDuplex(transmitBuffer, receiveBuffer);

            // first byte: "don't care"
            // second byte: the two LSBs indicate result's two MSBs, we only want 00000011 (mask of &3) 
            // third byte: rest of the result (total of 10 bits together with second byte)
            int digitalValue = ((receiveBuffer[1] & 3) << 8) + receiveBuffer[2];

            // Vout = Digital * Vin / 1024
            // in miliVolts for better floating point precision in future calculations
            double analogValue = digitalValue * (3300.0 / 1024.0);

            return analogValue;
        }

        /// <summary>
        /// calculates the voltage to weight conversion ratio based on linear fitting of previous measurements
        /// </summary>
        public void Calibrate(List<double> weights, List<double> voltages)
        {
            int measuresCount = Math.Min(weights.Count, voltages.Count);
            double rsquared, yintercept, slope;

            LinearRegression(
                voltages.ToArray(), weights.ToArray(),      // y = a*x+b => weight = coeff*volt
                0, measuresCount,
                out rsquared, out yintercept, out slope
                );

            if (slope > 0)
            {
                Coefficient = slope;
                IsWorking = true;
            }
            else
            {
                Coefficient = 0;
                IsWorking = false;
            }
        }

        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="inclusiveStart">The inclusive inclusiveStart index.</param>
        /// <param name="exclusiveEnd">The exclusive exclusiveEnd index.</param>
        /// <param name="rsquared">The r^2 value of the line.</param>
        /// <param name="yintercept">The y-intercept value of the line (i.e. y = ax + b, yintercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        private static void LinearRegression(double[] xVals, double[] yVals,
                                            int inclusiveStart, int exclusiveEnd,
                                            out double rsquared, out double yintercept,
                                            out double slope)
        {
            Debug.Assert(xVals.Length == yVals.Length);
            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double ssX = 0;
            double ssY = 0;
            double sumCodeviates = 0;
            double sCo = 0;
            double count = exclusiveEnd - inclusiveStart;

            for (int ctr = inclusiveStart; ctr < exclusiveEnd; ctr++)
            {
                double x = xVals[ctr];
                double y = yVals[ctr];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }
            ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            ssY = sumOfYSq - ((sumOfY * sumOfY) / count);
            double RNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double RDenom = (count * sumOfXSq - (sumOfX * sumOfX))
             * (count * sumOfYSq - (sumOfY * sumOfY));
            sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = RNumerator / Math.Sqrt(RDenom);
            rsquared = dblR * dblR;
            yintercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }

        #endregion
    }
}
