using LabTrack.Core;
using Xunit;

namespace LabTrack.Tests;

public sealed class StoreTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "LabTrack-" + Guid.NewGuid());
    private readonly SampleStore store;
    private static Sample Valid(string code = "W-001") => new(0, code, "Water", "Shelf A", "Received", "Demo");
    public StoreTests() => store = new SampleStore(Path.Combine(folder, "samples.db"));
    public void Dispose() => Directory.Delete(folder, true);

    [Fact] public void NewDatabaseIsEmpty() => Assert.Empty(store.List());
    [Fact] public void DataSurvivesReopening() { store.Save(Valid()); Assert.Equal("W-001", new SampleStore(Path.Combine(folder, "samples.db")).List().Single().Code); }
    [Fact] public void UpdatesPreserveIdentity() { var id = store.Save(Valid()); Assert.Equal(id, store.Save(Valid() with { Id = id, Status = "Completed" })); Assert.Equal("Completed", store.List().Single().Status); }
    [Fact] public void DeletePersists() { store.Delete(store.Save(Valid())); Assert.Empty(new SampleStore(Path.Combine(folder, "samples.db")).List()); }
    [Fact] public void MissingUpdateDoesNotInsert() { Assert.Throws<InvalidOperationException>(() => store.Save(Valid() with { Id = 99 })); Assert.Empty(store.List()); }
    [Fact] public void MissingDeleteIsReported() => Assert.Throws<InvalidOperationException>(() => store.Delete(99));
    [Fact] public void DuplicateCodeIsCaseInsensitive() { store.Save(Valid()); Assert.Throws<ArgumentException>(() => store.Save(Valid("w-001"))); Assert.Single(store.List()); }
    [Fact] public void DuplicateUpdateLeavesOriginalIntact() { store.Save(Valid()); var id = store.Save(Valid("W-002")); Assert.Throws<ArgumentException>(() => store.Save(Valid() with { Id = id })); Assert.Equal("W-002", store.List().Single(s => s.Id == id).Code); }
    [Theory] [InlineData("")] [InlineData("   ")] [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void RejectsInvalidCode(string code) { Assert.Throws<ArgumentException>(() => store.Save(Valid(code))); Assert.Empty(store.List()); }
    [Fact] public void RejectsMissingType() => Assert.Throws<ArgumentException>(() => store.Save(Valid() with { Kind = " " }));
    [Fact] public void RejectsMissingLocation() => Assert.Throws<ArgumentException>(() => store.Save(Valid() with { Location = "" }));
    [Fact] public void RejectsUnknownStatus() => Assert.Throws<ArgumentException>(() => store.Save(Valid() with { Status = "Unknown" }));
    [Fact] public void RejectsOverlongNotes() => Assert.Throws<ArgumentException>(() => store.Save(Valid() with { Notes = new string('a', 1001) }));
    [Fact] public void TrimsInputs() { store.Save(Valid(" W-001 ")); Assert.Equal("W-001", store.List().Single().Code); }
    [Fact] public void FiltersStatusAndSearchTogether() { store.Save(Valid()); store.Save(Valid("W-002") with { Status = "Completed" }); Assert.Single(store.List("shelf a", "Completed")); Assert.Empty(store.List("missing", "Completed")); }
    [Fact] public void SearchTreatsSqlAndWildcardsAsText() { store.Save(Valid()); Assert.Empty(store.List("%' OR 1=1 --")); Assert.Empty(store.List("%")); Assert.Single(store.List()); }
    [Fact] public void ApostrophesAndUnicodeRoundTrip() { store.Save(Valid("Mẫu-01") with { Notes = "O'Brien's sample" }); Assert.Equal("O'Brien's sample", store.List().Single().Notes); }
    [Fact] public void CsvEscapesQuotesCommaAndNewline() { var csv = SampleStore.Csv([Valid() with { Notes = "a,\"b\"\nc" }]); Assert.Contains("\"a,\"\"b\"\"\nc\"", csv); }
    [Theory] [InlineData("=1+1")] [InlineData("+cmd")] [InlineData("-1")] [InlineData("@SUM(A1)")] [InlineData("  =1+1")] [InlineData("\t=1")]
    public void CsvNeutralizesFormulas(string value) => Assert.Contains("\"'" + value + "\"", SampleStore.Csv([Valid() with { Notes = value }]));
    [Fact] public void EmptyExportStillHasHeader() => Assert.Equal("Id,Code,Type,Location,Status,Notes\r\n", SampleStore.Csv([]));
}
