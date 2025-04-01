using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Consumer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static MainWindow _instance;

    private DispatcherTimer _previewTimer;
    private ListBoxItem _currentlyPlayingItem;

    private static string videoFolder => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Program.videoFolder.Substring(2));

    public MainWindow()
    {
        InitializeComponent();
        _instance = this;
        ReloadVideoList();
    }

    private async void connectBtn_Click(object sender, RoutedEventArgs e)
    {
        statusLabel.Content = "Connecting to Producer...";

        bool success = false;
        await Task.Run(() =>
        {
            success = Program.ConnectToProducer();
        });

        if (success)
            statusLabel.Content = "Connected to Producer";
        else
            statusLabel.Content = "Failed to connect to Producer";
    }

    private async void downloadBtn_Click(object sender, RoutedEventArgs e)
    {
        statusLabel.Content = "Downloading videos...";

        bool success = false;
        await Task.Run(() =>
        {
            success = Program.StartDownloadingVideos();
        });

        if (success)
            statusLabel.Content = "Videos downloaded";
        else
            statusLabel.Content = "Failed to download videos";
    }

    private void openVideoFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        // Open the video folder
        System.Diagnostics.Process.Start("explorer.exe", videoFolder);
    }

    public static void AddVideoToList(string videoFileName)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _instance.VideoList.Items.Add(videoFileName);
        });
    }

    public static void ReloadVideoList()
    {
        // Clear the list
        _instance.VideoList.Items.Clear();

        try
        {             
            // Check if video folder exists
            // if not, create it
            if (!Directory.Exists(videoFolder))
            {
                Directory.CreateDirectory(videoFolder);
            }
            var videoFiles = Directory.GetFiles(videoFolder, "*.mp4");
            foreach (var videoFile in videoFiles)
            {
                _instance.VideoList.Items.Add(System.IO.Path.GetFileName(videoFile));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[UI Thread] Error reloading videos: " + ex.Message);
            MessageBox.Show("[UI Thread] Error reloading videos: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
    {
        try
        {
            if (sender is ListBoxItem item && item.Content != null)
            {
                // Stop the current preview if another item is being previewed
                if (_currentlyPlayingItem != null && _currentlyPlayingItem != item)
                {
                    PreviewPlayer.Stop();
                    _currentlyPlayingItem.MouseLeave -= ListBoxItem_MouseLeave;
                }

                // Stop the existing timer if it is running
                _previewTimer?.Stop();

                _currentlyPlayingItem = item;

                string videoFileName = item.Content.ToString();
                string selectedVideoPath = System.IO.Path.Combine(videoFolder, videoFileName);
                PreviewPlayer.Source = new Uri(selectedVideoPath, UriKind.Relative);
                PreviewPlayer.Position = TimeSpan.Zero;

                PreviewPlayer.LoadedBehavior = MediaState.Manual;
                PreviewPlayer.UnloadedBehavior = MediaState.Manual;

                PreviewPlayer.Play();

                _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                _previewTimer.Tick += (s, args) =>
                {
                    PreviewPlayer.Stop();
                    _previewTimer.Stop();
                    _currentlyPlayingItem = null;
                };
                _previewTimer.Start();

                item.MouseLeave += ListBoxItem_MouseLeave;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[UI Thread] Error previewing video: " + ex.Message);
            MessageBox.Show("[UI Thread] Error previewing video: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ListBoxItem_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is ListBoxItem item)
        {
            PreviewPlayer.Stop();
            item.MouseLeave -= ListBoxItem_MouseLeave;
            _currentlyPlayingItem = null;

            // Stop the timer when the mouse leaves the item
            _previewTimer?.Stop();
        }
    }

    private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (sender is ListBoxItem item && item.Content != null)
            {
                string videoFileName = item.Content.ToString();
                string selectedVideoPath = System.IO.Path.Combine(videoFolder, videoFileName);
                Window window = new Window
                {
                    Title = "Video Player",
                    Content = new MediaElement
                    {
                        Source = new Uri(selectedVideoPath, UriKind.Relative),
                        LoadedBehavior = MediaState.Play,
                        Stretch = Stretch.Uniform
                    },
                    Width = 800,
                    Height = 600
                };
                window.Show();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[UI Thread] Error showing video: " + ex.Message);
            MessageBox.Show("[UI Thread] Error showing video: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}