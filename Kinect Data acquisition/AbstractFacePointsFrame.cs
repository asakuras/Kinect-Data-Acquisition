using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace Kinect_Data_acquisition
{
    [Serializable]
    public class AbstractFacePointsFrame : AbstractFrame
    {
        public DateTime arrivedTime;
        public CameraSpacePoint[] buffer = null;
        public string content;
    }
}
