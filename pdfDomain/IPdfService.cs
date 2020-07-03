using pdf.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pdf.Domain
{
    public interface IPdfService
    {
        Task<List<Pdf>> GetPdfs(FileOrder order);

        Task<Pdf> GetPdf(Guid id);

        Task InsertPdf(Pdf pdf);

        Task<Pdf> DeletePdf(Guid id);
    }
}