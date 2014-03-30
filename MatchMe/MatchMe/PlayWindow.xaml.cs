using System.Windows;

namespace MatchMe
{
    /// <summary>
    /// Interaction logic for PlayWindow.xaml
    /// </summary>
    public partial class PlayWindow : Window
    {
        EndWindow newEndWindow;
        string gameOption; // use this to check for which game the player selected

        public PlayWindow(string gameChosen)
        {
            InitializeComponent();
            // gameChosen is passed from MainWindow when it opens this PlayWindow
            // gameChosen will be either "shape", "color", or "both"
            gameOption = gameChosen;
        }

        // open EndWindow when user quits at playing
        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            newEndWindow = new EndWindow();
            newEndWindow.Show();
            this.Close();
        }
    }
}
