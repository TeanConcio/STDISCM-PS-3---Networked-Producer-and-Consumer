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

namespace STDISCM_PS_3___Networked_Producer_and_Consumer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Default values for configurations
    private const uint DEFAULT_NUMBER_OF_CONSUMER_THREADS = 4;
    private const uint DEFAULT_MAX_QUEUE_SIZE = 4;
    private static readonly IPAddress DEFAULT_PRODUCER_IP_ADDRESS = IPAddress.Parse("127.0.0.1");
    private const uint DEFAULT_PRODUCER_PORT_NUMBER = 9000;

    // Configurations
    private static uint numConsumerThreads = DEFAULT_NUMBER_OF_CONSUMER_THREADS;
    private static uint maxQueueSize = DEFAULT_MAX_QUEUE_SIZE;
    private static IPAddress producerIPAddress = DEFAULT_PRODUCER_IP_ADDRESS;
    private static uint producerPortNumber = DEFAULT_PRODUCER_PORT_NUMBER;

    private TcpClient client;
    private NetworkStream stream;

    private const string videoFolder = "./downloaded_videos";
    private string selectedVideoPath;

    public MainWindow()
    {
        InitializeComponent();

        LoadVideoList();
    }

    public static void GetConfig()
    {
        Console.WriteLine("Getting Configurations from config.txt");
        Console.WriteLine();

        bool hasErrorWarning = false;

        string[] lines = System.IO.File.ReadAllLines("config.txt");
        foreach (string line in lines)
        {
            // Skip empty lines or comments (#)
            if (line.Trim() == "" || line.Trim().StartsWith("#"))
            {
                continue;
            }

            // Split line by "="
            string[] parts = line.Split("=");

            switch (parts[0].Trim().ToUpper())
            {
                case "NUMBER_OF_CONSUMER_THREADS (C) = ":
                    if (!uint.TryParse(parts[1].Trim(), out uint tempNumConsumerThreads) || tempNumConsumerThreads > int.MaxValue || tempNumConsumerThreads < 1)
                    {
                        Console.WriteLine($"Error: Invalid Number of Instances. Setting Number of Instances to {DEFAULT_NUMBER_OF_CONSUMER_THREADS}.");
                        hasErrorWarning = true;
                    }
                    else
                    {
                        // Check if very large number of instances
                        if (tempNumConsumerThreads >= 50)
                        {
                            Console.WriteLine($"Warning: Very large number of threads. Please expect a very long initialization time.");
                            hasErrorWarning = true;
                        }

                        numConsumerThreads = tempNumConsumerThreads;
                    }
                    break;

                case "MAX_QUEUE_SIZE (Q) = ":
                    if (!uint.TryParse(parts[1].Trim(), out uint tempMaxQueueSize) || tempMaxQueueSize > int.MaxValue || tempMaxQueueSize < 1)
                    {
                        Console.WriteLine($"Error: Invalid Max Queue Size. Setting Max Queue Size to {DEFAULT_MAX_QUEUE_SIZE}.");
                        hasErrorWarning = true;
                    }
                    else
                    {
                        maxQueueSize = tempMaxQueueSize;
                    }
                    break;

                case "PRODUCER_IP_ADDRESS = ":
                    if (!IPAddress.TryParse(parts[1].Trim(), out IPAddress tempProducerIPAddress))
                    {
                        Console.WriteLine($"Error: Invalid Producer IP Address. Setting Producer IP Address to {DEFAULT_PRODUCER_IP_ADDRESS}.");
                        hasErrorWarning = true;
                    }
                    else
                    {
                        producerIPAddress = tempProducerIPAddress;
                    }
                    break;

                case "PRODUCER_PORT_NUMBER = ":
                    if (!ushort.TryParse(parts[1].Trim(), out ushort tempConsumerPortNumber) || tempConsumerPortNumber > ushort.MaxValue || tempConsumerPortNumber < 1)
                    {
                        Console.WriteLine($"Error: Invalid Producer Port Number. Setting Producer Port Number to {DEFAULT_PRODUCER_PORT_NUMBER}.");
                        hasErrorWarning = true;
                    }
                    else
                    {
                        producerPortNumber = tempConsumerPortNumber;
                    }
                    break;
            }
        }

        // If there is an error, print a line
        if (hasErrorWarning)
        {
            Console.WriteLine();
        }

        // Print Configurations
        Console.WriteLine("Number of Consumer Threads: " + numConsumerThreads);
        Console.WriteLine("Max Queue Size: " + maxQueueSize);
        Console.WriteLine("Producer IP Address: " + producerIPAddress);
        Console.WriteLine("Producer Port Number: " + producerPortNumber);

        Console.WriteLine();
        Console.WriteLine();
    }


    private async void connectBtn_Click(object sender, RoutedEventArgs e)
    {
        // Give message box to user
        //MessageBox.Show("Connected to server", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
        //logTextBox.AppendText("Connecting to producer...\n");

        try
        {
            client = new TcpClient();
            await client.ConnectAsync(producerIPAddress, (int)producerPortNumber);
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