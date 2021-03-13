using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using SharpCR.Features;

namespace SharpCR.Registry.Features
{
    internal static class FeatureLoader
    {
        public static IReadOnlyList<IFeature> LoadFeatures(IConfiguration configuration)
        {
            const string theToggleFeature = "Toggles";
            var dic = configuration.GetSection($"Features:{theToggleFeature}")?.Get<Dictionary<string, bool>>() ?? new Dictionary<string, bool>();
            var toggles = new Dictionary<string, bool>(dic, StringComparer.OrdinalIgnoreCase);
            toggles[theToggleFeature] = false; // disable the "Toggles" feature, which is taken by the `Toggles` configuration. 
            
            return LoadFeaturesFromDisk(toggles);
        }
        
        static IReadOnlyList<IFeature> LoadFeaturesFromDisk(IReadOnlyDictionary<string, bool> toggles)
        {
            const string featurePrefix = "SharpCR.Features.";
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (baseDir == null)
            {
                // we can't load assemblies aside an assembly from memory.
                return new IFeature[0];
            }
            
            var dlls = Directory.GetFiles(baseDir, "*.dll")
                .Where(dll => Path.GetFileName(dll).StartsWith(featurePrefix))
                .Select(dll => Path.GetFileNameWithoutExtension(dll).Substring(featurePrefix.Length))
                .Where(feature => feature.Length > 0 && (!toggles.TryGetValue(feature, out var enabled) || enabled))
                .ToArray();

            var featureInterfaceType = typeof(IFeature);
            var featureList = new List<IFeature>(dlls.Length);
            var loadedFeatures = dlls
                .Select(dll => LoadFeatureFromAssembly(Path.Combine(baseDir, string.Concat(featurePrefix, dll, ".dll")), featureInterfaceType))
                .Where(feature => feature != null);
            featureList.AddRange(loadedFeatures);
            return featureList;
        }

        static IFeature LoadFeatureFromAssembly(string assemblyPath, Type featureInterfaceType)
        {
            var loadContext = new FeatureAssemblyLoadContext(assemblyPath);
            try
            {
                var featureAssembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath)));
                var exportedTypes = featureAssembly.GetExportedTypes();
                var featureType = exportedTypes.FirstOrDefault(t => t.IsPublic && featureInterfaceType.IsAssignableFrom(t));

                if (featureType != null)
                {
                    try
                    {
                        return (Activator.CreateInstance(featureType) as IFeature);
                    }
                    catch (Exception ex)
                    {
                        // todo print to logger
                        Console.WriteLine($"Error creating feature instance from type {featureType.FullName} with error:");
                        Console.WriteLine(ex);
                    }
                }
                else
                {
                    // todo print to logger
                    Console.WriteLine($"No class implemented IFeature in assembly {assemblyPath}. Ignoring the assembly.");
                }
            }
            catch (Exception ex)
            {
                // todo print to logger
                Console.WriteLine($"Error loading feature assembly {assemblyPath} with error:");
                Console.WriteLine(ex);
            }

            return null;
        }

        public static void ForEach<T>(this IReadOnlyList<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }
    }
}