using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Text;
using Bicep.Core.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public static IList<string> Build(string bicepPath, bool noRestore = false) => joinableTaskFactory.Run(() => BuildAsync(bicepPath, noRestore));

    public static async Task<IList<string>> BuildAsync(string bicepPath, bool noRestore = false)
    {
        using var sw = new StringWriter();
        using var writer = new SourceAwareJsonTextWriter(sw)
        {
            Formatting = Formatting.Indented
        };

        var inputUri = PathHelper.FilePathToFileUrl(bicepPath);

        // Create separate configuration for the build, to account for custom rule changes
        var buildConfiguration = configurationManager.GetConfiguration(inputUri);

        var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, buildConfiguration);

        // If user did not specify NoRestore, restore modules and rebuild
        if (!noRestore)
        {
            if (await moduleDispatcher.RestoreModules(buildConfiguration, moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.GetModulesToRestore(), buildConfiguration)))
            {
                sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping, buildConfiguration);
            }
        }

        var compilation = new Compilation(featureProvider, namespaceProvider, sourceFileGrouping, buildConfiguration, apiVersionProvider, new LinterAnalyzer(buildConfiguration));
        var template = new List<string>();

        bool success = LogDiagnostics(compilation.GetAllDiagnosticsByBicepFile());
        if (success)
        {
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), new EmitterSettings(featureProvider));
            emitter.Emit(writer);
            template.Add(sw.ToString());
        }

        return template;
    }
}