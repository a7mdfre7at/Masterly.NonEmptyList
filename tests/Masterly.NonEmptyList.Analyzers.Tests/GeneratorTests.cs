using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Masterly.NonEmptyList.Analyzers.Tests;

/// <summary>Drives the generator directly to check diagnostics and that the emitted code compiles.</summary>
public class GeneratorTests
{
    private static (Compilation Output, ImmutableArray<Diagnostic> GeneratorDiagnostics, ImmutableArray<SyntaxTree> Generated) Run(string source)
    {
        IEnumerable<MetadataReference> references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p).StartsWith("System", StringComparison.Ordinal) || Path.GetFileName(p) is "netstandard.dll" or "mscorlib.dll")
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Append(MetadataReference.CreateFromFile(typeof(NonEmptyList<>).Assembly.Location));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTest",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new NonEmptyCollectionGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> diagnostics);
        ImmutableArray<SyntaxTree> generated = driver.GetRunResult().GeneratedTrees;
        return (output, diagnostics, generated);
    }

    [Fact]
    public void ValidDeclaration_ShouldGenerateCompilableCode()
    {
        const string source = """
            using Masterly.NonEmptyList;

            namespace Shop;

            public sealed record OrderLine(string Sku, int Quantity);

            [NonEmptyCollection(typeof(OrderLine), Distinct = true, MaxCount = 500)]
            public sealed partial class OrderLines;
            """;

        var (output, generatorDiagnostics, generated) = Run(source);

        Assert.Empty(generatorDiagnostics);
        SyntaxTree tree = Assert.Single(generated);
        string code = tree.ToString();
        Assert.Contains("partial class OrderLines : global::Masterly.NonEmptyList.NonEmptyList<global::Shop.OrderLine>", code);
        Assert.Contains("public const int MaxCount = 500;", code);
        Assert.Contains("public const bool IsDistinct = true;", code);
        Assert.Contains("protected override void ValidateItem(", code);
        Assert.Contains("protected override void ValidateCount(int proposedCount)", code);
        Assert.DoesNotContain("EnsureMutable", code);
        Assert.Contains("CollectionBuilder(typeof(OrderLines), nameof(OrderLines.Create))", code);
        Assert.Contains("public sealed class JsonConverter", code);
        Assert.Empty(output.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()));
    }

    [Fact]
    public void GlobalNamespace_Immutable_Internal_ShouldGenerate()
    {
        const string source = """
            using Masterly.NonEmptyList;

            [NonEmptyCollection(typeof(int), Immutable = true, MinCount = 2)]
            internal partial class Pair;
            """;

        var (output, generatorDiagnostics, generated) = Run(source);

        Assert.Empty(generatorDiagnostics);
        string code = Assert.Single(generated).ToString();
        Assert.DoesNotContain("namespace", code);
        Assert.Contains("protected override void EnsureMutable()", code);
        Assert.Contains("public const int MinCount = 2;", code);
        Assert.Contains("public const int MaxCount = int.MaxValue;", code);
        Assert.Contains("protected override void ValidateCount(int proposedCount)", code);
        Assert.DoesNotContain("ValidateItem", code);
        Assert.Empty(output.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void NonPartialClass_ShouldReportNEL101()
    {
        const string source = """
            using Masterly.NonEmptyList;

            [NonEmptyCollection(typeof(int))]
            public sealed class Numbers;
            """;

        var (_, diagnostics, generated) = Run(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(Descriptors.CollectionMustBePartialId, diagnostic.Id);
        Assert.Contains("Numbers", diagnostic.GetMessage());
        Assert.Empty(generated);
    }

    [Fact]
    public void NestedClass_ShouldReportNEL102()
    {
        const string source = """
            using Masterly.NonEmptyList;

            public static class Outer
            {
                [NonEmptyCollection(typeof(int))]
                public sealed partial class Numbers;
            }
            """;

        var (_, diagnostics, _) = Run(source);

        Assert.Contains(diagnostics, d => d.Id == Descriptors.CollectionMustNotBeNestedId);
    }

    [Fact]
    public void InvalidBounds_ShouldReportNEL103()
    {
        const string source = """
            using Masterly.NonEmptyList;

            [NonEmptyCollection(typeof(int), MinCount = 0)]
            public sealed partial class A;

            [NonEmptyCollection(typeof(int), MinCount = 5, MaxCount = 2)]
            public sealed partial class B;

            [NonEmptyCollection(typeof(int), MaxCount = -1)]
            public sealed partial class C;
            """;

        var (_, diagnostics, generated) = Run(source);

        Assert.Equal(3, diagnostics.Count(d => d.Id == Descriptors.CollectionInvalidBoundsId));
        Assert.Empty(generated);
    }

    [Fact]
    public void ExplicitBaseType_ShouldReportNEL104()
    {
        const string source = """
            using Masterly.NonEmptyList;

            public class Base { }

            [NonEmptyCollection(typeof(int))]
            public sealed partial class Numbers : Base;
            """;

        var (_, diagnostics, _) = Run(source);

        Assert.Contains(diagnostics, d => d.Id == Descriptors.CollectionMustNotHaveBaseTypeId);
    }

    [Fact]
    public void ExplicitNonEmptyListBaseType_ShouldBeAccepted()
    {
        const string source = """
            using Masterly.NonEmptyList;

            [NonEmptyCollection(typeof(int))]
            public sealed partial class Numbers : NonEmptyList<int>;
            """;

        var (output, diagnostics, generated) = Run(source);

        Assert.Empty(diagnostics);
        Assert.Single(generated);
        Assert.Empty(output.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Descriptors_ShouldHaveHelpLinksAndCategories()
    {
        foreach (DiagnosticDescriptor descriptor in new[]
                 {
                     Descriptors.EmptyCollectionExpression, Descriptors.EmptyFactoryArgument, Descriptors.ThrowingLinqOnNonEmpty,
                     Descriptors.RedundantEmptinessCheck, Descriptors.UnprovenSequence, Descriptors.CollectionMustBePartial,
                     Descriptors.CollectionMustNotBeNested, Descriptors.CollectionInvalidBounds, Descriptors.CollectionMustNotHaveBaseType,
                 })
        {
            Assert.StartsWith("NEL", descriptor.Id);
            Assert.Equal("Masterly.NonEmptyList", descriptor.Category);
            Assert.EndsWith(descriptor.Id.ToLowerInvariant(), descriptor.HelpLinkUri);
        }

        Assert.False(Descriptors.UnprovenSequence.IsEnabledByDefault);
    }
}
