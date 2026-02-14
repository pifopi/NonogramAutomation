namespace NonogramAutomation
{
    public class ProgramDownload : Program
    {
        public ProgramDownload(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            try
            {
                await _adbInstance.ConnectToInstanceAsync(_token);

                string query = $"//node[@resource-id='com.ucdevs.jcross:id/imgSizeHolder'][descendant::node[@resource-id='com.ucdevs.jcross:id/imgDwlMini']]";

                System.Xml.XmlDocument? lastScreen = null;
                System.Xml.XmlDocument? currentScreen = await Utils.DumpXMLAsync(_adbInstance, _token);

                while (lastScreen != currentScreen)
                {
                    using var searchTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    using var searchLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(_token, searchTimeoutCts.Token);
                    AdvancedSharpAdbClient.DeviceCommands.Models.Element? element = await Utils.FindElementAsync(_adbInstance, query, TimeSpan.FromSeconds(2), searchLinkedCts.Token);
                    if (element is null)
                    {
                        Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"No download button found, scrolling");
                        await Utils.SwipeToBottomAsync(_adbInstance, _token);
                    }
                    else
                    {
                        Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Clicking on download button");
                        await element.ClickAsync(_token);
                        await Task.Delay(TimeSpan.FromMilliseconds(100), _token);
                    }
                }

                Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> Done processing all download");
            }
            catch (OperationCanceledException exception)
            {
                Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"An exception has been raised:{exception}");
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
        }
    }
}