using System.Windows;
using Microsoft.Kinect;
using System.Windows.Shapes;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MatchMe
{
    /// <summary>
    /// Interaction logic for PlayWindow.xaml
    /// </summary>
    public partial class PlayWindow : Window
    {
        EndWindow newEndWindow;
        string gameOption; // use this to check for which game the player selected
        KinectSensor sensor;
        Skeleton[] totalSkeleton = new Skeleton[6];
        Skeleton skeleton;
        int currentSkeletonID = 0;

        // image variables
        WriteableBitmap colorBitmap;
        byte[] colorPixels;

        public PlayWindow(string gameChosen, KinectSensor kinect_sensor)
        {
            InitializeComponent();
            // gameChosen is passed from MainWindow when it opens this PlayWindow
            // gameChosen will be either "shape", "color", or "both"
            gameOption = gameChosen;

            sensor = kinect_sensor;
        }

        void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //mainCanvas.Children.Clear();
            ClearSkeleton();


            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null) { return; }
                skeletonFrame.CopySkeletonDataTo(totalSkeleton);
                skeleton = (from trackskeleton in totalSkeleton
                            where trackskeleton.TrackingState == SkeletonTrackingState.Tracked
                            select trackskeleton).FirstOrDefault();
                if (skeleton == null) { return; }
                if (skeleton != null && this.currentSkeletonID != skeleton.TrackingId)
                {
                    this.currentSkeletonID = skeleton.TrackingId;
                    int totalTrackedJoints = skeleton.Joints.Where(item => item.TrackingState == JointTrackingState.Tracked).Count();
                    string status = "\nSkeleton ID:" + this.currentSkeletonID + ", total tracked joints: " + totalTrackedJoints;
                    this.statusBar.Text += status;
                }
                DrawSkeleton(skeleton);
            }
        }

        private void ClearSkeleton()
        {
            var childrenToRemove = mainCanvas.Children.OfType<UIElement>().
                                        Where(c => UIElementExtensions.GetGroupID(c) == 1);
            foreach(var child in childrenToRemove.ToArray())
            {
               mainCanvas.Children.Remove(child);
            }
            //statusBar.Text += childrenToRemove.ToString();
        }

        private void DrawSkeleton(Skeleton skeleton)
        {
            drawBone(skeleton.Joints[JointType.Head], skeleton.Joints[JointType.ShoulderCenter]);
            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.Spine]);
            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderLeft]);
            drawBone(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft]);
            drawBone(skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft]);
            drawBone(skeleton.Joints[JointType.WristLeft], skeleton.Joints[JointType.HandLeft]);
            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderRight]);
            drawBone(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight]);
            drawBone(skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.WristRight]);
            drawBone(skeleton.Joints[JointType.WristRight], skeleton.Joints[JointType.HandRight]);
            drawBone(skeleton.Joints[JointType.Spine], skeleton.Joints[JointType.HipCenter]);
            drawBone(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipLeft]);
            drawBone(skeleton.Joints[JointType.HipLeft], skeleton.Joints[JointType.KneeLeft]);
            drawBone(skeleton.Joints[JointType.KneeLeft], skeleton.Joints[JointType.AnkleLeft]);
            drawBone(skeleton.Joints[JointType.AnkleLeft], skeleton.Joints[JointType.FootLeft]);
            drawBone(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipRight]);
            drawBone(skeleton.Joints[JointType.HipRight], skeleton.Joints[JointType.KneeRight]);
            drawBone(skeleton.Joints[JointType.KneeRight], skeleton.Joints[JointType.AnkleRight]);
            drawBone(skeleton.Joints[JointType.AnkleRight], skeleton.Joints[JointType.FootRight]);
        }

        // draw single bone
        void drawBone(Joint trackedJoint1, Joint trackedJoint2)
        {
            Line bone = new Line();
            bone.Stroke = Brushes.Blue;
            bone.StrokeThickness = 4;
            Point joint1 = this.ScalePosition(trackedJoint1.Position);
            bone.X1 = joint1.X;
            bone.Y1 = joint1.Y;

            Point joint2 = this.ScalePosition(trackedJoint2.Position);
            bone.X2 = joint2.X;
            bone.Y2 = joint2.Y;

            UIElementExtensions.SetGroupID(bone, 1);
            mainCanvas.Children.Add(bone);
        }

        private Point ScalePosition(SkeletonPoint skeletonPoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skeletonPoint, DepthImageFormat.Resolution320x240Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }


        void colorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (null == imageFrame)
                    return;

                imageFrame.CopyPixelDataTo(colorPixels);
                int stride = imageFrame.Width * imageFrame.BytesPerPixel;

                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorPixels, stride, 0);

            }
        }


        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            statusBar.Text = "Play Window initialized";
            statusBar.Text += "\r\nsensor running: " + this.sensor.IsRunning.ToString();
            title.Content = "Match the " + gameOption + "s!!";

            this.sensor.Stop();     // stop sensor to re-enable componenents needed in play window

            this.sensor.SkeletonStream.Enable();

            // draw game frame elements
            DrawGameFrame(3);
            
            // color image setup
            this.sensor.ColorStream.Enable();
            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
            this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth,
                this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.image.Source = this.colorBitmap;
            this.sensor.ColorFrameReady += colorFrameReady;

            this.sensor.SkeletonFrameReady += skeletonFrameReady;
            this.sensor.Start();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        // open EndWindow when user quits at playing
        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            newEndWindow = new EndWindow();
            newEndWindow.Show();
            this.Close();
        }

        private void DrawGameFrame(int numberOfBoxes)
        {
            for (int i = 1; i < numberOfBoxes; i++)
            {
                Line frame = new Line();
                frame.Stroke = Brushes.Black;
                frame.StrokeThickness = 2;
                // add horizontal lines
                frame.X1 = mainCanvas.Width;
                frame.Y1 = (mainCanvas.Height / numberOfBoxes) * i;
                frame.X2 = mainCanvas.Width - 300;
                frame.Y2 = (mainCanvas.Height / numberOfBoxes) * i;

                UIElementExtensions.SetGroupID(frame, 2);
                mainCanvas.Children.Add(frame);
            }
        }
    }
}
