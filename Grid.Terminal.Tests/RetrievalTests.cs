using System.IO.Compression;
using Grid.AI;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class RetrievalTests
{
    private static RetrievalDocument Source(string text, string name = "notes.txt") => new(Guid.NewGuid(), new('A', 64), name, "utf8", text, DateTimeOffset.UtcNow);
    [Fact]
    public void DeepRelevantTextIsRetrievedWithVerifiableOffsetsAndHashes()
    {
        var source = Source(string.Concat(Enumerable.Repeat("Ordinary background information.\n", 200)) + "The deployment marker is DEEP_COBALT42.\n");
        using var index = new SqliteRetrievalIndex();
        var hit = index.Search("scope", [source], "deployment marker").First();
        Assert.True(hit.Chunk.Start > 700); Assert.Contains("DEEP_COBALT42", hit.Chunk.Text);
        Assert.Equal(source.Text.Substring(hit.Chunk.Start, hit.Chunk.Length), hit.Chunk.Text);
        Assert.Equal(RetrievalText.Hash(source.Text), hit.Chunk.TextHash); Assert.Equal(RetrievalText.Hash(hit.Chunk.Text), hit.Chunk.ChunkHash);
        Assert.Equal(source.Id, hit.Chunk.DocumentId); Assert.Equal(source.SourceHash, hit.Chunk.SourceHash);
    }
    [Fact]
    public void ReusingIndexCannotExposePreviousCollectionOrRemovedVersion()
    {
        using var index = new SqliteRetrievalIndex(); var first = Source("privatealpha secret");
        Assert.NotEmpty(index.Search("one", [first], "privatealpha"));
        Assert.Empty(index.Search("two", [], "privatealpha"));
        var changed = first with { Text = "newbeta updated", SourceHash = new('B', 64) };
        Assert.Empty(index.Search("one", [changed], "privatealpha"));
        Assert.Equal(new string('B', 64), index.Search("one", [changed], "newbeta").First().Chunk.SourceHash);
    }
    [Fact]
    public void ChangedExtractionWithSameSourceHashHasNewTextIdentity()
    {
        var first = Source("alpha first"); using var index = new SqliteRetrievalIndex();
        var old = index.Search("scope", [first], "alpha").First();
        var updated = index.Search("scope", [first with { Text = "alpha corrected" }], "alpha").First();
        Assert.NotEqual(old.Chunk.TextHash, updated.Chunk.TextHash); Assert.NotEqual(old.Chunk.ChunkHash, updated.Chunk.ChunkHash);
    }
    [Fact]
    public void NoLexicalMatchRequiresExplicitOverviewAndDoesNotBecomeSemanticMatch()
    {
        using var index = new SqliteRetrievalIndex(); var source = Source("banana orchard");
        Assert.Empty(index.Search("scope", [source], "automobiles"));
        var overview = Assert.Single(index.Search("scope", [source], "analyze document", allowOverview: true));
        Assert.True(overview.Overview); Assert.Equal(0, overview.Score);
    }
    [Fact]
    public void QueryAndScopeAreSqlParameters()
    {
        using var index = new SqliteRetrievalIndex();
        Assert.NotEmpty(index.Search("x'; DROP TABLE chunks; --", [Source("unique retrieval marker")], "unique' OR 1=1 --"));
        Assert.Empty(index.Search("next", [], "unique"));
    }
    [Fact]
    public void EmbeddingsAreDeterministicAndNormalized()
    {
        var embedding = new FeatureHashEmbedding(); var a = embedding.Embed("alpha beta beta");
        Assert.Equal(a, embedding.Embed("alpha beta beta")); Assert.Equal(128, a.Length);
        Assert.InRange(RetrievalText.Cosine(a, a), .9999, 1); Assert.All(embedding.Embed("the and"), v => Assert.Equal(0, v));
    }
    [Fact]
    public void ChunkingRespectsPdfMarkersAndDoesNotSplitSurrogatePairs()
    {
        var text = "[PDF page 1 of 2]\n" + new string('a', 580) + "🙂\n[PDF page 2 of 2]\nTARGET";
        var chunks = RetrievalText.Chunk(Source(text) with { Format = "pdf-pages" });
        Assert.Contains(chunks, c => c.Page == 2 && c.Text.Contains("TARGET"));
        Assert.All(chunks, c => { Assert.False(char.IsLowSurrogate(c.Text[0])); Assert.False(char.IsHighSurrogate(c.Text[^1])); });
    }
    [Fact]
    public void OversizedAndDuplicateCapturesAreRefused()
    {
        using var index = new SqliteRetrievalIndex(); var source = Source("alpha");
        Assert.Throws<ArgumentException>(() => index.Search("scope", [source, source], "alpha"));
        Assert.Throws<ArgumentException>(() => index.Search("scope", [Source(new('x', 256 * 1024 + 1))], "alpha"));
    }
    [Theory]
    [InlineData("pdf")][InlineData("docx")]
    public void OfflineDocumentCapturesAreSearchableAfterOriginalIsDeleted(string format)
    {
        var root = Path.Combine(Path.GetTempPath(), "grid-rag-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "capture." + format);
            if (format == "pdf")
            {
                var pdf = new PdfDocumentBuilder(); var font = pdf.AddStandard14Font(Standard14Font.Helvetica);
                pdf.AddPage(400, 500).AddText("First page", 12, new PdfPoint(20, 450), font);
                pdf.AddPage(400, 500).AddText("Retrieval marker ZEBRA42", 12, new PdfPoint(20, 450), font);
                File.WriteAllBytes(path, pdf.Build());
            }
            else
            {
                using var zip = ZipFile.Open(path, ZipArchiveMode.Create); using var writer = new StreamWriter(zip.CreateEntry("word/document.xml").Open());
                writer.Write("<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p><w:r><w:t>Retrieval marker ZEBRA42</w:t></w:r></w:p></w:body></w:document>");
            }
            var captured = LocalContextReader.Read(path, "docs", null); File.Delete(path);
            var hit = ContextRetrieval.Search([captured], "docs", "ZEBRA42").First();
            Assert.Contains("ZEBRA42", hit.Chunk.Text); Assert.Equal(captured.SourceHash, hit.Chunk.SourceHash);
            if (format == "pdf") Assert.Equal(2, hit.Chunk.Page); else Assert.Equal("docx-body", hit.Chunk.Format);
        }
        finally { Directory.Delete(root, true); }
    }
    private sealed class Model : ILocalInference
    {
        public string Prompt = "";
        public TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource? Release;
        public async Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
        { Prompt = prompt; Entered.TrySetResult(); if (Release is not null) await Release.Task.WaitAsync(token); return new(true, "PRIVATE_RESPONSE", "test"); }
    }
    private sealed class Host
    {
        public TerminalIdentitySession Owner = new(() => "retrieval-owner");
        public GridSimulationEngine Engine;
        public CommandDispatcher Commands;
        public Model Model = new();
        public Host()
        {
            var sessions = new TerminalSessionRegistry(); sessions.Add(Owner);
            Engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: sessions); Owner.Attach(Engine); Owner.Login(); Engine.StartAgents(Owner.Reference);
            Commands = DefaultCommands.Create(Engine); ContextCommands.Register(Commands, Engine, Owner); ChatCommands.Register(Commands, Engine, Owner, Model);
            Commands.UseIdentity(Owner); Commands.Execute("connect MCP -inline"); Commands.Execute("memory scope grid");
        }
        public ContextDocument Add(string text, string project = "grid", string role = "MCP")
        {
            var document = new ContextDocument(Guid.NewGuid(), project, "notes.txt", new('A', 64), text.Length, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "utf8", text, Engine.ResolveProgram(role).Id);
            Engine.AddContext(Owner.Reference, document, false); return document;
        }
    }
    [Fact]
    public async Task AssistanceRetrievesDeepChunksWithoutAnotherAgentOrProject()
    {
        var host = new Host(); host.Add(new string('x', 5000) + "\nmarker DEEP42");
        host.Add("marker OTHER_AGENT", role: "JN"); host.Add("marker OTHER_PROJECT", project: "other");
        var command = host.Commands.Execute("assist analyze marker"); var reply = await command.BrokerOperation!(default);
        Assert.Contains("DEEP42", host.Model.Prompt); Assert.DoesNotContain("OTHER_AGENT", host.Model.Prompt); Assert.DoesNotContain("OTHER_PROJECT", host.Model.Prompt);
        Assert.Contains("source ", reply.Text); Assert.Contains("lines ", reply.Text);
    }
    [Fact]
    public async Task RevocationDuringInferenceDiscardsRetrievedAnswer()
    {
        var host = new Host(); var document = host.Add("marker PRIVATE"); host.Model.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = host.Commands.Execute("assist analyze marker").BrokerOperation!(default);
        await host.Model.Entered.Task; host.Engine.ChangeContext(host.Owner.Reference, document.Id, false); host.Model.Release.TrySetResult();
        var reply = await operation; Assert.DoesNotContain("PRIVATE_RESPONSE", reply.Text);
        Assert.Empty(ContextRetrieval.Search(host.Engine.ContextFor(host.Engine.ResolveProgram("MCP").Id, host.Owner.SessionId), "grid", "marker"));
    }
    [Fact]
    public void SearchCommandRequiresProjectAndUsesCapturedVersions()
    {
        var host = new Host(); host.Add("querymarker CURRENT"); host.Add("querymarker HIDDEN", project: "other");
        var reply = host.Commands.Execute("context search grid querymarker").Text;
        Assert.Contains("CURRENT", reply); Assert.DoesNotContain("HIDDEN", reply);
        Assert.Contains("not reopened", reply);
    }
    [Fact]
    public void SavedCapturesRebuildRetrievalAfterRestartWithoutSourceFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "grid-rag-save-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var host = new Host(); var agent = host.Engine.ResolveProgram("MCP").Id;
            var document = new ContextDocument(Guid.NewGuid(), "grid", "missing-source.txt", new('A', 64), 15, DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow, "utf8", "savedmarker persisted", agent);
            host.Engine.AddContext(host.Owner.Reference, document, true);
            var path = Path.Combine(root, "world.db");
            using (var store = new WorldStore(path)) store.Create(new(host.Engine.GetSnapshot().WorldId, "retrieval", DateTimeOffset.UtcNow), host.Engine.GetSnapshot());
            using var reopened = new WorldStore(path); var restored = GridSimulationEngine.Restore(reopened.Load().State);
            var hit = ContextRetrieval.Search(restored.ContextFor(agent, Guid.NewGuid()), "grid", "savedmarker").First();
            Assert.Equal(document.Id, hit.Chunk.DocumentId); Assert.Contains("persisted", hit.Chunk.Text);
        }
        finally { Directory.Delete(root, true); }
    }
}
