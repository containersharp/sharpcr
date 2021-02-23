using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SharpCR.Registry.Controllers.ResponseModels;
using SharpCR.Registry.Models;

namespace SharpCR.Registry.Controllers
{
    public class TagController
    {
        [RegistryRoute("tags/list")]
        [HttpGet]
        public TagListResponse List(string repo, [FromQuery]int? n, [FromQuery]string last)
        {
            // QUESTION: should "last" by an integer or a string to indicate the last tag?
            // QUESTION: should we send a "link" in the response header?
            
            return new TagListResponse
            {
                name = repo,
                tags = new List<string>()
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