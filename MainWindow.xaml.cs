using NonogramAutomation.Models;
using NonogramAutomation.Services;

namespace NonogramAutomation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public System.Collections.ObjectModel.ObservableCollection<ADBInstanceRealPhoneViaIP> ADBInstancesRealPhoneViaIP { get; set; } = [];
        public MainWindow()
        {
            InitializeComponent();

            SettingsManager.GlobalSettings = SettingsManager.LoadSettings<GlobalSettings>(@"config\global.json");

            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(@"config\NLog.config");

            Logger.Log(Logger.LogLevel.Info, "", "-------------------------------------------------------------------------");
            Logger.Log(Logger.LogLevel.Info, "", $"<@{SettingsManager.GlobalSettings.DiscordUserId}> Starting the program");

            foreach (var s in SettingsManager.GlobalSettings.RealPhoneInstances)
            {
                ADBInstancesRealPhoneViaIP.Add(new ADBInstanceRealPhoneViaIP
                {
                    Name = s.Name,
                    IP = s.IP,
                    Port = s.Port
                });
            }

            Utils.Utils.StartADBServer();

            DataContext = this;
        }

        static private ADBInstance AsADBInstance(object sender)
        {
            if (sender == null)
            {
                throw new Exception("Null sender");
            }
            System.Windows.FrameworkElement frameworkElement = (System.Windows.FrameworkElement)sender;
            return (ADBInstance)frameworkElement.DataContext;
        }

        public void StartProgram(object sender, System.Windows.RoutedEventArgs e)
        {
            AsADBInstance(sender).StartProgram();
        }

        public void StopProgram(object sender, System.Windows.RoutedEventArgs e)
        {
            AsADBInstance(sender).StopProgram();
        }
    }
}