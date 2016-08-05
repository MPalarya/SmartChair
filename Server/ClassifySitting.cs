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
        private static double CORRECT_RATIO_THRESHOLD = 0.4; // = 40%

        private double[] normalizedInitMultipliers;

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

            int max = init.Max();

            for(int i = 0; i < init.Length; i++)
            {
                normalizedInitMultipliers[i] = (double)max / init[i] ;
            }
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
            return curr.Length < normalizedInitMultipliers.Length;
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
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.LeftMid);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.RightMid);
                    break;

                case EPostureErrorType.HighPressureLeftHandle:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Handles, EChairPartArea.LeftMid);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Handles, EChairPartArea.RightMid);
                    break;

                case EPostureErrorType.HighPressureLeftSeat:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Seat, EChairPartArea.LeftMid);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Seat, EChairPartArea.RightMid);
                    break;

                case EPostureErrorType.HighPressureRightBack:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.RightMid);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Back, EChairPartArea.LeftMid);
                    break;

                case EPostureErrorType.HighPressureRightHandle:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Handles, EChairPartArea.RightMid);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Handles, EChairPartArea.LeftMid);
                    break;

                case EPostureErrorType.HighPressureRightSeat:
                    highIndex = chairPartConverter.getIndexByChairPart(EChairPart.Seat, EChairPartArea.RightMid);
                    lowIndex = chairPartConverter.getIndexByChairPart(EChairPart.Seat, EChairPartArea.LeftMid);
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
            double ratio = (currNormalized[indexHigh] - currNormalized[indexLow]) / currNormalized[indexLow];
            if (ratio > CORRECT_RATIO_THRESHOLD)
                return false;

            return true;
        }

        #endregion
    }
}