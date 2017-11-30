using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using OrchardCore.Modules.FileProviders;

namespace OrchardCore.Modules
{
    public static class ModularApplicationContext
    {
        private const string ModuleNamesMap = "module.names.map";
        private const string ModuleAssetsMap = "module.assets.map";

        private static ConcurrentDictionary<string, IEnumerable<string>> _maps = new ConcurrentDictionary<string, IEnumerable<string>>();
        private static ConcurrentDictionary<string, Assembly> _assemblies = new ConcurrentDictionary<string, Assembly>();
        private static ConcurrentDictionary<string, IFileProvider> _fileProviders = new ConcurrentDictionary<string, IFileProvider>();
        private static ConcurrentDictionary<string, IFileInfo> _fileInfos = new ConcurrentDictionary<string, IFileInfo>();

        public static Assembly LoadApplicationAssembly(this IHostingEnvironment environment)
        {
            return Load(environment.ApplicationName);
        }

        public static Assembly LoadModuleAssembly(this IHostingEnvironment environment, string moduleId)
        {
            if (!GetModuleNames(environment).Contains(moduleId))
            {
                return null;
            }

            return Load(moduleId);
        }

        public static IEnumerable<string> GetModuleNames(this IHostingEnvironment environment)
        {
            var key = environment.ApplicationName + ModuleNamesMap;

            if (!_maps.ContainsKey(key))
            {
                _maps[key] = GetFileInfo(environment.ApplicationName, ModuleNamesMap).ReadAllLines();
            }

            return _maps[key];
        }

        public static IEnumerable<string> GetModuleAssets(this IHostingEnvironment environment, string moduleId)
        {
            if (!GetModuleNames(environment).Contains(moduleId))
            {
                return Enumerable.Empty<string>();
            }

            var key = moduleId + ModuleAssetsMap;

            if (!_maps.ContainsKey(key))
            {
                _maps[key] = GetFileInfo(moduleId, ModuleAssetsMap).ReadAllLines().Select(x => x.Replace('\\', '/'));
            }

            return _maps[key];
        }

        public static IFileInfo GetModuleFileInfo(this IHostingEnvironment environment, string moduleId, string fileName)
        {
            if (!GetModuleNames(environment).Contains(moduleId))
            {
                return null;
            }

            return GetFileInfo(moduleId, fileName);
        }

        private static Assembly Load(string assemblyName)
        {
            if (!_assemblies.ContainsKey(assemblyName))
            {
                _assemblies[assemblyName] = Assembly.Load(new AssemblyName(assemblyName));
            }

            return _assemblies[assemblyName];
        }

        private static IFileProvider GetFileProvider(string assemblyName)
        {
            if (!_fileProviders.ContainsKey(assemblyName))
            {
                var assembly = Load(assemblyName);
                _fileProviders[assemblyName] = new EmbeddedFileProvider(assembly);
            }

            return _fileProviders[assemblyName];
        }

        private static IFileInfo GetFileInfo(string assemblyName, string fileName)
        {
            var key = assemblyName + fileName;

            if (!_fileInfos.ContainsKey(key))
            {
                var fileProvider = GetFileProvider(assemblyName);
                _fileInfos[key] = fileProvider.GetFileInfo(fileName);
            }

            return _fileInfos[key];
        }
    }
}
