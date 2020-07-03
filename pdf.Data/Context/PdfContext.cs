using Microsoft.EntityFrameworkCore;
using pdf.Model;

namespace pdf.Data.Context
{
    public class PdfContext : DbContext
    {
        public PdfContext(DbContextOptions<PdfContext> options)
            : base(options)
        {
        }

        public DbSet<Pdf> Pdfs { get; set; }
    }
}