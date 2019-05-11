#if AForge
using AForge.Imaging;
using AForge.Imaging.Filters;
#endif
using ImageProcessor;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doorgen.Core.Helpers
{
    class ImagePostProcessHelper
    {
#if AForge
        public static void AforgeRotateAutoCrop(Bitmap image, double angle, string outputPath)
        {

            Logger logger = LogManager.GetCurrentClassLogger();

            // create filter - rotate for 30 degrees keeping original image size
            RotateBicubic rFilter = new RotateBicubic(angle, true);
            // apply the filter
            Bitmap newImage = rFilter.Apply(image);

            //return newImage;
            Bitmap autoCropImage = null;
            try
            {

                autoCropImage = newImage;
                // create grayscale filter (BT709)
                Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap grayImage = filter.Apply(autoCropImage);
                // create instance of skew checker
                DocumentSkewChecker skewChecker = new DocumentSkewChecker();
                // get documents skew angle
                angle = 0;//skewChecker.GetSkewAngle(grayImage);
                // create rotation filter
                RotateBilinear rotationFilter = new RotateBilinear(-angle);
                rotationFilter.FillColor = Color.White;
                // rotate image applying the filter
                Bitmap rotatedImage = rotationFilter.Apply(grayImage);
                new ContrastStretch().ApplyInPlace(rotatedImage);
                new Threshold(25).ApplyInPlace(rotatedImage);
                BlobCounter bc = new BlobCounter();
                bc.FilterBlobs = true;
                // bc.MinWidth = 500;
                //bc.MinHeight = 500;
                bc.ProcessImage(rotatedImage);
                Rectangle[] rects = bc.GetObjectsRectangles();

                if (rects.Length == 0)
                {
                    logger.Error("No rectangle found in image ");
                }
                else if (rects.Length == 1)
                {
                    autoCropImage = rotatedImage.Clone(rects[0], rotatedImage.PixelFormat); ;
                }
                else if (rects.Length > 1)
                {
                    // get largets rect
                    Console.WriteLine("Using largest rectangle found in image ");
                    var r2 = rects.OrderByDescending(r => r.Height * r.Width).ToList();
                    autoCropImage = rotatedImage.Clone(r2[1], rotatedImage.PixelFormat);
                }
                else
                {
                    Console.WriteLine("Huh? on image ");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }

            autoCropImage.Save(outputPath);
        }
#endif

        public static void ImageProcessorRotateAutoCrop(string imageFullPath, float angle)
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            string imageDir = Path.GetDirectoryName(imageFullPath);
            string resultFileName = $"{imageDir}\\{Path.GetFileNameWithoutExtension(imageFullPath)}_result.jpg";            

            try
            {
                using (ImageFactory imageFactory = new ImageFactory(preserveExifData: false))
                {
                    imageFactory
                        .Load(imageFullPath)
                        .Flip();

                    int w = imageFactory.Image.Width;
                    int h = imageFactory.Image.Height;

                    logger.Info($"- image postprocessing start, input resolution {w}x{h}");

                    int h1 = Convert.ToInt32(Math.Round(w * Math.Sin((angle / 180D) * Math.PI)));
                    int w1 = Convert.ToInt32(Math.Round(h * Math.Sin((angle / 180D) * Math.PI)));

                    imageFactory.Rotate(angle);

                    int newW = imageFactory.Image.Width;
                    int newH = imageFactory.Image.Height;

                    ImageProcessor.Imaging.CropLayer cropLayer = new ImageProcessor.Imaging.CropLayer(
                        w1, h1, newW - 2 * w1, newH - 2 * h1, ImageProcessor.Imaging.CropMode.Pixels);
                    
                    imageFactory                   
                        .Crop(cropLayer)
                        .Save(resultFileName);

                    logger.Info($"- image postprocessing successful, output resolution {imageFactory.Image.Width}x{imageFactory.Image.Height}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"- image postprocessing error: {ex.Message}");
            }
        }
    }
}
