using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Masterly.NonEmptyList.Analyzers;

/// <summary>
/// Completes a <c>[NonEmptyCollection(typeof(T))] partial class</c>: derives it from <c>NonEmptyList&lt;T&gt;</c> and adds
/// constructors, factories, validation, collection-expression support, a System.Text.Json converter, a debugger display
/// and the invariant overrides for <c>Distinct</c>, <c>MinCount</c>, <c>MaxCount</c> and <c>Immutable</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class NonEmptyCollectionGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Masterly.NonEmptyList.NonEmptyCollectionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Model> models = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeName, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => Build(ctx, ct))
            .Where(static m => m is not null)!;

        context.RegisterSourceOutput(models, static (spc, model) => Emit(spc, model!));
    }

    private static Model? Build(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol type || context.TargetNode is not ClassDeclarationSyntax declaration)
            return null;

        AttributeData attribute = context.Attributes[0];
        ITypeSymbol? elementType = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as ITypeSymbol : null;
        if (elementType is null)
            return null;

        bool distinct = false;
        bool immutable = false;
        int minCount = 1;
        int maxCount = 0;
        foreach (var named in attribute.NamedArguments)
        {
            switch (named.Key)
            {
                case "Distinct": distinct = named.Value.Value is true; break;
                case "Immutable": immutable = named.Value.Value is true; break;
                case "MinCount": minCount = named.Value.Value is int min ? min : 1; break;
                case "MaxCount": maxCount = named.Value.Value is int max ? max : 0; break;
            }
        }

        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        Location location = declaration.Identifier.GetLocation();
        string name = type.Name;

        if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            diagnostics.Add(Diagnostic.Create(Descriptors.CollectionMustBePartial, location, name));

        if (type.ContainingType is not null)
            diagnostics.Add(Diagnostic.Create(Descriptors.CollectionMustNotBeNested, location, name));

        if (declaration.BaseList is not null && type.BaseType is { SpecialType: not SpecialType.System_Object } baseType && !WellKnown.IsNonEmptyType(baseType))
            diagnostics.Add(Diagnostic.Create(Descriptors.CollectionMustNotHaveBaseType, location, name, elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

        if (minCount < 1)
            diagnostics.Add(Diagnostic.Create(Descriptors.CollectionInvalidBounds, location, name, "MinCount must be at least 1"));

        if (maxCount < 0 || (maxCount != 0 && maxCount < minCount))
            diagnostics.Add(Diagnostic.Create(Descriptors.CollectionInvalidBounds, location, name, "MaxCount must be 0 (unbounded) or at least MinCount"));

        string ns = type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString();
        return new Model(
            ns,
            name,
            elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)),
            !elementType.IsValueType || elementType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T,
            distinct,
            immutable,
            minCount,
            maxCount,
            diagnostics.ToImmutable());
    }

    private static void Emit(SourceProductionContext context, Model model)
    {
        foreach (Diagnostic diagnostic in model.Diagnostics)
            context.ReportDiagnostic(diagnostic);

        if (model.Diagnostics.Length > 0)
            return;

        string source = Render(model);
        string hint = (model.Namespace.Length == 0 ? "" : model.Namespace + ".") + model.Name + ".NonEmptyCollection.g.cs";
        context.AddSource(hint, SourceText.From(source, Encoding.UTF8));
    }

    private static string Render(Model m)
    {
        string T = m.ElementType;
        string N = m.Name;
        string max = m.MaxCount == 0 ? "int.MaxValue" : m.MaxCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        bool bounded = m.MinCount > 1 || m.MaxCount > 0;

        StringBuilder sb = new();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by Masterly.NonEmptyList.Analyzers. Do not edit.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS1591, CA2260");
        sb.AppendLine();
        if (m.Namespace.Length > 0)
        {
            sb.Append("namespace ").Append(m.Namespace).AppendLine(";");
            sb.AppendLine();
        }

        sb.AppendLine($"[global::System.Text.Json.Serialization.JsonConverter(typeof({N}.JsonConverter))]");
        sb.AppendLine($"[global::System.Runtime.CompilerServices.CollectionBuilder(typeof({N}), nameof({N}.Create))]");
        sb.AppendLine("[global::System.Diagnostics.DebuggerDisplay(\"Count = {Count}, Head = {Head}\")]");
        sb.AppendLine("[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Usage\", \"CA2260:Use correct type parameter\", Justification = \"NonEmptyList<T> is a collection base, not a curiously recurring template.\")]");
        sb.AppendLine($"partial class {N} : global::Masterly.NonEmptyList.NonEmptyList<{T}>");
        sb.AppendLine("{");

        // Metadata
        sb.AppendLine($"    /// <summary>The minimum number of elements. Always at least 1.</summary>");
        sb.AppendLine($"    public const int MinCount = {m.MinCount};");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>The maximum number of elements, or int.MaxValue when unbounded.</summary>");
        sb.AppendLine($"    public const int MaxCount = {max};");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Whether duplicate elements are rejected.</summary>");
        sb.AppendLine($"    public const bool IsDistinct = {(m.Distinct ? "true" : "false")};");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Whether the collection rejects every mutation.</summary>");
        sb.AppendLine($"    public const bool IsImmutable = {(m.Immutable ? "true" : "false")};");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>A human-readable description of the invariants.</summary>");
        sb.AppendLine($"    public static string Description => \"{BuildDescription(m)}\";");
        sb.AppendLine();

        // Constructors
        sb.AppendLine($"    /// <summary>Creates a {N} from a first element and optional others.</summary>");
        sb.AppendLine($"    public {N}({T} first, params {T}[] rest) : base(first, rest)");
        sb.AppendLine("    {");
        sb.AppendLine("        ValidateInvariants();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Creates a {N} from a sequence.</summary>");
        sb.AppendLine("    /// <exception cref=\"global::System.ArgumentException\">Thrown if the sequence is empty or violates an invariant.</exception>");
        sb.AppendLine($"    public {N}(global::System.Collections.Generic.IEnumerable<{T}> items) : base(global::Masterly.NonEmptyList.NonEmptyList<{T}>.From(items))");
        sb.AppendLine("    {");
        sb.AppendLine("        ValidateInvariants();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private {N}(global::Masterly.NonEmptyList.NonEmptyList<{T}> source) : base(source)");
        sb.AppendLine("    {");
        sb.AppendLine("        ValidateInvariants();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Factories
        sb.AppendLine($"    /// <summary>Creates a {N} from a sequence.</summary>");
        sb.AppendLine($"    public static new {N} From(global::System.Collections.Generic.IEnumerable<{T}> items) => new {N}(items);");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Creates a {N} from a first element and optional others.</summary>");
        sb.AppendLine($"    public static {N} Of({T} first, params {T}[] rest) => new {N}(first, rest);");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Creates a {N} from a span. Used by the compiler for collection expressions.</summary>");
        sb.AppendLine($"    public static {N} Create(global::System.ReadOnlySpan<{T}> items) => new {N}(global::Masterly.NonEmptyList.NonEmptyList<{T}>.FromSpan(items));");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Attempts to create a {N}, returning false instead of throwing.</summary>");
        sb.AppendLine($"    public static bool TryFrom(global::System.Collections.Generic.IEnumerable<{T}>? items, [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {N}? result)");
        sb.AppendLine("    {");
        sb.AppendLine("        result = null;");
        sb.AppendLine("        if (items is null)");
        sb.AppendLine("            return false;");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine($"            result = new {N}(items);");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (global::System.ArgumentException)");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Returns true if the sequence satisfies every invariant of {N}.</summary>");
        sb.AppendLine($"    public static bool Satisfies(global::System.Collections.Generic.IEnumerable<{T}>? items) => TryFrom(items, out _);");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Validates a sequence, producing accumulated errors instead of throwing.</summary>");
        sb.AppendLine($"    public static global::Masterly.NonEmptyList.Validation<{N}, global::Masterly.NonEmptyList.ValidationError> Validate(global::System.Collections.Generic.IEnumerable<{T}>? items, string? path = null)");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (items is null)");
        sb.AppendLine($"            return global::Masterly.NonEmptyList.Validation<{N}, global::Masterly.NonEmptyList.ValidationError>.Invalid(new global::Masterly.NonEmptyList.ValidationError(\"required\", \"A value is required\", path));");
        sb.AppendLine($"        global::System.Collections.Generic.List<{T}> list = new global::System.Collections.Generic.List<{T}>(items);");
        sb.AppendLine("        if (list.Count == 0)");
        sb.AppendLine($"            return global::Masterly.NonEmptyList.Validation<{N}, global::Masterly.NonEmptyList.ValidationError>.Invalid(new global::Masterly.NonEmptyList.ValidationError(\"nonEmpty\", \"At least one element is required\", path));");
        if (m.ElementCanBeNull)
        {
            sb.AppendLine("        if (list.Exists(static x => x is null))");
            sb.AppendLine($"            return global::Masterly.NonEmptyList.Validation<{N}, global::Masterly.NonEmptyList.ValidationError>.Invalid(new global::Masterly.NonEmptyList.ValidationError(\"noNulls\", \"Elements cannot be null\", path));");
        }
        sb.AppendLine($"        global::System.Collections.Generic.List<global::Masterly.NonEmptyList.ValidationError> errors = new global::System.Collections.Generic.List<global::Masterly.NonEmptyList.ValidationError>();");
        sb.AppendLine("        if (list.Count < MinCount)");
        sb.AppendLine("            errors.Add(new global::Masterly.NonEmptyList.ValidationError(\"minCount\", $\"At least {MinCount} elements are required\", path));");
        sb.AppendLine("        if (list.Count > MaxCount)");
        sb.AppendLine("            errors.Add(new global::Masterly.NonEmptyList.ValidationError(\"maxCount\", $\"At most {MaxCount} elements are allowed\", path));");
        if (m.Distinct)
        {
            sb.AppendLine($"        if (new global::System.Collections.Generic.HashSet<{T}>(list).Count != list.Count)");
            sb.AppendLine("            errors.Add(new global::Masterly.NonEmptyList.ValidationError(\"distinct\", \"Elements must be distinct\", path));");
        }

        sb.AppendLine("        if (errors.Count > 0)");
        sb.AppendLine($"            return global::Masterly.NonEmptyList.Validation<{N}, global::Masterly.NonEmptyList.ValidationError>.Invalid(global::Masterly.NonEmptyList.NonEmptyList<global::Masterly.NonEmptyList.ValidationError>.From(errors));");
        sb.AppendLine($"        return new {N}(global::Masterly.NonEmptyList.NonEmptyList<{T}>.From(list));");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Invariant hooks
        if (m.Immutable)
        {
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine("    protected override bool IsReadOnly => true;");
            sb.AppendLine();
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine($"    protected override void EnsureMutable() => throw new global::System.NotSupportedException(\"{N} is immutable\");");
            sb.AppendLine();
        }

        if (m.Distinct)
        {
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine($"    protected override void ValidateItem({T} item)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (Contains(item))");
            sb.AppendLine($"            throw new global::System.ArgumentException($\"{N} cannot contain duplicate elements: {{item}}\", nameof(item));");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine($"    protected override void ValidateRange(global::System.Collections.Generic.IReadOnlyList<{T}> items)");
            sb.AppendLine("    {");
            sb.AppendLine("        base.ValidateRange(items);");
            sb.AppendLine($"        if (new global::System.Collections.Generic.HashSet<{T}>(items).Count != items.Count)");
            sb.AppendLine($"            throw new global::System.ArgumentException(\"{N} cannot contain duplicate elements\", nameof(items));");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine($"    protected override void ValidateReplacement(int index, {T} item)");
            sb.AppendLine("    {");
            sb.AppendLine("        int existing = IndexOf(item);");
            sb.AppendLine("        if (existing >= 0 && existing != index)");
            sb.AppendLine($"            throw new global::System.ArgumentException($\"{N} cannot contain duplicate elements: {{item}}\", nameof(item));");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        if (bounded)
        {
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine("    protected override void ValidateCount(int proposedCount)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (proposedCount < MinCount)");
            sb.AppendLine($"            throw new global::System.InvalidOperationException($\"{N} must contain at least {{MinCount}} elements\");");
            sb.AppendLine("        if (proposedCount > MaxCount)");
            sb.AppendLine($"            throw new global::System.InvalidOperationException($\"{N} cannot contain more than {{MaxCount}} elements\");");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("    private void ValidateInvariants()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (Count < MinCount)");
        sb.AppendLine($"            throw new global::System.ArgumentException($\"{N} must contain at least {{MinCount}} elements\");");
        sb.AppendLine("        if (Count > MaxCount)");
        sb.AppendLine($"            throw new global::System.ArgumentException($\"{N} cannot contain more than {{MaxCount}} elements\");");
        if (m.Distinct)
        {
            sb.AppendLine($"        if (new global::System.Collections.Generic.HashSet<{T}>(this).Count != Count)");
            sb.AppendLine($"            throw new global::System.ArgumentException(\"{N} cannot contain duplicate elements\");");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public override string ToString() => \"{N}: [\" + string.Join(\", \", this) + \"]\";");
        sb.AppendLine();

        // JSON converter
        sb.AppendLine($"    /// <summary>System.Text.Json converter for {N}. Reads a JSON array and applies every invariant.</summary>");
        sb.AppendLine($"    public sealed class JsonConverter : global::System.Text.Json.Serialization.JsonConverter<{N}>");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <inheritdoc />");
        sb.AppendLine($"        public override {N}? Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (reader.TokenType == global::System.Text.Json.JsonTokenType.Null)");
        sb.AppendLine("                return null;");
        sb.AppendLine($"            global::System.Collections.Generic.List<{T}>? items = global::System.Text.Json.JsonSerializer.Deserialize<global::System.Collections.Generic.List<{T}>>(ref reader, options);");
        sb.AppendLine("            if (items is null || items.Count == 0)");
        sb.AppendLine($"                throw new global::System.Text.Json.JsonException(\"{N} cannot be empty\");");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine($"                return new {N}(items);");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (global::System.ArgumentException ex)");
        sb.AppendLine("            {");
        sb.AppendLine("                throw new global::System.Text.Json.JsonException(ex.Message, ex);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <inheritdoc />");
        sb.AppendLine($"        public override void Write(global::System.Text.Json.Utf8JsonWriter writer, {N} value, global::System.Text.Json.JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            writer.WriteStartArray();");
        sb.AppendLine($"            foreach ({T} item in value)");
        sb.AppendLine("                global::System.Text.Json.JsonSerializer.Serialize(writer, item, options);");
        sb.AppendLine("            writer.WriteEndArray();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildDescription(Model m)
    {
        StringBuilder sb = new();
        sb.Append("at least ").Append(m.MinCount).Append(m.MinCount == 1 ? " element" : " elements");
        if (m.MaxCount > 0)
            sb.Append(", at most ").Append(m.MaxCount);
        if (m.Distinct)
            sb.Append(", distinct");
        if (m.Immutable)
            sb.Append(", immutable");
        return sb.ToString();
    }

    private sealed record Model(
        string Namespace,
        string Name,
        string ElementType,
        bool ElementCanBeNull,
        bool Distinct,
        bool Immutable,
        int MinCount,
        int MaxCount,
        ImmutableArray<Diagnostic> Diagnostics);
}
