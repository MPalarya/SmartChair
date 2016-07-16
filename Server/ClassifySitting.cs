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

        private static ClassifySitting instance;

        #endregion

        #region Constructors
        private ClassifySitting(){}
        #endregion

        #region Properties

        public static ClassifySitting Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ClassifySitting();
                }
                return instance;
            }
        }

        #endregion

        #region Methods

        // TODO: decide and implement classification algoritm
        public bool isSittingCorrectly(int[] curr, int[] init)
        {
            return true;
        }
        
        #endregion
    }
}
