using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using pdf.Data.Context;
using pdf.Model;
using System;
using System.Collections.Generic;

namespace pdf.Data
{
    public class DataGenerator
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new PdfContext(
                serviceProvider.GetRequiredService<DbContextOptions<PdfContext>>()))
            {
                if (context.Pdfs.Any())
                {
                    return;   // Data was already seeded
                }

                context.Pdfs.AddRange(GetPdfs(true));

                context.SaveChanges();
            }
        }

        public static List<Pdf> GetPdfs(bool IsFixed = true)
        {
            return new List<Pdf>
            {
                new Pdf
                {
                    Id = IsFixed ? Guid.Parse("7e7ceb51-37e0-4ebb-897c-ce801b0e37fe") : Guid.NewGuid(),
                    Title = "TestPDF1",
                    UploadDate = DateTime.UtcNow,
                    Size = 0
                },
                new Pdf
                {
                    Id = IsFixed ? Guid.Parse("e3398817-b71e-479f-bc9f-31f857cf91f0") : Guid.NewGuid(),
                    Title = "TestPDF2",
                    UploadDate = DateTime.UtcNow,
                    Size = 0
                }
            };
        }
    }
}
