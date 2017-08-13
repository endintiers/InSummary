using AForge.Imaging.Filters;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InSummaryFunctions.Helpers
{
    public class VisionAPIHelper
    {
        public static void OCRPage(PDFPage thisPage, TraceWriter log)
        {
            foreach (var img in thisPage.ExtractedImages)
            {
                // Image must be Bitmap
                if (!(img is Bitmap))
                {
                    log.Info("Found image that was not Bitmap - ignoring");
                    continue;
                }
                // Image must be at least 40 x 40
                if (img.Width < 40 || img.Height < 40)
                {
                    log.Info("Image is too small - ignoring");
                    continue;
                }

                // Convert this image to grayscale (must be Bitmap as we discovered earlier)
                var bmp = (Bitmap)img; // In case Grayscale fails - just use it as-is
                if (img.PixelFormat != PixelFormat.Format1bppIndexed &&
                    img.PixelFormat != PixelFormat.Format8bppIndexed)
                {
                    try
                    {
                        bmp = Grayscale.CommonAlgorithms.BT709.Apply((Bitmap)img);
                    }
                    catch (Exception) { }
                }

                // Image can't be larger than 3200 x 3200
                if (bmp.Width > 3200 || bmp.Height > 3200)
                {
                    log.Info(string.Format("Image is too big {0} x {1} - dealing with that...", bmp.Width, bmp.Height));
                    if (bmp.Width < 3500 && bmp.Height < 3500)
                    {
                        // Lets crop it - hopefully there are margins
                        log.Info("Cropping the image");
                        // Calculate Crop Rectangle
                        var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
                        if (bmp.Width > 3200)
                        {
                            var halfWideCrop = (bmp.Width - 3200) / 2;
                            rect.X = halfWideCrop;
                            rect.Width = bmp.Width - halfWideCrop;

                        }
                        if (bmp.Height > 3200)
                        {
                            var halfHighCrop = (bmp.Width - 3200) / 2;
                            rect.Y = halfHighCrop;
                            rect.Height = bmp.Height - halfHighCrop;
                        }
                        Crop cropFilter = new Crop(rect);
                        bmp = cropFilter.Apply(bmp);
                    }
                    else
                    {
                        // Scale the image down
                        log.Info("Resizing the image");
                        // Calculate the shrinkage
                        int newWidth, newHeight;
                        if (bmp.Width > bmp.Height)
                        {
                            var scaleFactor = bmp.Width / 3200F;
                            newWidth = Convert.ToInt32(bmp.Width / scaleFactor);
                            newHeight = Convert.ToInt32(bmp.Height / scaleFactor);
                        }
                        else
                        {
                            var scaleFactor = bmp.Height / 3200F;
                            newWidth = Convert.ToInt32(bmp.Width / scaleFactor);
                            newHeight = Convert.ToInt32(bmp.Height / scaleFactor);
                        }
                        if (newWidth > 3200 || newHeight > 3200)
                            throw new ApplicationException("DOH! Miscalculated Scale");

                        try
                        {
                            ResizeBicubic resizeFilter = new ResizeBicubic(newWidth, newHeight);
                            bmp = resizeFilter.Apply(bmp);
                        }
                        catch (AForge.Imaging.UnsupportedImageFormatException)
                        {
                            return;
                        }
                    }
                }

                var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                ms.Position = 0;
                try
                {
                    Task<string> recoTask = Task.Run<string>(async () =>
                    {
                        var result = await UploadAndRecognizeImageTextAsync(ms, "en");
                        return result;
                    });
                    recoTask.Wait();
                    var ocrResult = recoTask.Result;
                    thisPage.OCRText += ocrResult.Trim() + "\r\n";
                }
                catch (Exception ex)
                {
                    log.Warning(string.Format("Page {0} OCR Exception: {1}", thisPage.Number, ex.Message));
                }

            }
        }

        /// <summary>
        /// Uploads the image to Project Oxford and performs OCR
        /// </summary>
        /// <param name="imageStream">The image file stream.
        /// Supported image formats: JPEG, PNG, GIF, BMP. 
        /// Image file size must be less than 4MB.
        /// Image dimensions must be between 40 x 40 and 3200 x 3200 pixels, and the image cannot be larger than 100 megapixels.
        /// </param>
        /// <param name="language">The language code to recognize for</param>
        /// <returns></returns>
        public static async Task<string> UploadAndRecognizeImageTextAsync(Stream imageStream, string language)
        {
            // Upload an image and perform OCR
            OcrResults ocrResult = await UploadAndRecognizeImageAsync(imageStream, language);
            return ConvertOcrResultsToString(ocrResult);
        }

        /// <summary>
        /// Uploads the image to Project Oxford and performs OCR
        /// </summary>
        /// <param name="imageStream">The image file stream.
        /// Supported image formats: JPEG, PNG, GIF, BMP. 
        /// Image file size must be less than 4MB.
        /// Image dimensions must be between 40 x 40 and 3200 x 3200 pixels, and the image cannot be larger than 100 megapixels.
        /// </param>
        /// <param name="language">The language code to recognize for</param>
        /// <returns></returns>
        public static async Task<OcrResults> UploadAndRecognizeImageAsync(Stream imageStream, string language)
        {
            // Create Project Oxford Vision API Service client
            VisionServiceClient VisionServiceClient = new VisionServiceClient(Constants.VisionAPIKey);

            // Upload an image and perform OCR
            OcrResults ocrResult = await VisionServiceClient.RecognizeTextAsync(imageStream, language);
            return ocrResult;
        }


        private static string ConvertOcrResultsToString(OcrResults results)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (results != null && results.Regions != null)
            {
                stringBuilder.AppendLine();
                foreach (var item in results.Regions)
                {
                    foreach (var line in item.Lines)
                    {
                        foreach (var word in line.Words)
                        {
                            stringBuilder.Append(word.Text);
                            stringBuilder.Append(" ");
                        }
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.AppendLine();
                }
            }
            return stringBuilder.ToString();
        }
    }
}
