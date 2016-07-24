using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPiConsole
{
    public interface IRPiServerProxy
    {
        void RPiServer_newDataSample(DateTime datetime, int[] pressure);
    }
}
