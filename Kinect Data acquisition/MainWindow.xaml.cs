namespace Kinect_Data_acquisition
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Face;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Windows.Controls;
    using System.Windows.Shapes;
    using System.Linq;

    public partial class MainWindow : Window
    {
       
        private KinectSensor kinectSensor = null;   //创建一个空的Kinect传感器对象
        private DateTime time1;
        private DateTime time2;
        #region Audio
        private AudioSource audioSource = null;
        private static readonly byte[] audioBuffer = new byte[1024];
        private AudioBeamFrameReader audioFrameReader = null;
        private string audioFolderPath;   //定义音频存储的文件路径
        private string audioFilePath;
        #endregion

        #region Color
        private ColorFrameSource colorFrameSource = null;
        private ColorFrameReader colorFrameReader = null;
        private FrameDescription colorFrameDescription = null;
        private WriteableBitmap colorBitmap = null;
        private Int32Rect colorBitmapRect;
        private int colorBitmapStride;     
        private Byte[] colorPixelData;
        #endregion

        #region Depth 
        private DepthFrameSource depthFrameSource = null;
        private DepthFrameReader depthFrameReader = null;
        private FrameDescription depthFrameDesription = null;
        private WriteableBitmap depthBitmap = null;
        private Int32Rect depthBitmapRect;
        private int depthStride;
        private ushort[] depthPixelData;
        #endregion

        #region FacePoints
        private BodyFrameSource _bodySource = null;
        private BodyFrameReader _bodyReader = null;
        private HighDefinitionFaceFrameSource _faceSource = null;
        private HighDefinitionFaceFrameReader _faceReader = null;
        private FaceAlignment _faceAlignment = null;
        private FaceModel _faceModel = null;
        private List<Ellipse> _points = new List<Ellipse>();
        #endregion

        #region AbstractFrame
        private AbstractColorFrame abstractColorFrame = new AbstractColorFrame();
        AbstractDepthFrame abstractDepthFrame = new AbstractDepthFrame();
        AbstractFacePointsFrame abstractFacePointsFrame = new AbstractFacePointsFrame();
        #endregion

        private Boolean isRecording = false;    //用于表征当前状态是否处于录词中
        private Boolean isEnded = false;   //用于表征窗体程序是否退出
        private StreamReader wordReader;    //取词器
        private string binaryFilePath;  //定义二进制文件“data.bin”的文件路径
        private AbstractQueue mainQueue = new AbstractQueue();    //创建了主队列
        private Thread mainWritingThread;
        private QueueSaver queueSaver;  //存储队列里的数据
        private bool isQueueEmpty = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            binaryFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, "data.bin");


            this.kinectSensor = KinectSensor.GetDefault();
            if (this.kinectSensor != null)
            {
                this.kinectSensor.Open();

                #region ColorFrame
                this.colorFrameSource = this.kinectSensor.ColorFrameSource;
                this.colorFrameReader = this.colorFrameSource.OpenReader();
                this.colorFrameDescription = this.colorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                this.colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
                this.colorPixelData = new Byte[this.colorFrameDescription.LengthInPixels * 4];
                this.colorBitmap = new WriteableBitmap(this.colorFrameDescription.Width,
                                                    this.colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.colorBitmapRect = new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight);
                this.colorBitmapStride = this.colorFrameDescription.Width * 4;

                #endregion

                #region DepthFrame
                this.depthFrameSource = this.kinectSensor.DepthFrameSource;
                this.depthFrameReader = this.depthFrameSource.OpenReader();
                this.depthFrameDesription = this.kinectSensor.DepthFrameSource.FrameDescription;
                this.depthFrameReader.FrameArrived += depthReader_FrameArrived;
                this.depthBitmap = new WriteableBitmap(this.depthFrameDesription.Width, this.depthFrameDesription.Height, 96.0, 96.0, PixelFormats.Gray16, null);
                this.depthBitmapRect = new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight);
                this.depthPixelData = new ushort[this.depthFrameDesription.LengthInPixels];           //取代宽度*高度，更加方便  
                this.depthStride = this.depthFrameDesription.Width * 2;
                #endregion

                #region FacePoints
                // Listen for body data.
                _bodySource = this.kinectSensor.BodyFrameSource;
                _bodyReader = _bodySource.OpenReader();
                _bodyReader.FrameArrived += BodyReader_FrameArrived;
                // Listen for HD face data.
                _faceSource = new HighDefinitionFaceFrameSource(this.kinectSensor);
                _faceReader = _faceSource.OpenReader();
                _faceReader.FrameArrived += FaceReader_FrameArrived;
                _faceModel = new FaceModel();
                _faceAlignment = new FaceAlignment();
                #endregion

                abstractColorFrame.buffer = new byte[1920 * 1080 * 4];
                abstractDepthFrame.buffer = new ushort[512 * 424];
                abstractFacePointsFrame.buffer = new CameraSpacePoint[1347];

                colorImage.Source = this.colorBitmap;
                depthImage.Source = this.depthBitmap;

                queueSaver = new QueueSaver(binaryFilePath);
                mainWritingThread = new Thread(new ThreadStart(saveAll2File));
                mainWritingThread.Start();
                string txtPath = System.IO.Path.Combine(Environment.CurrentDirectory, "word.txt");
                if (File.Exists(txtPath))
                {
                    wordReader = new StreamReader(txtPath, Encoding.Default);
                }
                else
                {
                    if (MessageBox.Show("提词器文本文件找不到！") == MessageBoxResult.OK)
                    {
                        Application.Current.Shutdown();
                    }
                }
                if (!wordReader.EndOfStream)
                {
                    this.txtBlock_narrator.Text = wordReader.ReadLine();
                }
                else
                {
                    this.txtBlock_narrator.Text = "采集结束。";
                    this.btn_startRecording.IsEnabled = false;
                }
            }
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            this.isEnded = true;

            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.depthFrameReader != null)
            {
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }
            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            this.mainWritingThread.Join();
            this.mainWritingThread.Abort();
        }

       
           


        #region Color processing
        //彩色帧处理事件  
        private void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            DateTime arrivedTime = DateTime.Now;
            
                using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
                {
                    if (colorFrame != null)
                    {
                        colorFrame.CopyConvertedFrameDataToArray(this.colorPixelData, ColorImageFormat.Bgra);
                        if (this.isRecording)
                        {
                            abstractColorFrame.content = this.txtBlock_narrator.Text;
                            this.colorPixelData.CopyTo(abstractColorFrame.buffer, 0);
                            abstractColorFrame.arrivedTime = arrivedTime;
                            this.mainQueue.Enqueue(abstractColorFrame);
                            this.isQueueEmpty = false;
                        }
                        this.colorBitmap.WritePixels(this.colorBitmapRect, this.colorPixelData, this.colorBitmapStride, 0);
                    }
                }
            
              
        }
        #endregion

        #region Depth processing
        private void depthReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            DateTime arrivedTime = DateTime.Now;
           
                using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
                {
                    if (depthFrame != null)
                    {
                        depthFrame.CopyFrameDataToArray(this.depthPixelData);
                        if (this.isRecording)
                        {
                            abstractDepthFrame.content = this.txtBlock_narrator.Text;
                            abstractDepthFrame.arrivedTime = arrivedTime;
                            this.depthPixelData.CopyTo(abstractDepthFrame.buffer, 0);
                            this.mainQueue.Enqueue(abstractDepthFrame);
                            this.isQueueEmpty = false;
                        }
                        this.depthBitmap.WritePixels(this.depthBitmapRect, this.depthPixelData, this.depthStride, 0);
                    }
                }
            
                
        }
        #endregion

        #region FacePoints processing
        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
          
           
                using (var frame = e.FrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        Body[] bodies = new Body[frame.BodyCount];
                        frame.GetAndRefreshBodyData(bodies);

                        Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

                        if (!_faceSource.IsTrackingIdValid)
                        {
                            if (body != null)
                            {
                                _faceSource.TrackingId = body.TrackingId;
                            }
                        }
                    }
                }
            
               
        }

        private void FaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            DateTime arrivedTime = DateTime.Now;
            
                using (var frame = e.FrameReference.AcquireFrame())
                {
                    if (frame != null && frame.IsFaceTracked)
                    {
                        frame.GetAndRefreshFaceAlignmentResult(_faceAlignment);
                        UpdateFacePoints(arrivedTime);
                    }
                }
            
                
        }

        private void UpdateFacePoints(DateTime arrivedTime)
        {
            if (_faceModel == null) return;
            var vertices = _faceModel.CalculateVerticesForAlignment(_faceAlignment);
            if (this.isRecording)
            {
                abstractFacePointsFrame.content = this.txtBlock_narrator.Text;

                abstractFacePointsFrame.arrivedTime = arrivedTime;
                vertices.ToArray<CameraSpacePoint>().CopyTo(abstractFacePointsFrame.buffer, 0);
                this.mainQueue.Enqueue(abstractFacePointsFrame);
                this.isQueueEmpty = false;
            }
            if (vertices.Count > 0)
            {
                if (_points.Count == 0)
                {
                    for (int index = 0; index < vertices.Count; index++)
                    {
                        Ellipse ellipse = new Ellipse
                        {
                            Width = 2.0,
                            Height = 2.0,
                            Fill = new SolidColorBrush(Colors.Blue)
                        };
                        _points.Add(ellipse);
                    }

                    foreach (Ellipse ellipse in _points)
                    {
                        canvas.Children.Add(ellipse);
                    }
                }
                for (int index = 0; index < vertices.Count; index++)
                {
                    CameraSpacePoint vertice = vertices[index];
                    DepthSpacePoint point = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(vertice);
                    if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) return;
                    Ellipse ellipse = _points[index];
                    Canvas.SetLeft(ellipse, point.X * 3 - 600);
                    Canvas.SetTop(ellipse, point.Y * 3 - 300);
                }
            }
        }



        #endregion
        #region save all to file
        private void saveAll2File()
        {
            while (true)
            {
                AbstractFrame tmp;
                if (this.mainQueue.TryDequeue(out tmp))
                {
                    queueSaver.OnDataRecieve(tmp);
                    tmp = null;
                   
                }
                else
                {
                    this.isQueueEmpty = true;
                    if (this.isEnded)
                    {
                        break;
                    }
                }
            }
        }
        #endregion
        #region ButtonEvents
        private void startRecording_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            time1 = DateTime.Now;
            this.isRecording = true;
            btn_startRecording.IsEnabled = false;
            btn_stopRecording.IsEnabled = true;
        }

        private void stopRecording_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            this.isRecording = false;
            time2 = DateTime.Now;
            Console.WriteLine(time2 - time1);

            if (!wordReader.EndOfStream)
            {
                while (true)
                {
                    if (isQueueEmpty)
                    {
                        break;
                    }
                }
                this.txtBlock_narrator.Text = wordReader.ReadLine();
                btn_startRecording.IsEnabled = true;
                btn_stopRecording.IsEnabled = false;
            }
            else
            {
                this.txtBlock_narrator.Text = "提词文本已结束。";
                this.btn_stopRecording.IsEnabled = false;
            }
        }
        #endregion

    }
}
