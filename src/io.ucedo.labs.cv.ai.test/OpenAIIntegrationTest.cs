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
    [Ignore("on demand")]
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
            var openAIKey = Environment.GetEnvironmentVariable("openai_api_key") ?? string.Empty;

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


        [Test]
        public async Task Image_Edits_From_Url_Test()
        {
            const string PROFILE_PICTURE_URL = "https://www.dropbox.com/scl/fi/segv08x17od7zrfgi4v4l/soledad_profile_picture.png?rlkey=nw60ocolxno65c7r8rb1046zm&dl=1";
            const string PROFILE_PICTURE_MASK_URL = "https://www.dropbox.com/scl/fi/lscebu9bbkljqpeh16ysb/soledad_profile_picture_mask.png?rlkey=09zseeodz9oqxkw5pvlt2jlxj&dl=1";

            using HttpClient httpClient = new();
            byte[] profilePictureResponse = await httpClient.GetByteArrayAsync(PROFILE_PICTURE_URL);
            byte[] profilePictureMaskResponse = await httpClient.GetByteArrayAsync(PROFILE_PICTURE_MASK_URL);


            const string systemRoleContent = "a disney princess";

            if (_httpClientFactory == null)
                throw new NullReferenceException(nameof(_httpClientFactory));

            var openAI = new OpenAI(_httpClientFactory, systemRoleContent);

            var prompt = $"In the following image of me, I need you to depict me as {systemRoleContent}";

            var response = await openAI.SendImagesEditsRequest(prompt, profilePictureResponse, profilePictureMaskResponse);

            Assert.IsNotNull(response);

            var url = response?.data.First().url;
            var isValidUrl = Uri.TryCreate(url, UriKind.Absolute, out var responseUri);
            Assert.IsTrue(isValidUrl);
            Assert.IsNotNull(responseUri);
        }

    }
}
