using Microsoft.EntityFrameworkCore;
using pdf.Data.Context;
using pdf.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace pdf.Domain
{
    public class PdfService : IPdfService
    {
        private readonly PdfContext _context;

        public PdfService(PdfContext context)
        {
            _context = context;
        }

        public async Task<Pdf> DeletePdf(Guid id)
        {
            var pdf = _context.Pdfs.Find(id);
            if (pdf != null)
            {
                _context.Pdfs.Remove(pdf);
            }

            return await Task.FromResult(pdf);
        }

        public async Task<Pdf> GetPdf(Guid id)
        {
            var pdf = await _context.Pdfs.FirstOrDefaultAsync(i => i.Id == id);

            return pdf;
        }

        public Task<List<Pdf>> GetPdfs(FileOrder order = FileOrder.NameAscending)
        {
            var pdfs = new List<Pdf>();

            switch(order)
            {
                case FileOrder.NameAscending:
                    pdfs = _context.Pdfs.OrderBy(pdf => pdf.Title).ToList();
                    break;
                case FileOrder.NameDescending:
                    pdfs = _context.Pdfs.OrderByDescending(pdf => pdf.Title).ToList();
                    break;
                case FileOrder.SizeAscending:
                    pdfs = _context.Pdfs.OrderBy(pdf => pdf.Size).ToList();
                    break;
                case FileOrder.SizeDescending:
                    pdfs = _context.Pdfs.OrderByDescending(pdf => pdf.Size).ToList();
                    break;
            }

            return Task.FromResult(pdfs);
        }

        public async Task InsertPdf(Pdf pdf)
        {
            var results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(pdf, new ValidationContext(pdf, null, null),
                results, true);

            if (isValid)
            {

                _context.Pdfs.Add(pdf);
                await _context.SaveChangesAsync();
            }
            else
            {
                await Task.FromException(null); // Testing this out in the tests
            }
        }
    }
}
