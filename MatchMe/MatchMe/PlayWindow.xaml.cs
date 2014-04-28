﻿using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


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

        // Drop zone objects
        //dropObject[] drop_object;
        //int NUM_DROP_OBJ = 3;

        // Object array for matching
        colorObject[] testObjects;
        public enum colors { red, green, blue }
        public enum shapes { square, circle }
        int object_id = -1;
        int TEST_LENGTH = 3;
        Random random = new Random();

        // Match boxes hit detection
        resultZone[] resultZones;
        bool finished = false;

        string gameOption; //game player selected
        int currentSkeletonID = 0;
        byte[] colorPixels;

        public PlayWindow(string gameChosen, KinectSensor kinect_sensor)
        {
            InitializeComponent();
            // gameChosen is passed from MainWindow when it opens this PlayWindow
            // gameChosen will be either "shape", "color", or "both"
            gameOption = gameChosen;
            sensor = kinect_sensor;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            statusBar.Text = "Play Window initialized";
            statusBar.Text += "\r\nsensor running: " + this.sensor.IsRunning.ToString();
            title.Content = "Match the " + gameOption + "s!";

            // stop sensor to add new play window components
            stopKinect();

            // draw game frame elements
            DrawGameFrame(3);

            // draw drop zone elements
            //DrawDropZone();

            // build test objects
            BuildTestObjects();

            // add the first object to play
            drawNextObject();

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

        private void stopKinect() // use this to stop the Kinect
        {
            if (this.sensor != null && this.sensor.IsRunning) // if Kinect's running
            {
                this.sensor.Stop(); // stop the sensor
            }
        }

        private void DrawGameFrame(int numberOfBoxes)
        {
            int frame_width = 300;
            this.resultZones = new resultZone[numberOfBoxes];

            for (int i = 0; i < numberOfBoxes; i++)
            {
                Line frameH = new Line();
                frameH.Stroke = Brushes.Black;
                frameH.StrokeThickness = 3;

                // add horizontal lines
                frameH.X1 = mainCanvas.Width - frame_width;
                frameH.Y1 = (mainCanvas.Height / numberOfBoxes) * i;
                frameH.X2 = mainCanvas.Width;
                frameH.Y2 = (mainCanvas.Height / numberOfBoxes) * i;

                UIElementExtensions.SetGroupID(frameH, 2);
                mainCanvas.Children.Add(frameH);

                // create resultZone boundaries
                resultZones[i].X1 = (int)frameH.X1;
                resultZones[i].X2 = (int)frameH.X2;

                resultZones[i].Y1 = (int)((mainCanvas.Height / numberOfBoxes) * i);
                resultZones[i].Y2 = (int)((mainCanvas.Height / numberOfBoxes) * (i + 1));

                resultZones[i].center.X = (int)((resultZones[i].X1 + resultZones[i].X2) / 2);
                resultZones[i].center.Y = (int)((resultZones[i].Y1 + resultZones[i].Y2) / 2);
            }

            Line frameV = new Line();
            frameV.Stroke = Brushes.Black;
            frameV.StrokeThickness = 2;
            // add vertical line
            frameV.X1 = mainCanvas.Width - frame_width;
            frameV.Y1 = mainCanvas.Height;
            frameV.X2 = mainCanvas.Width - frame_width;
            frameV.Y2 = 0;

            UIElementExtensions.SetGroupID(frameV, 2);
            mainCanvas.Children.Add(frameV);

            // Draw color or shape matching shapes
            if (gameOption == "color")
            {
                for (int i = 0; i < resultZones.Length; i++)
                {
                    Ellipse colorCircle = new Ellipse();
                    int size = 50;
                    colorCircle.Height = size;
                    colorCircle.Width = size;
                    colorCircle.Stroke = Brushes.Black;
                    colorCircle.StrokeThickness = 2;

                    // TODO: create colors programmically
                    if (i == 0)
                    {
                        colorCircle.Fill = Brushes.Red;
                    }
                    if (i == 1)
                    {
                        colorCircle.Fill = Brushes.Blue;
                    }
                    if (i == 2)
                    {
                        colorCircle.Fill = Brushes.Green;
                    }

                    Canvas.SetTop(colorCircle, resultZones[i].center.Y - size / 2);
                    Canvas.SetLeft(colorCircle, resultZones[i].center.X - size / 2);

                    UIElementExtensions.SetGroupID(colorCircle, 2);
                    mainCanvas.Children.Add(colorCircle);
                }
            }
        }

        /*private void DrawDropZone()
        {
            // Create drop objects
            this.drop_object = new dropObject[NUM_DROP_OBJ];
            //Red Circle
            drop_object[0].shape = new Ellipse();
            drop_object[0].shape.Width = 140;
            drop_object[0].shape.Height = 140;
            drop_object[0].shape.Fill = Brushes.Red;
            //Green Square
            drop_object[1].shape = new Rectangle();
            drop_object[1].shape.Width = 140;
            drop_object[1].shape.Height = 140;
            drop_object[1].shape.Fill = Brushes.Green;
            //Blue Triangle
            var triangle = new Polygon();
            triangle.Points.Add(new Point(70, 30));
            triangle.Points.Add(new Point(0, 150));
            triangle.Points.Add(new Point(140, 150));
            drop_object[2].shape = triangle;
            drop_object[2].shape.Fill = Brushes.Blue;

            // Place drop objects into drop zone            
            //Set positions and properties
            drop_object[0].shape.SetValue(Canvas.LeftProperty, 25.0);
            drop_object[0].shape.SetValue(Canvas.TopProperty, 30.0);

            drop_object[1].shape.SetValue(Canvas.LeftProperty, 25.0);
            drop_object[1].shape.SetValue(Canvas.TopProperty, 200.0);

            drop_object[2].shape.SetValue(Canvas.LeftProperty, 25.0);
            drop_object[2].shape.SetValue(Canvas.TopProperty, 350.0);
            //Add to canvas
            UIElementExtensions.SetGroupID(drop_object[0].shape, 2);
            UIElementExtensions.SetGroupID(drop_object[1].shape, 2);
            UIElementExtensions.SetGroupID(drop_object[2].shape, 2);
            mainCanvas.Children.Add(drop_object[0].shape);
            mainCanvas.Children.Add(drop_object[1].shape);
            mainCanvas.Children.Add(drop_object[2].shape);
        }*/

        private void BuildTestObjects()
        {
            //string status;
            statusBar.Text += "creating test objects\n";

            // set up array of color objects
            this.testObjects = new colorObject[TEST_LENGTH];
            //this.testObjects = new List<colorObject>();

            // define box where objects can appear
            int minX = 100;
            int maxX = 400;
            int minY = 100;
            int maxY = 400;

            for (int i = 0; i < TEST_LENGTH; i++)
            {
                // Total number of enum elements: http://stackoverflow.com/questions/856154/total-number-of-items-defined-in-an-enum
                colors color = (colors)random.Next(0, Enum.GetNames(typeof(colors)).Length);       // get a random color
                shapes shape = (shapes)random.Next(0, Enum.GetNames(typeof(shapes)).Length);       // get a random shape

                if (shape == shapes.circle)
                    testObjects[i].shape = new Ellipse();
                if (shape == shapes.square)
                    testObjects[i].shape = new Rectangle();

                if (color == colors.red)
                    testObjects[i].shape.Fill = Brushes.Red;
                if (color == colors.blue)
                    testObjects[i].shape.Fill = Brushes.Blue;
                if (color == colors.green)
                    testObjects[i].shape.Fill = Brushes.Green;
                //if (color == colors.yellow)
                //    testObjects[i].shape.Fill = Brushes.Yellow;


                // TODO: add different sizes
                testObjects[i].size = 100;
                testObjects[i].shape.Width = testObjects[i].size;
                testObjects[i].shape.Height = testObjects[i].size;

                testObjects[i].center.X = random.Next(minX, maxX);
                testObjects[i].center.Y = random.Next(minY, maxY);

                //UIElementExtensions.SetGroupID(testObjects[object_id].shape, 3);

                Canvas.SetTop(testObjects[i].shape, testObjects[i].center.Y);
                Canvas.SetLeft(testObjects[i].shape, testObjects[i].center.X);

                testObjects[i].shape.Stroke = Brushes.Black;
                testObjects[i].shape.StrokeThickness = 2;

            }

            statusBar.Text += "test objects created\n";
        }

        // Test function to add random object on click
        private void add_click(object sender, RoutedEventArgs e)
        {
            object_id++;

            if (object_id < TEST_LENGTH)
            {
                mainCanvas.Children.Add(testObjects[object_id].shape);
            }
            else
            {
                button_add_shape.IsEnabled = false;
            }
        }

        private bool drawNextObject()
        {
            object_id++;

            if (object_id < TEST_LENGTH)
            {
                mainCanvas.Children.Add(testObjects[object_id].shape);
            }
            else
            {
                this.finished = true;
                // reset and clear all <-- important 
                object_id = 0;
                mainCanvas.Children.Clear();
            }

            return this.finished;
        }

        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            stopKinect();
            Application.Current.Shutdown();
        }

        private void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
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

            // Determine if the current test object is touching the right hand, if it is make the object follow the hand
            Point RightHandPoint = ScalePosition(skeleton.Joints[JointType.HandRight].Position);

            if (object_id < TEST_LENGTH && testObjects[object_id].Touch(RightHandPoint))
            {
                testObjects[object_id].center.X = RightHandPoint.X;
                testObjects[object_id].center.Y = RightHandPoint.Y;

                Canvas.SetTop(testObjects[object_id].shape, testObjects[object_id].center.Y - testObjects[object_id].size / 2);
                Canvas.SetLeft(testObjects[object_id].shape, testObjects[object_id].center.X - testObjects[object_id].size / 2);

                //this.statusBar.Text += "grabbed!\n";

                // Determine of the current test object is in the zone
                if (!this.finished) //if not finished
                {
                    for (int i = 0; i < resultZones.Length; i++)
                    {
                        if (resultZones[i].inZone(testObjects[object_id].center))
                        {
                            this.statusBar.Text += "in Zone: " + i.ToString() + "\n";
                            testObjects[object_id].center = resultZones[i].center;

                            Canvas.SetTop(testObjects[object_id].shape, testObjects[object_id].center.Y - testObjects[object_id].size / 2);
                            Canvas.SetLeft(testObjects[object_id].shape, testObjects[object_id].center.X - testObjects[object_id].size / 2);

                            if (drawNextObject()) //if finished
                            {
                                title.Content = "Finished!";
                                // launch ending screen
                                if (newEndWindow == null)
                                {
                                    newEndWindow = new EndWindow(sensor);
                                    newEndWindow.Show();
                                };
                                // close this play window
                                this.Close();
                            }
                        }
                    }
                }
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

        // Skeleton scale code inspired by: http://social.msdn.microsoft.com/Forums/en-US/b71f7719-88bc-44c3-ab3c-76d3cf24cc94/kinectsdk-convert-skeleton-point-to-screen-point?forum=kinectsdknuiapi
        private Point ScalePosition(SkeletonPoint skeletonPoint)
        {
            double scale = mainCanvas.Height / 480.0;
            double x_offset = 0;// mainCanvas.Width - (640.0 * scale) / 2;

            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skeletonPoint, DepthImageFormat.Resolution640x480Fps30);

            Point point = new Point();
            point.X = x_offset + (scale * depthPoint.X);
            point.Y = (scale * depthPoint.Y);

            return new Point(x_offset + (scale * depthPoint.X), (scale * depthPoint.Y));
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
            float confidenceThreshold = 0.7f;
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
                    stopKinect();
                    Application.Current.Shutdown();
                    return;
                default:
                    return;
            }

        }

    }
}
