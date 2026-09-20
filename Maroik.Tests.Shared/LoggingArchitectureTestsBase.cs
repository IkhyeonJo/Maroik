using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Maroik.Tests.Shared;

/// <summary>
/// Source file linked into every <c>*.Tests</c> project (see <c>CLAUDE.md</c> → "Logging"): the
/// solution-wide logging rules. A project gets the rules by deriving one small class from this base.
/// <list type="number">
/// <item><b>Literal templates.</b> The message argument of every <c>logger.LogXxx(...)</c> call must be a
/// compile-time string literal with named placeholders (checked on the IL with Mono.Cecil, like the
/// NetArchTest rules). Interpolation (<c>$"…{x}"</c>), concatenation, <c>string.Format</c> or a variable
/// would freeze the values into the text: they stop being queryable properties, and a hostile value
/// could forge extra log lines.</item>
/// <item><b>Catch must log or rethrow.</b> Every <c>catch</c> must call a logger or throw (checked on the C#
/// source with Roslyn — an async method's <c>await</c>-in-catch is rewritten by the compiler, so the IL
/// handler no longer holds the catch body). A catch that deliberately does neither is listed in
/// <see cref="AllowedSilentCatches"/> with its reason, so the exception list is explicit and reviewed.</item>
/// </list>
/// </summary>
public abstract class LoggingArchitectureTestsBase
{
    /// <summary>The production assembly these rules run against.</summary>
    protected abstract Assembly ProductionAssembly { get; }

    /// <summary>
    /// Catch clauses that legitimately neither log nor rethrow, keyed <c>Namespace.Type.Member</c>, with the reason.
    /// </summary>
    protected virtual IReadOnlyDictionary<string, string> AllowedSilentCatches { get; } =
        new Dictionary<string, string>();

    /// <summary>Methods that legitimately build a log message dynamically, keyed <c>Namespace.Type.Method</c>, with the reason.</summary>
    protected virtual IReadOnlyDictionary<string, string> AllowedDynamicTemplates { get; } =
        new Dictionary<string, string>();

    [Fact]
    public void LogTemplates_MustBeStringLiterals()
    {
        List<string> violations = [.. LoggingRules.NonLiteralTemplates(LoggingRules.MethodsOf(ProductionAssembly))
            .Where(v => !AllowedDynamicTemplates.ContainsKey(v.Method)).Select(v => v.ToString())];

        Assert.True(violations.Count == 0,
            "Log message templates must be string literals with named placeholders " +
            "(logger.LogError(ex, \"Failed for {Email}\", email)) — never interpolation, concatenation, " +
            "string.Format or a variable.\n" + string.Join("\n", violations.Distinct().Order()));
    }

    /// <summary>
    /// The template rule must actually SEE this project's logging: if the IL walk ever stops recognising
    /// the logger calls (a compiler / logging-library change), the literal-template test above would pass
    /// vacuously on zero calls.
    /// </summary>
    [Fact]
    public void LogTemplateRule_MustAnalyzeTheProjectsLogCalls() =>
        Assert.True(LoggingRules.LoggerCallCount(LoggingRules.MethodsOf(ProductionAssembly)) > 0,
            "No logger call was found in " + ProductionAssembly.GetName().Name + " — the rule is not analyzing anything.");

    [Fact]
    public void CatchHandlers_MustLogOrRethrow()
    {
        List<string> violations = [.. LoggingRules.SilentCatchesIn(LoggingRules.SourceFilesOf(ProductionAssembly))
            .Where(k => !AllowedSilentCatches.ContainsKey(k))];

        Assert.True(violations.Count == 0,
            "Every catch must log (with the exception as first argument) or rethrow. A deliberately silent " +
            "catch must be listed in AllowedSilentCatches with its reason.\n" +
            string.Join("\n", violations.Distinct().Order()));
    }

    /// <summary>
    /// Guards the allow-lists themselves: an entry that no longer matches a silent catch / dynamic
    /// template is dead weight that would silently exempt a future one.
    /// </summary>
    [Fact]
    public void AllowLists_MustNotContainStaleEntries()
    {
        HashSet<string> silent = [.. LoggingRules.SilentCatchesIn(LoggingRules.SourceFilesOf(ProductionAssembly))];
        HashSet<string> dynamic = [.. LoggingRules.NonLiteralTemplates(LoggingRules.MethodsOf(ProductionAssembly)).Select(v => v.Method)];

        string[] stale =
        [
            .. AllowedSilentCatches.Keys.Where(k => !silent.Contains(k)).Select(k => "silent catch: " + k),
            .. AllowedDynamicTemplates.Keys.Where(k => !dynamic.Contains(k)).Select(k => "dynamic template: " + k),
        ];
        Assert.True(stale.Length == 0, "Stale allow-list entries:\n" + string.Join("\n", stale));
    }
}

