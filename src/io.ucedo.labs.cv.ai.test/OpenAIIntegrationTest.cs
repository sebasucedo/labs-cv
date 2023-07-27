using io.ucedo.labs.cv.ai.domain;
using io.ucedo.labs.cv.ai.openai;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai.test
{
    [TestFixture]
    public class OpenAIIntegrationTest
    {
        IHttpClientFactory? _httpClientFactory;

        private static IHttpClientFactory GetHttpClientFactory(string openAiKey)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(Constants.OPENAI_CLIENT_NAME, client =>
            {
                client.BaseAddress = new Uri("https://api.openai.com/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            if (httpClientFactory == null)
                throw new NullReferenceException(nameof(httpClientFactory));

            return httpClientFactory;
        }


        [SetUp]
        public void SetUp()
        {
            var openAIKey = "{KEY}";

            _httpClientFactory = GetHttpClientFactory(openAIKey);
        }

        [Test]
        public async Task Experience_Software_Development_Manager_Test()
        {
            const string systemRoleContent = "software development manager";

            if (_httpClientFactory == null)
                throw new NullReferenceException(nameof(_httpClientFactory));

            var openAI = new OpenAI(_httpClientFactory, systemRoleContent);

            var prompt = $"Now i need a brief resume, really short, for my position in Ministerio de Educación de la Provincia de Tucumán as Senior .net Developer at this time period Jul 2007 - Feb 2012 based on: ASP.NET Webforms - ASP.NET mvc - SqlServer - Entity Framework - jQuery, drawn up as if it were software development manager but without saying that it is software development manager";

            Stopwatch stopwatch = new();
            stopwatch.Start();

            var response = await openAI.SendChatCompletionRequest(prompt);

            stopwatch.Stop();
            long elapsed = stopwatch.ElapsedMilliseconds;

            Assert.IsNotEmpty(response);
            Assert.IsTrue(elapsed < 60000);
        }

        [Test]
        public async Task About_Chat_Completion_As_Pirate_Test()
        {
            const string systemRoleContent = "a pirate";

            string about = "software manager | software architect | 15 years of experience | led development teams | skills in project management, team leadership, and process improvement";
            string drawnUpAs = $", drawn up as if it were {systemRoleContent} but without saying that it is {systemRoleContent}";

            if (_httpClientFactory == null) 
                throw new NullReferenceException(nameof(_httpClientFactory));

            var openAI = new OpenAI(_httpClientFactory, systemRoleContent);

            var prompt = $"I need an about for my CV based on: {about}{drawnUpAs}";

            Stopwatch stopwatch = new();
            stopwatch.Start();

            var response = await openAI.SendChatCompletionRequest(prompt);

            stopwatch.Stop();
            long elapsed = stopwatch.ElapsedMilliseconds;

            Assert.IsNotEmpty(response);
            Assert.IsTrue(elapsed < 60000);
        }

        [Test]
        public async Task Image_Edits_As_A_Pirate_Test()
        {
            const string systemRoleContent = "a pirate";

            if (_httpClientFactory == null)
                throw new NullReferenceException(nameof(_httpClientFactory));

            var openAI = new OpenAI(_httpClientFactory, systemRoleContent);

            var prompt = $"In the following image of me, I need you to depict me as {systemRoleContent}";

            var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "profile_web.png");
            var maskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "profile_web_mask.png");

            var response = await openAI.SendImagesEditsRequest(prompt, imagePath, maskPath);

            Assert.IsNotNull(response);

            var url = response?.data.First().url;
            var isValidUrl = Uri.TryCreate(url, UriKind.Absolute, out var responseUri);
            Assert.IsTrue(isValidUrl);
            Assert.IsNotNull(responseUri);
        }
    }
}
