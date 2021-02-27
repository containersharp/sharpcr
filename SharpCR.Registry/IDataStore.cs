using System;
using System.Linq;
using SharpCR.Registry.Models;
using SharpCR.Registry.Records;

namespace SharpCR.Registry
{
    public interface IDataStore
    {
        RepositoryRecord GetRepository(string repoName);
        void CreateRepository(string repo);
        
        IQueryable<ImageRecord> ListImages(string repoName);
        
        ImageRecord GetImagesByTag(string repoName, string tag);
        
        ImageRecord GetImagesByDigest(string repoName, string digestString);

        void DeleteImage(ImageRecord imageRecord);
        void UpdateImage(ImageRecord imageRecord);
        void CreateImage(ImageRecord imageRecord);
    }
}