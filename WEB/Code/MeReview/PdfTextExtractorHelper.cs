using System;
using System.IO;
using System.Text;
using iTextSharp.text.pdf;

namespace Monic.Web.Code.MeReview
{
    public static class PdfTextExtractorHelper
    {
        public static string ExtractText(byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length == 0) return string.Empty;

            var sb = new StringBuilder();
            try
            {
                using (var reader = new PdfReader(pdfBytes))
                {
                    for (int page = 1; page <= reader.NumberOfPages; page++)
                    {
                        byte[] pageBytes = reader.GetPageContent(page);
                        if (pageBytes == null || pageBytes.Length == 0) continue;

                        var tokeniser = new PrTokeniser(pageBytes);
                        while (tokeniser.NextToken())
                        {
                            if (tokeniser.TokenType == PrTokeniser.TK_STRING)
                            {
                                sb.Append(tokeniser.StringValue).Append(" ");
                            }
                        }
                        sb.AppendLine();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse PDF document streams: {ex.Message}", ex);
            }

            return sb.ToString().Trim();
        }

        public static string ExtractText(Stream stream)
        {
            if (stream == null) return string.Empty;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ExtractText(ms.ToArray());
            }
        }
    }
}
