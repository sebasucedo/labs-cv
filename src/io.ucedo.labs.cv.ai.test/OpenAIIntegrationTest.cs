using io.ucedo.labs.cv.ai.openai;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai.test
{
    [TestFixture]
    public class OpenAIIntegrationTest
    {
        string _openAIKey;

        [SetUp]
        public void SetUp()
        {
            _openAIKey = "{KEY}";
        }

        [Test]
        public async Task About_Chat_Completion_As_Pirate_Test()
        {
            const string systemRoleContent = "a pirate";

            string about = "software manager | software architect | 15 years of experience | led development teams | skills in project management, team leadership, and process improvement";
            string drawnUpAs = $", drawn up as if it were {systemRoleContent} but without saying that it is {systemRoleContent}";

            var openAI = new OpenAI(_openAIKey, systemRoleContent);

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

            var openAI = new OpenAI(_openAIKey, systemRoleContent);

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
