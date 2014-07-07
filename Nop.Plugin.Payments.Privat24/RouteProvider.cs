using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.Privat24
{
    public class RouteProvider : IRouteProvider
    {
        public int Priority
        {
            get { return 1; }
        }

        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Nop.Plugin.Payments.Privat24.Configure", "Plugins/PaymentPrivat24/Configure",
                new RouteDictionary("PaymentPrivat24", "Configure"), new[]
                {
                    "Nop.Plugin.Payments.Privat24.Controllers"
                });
            routes.MapRoute("Nop.Plugin.Payments.Privat24.PaymentInfo", "Plugins/PaymentPrivat24/PaymentInfo",
                new RouteDictionary("PaymentPrivat24", "PaymentInfo"), new[]
                {
                    "Nop.Plugin.Payments.Privat24.Controllers"
                });
            routes.MapRoute("Nop.Plugin.Payments.Privat24.SubmitButton", "Plugins/PaymentPrivat24/SubmitButton",
                new RouteDictionary("PaymentPrivat24", "SubmitButton"), new[]
                {
                    "Nop.Plugin.Payments.Privat24.Controllers"
                });
            routes.MapRoute("Nop.Plugin.Payments.Privat24.IPNHandler", "Plugins/PaymentPrivat24/IPNHandler",
                new RouteDictionary("PaymentPrivat24", "IPNHandler"), new[]
                {
                    "Nop.Plugin.Payments.Privat24.Controllers"
                });
            routes.MapRoute("Nop.Plugin.Payments.Privat24.Return", "Plugins/PaymentPrivat24/Return",
                new RouteDictionary("PaymentPrivat24", "Return"), new[]
                {
                    "Nop.Plugin.Payments.Privat24.Controllers"
                });
        }

        class RouteDictionary : RouteValueDictionary
        {
            public RouteDictionary(string controller, string action)
            {
                Add("controller", controller);
                Add("action", action);
            }
        }
    }
}