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

        public Dictionary<EChairPartArea, CSensor> Back { get; set; } = new Dictionary<EChairPartArea, CSensor>();
        public Dictionary<EChairPartArea, CSensor> Seat { get; set; } = new Dictionary<EChairPartArea, CSensor>();
        public Dictionary<EChairPartArea, CSensor> Handles { get; set; } = new Dictionary<EChairPartArea, CSensor>();

        #endregion
    }
}
