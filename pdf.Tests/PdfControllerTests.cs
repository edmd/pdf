using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using pdf.Controllers;
using pdf.Data;
using pdf.Data.Context;
using pdf.Domain;
using pdf.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;

namespace pdf.Tests
{
    [TestFixture]
    public class PdfControllerTests
    {
        private Mock<IPdfService> mockService;
        private Mock<ILogger<PdfController>> mockLogger;
        private PdfController controller;
        private Guid trueGuid, falseGuid;
        private Pdf validPdf;
        private Pdf invalidPdf;
        private List<Pdf> pdfs;
        private DbContextOptions<PdfContext> options;

        // For integration tests
        private HttpClient _client { get; set; }
        private ApiWebApplicationFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new ApiWebApplicationFactory();
            _client = _factory.CreateClient();

            pdfs = DataGenerator.GetPdfs();
            options = new DbContextOptionsBuilder<PdfContext>()
                .UseInMemoryDatabase(databaseName: "PdfDatabase")
                .EnableSensitiveDataLogging(true)
                .Options;

            mockService = new Mock<IPdfService>();
            mockLogger = new Mock<ILogger<PdfController>>();

            var list = new List<Pdf>();

            trueGuid = new Guid("7e7ceb51-37e0-4ebb-897c-ce801b0e37fe");
            falseGuid = new Guid("12345678901234567890123456789011");
            validPdf = new Pdf() { Id = trueGuid };
            invalidPdf = new Pdf() { Id = falseGuid };

            mockService.Setup(repo => repo.GetPdfs(FileOrder.NameAscending)).ReturnsAsync(list);

            mockService.Setup(repo => repo.GetPdf(trueGuid)).ReturnsAsync(validPdf);
            mockService.Setup(repo => repo.GetPdf(falseGuid)).ReturnsAsync((Pdf)null);


            mockService.Setup(repo => repo.InsertPdf(validPdf)).Returns(Task.FromResult(1));
            mockService.Setup(repo => repo.InsertPdf(invalidPdf)).Returns(Task.FromResult(0));


            mockService.Setup(repo => repo.DeletePdf(trueGuid)).Throws(new Exception($"Pdf '{trueGuid}' is active, can't be deleted"));
            mockService.Setup(repo => repo.DeletePdf(falseGuid)).Returns(Task.FromResult(invalidPdf));


            controller = new PdfController(mockLogger.Object, mockService.Object);
        }

        [Test, Category("Integration")]
        public void UploadValidPdfTest()
        {
            var fileName = "pdf.pdf";
            var io = File.Open(fileName, FileMode.Open);
            byte[] Bytes = new byte[io.Length + 1];
            io.Read(Bytes, 0, Bytes.Length);

            using (var content = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(Bytes);
                fileContent.Headers.ContentDisposition 
                    = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { FileName = fileName };
                content.Add(fileContent);

                var response = _client.PostAsync("/api/pdf", content).Result;
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                var pdf = JsonConvert.DeserializeObject<Pdf>(response.Content.ReadAsStringAsync().Result);
                pdf.Should().NotBeNull();
            }
        }

        [Test, Category("Integration")]
        public void UploadInvalidPdfTest()
        {
            var fileName = "largepdf.pdf";
            var io = File.Open(fileName, FileMode.Open);
            byte[] Bytes = new byte[io.Length + 1];
            io.Read(Bytes, 0, Bytes.Length);

            using (var content = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(Bytes);
                fileContent.Headers.ContentDisposition
                    = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { FileName = fileName };
                content.Add(fileContent);

                var response = _client.PostAsync("/api/pdf", content).Result;
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
        }

        [Test, Category("Integration")]
        public void ReturnAllPdfsIntTest()
        {
            var response = _client.GetAsync("/api/pdf?order=NameDescending").Result;

            Assert.IsInstanceOf<ActionResult<IEnumerable<Pdf>>>(response);
            Assert.IsNotNull(response);
        }

        [Test, Category("Integration")]
        public void ReturnIndividualPdfIntTest()
        {
            var response = _client.GetAsync($"/api/pdf/{trueGuid}").Result;

            Assert.IsInstanceOf<ActionResult<Pdf>>(response);

            //Assert.AreEqual(response.v, trueGuid, "Guids are not equal");
        }

        [Test, Category("Integration")]
        public void ReturnMissingPdfIntTest()
        {
            var response = _client.GetAsync($"/api/pdf/{falseGuid}").Result;

            Assert.IsInstanceOf<ActionResult<Pdf>>(response);

            response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Test, Category("Integration")]
        public void DeleteValidPdfIntTest()
        {
            var response = _client.DeleteAsync($"/api/pdf/{trueGuid}").Result;
        }

        [Test, Category("Integration")]
        public void DeleteInvalidPdfIntTest()
        {
            var response = _client.DeleteAsync($"/api/pdf/{falseGuid}").Result;
        }

        [Test, Category("Unit")]
        public void ReturnAllPdfsTest()
        {
            var response = controller.GetPdfs(FileOrder.NameAscending).Result;

            Assert.IsInstanceOf<ActionResult<IEnumerable<Pdf>>>(response);

            Assert.IsNotNull(response.Value);
        }

        [Test, Category("Unit")]
        public void ReturnIndividualPdfTest()
        {
            var response = controller.GetPdf(trueGuid).Result;

            Assert.IsInstanceOf<ActionResult<Pdf>>(response);

            Assert.AreEqual(response.Value.Id, trueGuid, "Guids are not equal");
        }

        [Test, Category("Unit")]
        public void ReturnMissingPdfTest()
        {
            var response = controller.GetPdf(falseGuid).Result;

            Assert.IsInstanceOf<ActionResult<Pdf>>(response);

            (response.Result as StatusCodeResult).StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Test, Category("Unit")]
        public void DeleteValidPdfTest()
        {
            var ex = Assert.ThrowsAsync<Exception>(() => controller.DeletePdf(trueGuid));

            Assert.AreEqual(ex.Message, $"Pdf '{trueGuid}' is active, can't be deleted",
                "Exception message was not in the expected format");
        }

        [Test, Category("Unit")]
        public void DeleteInvalidPdfTest()
        {
            var expectedPdf = controller.DeletePdf(falseGuid).Result.Value;

            Assert.AreEqual(invalidPdf.Id, expectedPdf.Id,
                "Expected Pdf object was not returned for unsuccessful deletion.");
        }
    }
}
