using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace Kinect_Data_acquisition
{
    [Serializable]
    public class AbstractColorFrame : AbstractFrame
    {
        public DateTime arrivedTime;
        public byte[] buffer;
        public string content;
    }
}
