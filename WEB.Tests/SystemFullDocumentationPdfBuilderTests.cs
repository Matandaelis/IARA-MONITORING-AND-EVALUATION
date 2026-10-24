using System;
using System.IO;
using System.Text;
using Monic.Web.Code;
using Xunit;

namespace Monic.Web.Tests
{
    public class SystemFullDocumentationPdfBuilderTests
    {
        [Fact]
        public void BuildFullDocumentationPdf_GeneratesComprehensivePdfDocument()
        {
            // Act
            var pdfBytes = SystemFullDocumentationPdfBuilder.BuildFullDocumentationPdf();

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 5000, $"Expected full documentation PDF to be substantial, but was {pdfBytes.Length} bytes");

            // Verify PDF header magic bytes "%PDF"
            var header = Encoding.ASCII.GetString(pdfBytes, 0, 4);
            Assert.Equal("%PDF", header);

            // Write to project workspace root
            var projectRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../MONIC_Full_System_Detailed_Documentation.pdf");
            var normalizedProjectRootPath = Path.GetFullPath(projectRootPath);
            File.WriteAllBytes(normalizedProjectRootPath, pdfBytes);

            // Also write to brain artifacts directory if available
            var brainDir = @"C:\Users\Administrator\.gemini\antigravity-cli\brain\25a5350f-b2ec-48fd-a2cb-741ed187194f";
            if (Directory.Exists(brainDir))
            {
                File.WriteAllBytes(Path.Combine(brainDir, "MONIC_Full_System_Detailed_Documentation.pdf"), pdfBytes);
            }
        }
    }
}
