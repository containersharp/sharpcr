using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharpCR.Registry.Controllers.ResponseModels;

namespace SharpCR.Registry.Controllers
{
    public class TagController
    {
        private readonly IRecordStore _recordStore;

        public TagController(IRecordStore recordStore)
        {
            _recordStore = recordStore;
        }
        
        
        [RegistryRoute("tags/list")]
        [HttpGet]
        public async Task<ActionResult<TagListResponse>> List(string repo, [FromQuery]int? n, [FromQuery]string last)
        {
            n ??= 0;
            IEnumerable<string> returnList = null;
            var queryableArtifacts = (await _recordStore.ListArtifactAsync(repo)).OrderBy(t => t.Tag);
            if (!string.IsNullOrEmpty(last))
            {
                var allTags = queryableArtifacts.Select(t => t.Tag).ToList();
                var indexOfLast = allTags.FindIndex(t => string.Equals(t, last, StringComparison.OrdinalIgnoreCase));
                if (indexOfLast >= 0)
                {
                    returnList = allTags.Skip(indexOfLast + 1);
                }
            }
            
            returnList ??= queryableArtifacts.Select(t => t.Tag).ToList();
            returnList = n > 0 ? returnList.Take(n.Value) : returnList;
            return new TagListResponse
            {
                name = repo,
                tags = returnList.ToList()
            };
        }
        
        /*
         * Formats:
        // Grammar
        //
        // repo name:    [a-z0-9]+([._-][a-z0-9]+)*(/[a-z0-9]+([._-][a-z0-9]+)*)*
        //  reference                       := name [ ":" tag ] [ "@" digest ]
        //  name                            := [hostname '/'] component ['/' component]*
        //  hostname                        := hostcomponent ['.' hostcomponent]* [':' port-number]
        //  hostcomponent                   := /([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9-]*[a-zA-Z0-9])/
        //  port-number                     := /[0-9]+/
        //  component                       := alpha-numeric [separator alpha-numeric]*
        //  alpha-numeric                   := /[a-z0-9]+/
        //  separator                       := /[_.]|__|[-]*/
        //
        //  tag                             := /[\w][\w.-]{0,127}/
        //
        //  digest                          := digest-algorithm ":" digest-hex
        //  digest-algorithm                := digest-algorithm-component [ digest-algorithm-separator digest-algorithm-component ]
        //  digest-algorithm-separator      := /[+.-_]/
        //  digest-algorithm-component      := /[A-Za-z][A-Za-z0-9]*/
        //  digest-hex                      := /[0-9a-fA-F]{32,}/ ; At least 128 bit digest value
// */
    }
}