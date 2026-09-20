using Maroik.Tests.Shared;
using Microsoft.Extensions.Logging;

namespace Maroik.Website.Tests.Architecture;

/// <summary>
/// The logging architecture rules must be able to FAIL: these run <see cref="LoggingRules"/> against
/// known-bad and known-good code, so a rule that silently stopped detecting (a refactor of the IL walk,
/// a compiler change in how interpolation is emitted) is caught here rather than passing forever.
/// </summary>
public class LoggingRulesSelfTests
{
    // ---- literal templates: compiled fixtures ------------------------------------------------

    private static class Fixtures
    {
        public static void Literal(ILogger l, string x) => l.LogInformation("ok {X}", x);
        public static void LiteralWithException(ILogger l, Exception e, string x) => l.LogError(e, "ok {X} and {Y}", x, x);
        public static void LiteralNoArgs(ILogger l) => l.LogWarning("nothing to interpolate");
        public static void ConstantConcatenation(ILogger l) => l.LogWarning("a" + "b {X}", 1);
        public static void Interpolated(ILogger l, string x) => l.LogInformation($"bad {x}");
        public static void InterpolatedWithException(ILogger l, Exception e, string x) => l.LogError(e, $"bad {x}");
        public static void Concatenated(ILogger l, string x) => l.LogWarning("bad " + x);
        public static void Formatted(ILogger l, string x) => l.LogWarning(string.Format("bad {0}", x));
        public static void Variable(ILogger l, string template, string x) => l.LogWarning(template, x);
        public static void SerilogLiteral(string x) => Serilog.Log.Warning("ok {X}", x);
        public static void SerilogInterpolated(string x) => Serilog.Log.Warning($"bad {x}");
    }

    private static List<string> Violations() => LoggingRules
        .NonLiteralTemplates(LoggingRules.MethodsOf(typeof(LoggingRulesSelfTests).Assembly))
        .Where(v => v.Method.Contains(nameof(LoggingRulesSelfTests) + "/" + nameof(Fixtures), StringComparison.Ordinal))
        .Select(v => v.Method[(v.Method.LastIndexOf('.') + 1)..])
        .ToList();

    [Theory]
    [InlineData(nameof(Fixtures.Interpolated))]
    [InlineData(nameof(Fixtures.InterpolatedWithException))]
    [InlineData(nameof(Fixtures.Concatenated))]
    [InlineData(nameof(Fixtures.Formatted))]
    [InlineData(nameof(Fixtures.Variable))]
    [InlineData(nameof(Fixtures.SerilogInterpolated))]
    public void TemplateRule_FlagsANonLiteralMessage(string fixture) => Assert.Contains(fixture, Violations());

    [Theory]
    [InlineData(nameof(Fixtures.Literal))]
    [InlineData(nameof(Fixtures.LiteralWithException))]
    [InlineData(nameof(Fixtures.LiteralNoArgs))]
    [InlineData(nameof(Fixtures.ConstantConcatenation))]
    [InlineData(nameof(Fixtures.SerilogLiteral))]
    public void TemplateRule_AcceptsALiteralMessage(string fixture) => Assert.DoesNotContain(fixture, Violations());

    // ---- catch must log or rethrow: sources --------------------------------------------------

    private static List<string> Silent(string catchClause) => LoggingRules.SilentCatchesIn(
    [
        "namespace Demo.Ns; public class Svc { public async System.Threading.Tasks.Task Run() { try { } " + catchClause + " } }",
    ]).ToList();

    [Theory]
    [InlineData("catch (System.Exception) { }")]
    [InlineData("catch { }")]
    [InlineData("catch (System.Exception e) { var x = e.Message; }")]
    [InlineData("catch (System.Exception) { await System.Threading.Tasks.Task.Delay(1); }")]
    [InlineData("catch (System.Exception e) { Helper(e); }")]
    public void CatchRule_FlagsACatchThatNeitherLogsNorThrows(string catchClause) =>
        Assert.Equal(["Demo.Ns.Svc.Run"], Silent(catchClause));

    [Theory]
    [InlineData("catch (System.Exception e) { logger.LogError(e, \"x\"); }")]
    [InlineData("catch (System.Exception e) { logger?.LogWarning(e, \"x\"); }")]
    [InlineData("catch (System.Exception e) { Log.Fatal(e, \"x\"); }")]
    [InlineData("catch (System.Exception e) { Log.Fatal(e, \"x\"); throw; }")]
    [InlineData("catch (System.Exception e) { throw new System.InvalidOperationException(\"x\", e); }")]
    [InlineData("catch (System.Exception) { await rollback(); throw; }")]
    [InlineData("catch (System.Exception e) when (e.IsPostgresUniqueViolation()) { }")]
    [InlineData("catch (System.Exception e) when (e.IsPostgresUniqueViolationOn(\"c\")) { return; }")]
    public void CatchRule_AcceptsACatchThatLogsThrowsOrTranslatesAUniqueViolation(string catchClause) =>
        Assert.Empty(Silent(catchClause));

    [Fact]
    public void CatchRule_KeysNestedTypesAndConstructors()
    {
        const string source = "namespace A.B { class Outer { class Inner { Inner() { try { } catch { } } } } }";

        Assert.Equal(["A.B.Outer.Inner.Inner"], LoggingRules.SilentCatchesIn([source]));
    }
}