/// <summary>The analysis behind <see cref="LoggingArchitectureTestsBase"/>, separate so it can be tested against known-bad code.</summary>
public static class LoggingRules
{
    private const string LoggerExtensionsType = "Microsoft.Extensions.Logging.LoggerExtensions";
    private const string LoggerInterfaceType = "Microsoft.Extensions.Logging.ILogger";

    private static readonly HashSet<string> _logMethodNames =
        ["Log", "LogTrace", "LogDebug", "LogInformation", "LogWarning", "LogError", "LogCritical"];

    /// <summary>A logger call whose message is not a string literal.</summary>
    public sealed record TemplateViolation(string Method, string Logger, string Producer)
    {
        public override string ToString() => $"{Method} → {Logger}: message is {Producer}";
    }

    // ---- Literal templates (IL) --------------------------------------------------------------

    public static IEnumerable<MethodDefinition> MethodsOf(Assembly assembly)
    {
        // Deliberately not disposed: the analysis reads operands lazily, and the process is short-lived.
        AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(assembly.Location);
        return definition.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).ToList();
    }

    public static IEnumerable<TemplateViolation> NonLiteralTemplates(IEnumerable<MethodDefinition> methods)
    {
        foreach (MethodDefinition method in methods)
        {
            foreach (Instruction call in method.Body.Instructions)
            {
                if (!IsLoggerCall(call, out MethodReference? target)) continue;

                // The template is the first string parameter of every LoggerExtensions overload
                // ((logger, [eventId], [exception], string message, params object[] args)). Parameter NAMES are
                // not available on a reference to another assembly's method, only their types.
                int messageIndex = target!.Parameters.ToList().FindIndex(p => p.ParameterType.FullName == "System.String");
                if (messageIndex < 0) continue; // e.g. ILogger.Log(..., formatter): not a template call

                Instruction? producer = FindArgumentProducer(call, target.Parameters.Count - 1 - messageIndex);
                if (producer is { OpCode.Code: Code.Ldstr }) continue;

                yield return new TemplateViolation(Attribute(method), target.Name,
                    producer == null ? "not analyzable" : $"produced by {producer.OpCode} {producer.Operand}");
            }
        }
    }

    /// <summary>How many logger calls the rule recognises in the given methods.</summary>
    public static int LoggerCallCount(IEnumerable<MethodDefinition> methods) =>
        methods.Sum(m => m.Body.Instructions.Count(i => IsLoggerCall(i, out _)));

    private static readonly HashSet<string> _serilogMethodNames = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal", "Write"];

    private static bool IsLoggerCall(Instruction instruction, out MethodReference? target)
    {
        target = null;
        if (instruction.OpCode.Code is not (Code.Call or Code.Callvirt)) return false;
        if (instruction.Operand is not MethodReference reference) return false;

        string declaring = reference.DeclaringType.FullName;
        bool isLog = declaring == LoggerExtensionsType && reference.Name.StartsWith("Log", StringComparison.Ordinal)
                     || declaring == LoggerInterfaceType && reference.Name == "Log"
                     // Serilog's static Log / ILogger (used by the hosts' Program.cs): (…, string messageTemplate, …)
                     || declaring is "Serilog.Log" or "Serilog.ILogger" or "Serilog.Core.Logger" && _serilogMethodNames.Contains(reference.Name);
        if (isLog) target = reference;
        return isLog;
    }

    /// <summary>
    /// Finds the instruction that pushed the call argument sitting <paramref name="slotsAbove"/> slots
    /// below the top of the evaluation stack, by walking the IL backwards and balancing stack effects.
    /// Returns <see langword="null"/> when the walk reaches the start of the method (a branch in the
    /// argument expression) — treated as a violation, i.e. fail closed.
    /// </summary>
    private static Instruction? FindArgumentProducer(Instruction call, int slotsAbove)
    {
        int skip = slotsAbove;
        for (Instruction? current = call.Previous; current != null; current = current.Previous)
        {
            (int pops, int pushes) = StackEffect(current);
            if (pushes > 0)
            {
                if (skip < pushes) return current;
                skip -= pushes;
            }

            skip += pops;
        }

        return null;
    }

    private static (int Pops, int Pushes) StackEffect(Instruction instruction)
    {
        OpCode op = instruction.OpCode;
        if (op.Code is Code.Call or Code.Callvirt or Code.Newobj)
        {
            var method = (MethodReference)instruction.Operand;
            int pops = method.Parameters.Count + (method.HasThis && !method.ExplicitThis && op.Code != Code.Newobj ? 1 : 0);
            int pushes = op.Code == Code.Newobj || method.ReturnType.FullName != "System.Void" ? 1 : 0;
            return (pops, pushes);
        }

        int pop = op.StackBehaviourPop switch
        {
            StackBehaviour.Pop0 => 0,
            StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref => 1,
            StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi
                or StackBehaviour.Popi_popi8 or StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8
                or StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
            StackBehaviour.Popi_popi_popi or StackBehaviour.Popref_popi_popi or StackBehaviour.Popref_popi_popi8
                or StackBehaviour.Popref_popi_popr4 or StackBehaviour.Popref_popi_popr8
                or StackBehaviour.Popref_popi_popref => 3,
            StackBehaviour.Varpop => op.Code == Code.Ret ? 1 : 0,
            _ => 0,
        };
        int push = op.StackBehaviourPush switch
        {
            StackBehaviour.Push0 => 0,
            StackBehaviour.Push1_push1 => 2,
            StackBehaviour.Varpush => 0,
            _ => 1,
        };
        return (pop, push);
    }

    /// <summary>
    /// <c>Namespace.Type.Method</c>, attributing async state machines (<c>&lt;Foo&gt;d__3.MoveNext</c>) and
    /// lambdas / local functions (<c>&lt;Foo&gt;b__0_0</c>, <c>&lt;Foo&gt;g__Local|0_0</c>) to the method they were written in.
    /// </summary>
    private static string Attribute(MethodDefinition method)
    {
        TypeDefinition type = method.DeclaringType;
        string name = method.Name;

        while (type.IsNested && type.Name.StartsWith('<'))
        {
            int close = type.Name.IndexOf('>');
            if (type.Name.StartsWith("<>c"))
                name = OriginOf(method.Name) ?? name; // lambda holder: the method name carries the origin
            else if (close > 1)
                name = type.Name[1..close];

            type = type.DeclaringType;
        }

        if (name.StartsWith('<')) name = OriginOf(name) ?? name;
        return $"{type.FullName}.{name}";
    }

    private static string? OriginOf(string compilerGeneratedName)
    {
        int close = compilerGeneratedName.IndexOf('>');
        return compilerGeneratedName.StartsWith('<') && close > 1 ? compilerGeneratedName[1..close] : null;
    }

    // ---- Catch must log or rethrow (source) --------------------------------------------------

    /// <summary>The C# sources of the assembly's project (assembly name = project folder name), without obj/ and bin/.</summary>
    public static IEnumerable<string> SourceFilesOf(Assembly assembly)
    {
        string projectDirectory = Path.Combine(RepositoryRoot(), assembly.GetName().Name!);
        return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                string relative = Path.GetRelativePath(projectDirectory, f);
                return !relative.StartsWith("obj" + Path.DirectorySeparatorChar) && !relative.StartsWith("bin" + Path.DirectorySeparatorChar);
            })
            .Select(File.ReadAllText)
            .ToList();
    }

    /// <summary>Keys (<c>Namespace.Type.Member</c>) of every catch clause, in the given sources, that neither logs nor throws.</summary>
    public static IEnumerable<string> SilentCatchesIn(IEnumerable<string> sources)
    {
        foreach (string source in sources)
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();
            foreach (CatchClauseSyntax clause in root.DescendantNodes().OfType<CatchClauseSyntax>())
            {
                // Documented exemption: `catch (...) when (ex.IsPostgresUniqueViolation…)` turns an
                // EXPECTED database race into a Conflict / idempotent-success result — not an error.
                if (clause.Filter?.FilterExpression.ToString().Contains("IsPostgresUniqueViolation", StringComparison.Ordinal) == true)
                    continue;

                bool handles = clause.Block.DescendantNodes().Any(node =>
                    node is ThrowStatementSyntax or ThrowExpressionSyntax
                    || node is InvocationExpressionSyntax invocation
                    && (_logMethodNames.Contains(InvokedName(invocation)) || IsSerilogStaticLog(invocation)));
                if (!handles) yield return MemberKey(clause);
            }
        }
    }

    private static bool IsSerilogStaticLog(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Log" } } member
        && _serilogMethodNames.Contains(member.Name.Identifier.ValueText);

    private static string InvokedName(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText, // logger?.LogWarning(...)
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => "",
    };

    private static string MemberKey(SyntaxNode node)
    {
        string member = node.Ancestors().Select(a => a switch
        {
            MethodDeclarationSyntax m => m.Identifier.ValueText,
            ConstructorDeclarationSyntax c => c.Identifier.ValueText,
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            _ => null,
        }).FirstOrDefault(name => name != null) ?? "<top-level>";

        string type = string.Join(".", node.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.ValueText));
        string ns = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
        return string.Join(".", new[] { ns, type, member }.Where(part => part.Length > 0));
    }

    private static string RepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Maroik.sln"))) return directory.FullName;
        }

        throw new InvalidOperationException("Maroik.sln not found above " + AppContext.BaseDirectory);
    }
}
