using System;
using System.IO;
using Monic.Web.Code;
using Xunit;

namespace Monic.Web.Tests
{
    public class SystemShowcasePdfBuilderTests
    {
        [Fact]
        public void BuildShowcasePdf_GeneratesComprehensivePdf_AndSavesArtifacts()
        {
            var pdfBytes = SystemShowcasePdfBuilder.BuildShowcasePdf();

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 2000, "Showcase PDF should be substantial in size");

            // Verify %PDF header (0x25, 0x50, 0x44, 0x46)
            Assert.Equal(0x25, pdfBytes[0]);
            Assert.Equal(0x50, pdfBytes[1]);
            Assert.Equal(0x44, pdfBytes[2]);
            Assert.Equal(0x46, pdfBytes[3]);

            // Save to project root for user / client access
            var projectRoot = Directory.GetCurrentDirectory();
            // In test runner Directory.GetCurrentDirectory() is bin\Debug\net10.0 or project root
            var targetWorkspacePath = Path.Combine("..", "..", "..", "..", "MONIC_System_Showcase_Achievements_And_Gaps.pdf");
            try
            {
                File.WriteAllBytes("MONIC_System_Showcase_Achievements_And_Gaps.pdf", pdfBytes);
                if (Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(targetWorkspacePath))))
                {
                    File.WriteAllBytes(targetWorkspacePath, pdfBytes);
                }
            }
            catch
            {
                // Fallback writing handled directly
            }

            // Save to Artifacts directory
            var artifactDir = @"C:\Users\Administrator\.gemini\antigravity-cli\brain\25a5350f-b2ec-48fd-a2cb-741ed187194f";
            if (Directory.Exists(artifactDir))
            {
                File.WriteAllBytes(Path.Combine(artifactDir, "MONIC_System_Showcase_Achievements_And_Gaps.pdf"), pdfBytes);
            }

            var workspacePath = @"C:\Users\Administrator\Downloads\MONICv2\MONICv.001\MONIC_System_Showcase_Achievements_And_Gaps.pdf";
            File.WriteAllBytes(workspacePath, pdfBytes);
            Assert.True(File.Exists(workspacePath));
        }
    }
}
