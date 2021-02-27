using System;
using System.Collections.Generic;
using System.Linq;
using SharpCR.Registry.Models;
using SharpCR.Registry.Records;

namespace SharpCR.Registry.Tests
{
    public class DataStoreStub : IDataStore
    {
        private RepositoryRecord[] _repositories;
        private List<ImageRecord> _images;
        public DataStoreStub(params ImageRecord[] images)
        {
            _images = new List<ImageRecord>(images ?? new ImageRecord[0]);
            ImagesUpdated();
        }

        void ImagesUpdated()
        {
            _repositories = _images.Select(img => new RepositoryRecord{ Name = img.RepositoryName}).ToArray();
        }
        
        public RepositoryRecord GetRepository(string repoName)
        {
            return _repositories.FirstOrDefault(r => 
                string.Equals(repoName, r.Name, StringComparison.OrdinalIgnoreCase));
        }

        public void CreateRepository(string repo)
        {
            
        }

        public IQueryable<ImageRecord> ListImages(string repoName)
        {
            return _images
                .Where(img => string.Equals(img.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase))
                .AsQueryable();
        }

        public ImageRecord GetImagesByTag(string repoName, string tag)
        {
            return _images.FirstOrDefault(t =>
                    string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        public ImageRecord GetImagesByDigest(string repoName, string digestString)
        {
            return _images.FirstOrDefault(t =>
                string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.DigestString, digestString, StringComparison.OrdinalIgnoreCase));

        }

        public void DeleteImage(ImageRecord imageRecord)
        {
            var index = _images.IndexOf(imageRecord);
            if (index >= 0)
            {
                _images.RemoveAt(index);
                ImagesUpdated();
            }
        }

        public void UpdateImage(ImageRecord imageRecord)
        {
            DeleteImage(imageRecord);
            CreateImage(imageRecord);
        }

        public void CreateImage(ImageRecord imageRecord)
        {
            _images.Add(imageRecord);
            ImagesUpdated();
        }
    }
}