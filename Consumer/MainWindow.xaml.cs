using System.IO;
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

namespace STDISCM_PS_3___Networked_Producer_and_Consumer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private TcpClient client;
    private NetworkStream stream;
    private const string producerIP = "127.0.0.1";  // Localhost for testing; change to actual IP
    private const int port = 9000;

    private const string videoFolder = "./downloaded_videos";
    private string selectedVideoPath;

    public MainWindow()
    {
        InitializeComponent();

        LoadVideoList();
    }

    private async void connectBtn_Click(object sender, RoutedEventArgs e)
    {
        // Give message box to user
        //MessageBox.Show("Connected to server", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
        //logTextBox.AppendText("Connecting to producer...\n");

        try
        {
            client = new TcpClient();
            await client.ConnectAsync(producerIP, port);
            stream = client.GetStream();
            //logTextBox.AppendText("✅ Connected to producer.\n");
        }
        catch (Exception ex)
        {
            //logTextBox.AppendText("❌ Connection failed: " + ex.Message + "\n");
        }
    }

    private async void downloadBtn_Click(object sender, RoutedEventArgs e)
    {
        if (stream == null)
        {
            //logTextBox.AppendText("⚠️ Not connected. Click 'Connect' first.\n");
            return;
        }

        try
        {
            //logTextBox.AppendText("📥 Downloading video...\n");
            using FileStream fileStream = File.Create(videoFolder);
            await stream.CopyToAsync(fileStream);
            //logTextBox.AppendText("✅ Download complete. Saved to: " + savePath + "\n");
        }
        catch (Exception ex)
        {
            //logTextBox.AppendText("❌ Download failed: " + ex.Message + "\n");
        }
    }

    private void LoadVideoList()
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
            VideoList.Items.Add(System.IO.Path.GetFileName(videoFile));
        }
    }

    private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
    {
        //try {
            if (sender is ListBoxItem item && item.Content != null)
            {
                string videoFileName = item.Content.ToString();
                selectedVideoPath = System.IO.Path.Combine(videoFolder, videoFileName);
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
        //}
        //catch (Exception ex) {
        //    MessageBox.Show("Error: " + ex.Message);
        //}
    }

    private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item && item.Content != null)
        {
            string videoFileName = item.Content.ToString();
            selectedVideoPath = System.IO.Path.Combine(videoFolder, videoFileName);
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