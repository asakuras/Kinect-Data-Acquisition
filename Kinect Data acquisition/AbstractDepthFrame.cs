using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace Kinect_Data_acquisition
{
    [Serializable]
    public class AbstractDepthFrame : AbstractFrame
    {
        public DateTime arrivedTime;
        public ushort[] buffer;
        public string content;
    }
}
