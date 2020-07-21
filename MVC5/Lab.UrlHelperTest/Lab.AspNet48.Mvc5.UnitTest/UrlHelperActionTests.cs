using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Lab.AspNet48.Mvc5.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Lab.AspNet48.Mvc5.UnitTest
{
    [TestClass]
    public class UrlHelperActionTests
    {
        private HtmlHelper<T> CreateHtmlHelper<T>(ViewDataDictionary viewData)
        {
            new FakeHttpContext.FakeHttpContext();

            var httpContext     = new HttpContextWrapper(HttpContext.Current);
            var routeCollection = new RouteCollection();
            RouteConfig.RegisterRoutes(routeCollection);
            var routeData = routeCollection.GetRouteData(httpContext);

        

            var controllerContext = new ControllerContext(httpContext,
                                                          routeData,
                                                          Substitute.For<ControllerBase>());

            var viewContext = Substitute.For<ViewContext>(
                                                          controllerContext,
                                                          Substitute.For<IView>(),
                                                          viewData,
                                                          new TempDataDictionary(),
                                                          TextWriter.Null);
            var viewDataContainer =  Substitute.For<IViewDataContainer>();
            viewDataContainer.ViewData.Returns(viewData);
            return new HtmlHelper<T>(viewContext, viewDataContainer);
        }

        [TestMethod]
        public void UrlHelper_Action()
        {
            new FakeHttpContext.FakeHttpContext();

            var httpContext = new HttpContextWrapper(HttpContext.Current);

            var routeCollection = new RouteCollection();
            RouteConfig.RegisterRoutes(routeCollection);
            var routeData = routeCollection.GetRouteData(httpContext);

            var requestContext = new RequestContext(httpContext, routeData);
            var urlHelper      = new UrlHelper(requestContext, routeCollection);
            var result         = urlHelper.Action("About", "Default");
            Assert.AreEqual("/Default/About", result);
        }

        [TestMethod]
        public void UrlHelper_RouteUrl()
        {
            new FakeHttpContext.FakeHttpContext();

            var httpContext = new HttpContextWrapper(HttpContext.Current);

            var routeCollection = new RouteCollection();
            RouteConfig.RegisterRoutes(routeCollection);
            var routeData = routeCollection.GetRouteData(httpContext);

            var requestContext = new RequestContext(httpContext, routeData);
            var urlHelper      = new UrlHelper(requestContext, routeCollection);
            var result = urlHelper.RouteUrl(new RouteValueDictionary
            {
                {"controller", "Default"},
                {"action", "About"},
            });
            Assert.AreEqual("/Default/About", result);
        }

        [TestMethod]
        public void 注入ControllerContext()
        {
            var controller = new HomeController();
            new FakeHttpContext.FakeHttpContext();

            var httpContext     = new HttpContextWrapper(HttpContext.Current);
            var routeCollection = new RouteCollection();
            RouteConfig.RegisterRoutes(routeCollection);
            var routeData = routeCollection.GetRouteData(httpContext);

            //var controllerContext = Substitute.For<ControllerContext>(httpContext,
            //                                                          routeData,
            //                                                          Substitute.For<ControllerBase>());

            var controllerContext = new ControllerContext(httpContext, routeData, controller);


            //var requestContext    = new RequestContext(httpContext, routeData);
            //controllerContext.RequestContext = requestContext;
            //controller.ControllerContext     = controllerContext;
            controller.Url = new UrlHelper(controllerContext.RequestContext, routeCollection);
            var result = (RedirectResult) controller.Index();
            Assert.AreEqual("/Default/About", result.Url);
        }
    }
}