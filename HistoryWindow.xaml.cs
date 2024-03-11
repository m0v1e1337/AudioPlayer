using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayer
{
    public partial class HistoryWindow : Window
    {
        public string SelectedFilePath { get; private set; }

        public HistoryWindow(List<string> musicFiles)
        {
            InitializeComponent();

            foreach (var musicFile in musicFiles)
            {
                Button button = new Button();
                button.Content = musicFile;
                button.Click += MusicFileButton_Click;

                MusicFilesPanel.Children.Add(button);
            }
        }

        private void MusicFileButton_Click(object sender, RoutedEventArgs e)
        {

            SelectedFilePath = ((Button)sender).Content.ToString();
            DialogResult = true;
            Close();
        }
    }
}
