using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;

namespace DaJet.CodeGenerator.CSharp
{
    internal sealed class Compiler
    {
        public byte[] Compile(string sourceCode, string assemblyName)
        {
            //var sourceCode = File.ReadAllText(filepath);

            using (var stream = new MemoryStream())
            {
                var result = GenerateCode(sourceCode, assemblyName).Emit(stream);

                if (!result.Success)
                {
                    var failures = result.Diagnostics
                        .Where(diagnostic => diagnostic.IsWarningAsError
                        || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        //Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                    return null;
                }
                stream.Seek(0, SeekOrigin.Begin);
                return stream.ToArray();
            }
        }

        private static CSharpCompilation GenerateCode(string sourceCode, string assemblyName)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            //var dotNetCoreDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                //MetadataReference.CreateFromFile(typeof(EntityAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(RuntimeReflectionExtensions).Assembly.Location)
                //MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Runtime.dll"))
            };

            return CSharpCompilation.Create($"{assemblyName}",
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release));
        }
    }
}