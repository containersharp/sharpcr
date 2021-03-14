using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace SharpCR.Registry.FeatureLoading
{
    public class FeatureAssemblyLoadContext: AssemblyLoadContext
    {
        private static List<AssemblyName> _loadedAssemblies;
        private readonly AssemblyDependencyResolver _resolver;

        public FeatureAssemblyLoadContext(string assemblyPath)
        {
            _loadedAssemblies ??= AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName()).ToList();
            _resolver = new AssemblyDependencyResolver(assemblyPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var alreadyLoaded = _loadedAssemblies
                .FirstOrDefault(asm => AssemblyName.ReferenceMatchesDefinition(asm, assemblyName));
            if (alreadyLoaded != null)
            {
                return Assembly.Load(alreadyLoaded);
            }
            
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}