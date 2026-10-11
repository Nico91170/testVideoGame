using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 1 || !Directory.Exists(args[0]))
        {
            Console.Error.WriteLine("Usage : SyntaxChecks <répertoire Assets>");
            return 1;
        }
        int files = 0, errors = 0;
        var options = new CSharpParseOptions(LanguageVersion.CSharp9);
        foreach (string path in Directory.EnumerateFiles(args[0], "*.cs", SearchOption.AllDirectories))
        {
            files++;
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path);
            foreach (var diagnostic in tree.GetDiagnostics())
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    errors++;
                    Console.Error.WriteLine(diagnostic);
                }
        }
        Console.WriteLine("Analyse syntaxique C# 9 : " + files + " fichiers, " + errors + " erreurs. Ce contrôle ne compile pas contre les API Unity.");
        return errors == 0 ? 0 : 1;
    }
}
