using System.Configuration;
using System.Data;
using System.Windows;

namespace Consumer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Program.cs startup
        Program.GetConfig();
        Program.Initialize();
    }
}

