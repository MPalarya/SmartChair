using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;


namespace RPiConsole
{
    public enum ESensorType
    {
        FlexiForceA201,
        SquareForceResistor,
    }

    public class Sensor
    {
        #region Fields

        private static ManualResetEvent eventConnect = new ManualResetEvent(false);
        private static bool bIsConnected = false;
        public List<double> weights = new List<double>();
        public List<double> voltages = new List<double>();

        #endregion

        #region Constructors
        public Sensor(ESensorType type, byte channel)
        {
            Channel = channel;

            switch (type)
            {
                case ESensorType.FlexiForceA201:
                    MaxLoadInKg = LbToKgMultiplier * 100;
                    break;

                case ESensorType.SquareForceResistor:
                    MaxLoadInKg = 10;
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
        public double MaxLoadInKg { get; private set; } = -1;

        /// <summary>
        /// <para> min load is the effectice zero (of the system),                                                  </para>
        /// <para> the load detected by the sensor when no additional pressure is applied (as part of a system).    </para>
        /// <para> (besides the weight of the system and physical deviations).                                      </para>
        /// <para> value is in kilograms - calculated in calibration proccess after the system is fully assembled.  </para>
        /// </summary>
        private double MinLoadSystem { get; set; } = 0;

        /// <summary>
        /// <para> min load is the effectice zero (adjusted to a specific user),                       </para>
        /// <para> the load detected by the sensor when user is sitting correctly (guided).            </para>
        /// <para> compensates the asymmetricity of the user interation with the chair.                </para>
        /// <para> value is in kilograms - calculated in guided user calibration (using the Client).   </para>
        /// </summary>
        private double MinLoadUser { get; set; } = 0; // set to 0 - implemented in server

        /// <summary>
        /// MinLoad is the considering both System imperfection and user's asymmetric use.
        /// </summary>
        public double MinLoad
        {
            get { return MinLoadUser + MinLoadSystem; }
        }

        /// <summary>
        /// <para> conversation ratio between weight and voltage - calculated in calibration process.    </para>
        /// <para> satisfying: Weight(kilograms) = Coefficient * Vout(volt)                              </para>
        /// </summary>
        public double Coefficient { get; private set; } = 0;

        /// <summary>
        /// defines whether a sensor is in working condition.
        /// </summary>
        public bool IsWorking { get; set; } = false;

        #endregion

        #region Methods

        /// <summary>
        /// open a SPI connection between mcp3008 and RPi
        /// </summary>
        public async void connectAdcDeviceAsync()
        {
            if (bIsConnected)
                return;

            eventConnect.Reset();

            try
            {
                //using SPI0 on the RPi
                var spiSettings = new SpiConnectionSettings(0)
                {
                    ClockFrequency = 3600000, // 3.6 MHz
                    Mode = SpiMode.Mode0
                };

                var spiQuery = SpiDevice.GetDeviceSelector("SPI0");
            }

            finally
            {
                eventConnect.Set();
            }
        }


        /// <summary>
        /// return Sensor's averaged read value over several measures in Kg (after calibration)
        /// </summary>
        public int readKG()
        {
            return (int)(read() * Coefficient / 1000);
        }


        /// <summary>
        /// return Sensor's averaged read value over several measures
        /// </summary>
        public double read()
        {
            const int count = 3; // changing this value means re-calibrating the sensors!
            double sum = 0;

            for (int i = 0; i < count; i++)
            {
                sum += readSingle();
            }

            return sum / count;
        }

        /// <summary>
        /// return value is the Vin value in miliVolts.
        /// </summary>
        public double readSingle()
        {
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

            Task.Delay(20).Wait(); // wait to be able to re-read once returned

            double minLoad = Math.Max(0, MinLoad);
            return analogValue - minLoad;
        }

        /// <summary>
        /// calculates the voltage to weight conversion ratio based on linear fitting of previous measurements
        /// </summary>
        public void calibrate()
        {
            int measuresCount = Math.Min(weights.Count, voltages.Count);
            double rsquared, yintercept, slope;

            // linear fitting based on distances least mean squares
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
        /// assumes only chair's own weight is applied on the sensors. sets MinLoadSystem.
        /// </summary>
        public void CalibrateSystem()
        {
            MinLoadSystem = read();
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
