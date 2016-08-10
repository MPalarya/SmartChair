using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public sealed class ClassifySitting
    {
        #region Fields

        private static ChairPartConverter chairPartConverter = ChairPartConverter.Instance;
        private double[] normalizedInitMultipliers;
        private int[] init;

        #endregion

        #region Constructors
        public ClassifySitting(int[] init)
        {
            normalizeInitData(init);
        }

        #endregion

        #region Methods
        private void normalizeInitData(int[] init)
        {
            normalizedInitMultipliers = new double[init.Length];
            if (init.Length <= 0)
                return;
            removeZeroValuesFromInit(init);

            int max = init.Max();

            for (int i = 0; i < init.Length; i++)
            {
                normalizedInitMultipliers[i] = (double)max / init[i];
            }
        }

        private void removeZeroValuesFromInit(int[] init)
        {
            for (int i = 0; i < init.Length; i++)
            {
                init[i] += 1;
            }
            this.init = init;
        }

        public void updateInitData(int[] init)
        {
            normalizeInitData(init);
        }

        public EPostureErrorType isSittingCorrectly(int[] curr)
        {
            if (currAndInitDataIncomaptible(curr))
                return EPostureErrorType.CannotAnalyzeData;

            double[] currNormalized = normalizeCurrData(curr);

            return testAllErrorTypes(currNormalized);
        }

        private bool currAndInitDataIncomaptible(int[] curr)
        {
            return curr.Length < normalizedInitMultipliers.Length || normalizedInitMultipliers.Length <= 0;
        }

        public double[] normalizeCurrData(int[] curr)
        {
            double[] currNormalized = new double[curr.Length];

            for(int i = 0; i < curr.Length; i++)
            {
                currNormalized[i] = (double)curr[i] * normalizedInitMultipliers[i];
            }

            return currNormalized;
        }

        private EPostureErrorType testAllErrorTypes(double[] currNormalized)
        {
            EPostureErrorType currError;

            foreach (EPostureErrorType errorType in Enum.GetValues(typeof(EPostureErrorType)))
            {
                currError = testErrorType(errorType, currNormalized);
                if (currError != EPostureErrorType.Correct)
                    return currError;
            }

            return EPostureErrorType.Correct;
        }

        private EPostureErrorType testErrorType(EPostureErrorType errorType, double[] currNormalized)
        {
            int highIndex, lowIndex;

            switch (errorType)
            {
                case EPostureErrorType.Correct:
                case EPostureErrorType.CannotAnalyzeData:
                    return EPostureErrorType.Correct;

                case EPostureErrorType.HighPressureLeftBack:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.LeftBottom);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.RightBottom);
                    break;

                case EPostureErrorType.HighPressureLeftSeat:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Seat, EChairPartArea.LeftMid);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Seat, EChairPartArea.RightMid);
                    break;

                case EPostureErrorType.HighPressureRightBack:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.LeftBottom);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.RightBottom);
                    break;

                case EPostureErrorType.HighPressureRightSeat:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Seat, EChairPartArea.RightMid);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Seat, EChairPartArea.LeftMid);
                    break;

                case EPostureErrorType.HighPressureLowerBackToUpperBack:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.LeftBottom);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.LeftTop);
                    break;

                case EPostureErrorType.HighPressureUpperBackToLowerBack:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.LeftTop);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.LeftBottom);
                    break;

                default:
                    return EPostureErrorType.Correct;
            }

            if (indeciesOutOfBounds(currNormalized, highIndex, lowIndex))
                return EPostureErrorType.CannotAnalyzeData;

            if (!testCorrectPostureByIndex(currNormalized, highIndex, lowIndex))
            {
                return errorType;
            }
            return EPostureErrorType.Correct;
        }

        private static bool indeciesOutOfBounds(double[] currNormalized, int highIndex, int lowIndex)
        {
            return highIndex < 0 || lowIndex < 0 || highIndex >= currNormalized.Length || lowIndex >= currNormalized.Length;
        }

        private bool testCorrectPostureByIndex(double[] currNormalized, int indexHigh, int indexLow)
        {
            if (sensorsAreNotFunctioning(currNormalized[indexHigh], currNormalized[indexLow]))
                return true;

            double ratio = (currNormalized[indexHigh] - currNormalized[indexLow]) / currNormalized[indexLow];
            if (ratio > Globals.CORRECT_SITTING_RATIO_THRESHOLD)
                return false;

            return true;
        }

        private static bool sensorsAreNotFunctioning(double sensorHigh, double sensorLow)
        {
            return sensorHigh <= 0 || sensorLow <= 0;
        }

        public List<List<object>> convertLogsToStdErr(List<List<object>> logs)
        {
            List<List<object>> logsStdErr = logs;
            for(int i = 0; i < logsStdErr.Count; i++)
            {
                logsStdErr[i][1] = convertDatapointToStdErr(JsonConvert.DeserializeObject<int[]>(logsStdErr[i][1].ToString()));
            }

            return logsStdErr;
        }

        private int convertDatapointToStdErr(int[] log)
        {
            int sum = 0;
            for (int i = 0; i < log.Length; i++)
            {
                sum += (log[i] - init[i]) * (log[i] - init[i]);
            }

            return (int)Math.Sqrt(sum);
        }
        #endregion
    }
}