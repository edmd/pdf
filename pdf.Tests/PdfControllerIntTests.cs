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
    public class PdfControllerIntTests
    {
        
        private Guid trueGuid, falseGuid;
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
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Needs to be torn down fully for each test
                .EnableSensitiveDataLogging(true)
                .Options;

            trueGuid = new Guid("7e7ceb51-37e0-4ebb-897c-ce801b0e37fe");
            falseGuid = new Guid("12345678901234567890123456789011");
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
        public void UploadLargePdfTest()
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
        public void UploadNotPdfTest()
        {
            var fileName = "pdf.not";
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
                var stringContent = response.Content.ReadAsStringAsync().Result;

                Assert.AreEqual(stringContent, "{\"File\":[\"The file type isn't permitted or the file's signature doesn't match the file's extension.\"]}", "Unexpected message returned.");
            }
        }

        [Test, Category("Integration")]
        public void UploadEmptyPdfTest()
        {
            var fileName = "empty.pdf";
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
                var stringContent = response.Content.ReadAsStringAsync().Result;

                Assert.AreEqual(stringContent, "{\"File\":[\"The file is empty.\"]}", "Unexpected message returned.");
            }
        }

        [Test, Category("Integration")]
        public void ReturnAllPdfsIntTest()
        {
            var response = _client.GetAsync("/api/pdf?order=NameDescending").Result;

            Assert.IsNotNull(response);
            response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Test, Category("Integration")]
        public void ReturnIndividualPdfIntTest()
        {
            var response = _client.GetAsync($"/api/pdf/{trueGuid}").Result;

            Assert.IsNotNull(response);
            response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Test, Category("Integration")]
        public void ReturnMissingPdfIntTest()
        {
            var response = _client.GetAsync($"/api/pdf/{falseGuid}").Result;

            response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Test, Category("Integration")]
        public void DeleteValidPdfIntTest()
        {
            var response = _client.DeleteAsync($"/api/pdf/{trueGuid}").Result;

            response.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }

        [Test, Category("Integration")]
        public void DeleteInvalidPdfIntTest()
        {
            var response = _client.DeleteAsync($"/api/pdf/{falseGuid}").Result;

            response.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }
    }
}
