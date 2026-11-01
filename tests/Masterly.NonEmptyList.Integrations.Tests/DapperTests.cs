using System.Data;
using Dapper;
using Masterly.NonEmptyList.Dapper;
using Microsoft.Data.Sqlite;

namespace Masterly.NonEmptyList.Integrations.Tests;

[Collection("Dapper")]
public class DapperTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DapperTests()
    {
        NonEmptyDapper.AddTypeHandlers<string>();
        NonEmptyDapper.AddTypeHandlers<int>();
        NonEmptyDapper.AddDictionaryHandler<string, int>();
        NonEmptyDapper.AddConstrainedHandler<int, AtLeast<Two>>();
        NonEmptyDapper.AddNonEmptyStringHandler();
        NonEmptyDapper.AddTypeHandlersFor<ProductRow>();

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _connection.Execute("CREATE TABLE Products (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Tags TEXT, Prices TEXT, Codes TEXT, Limits TEXT, Pair TEXT)");
    }

    public void Dispose() => _connection.Dispose();

    public sealed class ProductRow
    {
        public long Id { get; set; }
        public NonEmptyString Name { get; set; }
        public NonEmptyList<string>? Tags { get; set; }
        public ImmutableNonEmptyList<int>? Prices { get; set; }
        public NonEmptySet<string>? Codes { get; set; }
        public NonEmptyDictionary<string, int>? Limits { get; set; }
        public ConstrainedList<int, AtLeast<Two>>? Pair { get; set; }
    }

    [Fact]
    public void InsertAndRead_ShouldRoundTripJsonColumns()
    {
        ProductRow row = new()
        {
            Id = 1,
            Name = new NonEmptyString("Widget"),
            Tags = NonEmptyList.Of("a", "b"),
            Prices = ImmutableNonEmptyList.Of(10, 20),
            Codes = NonEmptySet.Of("x", "y"),
            Limits = NonEmptyDictionary.OfPairs(("gold", 1), ("silver", 2)),
            Pair = ConstrainedList<int, AtLeast<Two>>.From(new[] { 3, 4 }),
        };

        _connection.Execute("INSERT INTO Products (Id, Name, Tags, Prices, Codes, Limits, Pair) VALUES (@Id, @Name, @Tags, @Prices, @Codes, @Limits, @Pair)", row);
        ProductRow back = _connection.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = 1");
        string rawTags = _connection.ExecuteScalar<string>("SELECT Tags FROM Products WHERE Id = 1")!;

        Assert.Equal("[\"a\",\"b\"]", rawTags);
        Assert.Equal("Widget", back.Name.Value);
        Assert.Equal(row.Tags, back.Tags);
        Assert.Equal(row.Prices, back.Prices);
        Assert.Equal(row.Codes, back.Codes);
        Assert.Equal(row.Limits, back.Limits);
        Assert.Equal(row.Pair, back.Pair);
    }

    [Fact]
    public void NullColumns_ShouldReadAsNull()
    {
        _connection.Execute("INSERT INTO Products (Id, Name) VALUES (2, 'Plain')");

        ProductRow back = _connection.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = 2");

        Assert.Null(back.Tags);
        Assert.Null(back.Prices);
        Assert.Null(back.Codes);
        Assert.Null(back.Limits);
        Assert.Null(back.Pair);
        Assert.Equal("Plain", back.Name.Value);
    }

    [Fact]
    public void EmptyOrInvalidColumns_ShouldThrowDataException()
    {
        _connection.Execute("INSERT INTO Products (Id, Name, Tags) VALUES (3, 'Bad', '[]')");
        _connection.Execute("INSERT INTO Products (Id, Name, Tags) VALUES (4, 'Bad', '   ')");
        _connection.Execute("INSERT INTO Products (Id, Name, Tags) VALUES (5, '  ', '[\"ok\"]')");
        _connection.Execute("INSERT INTO Products (Id, Name, Pair) VALUES (6, 'Bad', '[1]')");

        Assert.ThrowsAny<DataException>(() => _connection.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = 3"));
        Assert.ThrowsAny<DataException>(() => _connection.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = 4"));
        Assert.ThrowsAny<DataException>(() => _connection.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = 5"));
        Assert.ThrowsAny<DataException>(() => _connection.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = 6"));
    }

    [Fact]
    public void ScalarQueries_ShouldUseHandlers()
    {
        _connection.Execute("INSERT INTO Products (Id, Name, Tags) VALUES (7, 'Scalar', @Tags)", new { Tags = NonEmptyList.Of("only") });

        NonEmptyList<string> tags = _connection.QuerySingle<NonEmptyList<string>>("SELECT Tags FROM Products WHERE Id = 7");
        NonEmptyString name = _connection.QuerySingle<NonEmptyString>("SELECT Name FROM Products WHERE Id = 7");

        Assert.Equal("only", tags.Head);
        Assert.Equal("Scalar", name.Value);
    }

    [Fact]
    public void Handlers_ShouldBeIdempotentAndHandleParameters()
    {
        NonEmptyDapper.AddTypeHandlers<string>();
        NonEmptyDapper.AddNonEmptyStringHandler();

        NonEmptyJsonTypeHandler<NonEmptyList<int>> handler = new();
        SqliteParameter parameter = new();
        handler.SetValue(parameter, null);
        Assert.Equal(DBNull.Value, parameter.Value);
        handler.SetValue(parameter, NonEmptyList.Of(1));
        Assert.Equal("[1]", parameter.Value);
        Assert.Null(handler.Parse(DBNull.Value));
        Assert.Equal(1, handler.Parse(System.Text.Encoding.UTF8.GetBytes("[1]"))!.Head);
        Assert.Throws<DataException>(() => handler.Parse("not json"));

        NonEmptyStringTypeHandler stringHandler = new();
        stringHandler.SetValue(parameter, default);
        Assert.Equal(DBNull.Value, parameter.Value);
        stringHandler.SetValue(parameter, new NonEmptyString("v"));
        Assert.Equal("v", parameter.Value);
        Assert.Throws<DataException>(() => stringHandler.Parse(DBNull.Value));
    }

    private sealed class NullText
    {
        public override string? ToString() => null;
    }

    [Fact]
    public void Parse_ShouldAcceptAnyValueWhoseTextIsJson_AndRejectJsonNull()
    {
        NonEmptyJsonTypeHandler<NonEmptyList<int>> handler = new();

        Assert.Equal(new[] { 1, 2 }, handler.Parse(new System.Text.StringBuilder("[1,2]"))!.ToArray());
        DataException blank = Assert.Throws<DataException>(() => handler.Parse(new NullText()));
        DataException jsonNull = Assert.Throws<DataException>(() => handler.Parse("null"));

        Assert.Contains("is empty", blank.Message);
        Assert.Contains("deserialized to null", jsonNull.Message);
    }

    public sealed class AuditRow
    {
        public long Id { get; set; }
        public NonEmptyString? Author { get; set; }
        public NonEmptyList<Guid>? Ids { get; set; }
        public Readings? Readings { get; set; }
        public List<int>? Plain { get; set; }
        public string? Note { get; set; }
    }

    [Fact]
    public void AddTypeHandlersFor_ShouldRegisterEachNewTypeOnce_IncludingDerivedCollections()
    {
        NonEmptyDapper.AddTypeHandlersFor<AuditRow>();
        NonEmptyDapper.AddTypeHandlersFor<AuditRow>();
        _connection.Execute("CREATE TABLE Audit (Id INTEGER PRIMARY KEY, Author TEXT, Ids TEXT, Readings TEXT)");
        Guid id = Guid.NewGuid();

        _connection.Execute("INSERT INTO Audit (Id, Author, Ids, Readings) VALUES (1, @Author, @Ids, @Readings)",
            new { Author = new NonEmptyString("ada"), Ids = NonEmptyList.Of(id), Readings = Readings.From(new[] { 3, 1 }) });
        _connection.Execute("INSERT INTO Audit (Id, Readings) VALUES (2, '[5,5]')");
        AuditRow back = _connection.QuerySingle<AuditRow>("SELECT * FROM Audit WHERE Id = 1");

        Assert.Equal("ada", back.Author!.Value.Value);
        Assert.Equal(id, back.Ids!.Head);
        Assert.Equal(new[] { 3, 1 }, back.Readings!.ToArray());
        // The generated collection re-checks Distinct while reading, so a duplicate stored by hand fails loudly.
        Assert.ThrowsAny<DataException>(() => _connection.QuerySingle<AuditRow>("SELECT * FROM Audit WHERE Id = 2"));
    }
}
