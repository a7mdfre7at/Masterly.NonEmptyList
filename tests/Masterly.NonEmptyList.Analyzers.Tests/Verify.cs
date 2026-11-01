using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Masterly.NonEmptyList.Analyzers.Tests;

/// <summary>Shared setup: .NET 8 reference assemblies, the library under test, latest language version.</summary>
internal static class Verify
{
    public const string Usings = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using Masterly.NonEmptyList;

        """;

    public static CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> Analyzer<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> { TestCode = Usings + source };
        Configure(test);
        test.ExpectedDiagnostics.AddRange(expected);
        return test;
    }

    public static CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> CodeFix<TAnalyzer, TCodeFix>(string source, string fixedSource, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = Usings + source,
            FixedCode = Usings + fixedSource,
        };
        Configure(test);
        test.ExpectedDiagnostics.AddRange(expected);
        return test;
    }

    public static void Configure<TVerifier>(AnalyzerTest<TVerifier> test) where TVerifier : IVerifier, new()
    {
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(NonEmptyList<>).Assembly.Location));
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var parse = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.Latest));
        });
    }

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) => new(descriptor);

    public static DiagnosticResult Enabled(DiagnosticDescriptor descriptor, DiagnosticSeverity severity) => new DiagnosticResult(descriptor).WithSeverity(severity);
}
