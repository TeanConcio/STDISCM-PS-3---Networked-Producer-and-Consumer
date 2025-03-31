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

    private static string videoFolder => Program.videoFolder;

    public MainWindow()
    {
        InitializeComponent();
        _instance = this;
        ReloadVideoList();
    }

    private async void connectBtn_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(async () => { Program.ConnectToProducer(); });
    }

    private async void downloadBtn_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(async () => { Program.StartDownloadingVideos(); }); 
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
            Console.WriteLine("Error: " + ex.Message);
            MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
    {
        try {
            if (sender is ListBoxItem item && item.Content != null)
            {
                string videoFileName = item.Content.ToString();
                string selectedVideoPath = System.IO.Path.Combine(videoFolder, videoFileName);
                PreviewPlayer.Source = new Uri(selectedVideoPath, UriKind.Relative);
                PreviewPlayer.Position = TimeSpan.Zero;
                PreviewPlayer.Play();

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                timer.Tick += (s, args) =>
                {
                    PreviewPlayer.Stop();
                    timer.Stop();
                };
                timer.Start();
            }
        }
        catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
            MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
}