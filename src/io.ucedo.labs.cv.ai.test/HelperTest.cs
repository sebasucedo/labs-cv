using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai.test
{
    [TestFixture]
    public class HelperTest
    {

        [Test]
        [TestCase("key=value")]
        [TestCase("as=a pirate")]
        [TestCase("as=Maradona&language=spanish")]
        public void StringHelper_Parse_Single_QueryString_Test(string queryString)
        {
            var result = StringHelper.ParseQueryString(queryString);

            Assert.IsTrue(result.Count > 0);
        }

        [Test]
        public void StringHelper_Parse_Single_Value_QueryString_Test()
        {
            var queryString = "key=value";
            var result = StringHelper.ParseQueryString(queryString);

            Assert.IsTrue(result.Count == 1);
            Assert.IsTrue(result["key"] == "value");
        }

        [Test]
        public void StringHelper_Parse_Empty_QueryString_Test()
        {
            var queryString = "";
            var result = StringHelper.ParseQueryString(queryString);

            Assert.IsTrue(result.Count == 0);
        }

        [Test]
        public void StringHelper_Parse_No_key_value_QueryString_Test()
        {
            var queryString = "get a tomato";
            var result = StringHelper.ParseQueryString(queryString);

            Assert.IsTrue(result.Count == 0);
        }


        [Test]
        public void StringHelper_Parse_Url_inside_QueryString_Test()
        {
            var queryString = "datasource='https://farfaraway.com?key1=value1&key2=value2'";
            var result = StringHelper.ParseQueryString(queryString);

            Assert.IsTrue(result.Count == 1);
        }
    }
}
