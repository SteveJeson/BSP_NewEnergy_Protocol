using Nancy;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSP_NewEnergy_Protocol.Module
{
    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get["/"] = x => "Hello";
            Get["/login"] = x => { return "person name :" + Request.Query.name + " age : " + Request.Query.age; };
            Post["/test"] = parameters =>
            {
                var q = Request.Query;
                return q;
            };
        }
    }
}
