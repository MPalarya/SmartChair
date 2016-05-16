using System.Collections.Generic;

namespace RPi.RPi_Hardware
{
    internal enum EChairPart
    {
        Seat,
        Back,
        Handles,
    }

    internal enum EChairPartArea
    {
        LeftFront,
        LeftMid,
        LeftRear,
        LeftTop = LeftFront,
        LeftBottom = LeftRear,

        RightFront,
        RightMid,
        RightRear,
        RightTop = RightFront,
        RightBottom = RightRear,
    }
    
    internal class CChair
    {
        #region Fields

        public Dictionary<EChairPartArea, CSensor> Back;
        public Dictionary<EChairPartArea, CSensor> Seat;
        public Dictionary<EChairPartArea, CSensor> Handles;

        #endregion
    }
}
