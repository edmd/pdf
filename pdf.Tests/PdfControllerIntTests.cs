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
using System.Linq;

namespace pdf.Tests
{
    [TestFixture]
    public class PdfControllerIntTests
    {
        
        private Guid trueGuid, falseGuid;
        private List<Pdf> pdfs;
        private Pdf pdf;
        private DbContextOptions<PdfContext> options;

        // For integration tests
        private HttpClient _client { get; set; }
        private ApiWebApplicationFactory _factory;
        private PdfService pdfService;
        private PdfContext pdfContext;

        [SetUp]
        public void Setup()
        {
            _factory = new ApiWebApplicationFactory();
            _client = _factory.CreateClient();

            var getResponse = _client.GetAsync("/api/pdf?order=NameDescending").Result;
            var stringResponse = getResponse.Content.ReadAsStringAsync().Result;
            pdfs = JsonConvert.DeserializeObject<List<Pdf>>(stringResponse);

            if(!pdfs.Any()) {
                var fileName = "pdf.pdf";
                var io = File.Open(fileName, FileMode.Open);
                byte[] Bytes = new byte[io.Length + 1];
                io.Read(Bytes, 0, Bytes.Length);

                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(Bytes);

                    fileContent.Headers.ContentDisposition
                        = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { FileName = "1" + fileName };
                    content.Add(fileContent);
                    var response1 = _client.PostAsync("/api/pdf", content).Result;

                    fileContent.Headers.ContentDisposition
                        = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { FileName = "2" + fileName };
                    var response2 = _client.PostAsync("/api/pdf", content).Result;

                    var allResponse = _client.GetAsync("/api/pdf?order=NameDescending").Result;
                    var allStringResponse = allResponse.Content.ReadAsStringAsync().Result;
                    pdfs = JsonConvert.DeserializeObject<List<Pdf>>(allStringResponse);
                    trueGuid = pdfs.First().Id;
                }
            }
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

            var stringResponse = response.Content.ReadAsStringAsync().Result;
            pdf = JsonConvert.DeserializeObject<Pdf>(stringResponse);
            Assert.AreEqual(pdf.Id, trueGuid, $"Pdf with Id {trueGuid} is not present");
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

            var stringResponse = response.Content.ReadAsStringAsync().Result;
            pdf = JsonConvert.DeserializeObject<Pdf>(stringResponse);
            Assert.AreEqual(pdf.Id, trueGuid, $"Pdf with Id {trueGuid} is not present");

            response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Test, Category("Integration")]
        public void DeleteInvalidPdfIntTest()
        {
            var response = _client.DeleteAsync($"/api/pdf/{falseGuid}").Result;

            response.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }
    }
}
