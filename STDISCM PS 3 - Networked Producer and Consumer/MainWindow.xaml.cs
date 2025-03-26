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

namespace STDISCM_PS_3___Networked_Producer_and_Consumer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private TcpClient client;
    private NetworkStream stream;
    private const string producerIP = "192.168.1.12"; // Replace with real IP
    private const int port = 9000;
    private const string savePath = "./saved_vids/ReceivedVideo.mp4";

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void connectBtn_Click(object sender, RoutedEventArgs e)
    {
        // Give message box to user
        //MessageBox.Show("Connected to server", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
        logTextBox.AppendText("Connecting to producer...\n");

        try
        {
            client = new TcpClient();
            await client.ConnectAsync(producerIP, port);
            stream = client.GetStream();
            logTextBox.AppendText("✅ Connected to producer.\n");
        }
        catch (Exception ex)
        {
            logTextBox.AppendText("❌ Connection failed: " + ex.Message + "\n");
        }
    }

    private async void downloadBtn_Click(object sender, RoutedEventArgs e)
    {
        if (stream == null)
        {
            logTextBox.AppendText("⚠️ Not connected. Click 'Connect' first.\n");
            return;
        }

        try
        {
            logTextBox.AppendText("📥 Downloading video...\n");
            using FileStream fileStream = File.Create(savePath);
            await stream.CopyToAsync(fileStream);
            logTextBox.AppendText("✅ Download complete. Saved to: " + savePath + "\n");
        }
        catch (Exception ex)
        {
            logTextBox.AppendText("❌ Download failed: " + ex.Message + "\n");
        }

    }
}