using System.Linq;
using SharpCR.Registry.Records;

namespace SharpCR.Registry
{
    public interface IDataStore
    {
        IQueryable<ImageRecord> ListImages(string repoName);
        
        ImageRecord GetImageByTag(string repoName, string tag);
        
        ImageRecord GetImageByDigest(string repoName, string digestString);

        void DeleteImage(ImageRecord imageRecord);
        void UpdateImage(ImageRecord imageRecord);
        void CreateImage(ImageRecord imageRecord);
    }
}