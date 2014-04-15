using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

// C# color table: http://www.dotnetperls.com/color-table

namespace MatchingGame_v1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        Skeleton[] totalSkeleton = new Skeleton[6];
        WriteableBitmap colorBitmap;
        byte[] colorPixels;
        Skeleton skeleton;
        int currentSkeletonID = 0;

        // test colorObject
        colorObject testObject;

        private struct colorObject
        {
            public System.Windows.Point center;
            public Ellipse shape;


            public bool Touch(System.Windows.Point joint1, System.Windows.Point joint2)
            {
                double minDxSquared = this.shape.RenderSize.Width;  // calculate the minimum distance both hands must be from center
                minDxSquared *= minDxSquared;
                
                // calculate distance of both hands from center of object
                double dist1 = SquaredDistance(center.X, center.Y, joint1.X, joint1.Y);
                double dist2 = SquaredDistance(center.X, center.Y, joint2.X, joint2.Y);

                if (dist1 <= minDxSquared && dist2 <= minDxSquared) { return true; }
                else { return false; }
            }
        }

        private static double SquaredDistance(double x1, double y1, double x2, double y2)
        {
            return ((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1));
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                this.sensor = KinectSensor.KinectSensors[0];
                this.sensor.SkeletonStream.Enable();
                this.sensor.ColorStream.Enable();
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth,
                    this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                this.imagePreview.Source = this.colorBitmap;


                // map event handler for skeleton frame ready
                this.sensor.SkeletonFrameReady += skeletonFrameReady;
                // map event handler for color stream
                this.sensor.ColorFrameReady += colorFrameReady;

                this.sensor.Start();
                this.status.Text = "Sensor ready";

                // create shape
                testObject.shape = new Ellipse();
                testObject.shape.Width = 40;
                testObject.shape.Height = 40;
                testObject.shape.Fill = Brushes.Red;
                testObject.center.X = 300;
                testObject.center.Y = 50;
                testObject.shape.SetValue(Canvas.LeftProperty, testObject.center.X - testObject.shape.Width);
                testObject.shape.SetValue(Canvas.TopProperty, testObject.center.Y - testObject.shape.Height);
                canvas.Children.Add(testObject.shape);
            }
        }

        // Draw kinect color image onto preview image on screen
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

        // Draw kinect skeleton on screen
        void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            canvas.Children.Clear();
            updateObjectPositions();
            canvas.Children.Add(testObject.shape);

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
                    string TrackedTime = DateTime.Now.ToString("hh:mm:ss");
                    string status = "Skeleton ID:" + this.currentSkeletonID + ", total tracked joints: " + totalTrackedJoints + ", TrackTime: " + TrackedTime + "\n";
                    this.status.Text += status;
                }
                DrawSkeleton(skeleton);
                Point leftHandPoint = ScalePosition(skeleton.Joints[JointType.HandLeft].Position);
                Point rightHandPoint = ScalePosition(skeleton.Joints[JointType.HandRight].Position);

                bool grabState = testObject.Touch(leftHandPoint, rightHandPoint);
                if (grabState == true)
                {
                    testObject.center.X = rightHandPoint.X;
                    testObject.center.Y = rightHandPoint.Y;
                    this.status.Text += "GRABBED!\n";
                }

                
            }
        }

        // draw entire skeleton
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
            drawHead(skeleton.Joints[JointType.Head]);
        }

        // convert skeleton point onto canvas point
        private Point ScalePosition(SkeletonPoint skeletonPoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skeletonPoint, DepthImageFormat.Resolution320x240Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        // draw single bone
        void drawBone(Joint trackedJoint1, Joint trackedJoint2)
        {
            Line bone = new Line();
            bone.Stroke = Brushes.MidnightBlue;
            bone.StrokeThickness = 3;
            Point joint1 = this.ScalePosition(trackedJoint1.Position);
            bone.X1 = joint1.X;
            bone.Y1 = joint1.Y;

            Point joint2 = this.ScalePosition(trackedJoint2.Position);
            bone.X2 = joint2.X;
            bone.Y2 = joint2.Y;

            canvas.Children.Add(bone);
        }

        void drawHead(Joint headJoint)
        {
            Point headPoint = this.ScalePosition(headJoint.Position);
            Ellipse head = new Ellipse();
            head.Fill = Brushes.MediumTurquoise;
            head.Width = 20;
            head.Height = 40;
            head.SetValue(Canvas.LeftProperty, headPoint.X - head.Width/2);
            head.SetValue(Canvas.TopProperty, headPoint.Y - head.Height/2);
            canvas.Children.Add(head);
        }

        void updateObjectPositions()
        {
            testObject.shape.SetValue(Canvas.LeftProperty, testObject.center.X - testObject.shape.Width / 2);
            testObject.shape.SetValue(Canvas.TopProperty, testObject.center.Y - testObject.shape.Height / 2);
        }
    }
}
