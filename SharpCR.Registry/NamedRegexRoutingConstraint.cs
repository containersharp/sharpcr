using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SharpCR.Registry
{
    public class RegexNamedGroupRoutingConstraint : IRouteConstraint
    {
        private readonly List<Regex> _regexes = new List<Regex>();

        public RegexNamedGroupRoutingConstraint(params string[] regexes)
        {
            _regexes.AddRange(regexes.Select(regex => new Regex(regex)));
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
            var firstMatch = _regexes.Select(regex => regex.Match(url!)).FirstOrDefault(match => match.Success);
            if (firstMatch == null)
            {
                return false;
            }

            foreach (Group group in firstMatch.Groups)
            {
                values.Add(string.IsNullOrEmpty(@group.Name) ? group.Index.ToString() : group.Name, @group.Value);
            }
            return true;
        }
    }
}