using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.SpeedTest.UnitTests
{
    [TestClass]
    public class MainTests
    {
        private Main main;

        [TestInitialize]
        public void TestInitialize()
        {
            main = new Main();
        }

        [TestMethod]
        public void Query_should_return_results()
        {
            var results = main.Query(new("search"));

            Assert.IsNotNull(results.First());
        }

        [TestMethod]
        public void LoadContextMenus_should_return_results()
        {
            // Create a result with valid context data
            // In the Main class, LoadContextMenus checks if ContextData is a non-empty string
            string validUrl = "https://www.speedtest.net/result/12345678";
            var result = new Result { ContextData = validUrl };
            
            var contextMenus = main.LoadContextMenus(result);

            Assert.IsNotNull(contextMenus);
            Assert.IsTrue(contextMenus.Count > 0);
            Assert.IsNotNull(contextMenus.First());
        }
    }
}