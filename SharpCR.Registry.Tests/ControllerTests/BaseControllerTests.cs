using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using SharpCR.Registry.Controllers;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class BaseControllerTests
    {
        [Fact]
        public void GetBase()
        {
            var controller = new BaseController();
            
            var baseResult = controller.Base();
            
            Assert.NotNull(baseResult);
            Assert.IsType<OkResult>(baseResult);
        }
        
        [Fact(Skip = "Not yet implemented")]
        public void GetCatalog()
        {
            var controller = new BaseController();
            
            var catalogResult = controller.Catalog();
            
            Assert.NotNull(catalogResult);
        }
    }
}