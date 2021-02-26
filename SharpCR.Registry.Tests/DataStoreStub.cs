using System;
using System.Linq;
using SharpCR.Registry.Models;

namespace SharpCR.Registry.Tests
{
    public class DataStoreStub : IDataStore
    {
        private readonly ImageRepository[] _repositories;
        private readonly Image[] _images;
        public DataStoreStub(params Image[] images)
        {
            _images = images;
            _repositories = images.Select(img => new ImageRepository{ Name = img.RepositoryName}).ToArray();
        }
        
        public ImageRepository GetRepository(string repoName)
        {
            return _repositories.FirstOrDefault(r => 
                string.Equals(repoName, r.Name, StringComparison.OrdinalIgnoreCase));
        }

        public IQueryable<Image> ListImages(string repoName)
        {
            return _images
                .Where(img => string.Equals(img.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase))
                .AsQueryable();
        }

        public Image GetImagesByTag(string repoName, string tag)
        {
            return _images.FirstOrDefault(t =>
                    string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        public Image GetImagesByDigest(string repoName, string digestString)
        {
            return _images.FirstOrDefault(t =>
                string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.DigestString, digestString, StringComparison.OrdinalIgnoreCase));

        }
    }
}