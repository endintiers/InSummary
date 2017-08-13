using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InSummaryFunctions.Helpers
{
    public class PDFPage
    {
        public int Number { get; set; }
        public string Text {
            get
            {
                if (!string.IsNullOrWhiteSpace(OCRText))
                    return PageText + Environment.NewLine + "OCRText: " + OCRText;
                else
                    return PageText;
            }
        }
        public string PageText { get; set; }
        public List<Image> ExtractedImages { get; set; }
        public string OCRText { get; set; }
        public string KeyPhrases { get; set; }
    }
}
