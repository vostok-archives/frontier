using System.IO;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Vostok.Logging.Logs;

namespace Vostok.FrontReport.IntegrationTests
{
    public class HandlerTests
    {
        private readonly ConsoleLog log;

        public HandlerTests()
        {
            log = new ConsoleLog();
        }

        [Test]
        public void TestCsp()
        {
            var message = File.ReadAllText($"messages\\csp.txt");
            var httpClient = new HttpClient();

            var responseMessage = httpClient.PostAsync("http://localhost:6307/_csp", new StringContent(message)).Result;
            Assert.AreEqual(HttpStatusCode.NoContent, responseMessage.StatusCode);
        }
    }
}