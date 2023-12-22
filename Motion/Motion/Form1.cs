using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Motion
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection VideoCaptureDevices;
        private VideoCaptureDevice FinalVideo;
        
        private Bitmap backgroundFrame;
        private Bitmap currentFrame;

        public Form1()
        {
            InitializeComponent();
            {
                VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                foreach (FilterInfo VideoCaptureDevice in VideoCaptureDevices)
                {
                    cboDevices.Items.Add(VideoCaptureDevice.Name);
                }
                cboDevices.SelectedIndex = 0;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            FinalVideo = new VideoCaptureDevice(VideoCaptureDevices[cboDevices.SelectedIndex].MonikerString);
            FinalVideo.NewFrame += new NewFrameEventHandler(FinalVideo_NewFrame);
            FinalVideo.Start();
        }

        void FinalVideo_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Copy of the Frame as bitmap so we dont lock it!
            Bitmap video = (Bitmap)eventArgs.Frame.Clone();
            
            // Fit and Mirror the Frame
            var filter = new FiltersSequence
            (
                new ResizeBilinear(400, 400),
                new Mirror( false, true)
            );
            video = filter.Apply(video);
            
            // Take first Frame as Background Frame
            if(backgroundFrame == null)
                backgroundFrame = Grayscale.CommonAlgorithms.RMY.Apply(video);

            // Grayscale Copy of RGB
            currentFrame = Grayscale.CommonAlgorithms.RMY.Apply(video);

            // move background towards current frame
            MoveTowards moveTowardsFilter = new MoveTowards();
            moveTowardsFilter.OverlayImage = currentFrame;
            Bitmap tmp = moveTowardsFilter.Apply(backgroundFrame);
            backgroundFrame.Dispose();
            backgroundFrame = tmp;

            // create processing filters sequence
            FiltersSequence processingFilter = new FiltersSequence();
            processingFilter.Add(new Difference(backgroundFrame));
            processingFilter.Add(new Threshold(25));
            processingFilter.Add(new Opening());
            processingFilter.Add(new SobelEdgeDetector());
            Bitmap tmp1 = processingFilter.Apply(currentFrame);

            // extract red channel from the original image
            IFilter extrachChannel = new ExtractChannel(RGB.R);
            Bitmap redChannel = extrachChannel.Apply(video);
            //  merge red channel with moving object borders
            Merge mergeFilter = new Merge();
            mergeFilter.OverlayImage = tmp1;
            Bitmap tmp2 = mergeFilter.Apply(redChannel);
            // replace red channel in the original image
            ReplaceChannel replaceChannel = new ReplaceChannel(RGB.R, tmp2);
            replaceChannel.ChannelImage = tmp2;
            Bitmap tmp3 = replaceChannel.Apply(video);

            // Set picture box transformed image
            picCamera.Image = tmp3;

            // Keep last frame of video as background frame
            backgroundFrame = Grayscale.CommonAlgorithms.RMY.Apply(video);

        }
    }
}