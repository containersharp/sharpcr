using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SharpCR.Registry
{
    public class RegistryRouteAttribute : Attribute, IActionModelConvention, IResourceFilter
    {
        private readonly string[] _templates;

        public RegistryRouteAttribute(params string[] templates)
        {
            _templates = templates;
        }
        
        public void Apply(ActionModel actionModel)
        {
            var existingSelector = actionModel.Selectors.Single();
            var prefixes = new[]
            {
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/{repo5}/",
                "v2/{repo1}/{repo2}/{repo3}/{repo4}/",
                "v2/{repo1}/{repo2}/{repo3}/",
                "v2/{repo1}/{repo2}/",
                "v2/{repo1}/",
            };

            actionModel.Selectors.Clear();
            foreach (var prefix in prefixes)
            {
                foreach (var template in _templates)
                {
                    var newSelector = new SelectorModel(existingSelector)
                    {
                        AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = string.Concat(prefix, template)
                        }
                    };

                    actionModel.Selectors.Add(newSelector);
                }
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var values = context.RouteData.Values;
            var repoParts = new List<string>();

            if (values.TryGetValue("repo1", out var repo1))
            {
                repoParts.Add((string)repo1);
                values.Remove("repo1");
            }
            if (values.TryGetValue("repo2", out var repo2))
            {
                repoParts.Add((string)repo2);
                values.Remove("repo2");
            }
            else
            {
                repoParts.Insert(0, "library");                
            }
            
            if (values.TryGetValue("repo3", out var repo3))
            {
                repoParts.Add((string)repo3);
                values.Remove("repo3");
            }
            if (values.TryGetValue("repo4", out var repo4))
            {
                repoParts.Add((string)repo4);
                values.Remove("repo4");
            }
            if (values.TryGetValue("repo5", out var repo5))
            {
                repoParts.Add((string)repo5);
                values.Remove("repo5");
            }

            var repoName = string.Join("/", repoParts);
            values["repo"] = repoName;
        }
    }

}