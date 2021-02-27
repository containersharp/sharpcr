using SharpCR.Registry.Models;
using SharpCR.Registry.Models.Manifests;

namespace SharpCR.Registry
{
    public interface IBlobStorage
    {
        bool BlobExists(Descriptor descriptor);
    }
}