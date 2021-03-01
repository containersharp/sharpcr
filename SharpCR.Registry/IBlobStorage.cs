using System.IO;

namespace SharpCR.Registry
{
    public interface IBlobStorage
    {
        Stream Get(string location);

        void Delete(string location);

        string Save(Stream stream);
    }
}
