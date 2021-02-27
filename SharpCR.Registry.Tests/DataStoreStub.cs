using System;
using System.Collections.Generic;
using System.Linq;
using SharpCR.Registry.Records;

namespace SharpCR.Registry.Tests
{
    public class DataStoreStub : IDataStore
    {
        private readonly List<ImageRecord> _images;
        public DataStoreStub(params ImageRecord[] images)
        {
            _images = new List<ImageRecord>(images ?? new ImageRecord[0]);
        }

        public IQueryable<ImageRecord> ListImages(string repoName)
        {
            return _images
                .Where(img => string.Equals(img.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase))
                .AsQueryable();
        }

        public ImageRecord GetImageByTag(string repoName, string tag)
        {
            return _images.FirstOrDefault(t =>
                    string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        public ImageRecord GetImageByDigest(string repoName, string digestString)
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
        }
    }
}