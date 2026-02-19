using AdvancedSharpAdbClient.DeviceCommands;

namespace NonogramAutomation
{
    public static class Utils
    {
        public static void ExecuteCmd(string fileName, string arguments)
        {
            System.Diagnostics.Process process = new()
            {
                StartInfo = new(fileName, arguments)
                //{
                //    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                //}
            };
            process.Start();
        }

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

        private static OpenCvSharp.Mat Extract(OpenCvSharp.Mat image, OpenCvSharp.Rect rectangle)
        {
            return new(image, rectangle);
        }

        public static OpenCvSharp.Mat Extract(OpenCvSharp.Mat image, double startXPercent, double startYPercent, double sizeXPercent, double sizeYPercent)
        {
            int startX = (int)(image.Width * startXPercent);
            int startY = (int)(image.Height * startYPercent);
            int sizeX = (int)(image.Width * sizeXPercent);
            int sizeY = (int)(image.Height * sizeYPercent);

            OpenCvSharp.Rect rectangle = new(startX, startY, sizeX, sizeY);

            return Extract(image, rectangle);
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

        public static async Task<FoundElement?> FindElementAsync(ADBInstance adbInstance, List<string> queries, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Find {queries}");

            try
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        System.Xml.XmlDocument? doc = await DumpXMLAsync(adbInstance, linkedCts.Token);
                        if (doc != null)
                        {
                            foreach (string query in queries)
                            {
                                System.Xml.XmlNode? xmlNode = doc.SelectSingleNode(query);
                                if (xmlNode != null)
                                {
                                    AdvancedSharpAdbClient.DeviceCommands.Models.Element? element = AdvancedSharpAdbClient.DeviceCommands.Models.Element.FromXmlNode(adbInstance.AdbClient, adbInstance.DeviceData, xmlNode);
                                    if (element != null)
                                    {
                                        Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Element {query} found");
                                        return new FoundElement(queries.IndexOf(query), query, element);
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Xml.XmlException)
                    {
                        // Ignore XmlException and try again
                    }
                    if (linkedCts.Token == default) { break; }
                }
            }
            catch (Exception e)
            {
                // If a cancellation was requested, this main loop is interrupted with an exception
                // because the socket is closed. In that case, we don't need to throw a ShellCommandUnresponsiveException.
                // In all other cases, something went wrong, and we want to report it to the user.
                if (!linkedCts.Token.IsCancellationRequested)
                {
                    throw new AdvancedSharpAdbClient.Exceptions.ShellCommandUnresponsiveException(e);
                }
            }
            return null;
        }
        public static async Task<FoundElement?> FindElementAsync(ADBInstance adbInstance, string query, TimeSpan timeout, CancellationToken token)
        {
            return await FindElementAsync(adbInstance, new List<string> { query }, timeout, token);
        }

        public static async Task ClickElementAsync(ADBInstance adbInstance, List<string> queries, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Searching for {queries}");

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                FoundElement? foundElement = await FindElementAsync(adbInstance, queries, TimeSpan.FromSeconds(2), linkedCts.Token);
                if (foundElement != null)
                {
                    Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, $"Clicking on {foundElement.Query}");
                    await foundElement.Element.ClickAsync(linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                    return;
                }
            }
        }

        public static async Task ClickElementAsync(ADBInstance adbInstance, string query, TimeSpan timeout, CancellationToken token)
        {
            await ClickElementAsync(adbInstance, new List<string> { query }, timeout, token);
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
            int x = (int)(framebuffer.Header.Width * 0.5);
            int yBottom = (int)(framebuffer.Header.Height * 0.80);
            int yTop = (int)(framebuffer.Header.Height * 0.20);
            await SwipeAsync(adbInstance, new System.Drawing.Point(x, yBottom), new System.Drawing.Point(x, yTop), 2000, token);
        }

        public static async Task<System.Xml.XmlDocument?> DumpXMLAsync(ADBInstance adbInstance, CancellationToken token)
        {
            Logger.Log(Logger.LogLevel.Info, adbInstance.LogHeader, "Dumping screen");

            return await adbInstance.AdbClient.DumpScreenAsync(adbInstance.DeviceData, token);
        }

        private static OpenCvSharp.Rect ParseBounds(System.Xml.XmlNode node)
        {
            System.Xml.XmlAttribute boundsAttr = (node.Attributes?["bounds"]) ?? throw new Exception("xml node has no bounds");
            var match = System.Text.RegularExpressions.Regex.Match(boundsAttr.Value, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
            if (!match.Success)
            {
                throw new Exception("Bounds string was invalid");
            }

            int left = int.Parse(match.Groups[1].Value);
            int top = int.Parse(match.Groups[2].Value);
            int right = int.Parse(match.Groups[3].Value);
            int bottom = int.Parse(match.Groups[4].Value);

            return new OpenCvSharp.Rect(left, top, right - left, bottom - top);
        }

        public static async Task DumpAllAsync(ADBInstance adbInstance, string name, bool dumpBounds, CancellationToken token)
        {
            string folderName = "Dumps";
            string fullName = $"{folderName}/{name}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
            System.IO.Directory.CreateDirectory(folderName);

            System.Xml.XmlDocument xml = await DumpXMLAsync(adbInstance, token) ?? throw new Exception("xml document was null");
            xml.Save($"{fullName}.xml");

            OpenCvSharp.Mat image = await GetImageAsync(adbInstance, TimeSpan.FromSeconds(10), token);
            image.SaveImage($"{fullName}.png");

            if (dumpBounds)
            {
                System.Xml.XmlNodeList nodes = xml.SelectNodes("//*[@bounds]") ?? throw new Exception("xml document is invalid");
                int count = nodes.Count;
                int index = 0;
                foreach (System.Xml.XmlNode node in nodes)
                {
                    OpenCvSharp.Rect bounds = ParseBounds(node);
                    Extract(image, bounds).SaveImage($"{fullName}_{index}_{count}.png");
                    index++;
                }
            }
        }
    }
}
