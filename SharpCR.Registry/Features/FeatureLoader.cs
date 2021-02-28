using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

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
                .Where(feature => !toggles.TryGetValue(feature, out var enabled) || enabled)
                .ToArray();

            var featureInterfaceType = typeof(IFeature);
            var featureList = new List<IFeature>(dlls.Length);
            featureList.AddRange(dlls.Select(dll => LoadFeatureFromAssembly(baseDir, dll, featureInterfaceType)).Where(feature => feature != null));
            return featureList;
        }

        static IFeature LoadFeatureFromAssembly(string baseDir, string dll, Type featureInterfaceType)
        {
            var dllPath = Path.Combine(baseDir, dll + ".dll");
            var loadContext = new FeatureAssemblyLoadContext(dllPath);
            try
            {
                var featureAssembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(dllPath)));
                var featureType = featureAssembly.GetExportedTypes()
                    .FirstOrDefault(t => t.IsPublic && featureInterfaceType.IsAssignableFrom(t));

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
            }
            catch (Exception ex)
            {
                // todo print to logger
                Console.WriteLine($"Error loading feature assembly {dllPath} with error:");
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