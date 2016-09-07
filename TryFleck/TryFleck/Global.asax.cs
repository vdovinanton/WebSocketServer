using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using Fleck;
using TryFleck.Hubs;

namespace TryFleck
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var orderHub = new OrderHub();
            FleckServer.Instance().Start();
        }

        protected void Application_End()
        {
            FleckServer.Instance().Dispose();
        }
    }
}
