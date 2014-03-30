using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.IO;
using System.Windows;


namespace MatchMe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class EndWindow : Window
    {
        KinectSensor sensor;
        Stream audioStream;
        SpeechRecognitionEngine speechEngine;
        RecognizerInfo recognizerInfo;
        PlayWindow newPlayWindow;

        public EndWindow()
        {
            InitializeComponent();
            //Loaded += new RoutedEventHandler(WindowLoaded);
        }

        // WINDOW LOADING CLOSING
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                // select the first available sensor
                this.sensor = KinectSensor.KinectSensors[0];
                if (this.sensor != null && !this.sensor.IsRunning)
                {
                    // start the sensor
                    this.sensor.Start();
                    // start audio stream to listen to voice
                    audioStream = this.sensor.AudioSource.Start();
                    recognizerInfo = GetKinectRecognizer();
                    if (recognizerInfo == null)
                    {
                        MessageBox.Show("Could not find Kinect speech recognizer.");
                        return;
                    }
                    // build grammar for Kinect to recognize
                    BuildGrammarforRecognizer(recognizerInfo);
                    statusBar.Text = "Speech Recognizer is ready";
                }
            }
            else // Kinect not connected
            {
                MessageBox.Show("Kinect sensor is not connected.");
                this.Close();
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // do nothing here because we don't want to stop the sensor when going to new window
        }

        private void stopKinect() // use this to stop the Kinect
        {
            if (this.sensor != null && this.sensor.IsRunning) // if Kinect's running
            {
                this.sensor.Stop(); // stop the sensor
            }
        }

        // BUTTONS CLICK
        private void shapeButton_Click(object sender, RoutedEventArgs e)
        {
            newPlayWindow = new PlayWindow("shape");
            newPlayWindow.Show();
            this.Close();
        }

        private void colorButton_Click(object sender, RoutedEventArgs e)
        {
            newPlayWindow = new PlayWindow("color");
            newPlayWindow.Show();
            this.Close();
        }

        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            stopKinect();
            this.Close();
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
            // first say 'Hello'
            grammarBuilder.Append(new Choices("Hello"));
            // add more choices
            var gameOptions = new Choices();
            gameOptions.Add("shape");
            gameOptions.Add("color");
            gameOptions.Add("quit");
            gameOptions.Add("both");
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
                case "shape":
                    //go to shape game
                    newPlayWindow = new PlayWindow("shape");
                    newPlayWindow.Show();
                    this.Close();
                    break;
                case "color":
                    // go to color game
                    newPlayWindow = new PlayWindow("color");
                    newPlayWindow.Show();
                    this.Close();
                    break;
                case "both":
                    // go to Easter egg game
                    newPlayWindow = new PlayWindow("both");
                    newPlayWindow.Show();
                    this.Close();
                    break;
                case "quit":
                    // exit the game
                    stopKinect();
                    this.Close();
                    break;
                default:
                    return;
            }

            /*var result = e.Result;
            Color objectColor;
            Shape drawObject;
            System.Collections.ObjectModel.ReadOnlyCollection<RecognizedWordUnit> words = e.Result.Words;
            
            if (words[0].Text == "draw")
            {
                string colorObject = words[1].Text;
                switch (colorObject)
                {
                    case "red": objectColor = Colors.Red;
                        break;
                    case "green": objectColor = Colors.Green;
                        break;
                    case "blue": objectColor = Colors.Blue;
                        break;
                    case "yellow": objectColor = Colors.Yellow;
                        break;
                    case "gray": objectColor = Colors.Gray;
                        break;
                    default:
                        return;
                }

                var shapeString = words[2].Text;
                switch (shapeString)
                {
                    case "circle":
                        drawObject = new Ellipse();
                        drawObject.Width = 100; drawObject.Height = 100;
                        break;
                    case "square":
                        drawObject = new Rectangle();
                        drawObject.Width = 100; drawObject.Height = 100;
                        break;
                    case "rectangle":
                        drawObject = new Rectangle();
                        drawObject.Width = 100; drawObject.Height = 60;
                        break;
                    case "triangle":
                        var polygon = new Polygon();
                        polygon.Points.Add(new Point(0, 30));
                        polygon.Points.Add(new Point(-60, -30));
                        polygon.Points.Add(new Point(60, -30));
                        drawObject = polygon;
                        break;
                    default:
                        return;
                }

                canvas1.Children.Clear();
                drawObject.SetValue(Canvas.LeftProperty, 80.0);
                drawObject.SetValue(Canvas.TopProperty, 80.0);
                drawObject.Fill = new SolidColorBrush(objectColor);
                canvas1.Children.Add(drawObject);
            }

            if (words[0].Text == "close" && words[1].Text == "the" && words[2].Text == "application")
            {
                this.Close();
            }*/
        }
    }
}
