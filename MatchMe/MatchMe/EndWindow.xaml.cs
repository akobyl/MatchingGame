using Microsoft.Kinect;
using System.Windows;


namespace MatchMe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class EndWindow : Window
    {
        KinectSensor sensor;
        PlayWindow newPlayWindow;

        public EndWindow(KinectSensor kinect_sensor)
        {
            InitializeComponent();
            //Loaded += new RoutedEventHandler(WindowLoaded);
            sensor = kinect_sensor;
        }

        // WINDOW LOADING CLOSING
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //do nothing
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
            Application.Current.Shutdown();
        }

    }
}
