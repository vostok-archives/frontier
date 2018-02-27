using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;
using Vstk.Airlock;
using Vstk.Airlock.Logging;
using Vstk.Hosting;
using Vstk.Logging;
using Vstk.Logging.Logs;
using Vstk.Metrics;

namespace Vstk.Frontier.Tests
{
    public class HandlerTests
    {
        private readonly HttpHandler httpHandler;
        private LogEventData logEventData;
        private string routingKey;
        private string project;

        public HandlerTests()
        {
            var log = new ConsoleLog();
            var metricScope = Substitute.For<IMetricScope>();
            var airlockClient = Substitute.For<IAirlockClient>();
            VostokHostingEnvironment.Current = new VostokHostingEnvironment()
            {
                Environment = "dev",
                Log = log,
                AirlockClient = airlockClient,
                MetricScope = metricScope
            };
            airlockClient.When(c => c.Push(Arg.Any<string>(), Arg.Any<LogEventData>(), Arg.Any<DateTimeOffset?>())).Do(
                x =>
                {
                    routingKey = x.Arg<string>();
                    logEventData = x.Arg<LogEventData>();
                    log.Debug(logEventData.ToPrettyJson());
                });
            httpHandler = new HttpHandler(new FrontierSetings { SourceMapBlacklist = new []{ "diadoc.kontur.ru" } }, metricScope, log, airlockClient);
        }

        private void InvokeTest(string type)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/_" + type;
            context.Request.Body = File.OpenRead($"messages\\{type}.txt");
            httpHandler.Invoke(context).Wait();
            RoutingKey.Parse(routingKey, out project, out _, out var service, out _);
            Assert.AreEqual("frontier-"+type, service);
            Assert.AreEqual(LogLevel.Error, logEventData.Level);
        }

        [Test]
        public void TestCsp()
        {
            InvokeTest("csp");
            Assert.IsNull(logEventData.Exceptions); 
            Assert.AreEqual(LogLevel.Error, logEventData.Level); 
            Assert.AreEqual("https://www.buhonline.ru/forum/index?g=posts&t=31646", logEventData.Properties["document-uri"]); 
            Assert.AreEqual("wss://www.buhonline.ru", logEventData.Properties["blocked-uri"]);
            Assert.AreEqual("buhonline", project);
        }

        [Test]
        public void TestPkp()
        {
            InvokeTest("pkp");
            Assert.IsNull(logEventData.Exceptions);
            Assert.AreEqual("443", logEventData.Properties["port"]);
            Assert.AreEqual("-----BEGINCERTIFICATE-----\nMIIAuyg[...]tqU0CkVDNx\n-----ENDCERTIFICATE-----", logEventData.Properties["served-certificate-chain"]);
            Assert.AreEqual("pin-sha256=\"dUezRu9zOECb901Md727xWltNsj0e6qzGk\", pin-sha256=\"E9CqVKB9+xZ9INDbd+2eRQozqbQ2yXLYc\"", logEventData.Properties["known-pins"]);
            Assert.AreEqual("focus", project);
        }

        [Test]
        public void TestStacktracejs()
        {
            InvokeTest("stacktracejs");
            Assert.IsNotNull(logEventData.Exceptions);
            Assert.AreEqual(1, logEventData.Exceptions.Count);
            Assert.AreEqual(10, logEventData.Exceptions[0].Stack.Count);
            Assert.AreEqual("o.constructor._getOrgName", logEventData.Exceptions[0].Stack[0].Function);
            Assert.AreEqual("Uncaught TypeError: Cannot read property 'shortName' of null", logEventData.Exceptions[0].Message);
            Assert.AreEqual(logEventData.Message, logEventData.Exceptions[0].Message);
            Assert.AreEqual("http://diadoc.kontur.ru/87264804-b812-48c1-b461-0edbf3a25e93/Folder/Outbox", logEventData.Properties["url"]);
            Assert.AreEqual("webpack:///diadoc.kontur.ru/scripts/combined/BD104ED26C5CF0529E16FC15380378E0.js", logEventData.Properties["sourceUrl"]);
            Assert.AreEqual("diadoc", project);
        }
    }
}