using System;
using System.Linq;
using SharpCR.Registry.Models;

namespace SharpCR.Registry
{
    public interface IDataStore
    {
        ImageRepository GetRepository(string repoName);
        
        IQueryable<Image> ListImages(string repoName);
        
        Image GetImagesByTag(string repoName, string tag);
        
        Image GetImagesByDigest(string repoName, string digestString);


        void DeleteImage(Image image);
    }
}