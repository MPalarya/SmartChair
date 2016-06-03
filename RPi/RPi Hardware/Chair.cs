using System.Collections.Generic;

namespace RPi.RPi_Hardware
{
    /// <summary>
    /// represents the part of chair that populates sensors.
    /// computations logic is different depending on selected part.
    /// </summary>
    public enum EChairPart
    {
        Seat,
        Back,
        Handles,
    }

    /// <summary>
    /// roughly divided into 3 rows and 2 coloumns to indicate the specific area onto part's surface.
    /// </summary>
    public enum EChairPartArea
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
    
    /// <summary>
    /// the chair holds record of sensors distribution on the surface of its different parts
    /// </summary>
    internal class CChair
    {
        #region Fields

        public Dictionary<EChairPartArea, CSensor> Back { get; set; } = new Dictionary<EChairPartArea, CSensor>();
        public Dictionary<EChairPartArea, CSensor> Seat { get; set; } = new Dictionary<EChairPartArea, CSensor>();
        public Dictionary<EChairPartArea, CSensor> Handles { get; set; } = new Dictionary<EChairPartArea, CSensor>();

        #endregion
    }
}
