using Blogifier.Core.Services.FileSystem;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Blogifier.Tests.Services.FileSystem
{
    public class BlogStorageTests
    {
        static Uri imgUri = new Uri("http://dnbe.net/v01/images/Picasa.png");
        static BlogStorage storage;
        static string separator = System.IO.Path.DirectorySeparatorChar.ToString();

        public BlogStorageTests()
        {
            storage = new BlogStorage("test");
        }

        [Fact]
        public void VerifyStorageLocation()
        {
            var path = string.Format("WebApp{0}wwwroot{0}blogifier{0}data{0}test", separator);
            Assert.True(storage.Location.EndsWith(path));
        }

        [Fact]
        public void CanCreateDeleteFolder()
        {
            // foo
            storage.CreateFolder("foo");
            var result = storage.GetAssets("");
            Assert.True(result.Contains(storage.Location + string.Format("{0}foo", separator)));

            storage.DeleteFolder("foo");
            result = storage.GetAssets("");
            Assert.False(result.Contains(storage.Location + string.Format("{0}foo", separator)));

            // foo/bar
            storage.CreateFolder("foo/bar");
            result = storage.GetAssets("foo");
            Assert.True(result.Contains(storage.Location + string.Format("{0}foo{0}bar", separator)));

            storage.DeleteFolder("foo");
            result = storage.GetAssets("foo");
            Assert.False(result.Contains(storage.Location + string.Format("{0}foo{0}bar", separator)));

            // foo\\bar
            storage.CreateFolder("foo\\bar");
            result = storage.GetAssets("foo");
            Assert.True(result.Contains(storage.Location + string.Format("{0}foo{0}bar", separator)));

            storage.DeleteFolder("foo");
            result = storage.GetAssets("foo");
            Assert.False(result.Contains(storage.Location + string.Format("{0}foo{0}bar", separator)));
        }

        [Fact]
        public async Task CanCreateDeleteFiles()
        {
            var result = await storage.UploadFromWeb(imgUri, "/");
            Assert.True(result.Url.EndsWith("Picasa.png", StringComparison.OrdinalIgnoreCase));
            storage.DeleteFile("Picasa.png");

            storage.CreateFolder("foo");
            result = await storage.UploadFromWeb(imgUri, "/", "foo");
            Assert.True(result.Url.EndsWith("foo/Picasa.png", StringComparison.OrdinalIgnoreCase));

            storage.DeleteFolder("foo");
            var assets = storage.GetAssets("");
            Assert.False(assets.Contains(storage.Location + separator + "foo"));
        }

        [Fact]
        public async Task CanGetAssets()
        {
            storage.CreateFolder("foo");
            var img = await storage.UploadFromWeb(imgUri, "/", "foo");

            var result = storage.GetAssets("");
            Assert.True(result.Count > 0);
            Assert.True(result[0].EndsWith(string.Format("{0}foo", separator)));

            result = storage.GetAssets("foo");
            Assert.True(result.Count > 0);
            Assert.True(result[0].EndsWith(string.Format("{0}foo{0}Picasa.png", separator), StringComparison.OrdinalIgnoreCase));

            storage.DeleteFolder("foo");
        }
    }
}