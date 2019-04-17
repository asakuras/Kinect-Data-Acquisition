using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Kinect_Data_acquisition
{
    class QueueSaver
    {
        private IFormatter m_formatter = new BinaryFormatter();
        public Stream m_stream;
        private string filename;

        public QueueSaver(string filename)
        {
            this.filename = filename;

            m_stream = new FileStream(filename,
                FileMode.Create, FileAccess.Write, FileShare.None);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            m_stream.Close();
        }

        public void OnDataRecieve(AbstractFrame frame)
        {
            m_formatter.Serialize(m_stream, frame);
        }
        
    }
}
