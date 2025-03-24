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
    public MainWindow()
    {
        InitializeComponent();
    }

    private void connectBtn_Click(object sender, RoutedEventArgs e)
    {
        // Give message box to user
        MessageBox.Show("Connected to server", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void downloadBtn_Click(object sender, RoutedEventArgs e)
    {

    }
}