using Maroik.Core.Contract.Tests.Fixtures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// A file whose (first) type is generic carries its type parameters in its name, StyleCop's convention:
/// <c>AggregateRoot&lt;TId&gt;</c> lives in <c>AggregateRoot{TId}.cs</c>, <c>GenericRepository&lt;TDomain, TEntity&gt;</c> in
/// <c>GenericRepository{TDomain,TEntity}.cs</c>, and a part of a partial generic type in <c>ErrorOr{TValue}.Else.cs</c>. A
/// non-generic type's file has no braces. SA1649 accepts both <c>Name.cs</c> and <c>Name{T}.cs</c> for a generic type, so the
/// build cannot hold this (since 2026-10-30, owner's request); it also keeps a generic and a non-generic type of the same name
/// (<c>IErrorOr</c> / <c>IErrorOr&lt;TValue&gt;</c>) apart.
/// <para>
/// It reads every <c>.cs</c> file of the production and test projects with the Roslyn parser, like
/// <see cref="FolderRoleArchitectureTests"/>; <c>Maroik.Core.PostgreSQL</c> is EF Core scaffold output and is left out.
/// </para>
/// </summary>
public class GenericFileNameArchitectureTests
{
    /// <summary>Every source file whose name does not match its first type, as "path: expected name".</summary>
    [Fact]
    public void AGenericTypesFile_IsNamedWithItsTypeParameters()
    {
        string root = RepositoryPaths.Root;

        string[] violations =
        [
            .. SourceFiles(root)
                .Select(path => (Path: Path.GetRelativePath(root, path), Expected: ExpectedFileStem(File.ReadAllText(path))))
                .Where(file => file.Expected is not null && IsJudged(Path.GetFileNameWithoutExtension(file.Path), file.Expected)
                    && !Matches(Path.GetFileNameWithoutExtension(file.Path), file.Expected))
                .Select(file => $"{file.Path}: name it {file.Expected}.cs (or {file.Expected}.<Part>.cs for a part of a partial type)"),
        ];

        Assert.True(violations.Length == 0, string.Join("\n", violations));
    }

    /// <summary>
    /// The expected name is built from the first top-level type, whatever its kind; a rule that silently found no type would pass
    /// every file.
    /// </summary>
    [Theory]
    [InlineData("namespace N;\npublic class Plain;\n", "Plain")]
    [InlineData("namespace N;\npublic abstract class AggregateRoot<TId> where TId : notnull;\n", "AggregateRoot{TId}")]
    [InlineData("namespace N;\ninternal sealed class Pair<TKey, TValue>;\n", "Pair{TKey,TValue}")]
    [InlineData("namespace N;\npublic interface IErrorOr<out TValue>;\n", "IErrorOr{TValue}")]
    [InlineData("namespace N;\npublic readonly partial record struct ErrorOr<TValue>;\n", "ErrorOr{TValue}")]
    [InlineData("namespace N;\npublic delegate TResult Map<in T, out TResult>(T value);\n", "Map{T,TResult}")]
    [InlineData("namespace N;\n[System.Obsolete]\npublic struct Box<T>;\npublic class Second<TOther>;\n", "Box{T}")]
    [InlineData("namespace N\n{\n    public class Wrapped<T>;\n}\n", "Wrapped{T}")]
    public void TheExpectedName_ComesFromTheFirstTypeAndItsTypeParameters(string source, string expected)
    {
        Assert.Equal(expected, ExpectedFileStem(source));
    }

    /// <summary>A file named exactly after its type, or after it plus a part name, matches; a bare name or other braces do not.</summary>
    [Theory]
    [InlineData("AggregateRoot{TId}", "AggregateRoot{TId}", true)]
    [InlineData("ErrorOr{TValue}.Else", "ErrorOr{TValue}", true)]
    [InlineData("AggregateRoot", "AggregateRoot{TId}", false)]
    [InlineData("AggregateRoot{T}", "AggregateRoot{TId}", false)]
    [InlineData("Plain{T}", "Plain", false)]
    [InlineData("Plain", "Plain", true)]
    public void AFileName_MatchesTheExpectedName_OrItPlusAPart(string fileStem, string expected, bool matches)
    {
        Assert.Equal(matches, Matches(fileStem, expected));
    }

    /// <summary>Only a generic first type or a braced file name is judged; a partial type's differently named file is not.</summary>
    [Theory]
    [InlineData("AggregateRoot", "AggregateRoot{TId}", true)]
    [InlineData("Plain{T}", "Plain", true)]
    [InlineData("ProgramAccessor", "Program", false)]
    public void OnlyGenericTypesAndBracedNames_AreJudged(string fileStem, string expected, bool judged)
    {
        Assert.Equal(judged, IsJudged(fileStem, expected));
    }

    /// <summary>The file name the first top-level type of <paramref name="source"/> asks for, or <see langword="null"/> when it has none.</summary>
    private static string? ExpectedFileStem(string source)
    {
        MemberDeclarationSyntax? first = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))
            .GetCompilationUnitRoot()
            .DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<MemberDeclarationSyntax>()
            .FirstOrDefault(member => member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax);

        (string Name, TypeParameterListSyntax? Parameters)? type = first switch
        {
            TypeDeclarationSyntax declaration => (declaration.Identifier.ValueText, declaration.TypeParameterList),
            BaseTypeDeclarationSyntax declaration => (declaration.Identifier.ValueText, null),
            DelegateDeclarationSyntax declaration => (declaration.Identifier.ValueText, declaration.TypeParameterList),
            _ => null,
        };

        return type switch
        {
            null => null,
            { Parameters: null } => type.Value.Name,
            _ => $"{type.Value.Name}{{{string.Join(",", type.Value.Parameters.Parameters.Select(p => p.Identifier.ValueText))}}}",
        };
    }

    /// <summary>
    /// True when this rule judges the file: its first type is generic, or its name has braces. Any other name is SA1649's to
    /// judge (which deliberately leaves a partial type such as <c>ProgramAccessor.cs</c>'s <c>Program</c> alone).
    /// </summary>
    private static bool IsJudged(string fileStem, string expected) => expected.Contains('{', StringComparison.Ordinal) || fileStem.Contains('{', StringComparison.Ordinal);

    /// <summary>True when <paramref name="fileStem"/> is <paramref name="expected"/>, or <paramref name="expected"/> followed by <c>.Part</c>.</summary>
    private static bool Matches(string fileStem, string expected) =>
        fileStem == expected || fileStem.StartsWith(expected + ".", StringComparison.Ordinal);

    /// <summary>Every <c>.cs</c> file of the Maroik projects (production and tests), the PostgreSQL scaffold and build output aside.</summary>
    private static IEnumerable<string> SourceFiles(string root) =>
        Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .OfType<string>()
            .Where(dir => !RepositoryPaths.HasSegment(dir, "bin") && !RepositoryPaths.HasSegment(dir, "obj"))
            .Where(dir => Path.GetFileName(dir) is var name
                && name.StartsWith("Maroik.", StringComparison.Ordinal)
                && name != "Maroik.Core.PostgreSQL")
            .SelectMany(dir => Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            .Where(path => !RepositoryPaths.HasSegment(path, "bin") && !RepositoryPaths.HasSegment(path, "obj"))
            .OrderBy(path => path, StringComparer.Ordinal);
}
