using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using Fleck;

namespace TryFleck
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            FleckServer.Instance().Start();
        }

        protected void Application_End()
        {
            FleckServer.Instance().Dispose();
        }
    }
}
