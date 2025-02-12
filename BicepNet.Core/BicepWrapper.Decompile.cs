using Bicep.Core.FileSystem;
using Bicep.Decompiler;
using System;
using System.Collections.Generic;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public static IDictionary<string, string> Decompile(string templatePath, string? outputDir = null, string? outputFile = null)
    {
        var inputPath = PathHelper.ResolvePath(templatePath);

        static string DefaultOutputPath(string path) => PathHelper.GetDefaultDecompileOutputPath(path);
        var outputPath = PathHelper.ResolveDefaultOutputPath(inputPath, outputDir, outputFile, DefaultOutputPath);

        Uri inputUri = PathHelper.FilePathToFileUrl(inputPath);
        Uri outputUri = PathHelper.FilePathToFileUrl(outputPath);

        var template = new Dictionary<string,string>();
        var templateDecompiler = new TemplateDecompiler(featureProvider, namespaceProvider, fileResolver, moduleRegistryProvider, configurationManager);
        var (entrypointUri, filesToSave) = templateDecompiler.DecompileFileWithModules(inputUri, outputUri);

        foreach (var (fileUri, bicepOutput) in filesToSave)
        {
            template.Add(fileUri.LocalPath,bicepOutput);
        }
        return template;
    }
}
