using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;



namespace MatchMe
{
    /// <summary>
    /// Interaction logic for PlayWindow.xaml
    /// </summary>
    public partial class PlayWindow : Window
    {
        KinectSensor sensor;
        Skeleton[] totalSkeleton = new Skeleton[6];
        Skeleton skeleton;
        WriteableBitmap colorBitmap;
        Stream audioStream;
        SpeechRecognitionEngine speechEngine;
        RecognizerInfo recognizerInfo;
        EndWindow newEndWindow;



        // Object array for matching
        colorObject[] testobject;
        public enum colors { red, green, blue, yellow }
        public enum shapes { square, circle }
        int object_id = 0;
        int TEST_LENGTH = 10;
        Random random = new Random();

        string gameOption; //game player selected
        int currentSkeletonID = 0;
        byte[] colorPixels;


        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            statusBar.Text = "Play Window initialized";
            statusBar.Text += "\r\nsensor running: " + this.sensor.IsRunning.ToString();
            title.Content = "Match the " + gameOption + "s!!";

            // stop sensor to add new play window components
            this.sensor.Stop();

            // draw game frame elements
            DrawGameFrame(3);

            // build test objects
            BuildTestObjects();

            // color image stream
            this.sensor.ColorStream.Enable();
            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
            this.colorBitmap = new WriteableBitmap(
                this.sensor.ColorStream.FrameWidth,
                this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null
            );
            this.image.Source = this.colorBitmap;
            this.sensor.ColorFrameReady += colorFrameReady;

            // skeleton stream
            this.sensor.SkeletonStream.Enable();
            this.sensor.SkeletonFrameReady += skeletonFrameReady;
            this.sensor.Start();

            // audio stream
            audioStream = this.sensor.AudioSource.Start();
            recognizerInfo = GetKinectRecognizer();
            if (recognizerInfo == null)
            {
                MessageBox.Show("Could not find Kinect speech recognizer.");
                return;
            }
            // build grammar for Kinect to recognize
            BuildGrammarforRecognizer(recognizerInfo);


        }



        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // nothing needed
        }

        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }



        public PlayWindow(string gameChosen, KinectSensor kinect_sensor)
        {
            InitializeComponent();
            // gameChosen is passed from MainWindow when it opens this PlayWindow
            // gameChosen will be either "shape", "color", or "both"
            gameOption = gameChosen;
            sensor = kinect_sensor;
        }

        private void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
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
            foreach (var child in childrenToRemove.ToArray())
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
        private void drawBone(Joint trackedJoint1, Joint trackedJoint2)
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


        private void colorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
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

        private void BuildTestObjects()
        {
            string status;
            statusBar.Text += "creating test objects\n";

            // set up array of color objects
            this.testobject = new colorObject[TEST_LENGTH];

            for(int i=0; i<TEST_LENGTH; i++)
            {
                // Total number of enum elements: http://stackoverflow.com/questions/856154/total-number-of-items-defined-in-an-enum
                colors color = (colors)random.Next(0, Enum.GetNames(typeof(colors)).Length);       // get a random color
                shapes shape = (shapes)random.Next(0, Enum.GetNames(typeof(shapes)).Length);       // get a random shape


                if(shape == shapes.circle)
                        testobject[i].shape = new Ellipse();
                if(shape == shapes.square)
                        testobject[i].shape = new Rectangle();

  

                if(color == colors.red)
                        testobject[i].shape.Fill = Brushes.Red;
                if(color == colors.blue)
                        testobject[i].shape.Fill = Brushes.Blue;
                if(color == colors.green)
                        testobject[i].shape.Fill = Brushes.Green;
                if(color == colors.yellow)
                        testobject[i].shape.Fill = Brushes.Yellow;

      


                // TODO: add different sizes
                testobject[i].shape.Width = 100;
                testobject[i].shape.Height = 100;

                testobject[i].center.X = random.Next(0,(int)mainCanvas.Width);
                testobject[i].center.Y = random.Next(0, (int)mainCanvas.Height);

                //UIElementExtensions.SetGroupID(testobject[object_id].shape, 3);

                Canvas.SetTop(testobject[i].shape, testobject[i].center.Y);
                Canvas.SetLeft(testobject[i].shape, testobject[i].center.X);

 
            }

            statusBar.Text += "test objects created\n";
        }


        // SPEECH
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase)
                    && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }
            return null;
        }

        private void BuildGrammarforRecognizer(RecognizerInfo recognizerInfo)
        {
            var grammarBuilder = new GrammarBuilder { Culture = recognizerInfo.Culture };
            // add more choices
            var gameOptions = new Choices();
            gameOptions.Add("quit");
            // add choices to grammar builder
            grammarBuilder.Append(gameOptions);

            // Create Grammar from GrammarBuilder
            var grammar = new Grammar(grammarBuilder);

            // Start the speech recognizer
            speechEngine = new SpeechRecognitionEngine(recognizerInfo.Id);
            speechEngine.LoadGrammar(grammar); // loading grammer into recognizer            

            // Attach the speech audio source to the recognizer
            int samplesPerSecond = 16000; int bitsPerSample = 16;
            int channels = 1; int averageBytesPerSecond = 32000; int blockAlign = 2;
            speechEngine.SetInputToAudioStream(
                 audioStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm,
                 samplesPerSecond, bitsPerSample, channels, averageBytesPerSecond,
                 blockAlign, null)
            );

            // Register the event handler for speech recognition
            speechEngine.SpeechRecognized += speechRecognized;
            speechEngine.SpeechHypothesized += speechHypothesized;
            speechEngine.SpeechRecognitionRejected += speechRecognitionRejected;

            speechEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void speechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        { }

        private void speechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            //wordsTenative.Text = e.Result.Text;
        }

        private void speechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //wordsRecognized.Text = e.Result.Text;
            //confidenceTxt.Text = e.Result.Confidence.ToString();
            float confidenceThreshold = 0.6f;
            if (e.Result.Confidence > confidenceThreshold)
            {
                CommandsParser(e);
            }
        }

        private void CommandsParser(SpeechRecognizedEventArgs e)
        {
            string spokenCmd;
            System.Collections.ObjectModel.ReadOnlyCollection<RecognizedWordUnit> words = e.Result.Words;

            spokenCmd = words[0].Text;
            switch (spokenCmd)
            {
                case "quit":
                    // exit the game                    
                    Application.Current.Shutdown();
                    return;
                default:
                    return;
            }

        }

        // Test function to add random object on click
        private void add_click(object sender, RoutedEventArgs e)
        {
            mainCanvas.Children.Add(testobject[object_id].shape);

            if (object_id < TEST_LENGTH - 1)
            { object_id++; }
            else
            {
                button_add_shape.IsEnabled = false;
            }
        }
    }
}
