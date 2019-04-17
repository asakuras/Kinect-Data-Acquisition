using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Kinect_Data_acquisition
{
    public class AbstractQueue : ConcurrentQueue<AbstractFrame>
    {
    }
}
