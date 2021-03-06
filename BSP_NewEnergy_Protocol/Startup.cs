﻿using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace BSP_NewEnergy_Protocol
{
    class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();//有此配置，则可以按路由名称匹配controller方法
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional}
            );
            appBuilder.UseWebApi(config);
        }
    }
}
