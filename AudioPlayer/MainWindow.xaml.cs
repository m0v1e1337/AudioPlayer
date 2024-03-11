using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer;
        private List<string> musicFiles;
        private int currentTrackIndex;
        private bool isPlaying = false;
        private bool isRepeat = false;
        private bool isShuffle = false;
        private CancellationTokenSource sliderTokenSource;
        private CancellationTokenSource timerTokenSource;

        public MainWindow()
        {
            InitializeComponent();

            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    musicFiles = Directory.GetFiles(folderDialog.SelectedPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => file.ToLower().EndsWith(".mp3") || file.ToLower().EndsWith(".wav") || file.ToLower().EndsWith(".m4a"))
                        .ToList();

                    if (musicFiles.Any())
                    {
                        currentTrackIndex = 0;
                        await PlayMusic(musicFiles[currentTrackIndex]);
                    }
                }
            }
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                mediaPlayer.Pause();
                isPlaying = false;
                PlayPauseButton.Content = "Play";
                timerTokenSource.Cancel();
            }
            else
            {
                if (mediaPlayer.Source == null)
                {
                    if (musicFiles != null && musicFiles.Any())
                    {
                        await PlayMusic(musicFiles[currentTrackIndex]);
                    }
                }
                else
                {
                    mediaPlayer.Play();
                    isPlaying = true;
                    PlayPauseButton.Content = "Pause";
                    StartTimer();
                }
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            currentTrackIndex++;

            if (isShuffle)
            {
                currentTrackIndex = new Random().Next(0, musicFiles.Count);
            }

            if (currentTrackIndex >= musicFiles.Count)
            {
                currentTrackIndex = 0;
            }

            mediaPlayer.Stop();
            mediaPlayer.Close();
            mediaPlayer.Source = new Uri(musicFiles[currentTrackIndex]);

            if (isPlaying)
            {
                mediaPlayer.Play();
            }

            StartTimer();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            currentTrackIndex--;

            if (isShuffle)
            {
                currentTrackIndex = new Random().Next(0, musicFiles.Count);
            }

            if (currentTrackIndex < 0)

            {
                currentTrackIndex = musicFiles.Count - 1;
            }

            mediaPlayer.Stop();
            mediaPlayer.Close();
            mediaPlayer.Source = new Uri(musicFiles[currentTrackIndex]);

            if (isPlaying)
            {
                mediaPlayer.Play();
            }

            StartTimer();
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            isRepeat = !isRepeat;

            if (isRepeat)
            {
                RepeatButton.Background = Brushes.Green;
            }
            else
            {
                RepeatButton.ClearValue(BackgroundProperty);
            }
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            isShuffle = !isShuffle;

            if (isShuffle)
            {
                ShuffleButton.Background = Brushes.Green;
            }
            else
            {
                ShuffleButton.ClearValue(BackgroundProperty);
            }
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (isRepeat)
            {
                mediaPlayer.Stop();
                mediaPlayer.Play();
            }
            else
            {
                NextButton_Click(null, null);
            }
        }

        private async Task PlayMusic(string filePath)
        {
            mediaPlayer.Close();
            mediaPlayer.Source = new Uri(filePath);
            mediaPlayer.Play();

            isPlaying = true;
            PlayPauseButton.Content = "Pause";

            StartTimer();
        }

        private void StartTimer()
        {
            timerTokenSource?.Cancel();
            timerTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (timerTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    TimeSpan duration = mediaPlayer.NaturalDuration.TimeSpan;
                    TimeSpan position = mediaPlayer.Position;

                    Dispatcher.Invoke(() =>
                    {
                        DurationLabel.Content = duration.ToString(@"mm\:ss");
                        PositionSlider.Maximum = duration.TotalSeconds;
                        PositionSlider.Value = position.TotalSeconds;

                        PositionLabel.Content = position.ToString(@"mm\:ss");
                        RemainingLabel.Content = (duration - position).ToString(@"mm\:ss");
                    });

                    Thread.Sleep(1000);
                }
            }, timerTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sliderTokenSource?.Cancel();
            sliderTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (sliderTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        mediaPlayer.Position = TimeSpan.FromSeconds(PositionSlider.Value);
                    });

                    Thread.Sleep(100);
                }
            });
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            mediaPlayer.Volume = VolumeSlider.Value;
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow historyWindow = new HistoryWindow(musicFiles);
            bool? result = historyWindow.ShowDialog();

            if (result == true && historyWindow.SelectedFilePath != null)
            {
                currentTrackIndex = musicFiles.IndexOf(historyWindow.SelectedFilePath);
                PlayMusic(historyWindow.SelectedFilePath);
            }
        }
    }
}
