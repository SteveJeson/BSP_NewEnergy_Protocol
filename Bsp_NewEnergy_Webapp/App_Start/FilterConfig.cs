using System.Web;
using System.Web.Mvc;

namespace Bsp_NewEnergy_Webapp
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
