using AdvancedSharpAdbClient.DeviceCommands;

namespace NonogramAutomation
{
    public static class Utils
    {
        public static async Task<AdvancedSharpAdbClient.Models.DeviceData> GetDeviceDataFromAsync(AdvancedSharpAdbClient.AdbClient adbClient, string key, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                foreach (AdvancedSharpAdbClient.Models.DeviceData device in await adbClient.GetDevicesAsync(linkedCts.Token))
                {
                    if (device.Serial == key)
                    {
                        return device;
                    }
                }
            }
        }

        public static void StartADBServer()
        {
            if (!AdvancedSharpAdbClient.AdbServer.Instance.GetStatus().IsRunning)
            {
                AdvancedSharpAdbClient.AdbServer server = new();
                AdvancedSharpAdbClient.Models.StartServerResult resultStartServer = server.StartServer("adb", false);
                if (resultStartServer != AdvancedSharpAdbClient.Models.StartServerResult.Started)
                {
                    throw new Exception("Can't start adb server, make sure you add adb.exe to your PATH");
                }
            }
        }

        public static async Task<OpenCvSharp.Mat> GetImageAsync(ADBInstance adbInstance, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            AdvancedSharpAdbClient.Models.Framebuffer framebuffer = await adbInstance.AdbClient.GetFrameBufferAsync(adbInstance.DeviceData, linkedCts.Token);
            if (framebuffer.Header.Red.Length != 8 ||
                framebuffer.Header.Green.Length != 8 ||
                framebuffer.Header.Blue.Length != 8 ||
                framebuffer.Header.Alpha.Length != 8 ||
                framebuffer.Header.Red.Offset != 0 ||
                framebuffer.Header.Green.Offset != 8 ||
                framebuffer.Header.Blue.Offset != 16 ||
                framebuffer.Header.Alpha.Offset != 24)
            {
                throw new Exception($"The screenshot color are not encoded in the expected way (framebuffer:{framebuffer})");
            }
            if (framebuffer.Data == null)
            {
                throw new Exception($"The screenshot data buffer is null (framebuffer:{framebuffer})");
            }

            int height = (int)framebuffer.Header.Height;
            int width = (int)framebuffer.Header.Width;

            OpenCvSharp.Mat imageRGBA = OpenCvSharp.Mat.FromPixelData(height, width, OpenCvSharp.MatType.CV_8UC4, framebuffer.Data);
            OpenCvSharp.Mat imageBGR = new(imageRGBA.Size(), OpenCvSharp.MatType.CV_8UC3);
            OpenCvSharp.Cv2.CvtColor(imageRGBA, imageBGR, OpenCvSharp.ColorConversionCodes.RGBA2BGR);
            return imageBGR;
        }

        public static OpenCvSharp.Mat OpenImage(string filename)
        {
            if (!System.IO.File.Exists(filename))
            {
                throw new Exception($"file {filename} could not be found");
            }
            return OpenCvSharp.Cv2.ImRead(filename);
        }

        public static OpenCvSharp.Mat Extract(OpenCvSharp.Mat input, double startXPercent, double startYPercent, double sizeXPercent, double sizeYPercent)
        {
            int startX = (int)(input.Width * startXPercent);
            int startY = (int)(input.Height * startYPercent);
            int sizeX = (int)(input.Width * sizeXPercent);
            int sizeY = (int)(input.Height * sizeYPercent);

            OpenCvSharp.Rect rectangle = new(startX, startY, sizeX, sizeY);
            OpenCvSharp.Mat output = new(input, rectangle);

            return output;
        }

        private static bool IsBetween(double value, double min, double max)
        {
            return value >= min && value <= max;
        }

        private static bool AreNearlyEqual(double value, double reference, double epsilon = 10)
        {
            return IsBetween(value, reference - epsilon, reference + epsilon);
        }

        private static double Sum(OpenCvSharp.Scalar scalar)
        {
            return scalar.Val0 + scalar.Val1 + scalar.Val2;
        }

        public static bool IsMatching(OpenCvSharp.Mat mat, double red, double green, double blue, double expectedStdDevSum)
        {
            OpenCvSharp.Cv2.MeanStdDev(mat, out OpenCvSharp.Scalar mean, out OpenCvSharp.Scalar stddev);
            if (!AreNearlyEqual(mean.Val0, blue))
            {
                return false;
            }
            if (!AreNearlyEqual(mean.Val1, green))
            {
                return false;
            }
            if (!AreNearlyEqual(mean.Val2, red))
            {
                return false;
            }
            if (!AreNearlyEqual(Sum(stddev), expectedStdDevSum))
            {
                return false;
            }
            return true;
        }

        public static Tesseract.Pix ConvertToPix(OpenCvSharp.Mat mat)
        {
            OpenCvSharp.Cv2.ImEncode(".bmp", mat, out byte[] stream);
            return Tesseract.Pix.LoadFromMemory(stream);
        }

        public static async Task<AdvancedSharpAdbClient.DeviceCommands.Models.Element?> FindElementAsync(ADBInstance adbInstance, string query, CancellationToken token)
        {
            return await adbInstance.AdbClient.FindElementAsync(adbInstance.DeviceData, query, token);
        }

        public static async Task<bool> DetectElementAsync(ADBInstance adbInstance, string query, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            AdvancedSharpAdbClient.DeviceCommands.Models.Element? element = await FindElementAsync(adbInstance, query, linkedCts.Token);
            if (element is null)
            {
                Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Element {query} not found");
                return false;

            }
            else
            {
                Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Element {query} found");
                return true;
            }
        }

        public static async Task ClickElementAsync(ADBInstance adbInstance, string query, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Searching for {query}");

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                AdvancedSharpAdbClient.DeviceCommands.Models.Element? element = await FindElementAsync(adbInstance, query, linkedCts.Token);
                if (element != null)
                {
                    Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Clicking on {query}");
                    await element.ClickAsync(linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                    return;
                }
            }
        }

        public static async Task ClickBackButtonAsync(ADBInstance adbInstance, CancellationToken token)
        {
            Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, "Clicking back");

            await adbInstance.AdbClient.ClickBackButtonAsync(adbInstance.DeviceData, token);
        }

        public static async Task SwipeAsync(ADBInstance adbInstance, System.Drawing.Point first, System.Drawing.Point second, long speed, CancellationToken token)
        {
            Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Swiping from {first} to {second}");

            await adbInstance.AdbClient.SwipeAsync(adbInstance.DeviceData, first, second, speed, token);
        }

        public static async Task SwipeToBottomAsync(ADBInstance adbInstance, CancellationToken token)
        {
            Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, "Swiping to bottom");

            AdvancedSharpAdbClient.Models.Framebuffer framebuffer = await adbInstance.AdbClient.GetFrameBufferAsync(adbInstance.DeviceData);
            int height = (int)framebuffer.Header.Height;
            int width = (int)framebuffer.Header.Width;
            await SwipeAsync(adbInstance, new System.Drawing.Point(width / 2, height - 100), new System.Drawing.Point(width / 2, 100), 2000, token);
        }

        public static async Task<System.Xml.XmlDocument?> DumpScreenAsync(ADBInstance adbInstance, CancellationToken token)
        {
            Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, "Dumping screen");

            return await adbInstance.AdbClient.DumpScreenAsync(adbInstance.DeviceData, token);
        }
    }
}
