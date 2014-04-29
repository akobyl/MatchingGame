using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;


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

        // Timer
        private DispatcherTimer timer = new DispatcherTimer();

        public EndWindow(KinectSensor kinect_sensor)
        {
            InitializeComponent();
            //Loaded += new RoutedEventHandler(WindowLoaded);
            sensor = kinect_sensor;
        }

        // WINDOW LOADING CLOSING
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // stop sensor to add new play window components
            stopKinect();

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
            statusBar.Text = "Speech Recognizer is NOT ready";
            // a quick timer to wait for the speech engine to be ready
            // there is no 'engine-ready' function/boolean provided by the SDK API
            StartTimer();
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
            if (newPlayWindow == null)
            {
                newPlayWindow = new PlayWindow("shape", sensor);
                newPlayWindow.Show();
            }
            this.Close();
        }

        private void colorButton_Click(object sender, RoutedEventArgs e)
        {
            if (newPlayWindow == null)
            {
                newPlayWindow = new PlayWindow("color", sensor);
                newPlayWindow.Show();
            }
            this.Close();
        }

        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            stopKinect();
            //this.Close(); is not the right way to shutdown the app
            Application.Current.Shutdown();
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
            gameOptions.Add("shape");
            gameOptions.Add("color");
            gameOptions.Add("both");
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
                case "shape":
                    //go to shape game
                    if (newPlayWindow == null)
                    {
                        newPlayWindow = new PlayWindow("shape", sensor);
                        newPlayWindow.Show();
                    };
                    this.Close();
                    return;
                case "color":
                    // go to color game
                    if (newPlayWindow == null)
                    {
                        newPlayWindow = new PlayWindow("color", sensor);
                        newPlayWindow.Show();
                    };
                    this.Close();
                    return;
                case "both":
                    // go to Easter egg game
                    if (newPlayWindow == null)
                    {
                        newPlayWindow = new PlayWindow("both", sensor);
                        newPlayWindow.Show();
                    };
                    this.Close();
                    return;
                case "quit":
                    // exit the game                    
                    stopKinect();
                    Application.Current.Shutdown();
                    return;
                default:
                    return;
            }

        }

        // MISC
        private void StartTimer()
        {
            // Start timer
            timer.Interval = new TimeSpan(0, 0, 3);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void StopTimer()
        {
            timer.Stop();
            timer.Tick -= timer_Tick;
        }

        private void timer_Tick(object sender, System.EventArgs e)
        {
            // Re-enable buttons
            statusBar.Text = "Speech Recognizer is READY";
            // Stop timer
            StopTimer();
        }
    }
}
