using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using pdf.Data;
using pdf.Data.Context;
using pdf.Domain;
using pdf.Model;
using System;
using System.Collections.Generic;

namespace pdf.Tests
{
    [TestFixture]
    public class PdfServiceTests
    {
        private PdfContext pdfContext;
        private PdfService pdfService;
        private List<Pdf> pdfs;

        [SetUp]
        public void Setup()
        {
            pdfs = DataGenerator.GetPdfs();
            var options = new DbContextOptionsBuilder<PdfContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging(true)
                .Options;

            pdfContext = new PdfContext(options);
            pdfService = new PdfService(pdfContext);

            var pdf = pdfService.InsertPdf(pdfs[0]);
            pdf = pdfService.InsertPdf(pdfs[1]);
        }

        [Test, Category("Unit")]
        public void AddPdfSuccessTest()
        {
            pdfs[0].Id = Guid.NewGuid();
            var pdf = pdfService.InsertPdf(pdfs[0]);

            Assert.IsTrue(pdf.IsCompleted);
            Assert.IsFalse(pdf.IsFaulted);
        }

        [Test, Category("Unit")]
        public void GetPdfSuccessTest()
        {
            var responsePdf = pdfService.GetPdf(pdfs[0].Id);

            Assert.IsNotNull(responsePdf, "Pdf should not be returned empty");
        }

        [Test, Category("Unit")]
        public void GetPdfFailureTest()
        {
            var pdf = pdfService.GetPdf(Guid.NewGuid());

            Assert.IsNull(pdf.Result);
        }

        [Test, Category("Unit")]
        public void GetAllPdfsSuccessTest()
        {
            var result = pdfService.GetPdfs();

            Assert.IsTrue(result.Result.Count >= 2, "There should be 2 or more pdfs returned");
        }

        [Test, Category("Unit")]
        public void DeletePdfsSuccessTest()
        {
            var pdf = pdfService.DeletePdf(pdfs[0].Id).Result;

            Assert.IsTrue(pdf.Id == pdfs[0].Id, $"The Pdf with Id {pdfs[0].Id} has not been deleted.");
        }

        [Test, Category("Unit")]
        public void DeletePdfsFailureTest()
        {
            var responsePdf = pdfService.DeletePdf(Guid.NewGuid());

            Assert.IsNull(responsePdf.Result);
        }
    }
}