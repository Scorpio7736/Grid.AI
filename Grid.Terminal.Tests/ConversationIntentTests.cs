using Grid.Agent;
using Grid.Simulation.Tasks;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class ConversationIntentTests
{
    [Theory]
    [InlineData("{\"version\":2,\"kind\":\"ReadFile\",\"scope\":\"demo\",\"target\":\"a.txt\",\"text\":\"\"}")]
    [InlineData("{\"version\":1,\"kind\":\"0\",\"scope\":\"demo\",\"target\":\"a.txt\",\"text\":\"\"}")]
    [InlineData("{\"version\":1,\"kind\":\"CreateFile\",\"scope\":\"demo\",\"target\":\"a.txt\",\"text\":\"\",\"approved\":true}")]
    [InlineData("{\"version\":1,\"kind\":\"Shell\",\"scope\":\"\",\"target\":\"\",\"text\":\"whoami\"}")]
    [InlineData("{\"version\":1,\"kind\":\"ReadFile\",\"scope\":\"\",\"target\":\"a.txt\",\"text\":\"\"}")]
    [InlineData("{\"version\":1,\"kind\":\"Clarify\",\"scope\":\"\",\"target\":\"\",\"text\":\"\"}")]
    public void RejectsForgedOrAmbiguousIntent(string json) => Assert.Throws<ArgumentException>(() => ConversationIntent.Parse(json));

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void PlansValidateExplicitVersion(int version, bool valid)
    {
        var json = "{\"version\":" + version + ",\"steps\":[{\"agent\":\"VOLT\",\"capability\":\"volt.status\",\"argument\":\"current\"}]}";
        if (valid) Assert.Equal(1, AssistanceProposal.Parse(json).Version);
        else Assert.Throws<ArgumentException>(() => AssistanceProposal.Parse(json));
    }
}
