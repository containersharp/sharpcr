using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;

namespace SharpCR.Registry
{
    public class NamedRegexRoutingConstraint : IRouteConstraint
    {
        private readonly Regex _regex;

        public NamedRegexRoutingConstraint(string regex)
        {
            if (null == regex)
            {
                throw new ArgumentNullException(nameof(regex));
            }
            
            _regex = new Regex(regex);
        }

        public bool Match(HttpContext httpContext, IRouter route, string routeKey,
            RouteValueDictionary values, RouteDirection routeDirection)
        {
            var urlRouteValue = values[routeKey];
            if (urlRouteValue == null)
            {
                return false;
            }

            var url = urlRouteValue.ToString();
            var match = _regex.Match(url!);
            if (!match.Success)
            {
                return false;
            }

            foreach (Group group in match.Groups)
            {
                values.Add(string.IsNullOrEmpty(@group.Name) ? group.Index.ToString() : group.Name, @group.Value);
            }
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class NamedRegexRouteAttribute : Attribute
    {
        public NamedRegexRouteAttribute(string regex)
        {
            Regex = regex;
        }
        public NamedRegexRouteAttribute(string regex, params string[] allowedMethods)
        :this(regex)
        {
            AllowedMethods = allowedMethods;
        }
        
        public NamedRegexRouteAttribute(string regex, int orderWithinCtrl, params string[] allowedMethods)
        :this(regex)
        {
            OrderInController = orderWithinCtrl;
            AllowedMethods = allowedMethods;
        }

        public string Regex { get; }
        
        public int OrderInController { get; }
        public string[] AllowedMethods { get; } = {"Get", "Head", "Post", "Put", "Delete", "Patch"};
    }

    public static class RouteExtensions
    {
        public static void MapRegexRoute(this IEndpointRouteBuilder routes, string controller, string action, string regex, params string[] allowedMethods)
        {
            var name = $"{controller}_{action}_{string.Join('_', allowedMethods)}";
            routes.MapControllerRoute(name,
                "{*url}",
                new {controller, action},
                new
                {
                    httpMethod = new HttpMethodRouteConstraint(allowedMethods),
                    url = new NamedRegexRoutingConstraint(regex)
                });
        }

        public static void MapRegexRoutes<T>(this IEndpointRouteBuilder routes) where T: class
        {
            const string controllerSuffix = "Controller";
            
            var controllerType = typeof(T);
            if (!controllerType.Name.EndsWith(controllerSuffix))
            {
                return;
            }

            var controllerName = controllerType.Name.Substring(0, controllerType.Name.Length - controllerSuffix.Length);
            var actionMethods = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(a => Tuple.Create(a, a.GetCustomAttribute<NamedRegexRouteAttribute>()))
                .Where(a =>  a.Item2 != null)
                .OrderBy(a => a.Item2.OrderInController)
                .Select(a => a.Item1)
                .ToList();
            
            actionMethods.ForEach(method =>
            {
                var attr = method.GetCustomAttribute<NamedRegexRouteAttribute>();
                routes.MapRegexRoute(controllerName, method.Name, attr!.Regex, attr.AllowedMethods);
            });
        }
    }
}