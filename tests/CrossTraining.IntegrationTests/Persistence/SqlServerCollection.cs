namespace CrossTraining.IntegrationTests.Persistence;

/// <summary>
/// O singura instanta de <see cref="SqlServerFixture"/> pentru toate clasele care ating
/// baza de date.
///
/// Cu IClassFixture, fiecare clasa ar primi propria instanta, iar fixture-ul recreeaza
/// schema cu DROP TABLE: doua clase pornite in paralel ar sterge tabelele sub
/// interogarile celeilalte, ceea ce se manifesta ca deadlock in SQL Server. Clasele
/// dintr-o colectiune xUnit ruleaza secvential, deci problema dispare, iar schema se
/// creeaza o singura data pe rulare.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "sql-server";
}
