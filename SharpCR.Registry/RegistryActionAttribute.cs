using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace SharpCR.Registry
{
    public class RegistryRouteAttribute : Attribute, IActionModelConvention, IResourceFilter
    {
        private readonly string _template;

        public RegistryRouteAttribute(string template)
        {
            _template = template;
        }
        
        public void Apply(ActionModel actionModel)
        {
            var existingSelector = actionModel.Selectors.Single();
            var prefixes = new[]
            {
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/{repo5}/{repo6}/{repo7}/{repo8}/{repo9}/{repo10}/",
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/{repo5}/{repo6}/{repo7}/{repo8}/{repo9}/",
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/{repo5}/{repo6}/{repo7}/{repo8}/",
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/{repo5}/{repo6}/{repo7}/",
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/{repo5}/{repo6}/",
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/{repo5}/",
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/",
                "v2/{repo1}/{repo2}/{repo3}/",
                "v2/{repo1}/{repo2}/",
                "v2/{repo1}/",
            };

            actionModel.Selectors.Clear();
            foreach (var prefix in prefixes)
            {
                var newSelector = new SelectorModel(existingSelector)
                {
                    AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = string.Concat(prefix, _template)
                    }
                };

                actionModel.Selectors.Add(newSelector);
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var values = context.RouteData.Values;
            var repoParts = new List<string>();

            for (var num = 1; num <= 10; num++)
            {
                TryAddRepoRouteValue(values, repoParts, num);
            }
            
            if(repoParts.Count == 1)
            {
                repoParts.Insert(0, "library");                
            }
            var repoName = string.Join("/", repoParts);
            values["repo"] = repoName;
        }

        private static void TryAddRepoRouteValue(RouteValueDictionary values, List<string> repoParts, int fragmentNumber)
        {
            var key = $"repo{fragmentNumber}";
            if (values.TryGetValue(key, out var routeValue))
            {
                repoParts.Add((string) routeValue);
                values.Remove(key);
            }
        }
    }

}