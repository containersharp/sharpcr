using System;
using System.Linq;

namespace SharpCR.Registry
{
    public interface IDataStore<T>
    {
        void Save(T item); 
        IQueryable<T> All(); 
        T Get(Guid id); 
        void Delete(T item); 
        void Update(T item);
    }
}