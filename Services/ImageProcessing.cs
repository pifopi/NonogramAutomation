namespace NonogramAutomation.Services
{
    public static class ImageProcessing
    {
        private static readonly OpenCvSharp.Mat _playButton = Utils.Utils.OpenImage("Assets/playButton.png");

        private static (double, System.Drawing.Point)? Search(OpenCvSharp.Mat screen, OpenCvSharp.Mat template)
        {
            double ratio = screen.Width / 1080;
            screen = screen.Resize(new OpenCvSharp.Size { Width = 1080, Height = (int)(screen.Height / ratio) }); ;

            OpenCvSharp.Mat result = screen.MatchTemplate(template, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
            OpenCvSharp.Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal < 0.9)
            {
                return null;
            }
            return (maxVal, new((int)((maxLoc.X + template.Width / 2) * ratio), (int)((maxLoc.Y + template.Height / 2) * ratio)));
        }

        public static (double, System.Drawing.Point)? SearchPlayButton(OpenCvSharp.Mat image)
        {
            return Search(image, _playButton);
        }

        public static bool DetectPuzzleListMenu(OpenCvSharp.Mat screen)
        {
            OpenCvSharp.Mat location = Utils.Utils.Extract(screen, 0.105, 0.166, 0.806, 0.299);
            return Utils.Utils.IsMatching(location, 175, 205, 155, 25);
        }
    }
}
