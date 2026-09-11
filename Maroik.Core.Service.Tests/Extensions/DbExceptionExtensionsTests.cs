using Maroik.Core.Service.Extensions;

namespace Maroik.Core.Service.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="DbExceptionExtensions"/>.
/// Covers the reflective <c>SqlState</c>/<c>Code</c>/<c>ConstraintName</c> property checks (so the
/// classification works against a real Npgsql <c>PostgresException</c> without this project taking a
/// hard dependency on Npgsql), the message-substring fallback for exceptions that no longer carry
/// those typed properties (wrapped/rethrown/logged), and walking the <c>InnerException</c> chain.
/// </summary>
public class DbExceptionExtensionsTests
{
    // A stand-in for Npgsql's PostgresException: same property names/shapes, read reflectively by
    // the extensions under test, without requiring the Npgsql package here.
    private sealed class FakePostgresException(string message, string? sqlState = null, string? constraintName = null)
        : Exception(message)
    {
        public string? SqlState { get; } = sqlState;
        public string? ConstraintName { get; } = constraintName;
    }

    // -- IsPostgresUniqueViolation ---------------------------------------------

    /// <summary>A PostgresException-shaped exception with SqlState 23505 is recognized.</summary>
    [Fact]
    public void IsPostgresUniqueViolation_ReturnsTrue_WhenSqlStateIs23505()
    {
        var ex = new FakePostgresException("duplicate key value violates unique constraint", sqlState: "23505");

        Assert.True(ex.IsPostgresUniqueViolation());
    }

    /// <summary>A different SQLSTATE (e.g. a check-constraint violation, 23514) is not a unique violation.</summary>
    [Fact]
    public void IsPostgresUniqueViolation_ReturnsFalse_WhenSqlStateIsDifferent()
    {
        var ex = new FakePostgresException("new row violates check constraint", sqlState: "23514");

        Assert.False(ex.IsPostgresUniqueViolation());
    }

    /// <summary>A plain exception with no SqlState/Code property at all is not a unique violation.</summary>
    [Fact]
    public void IsPostgresUniqueViolation_ReturnsFalse_ForUnrelatedException()
    {
        var ex = new InvalidOperationException("boom");

        Assert.False(ex.IsPostgresUniqueViolation());
    }

    /// <summary>
    /// A wrapped/rethrown exception that lost its typed SqlState property still matches via the
    /// SQLSTATE-substring fallback in its message.
    /// </summary>
    [Fact]
    public void IsPostgresUniqueViolation_ReturnsTrue_WhenSqlStateSubstringInMessage()
    {
        var ex = new Exception("duplicate key value violates unique constraint \"Calendar_pk\" (23505)");

        Assert.True(ex.IsPostgresUniqueViolation());
    }

    /// <summary>The unique-violation SQLSTATE on an inner exception is found by walking the chain.</summary>
    [Fact]
    public void IsPostgresUniqueViolation_ReturnsTrue_WhenViolationIsOnInnerException()
    {
        var inner = new FakePostgresException("duplicate key", sqlState: "23505");
        var outer = new Exception("outer wrapper", inner);

        Assert.True(outer.IsPostgresUniqueViolation());
    }

    /// <summary>Neither the outer exception, its message, nor the inner exception's message match.</summary>
    [Fact]
    public void IsPostgresUniqueViolation_ReturnsFalse_WhenNoExceptionInChainMatches()
    {
        var inner = new InvalidOperationException("inner failure");
        var outer = new Exception("outer wrapper", inner);

        Assert.False(outer.IsPostgresUniqueViolation());
    }

    // -- IsPostgresUniqueViolationOn --------------------------------------------

    /// <summary>A unique violation whose typed ConstraintName matches the requested one is recognized.</summary>
    [Fact]
    public void IsPostgresUniqueViolationOn_ReturnsTrue_WhenConstraintNameMatches()
    {
        var ex = new FakePostgresException("duplicate key", sqlState: "23505", constraintName: "Calendar_AccountEmail_Name_unique");

        Assert.True(ex.IsPostgresUniqueViolationOn("Calendar_AccountEmail_Name_unique"));
    }

    /// <summary>
    /// A unique violation on a *different* constraint (e.g. the surrogate-key PK, which can collide
    /// if its sequence ever falls behind existing rows) must not be misreported as the named
    /// business-key constraint.
    /// </summary>
    [Fact]
    public void IsPostgresUniqueViolationOn_ReturnsFalse_WhenConstraintNameDiffers()
    {
        var ex = new FakePostgresException("duplicate key", sqlState: "23505", constraintName: "Calendar_pk");

        Assert.False(ex.IsPostgresUniqueViolationOn("Calendar_AccountEmail_Name_unique"));
    }

    /// <summary>A non-unique-violation exception never matches, regardless of constraint name.</summary>
    [Fact]
    public void IsPostgresUniqueViolationOn_ReturnsFalse_WhenNotAUniqueViolation()
    {
        var ex = new FakePostgresException("check violated", sqlState: "23514", constraintName: "Calendar_AccountEmail_Name_unique");

        Assert.False(ex.IsPostgresUniqueViolationOn("Calendar_AccountEmail_Name_unique"));
    }

    /// <summary>
    /// A wrapped exception with no typed ConstraintName still matches via the constraint-name
    /// substring fallback in its message, as long as the SQLSTATE is also present.
    /// </summary>
    [Fact]
    public void IsPostgresUniqueViolationOn_ReturnsTrue_WhenConstraintNameSubstringInMessage()
    {
        var ex = new Exception("duplicate key value violates unique constraint \"Calendar_AccountEmail_Name_unique\" (23505)");

        Assert.True(ex.IsPostgresUniqueViolationOn("Calendar_AccountEmail_Name_unique"));
    }

    /// <summary>The substring fallback does not match a different constraint's name.</summary>
    [Fact]
    public void IsPostgresUniqueViolationOn_ReturnsFalse_WhenDifferentConstraintNameSubstringInMessage()
    {
        var ex = new Exception("duplicate key value violates unique constraint \"Calendar_pk\" (23505)");

        Assert.False(ex.IsPostgresUniqueViolationOn("Calendar_AccountEmail_Name_unique"));
    }

    /// <summary>The named constraint is found by walking the InnerException chain.</summary>
    [Fact]
    public void IsPostgresUniqueViolationOn_ReturnsTrue_WhenMatchIsOnInnerException()
    {
        var inner = new FakePostgresException("duplicate key", sqlState: "23505", constraintName: "Account_Nickname_unique");
        var outer = new Exception("outer wrapper", inner);

        Assert.True(outer.IsPostgresUniqueViolationOn("Account_Nickname_unique"));
    }
}
