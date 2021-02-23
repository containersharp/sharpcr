using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SharpCR.Registry.Controllers.ResponseModels;
using SharpCR.Registry.Models;

namespace SharpCR.Registry.Controllers
{
    public class TagController
    {
        private readonly IRepository<ImageRepository> _imageRepositoryRepo;
        private readonly IRepository<Tag> _tagRepo;
        public TagController(IRepository<Tag> tagRepo, IRepository<ImageRepository> imageRepositoryRepo)
        {
            _tagRepo = tagRepo;
            _imageRepositoryRepo = imageRepositoryRepo;
        }
        
        
        [RegistryRoute("tags/list")]
        [HttpGet]
        public ActionResult<TagListResponse> List(string repo, [FromQuery]int? n, [FromQuery]string last)
        {
            var imageRepo = _imageRepositoryRepo.All()
                .FirstOrDefault(r => string.Equals(repo, r.Name, StringComparison.OrdinalIgnoreCase));
            if (imageRepo == null)
            {
                return new NotFoundResult();
            }

            n ??= 0;
            IEnumerable<string> returnList = null;
            var queryableTags = _tagRepo.All().Where(t => t.RepositoryId == imageRepo.Id).OrderBy(t => t.Name);
            if (!string.IsNullOrEmpty(last))
            {
                var allTags = queryableTags.Select(t => t.Name).ToList();
                var indexOfLast = allTags.FindIndex(t => string.Equals(t, last, StringComparison.OrdinalIgnoreCase));
                if (indexOfLast >= 0)
                {
                    returnList = allTags.Skip(indexOfLast + 1);
                }
            }
            
            returnList ??= queryableTags.Select(t => t.Name).ToList();
            returnList = n > 0 ? returnList.Take(n.Value) : returnList;
            return new TagListResponse
            {
                name = imageRepo.Name,
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