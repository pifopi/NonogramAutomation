namespace NonogramAutomation.Services
{
    public static class ImageProcessing
    {
        private static readonly OpenCvSharp.Mat _funnelButton = Utils.Utils.OpenImage("Assets/funnelButton.png");
        private static readonly OpenCvSharp.Mat _playButton = Utils.Utils.OpenImage("Assets/playButton.png");
        private static readonly OpenCvSharp.Mat _favoriteIcon = Utils.Utils.OpenImage("Assets/favoriteIcon.png");
        private static readonly OpenCvSharp.Mat _detailsButton = Utils.Utils.OpenImage("Assets/detailsButton.png");
        private static readonly OpenCvSharp.Mat _favoriteButton = Utils.Utils.OpenImage("Assets/favoriteButton.png");

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

        public static (double, System.Drawing.Point)? SearchFunnelButton(OpenCvSharp.Mat image)
        {
            return Search(image, _funnelButton);
        }

        public static (double, System.Drawing.Point)? SearchPlayButton(OpenCvSharp.Mat image)
        {
            return Search(image, _playButton);
        }

        public static (double, System.Drawing.Point)? SearchFavoriteIcon(OpenCvSharp.Mat image)
        {
            return Search(image, _favoriteIcon);
        }

        public static (double, System.Drawing.Point)? SearchDetailsButton(OpenCvSharp.Mat image)
        {
            return Search(image, _detailsButton);
        }

        public static (double, System.Drawing.Point)? SearchFavoriteButton(OpenCvSharp.Mat image)
        {
            return Search(image, _favoriteButton);
        }

        public static bool DetectSearchMenu(OpenCvSharp.Mat screen)
        {
            OpenCvSharp.Mat location = Utils.Utils.Extract(screen, 0.709, 0.353, 0.181, 0.209);
            return Utils.Utils.IsMatching(location, 255, 255, 255, 0);
        }

        public static bool DetectPuzzleListMenu(OpenCvSharp.Mat screen)
        {
            OpenCvSharp.Mat location = Utils.Utils.Extract(screen, 0.105, 0.166, 0.806, 0.299);
            return Utils.Utils.IsMatching(location, 175, 205, 155, 25);
        }

        public static bool DetectPuzzleDetailsMenu(OpenCvSharp.Mat screen)
        {
            OpenCvSharp.Mat location = Utils.Utils.Extract(screen, 0.531, 0.358, 0.353, 0.043);
            return Utils.Utils.IsMatching(location, 255, 255, 255, 8);
        }
    }
}
