using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace InSummaryFunctions.Helpers
{
    public class iTextPDFHelper
    {
        public static List<PDFPage> GetPDFPages(Stream pdfStream, TraceWriter log, bool ocrImages = false)
        {
            var result = new List<PDFPage>();

            pdfStream.Position = 0; // Ensure that we are at the start

            // Note: PdfReader Dispose closes the stream...
            using (PdfReader reader = new PdfReader(pdfStream))
            {
                var numberOfPages = reader.NumberOfPages;

                var parser = new PdfReaderContentParser(reader);
                ImageRenderListener listener = null; // = new ImageRenderListener(log);

                for (var i = 1; i <= reader.NumberOfPages; i++)
                {

                    var page = new PDFPage { Number = i };
                    try
                    {
                        parser.ProcessContent(i, (listener = new ImageRenderListener(log)));
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("Page {0} Image Processing Exception", i), ex);
                    }

                    if (listener.Images.Count > 0)
                    {
                        log.Info(string.Format("Found {0} images on page {1}.", listener.Images.Count, i));
                        page.ExtractedImages = listener.Images;
                        if (ocrImages)
                        {
                            if (listener.Images.Count < 10)
                            {
                                log.Info("Calling Vision API to OCR Page Images");
                                VisionAPIHelper.OCRPage(page, log);
                            }
                            else
                                log.Info("Too many Page Images for Vision API");
                        }
                    }
                    try
                    {
                        page.PageText = PdfTextExtractor.GetTextFromPage(reader, i, new SimpleTextExtractionStrategy());
                    }
                    catch (System.ArgumentException ex)
                    {
                        log.Error(string.Format("Page {0} Text Processing Exception", i), ex);
                    }

                    result.Add(page);
                }
            }
            return result;
        }

        internal class ImageRenderListener : IRenderListener
        {
            private List<System.Drawing.Image> _images = new List<Image>();
            private TraceWriter _log;
            public List<System.Drawing.Image> Images
            {
                get { return _images; }
            }

            public ImageRenderListener(TraceWriter log)
            {
                _log = log;
            }

            public void RenderImage(ImageRenderInfo renderInfo)
            {
                PdfImageObject image = null;
                Image drawingImage = null;
                try
                {
                    image = renderInfo.GetImage();
                    var imgBytesLen = image.GetImageAsBytes().Length;
                    // Smallest image we can OCR is 40 x 40
                    if (imgBytesLen > 1600)
                    {
                        drawingImage = image.GetDrawingImage();
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("Exception in GetImage or GetDrawingImage: {0}", ex);
                }

                if (drawingImage != null)
                    this.Images.Add(drawingImage);
            }

            public void BeginTextBlock() { }
            public void EndTextBlock() { }
            public void RenderText(TextRenderInfo renderInfo) { }

        }
    }
}
