/* using System.Text;
using Refresh.GameServer.Middlewares;

namespace RefreshTests.GameServer.Tests.Middlewares;

public class DigestMiddlewareTests : GameServerTest
{
    [Test]
    public void DoesntIncludeDigestWhenOutsideOfGame()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddMiddleware<DigestMiddleware>();

        HttpResponseMessage response =  context.Http.GetAsync("/api/v3/instance").Result;
        
        Assert.Multiple(() =>
        {
            Assert.That(response.Headers.Contains("X-Digest-A"), Is.False);
            Assert.That(response.Headers.Contains("X-Digest-B"), Is.False);
        });
    }
    
    [Test]
    public void IncludesDigestInGame()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddEndpointGroup<TestEndpoints>();
        context.Server.Value.Server.AddMiddleware<DigestMiddleware>();

        HttpResponseMessage response =  context.Http.GetAsync("/LITTLEBIGPLANETPS3_XML/eula").Result;
        
        Assert.Multiple(() =>
        {
            Assert.That(response.Headers.Contains("X-Digest-A"), Is.True);
            Assert.That(response.Headers.Contains("X-Digest-B"), Is.True);
        });
    }
    
    [Test]
    public void DigestIsCorrect()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddEndpointGroup<TestEndpoints>();
        context.Server.Value.Server.AddMiddleware<DigestMiddleware>();

        const string endpoint = "/LITTLEBIGPLANETPS3_XML/test";
        const string expectedResultStr = "test";
        
        using MemoryStream blankMs = new();
        using MemoryStream expectedResultMs = new(Encoding.ASCII.GetBytes(expectedResultStr));
        
        string serverDigest = DigestMiddleware.CalculateDigest(endpoint, expectedResultMs, "", null, null);
        string clientDigest = DigestMiddleware.CalculateDigest(endpoint, blankMs, "", null, null);

        context.Http.DefaultRequestHeaders.Add("X-Digest-A", clientDigest);
        HttpResponseMessage response =  context.Http.GetAsync(endpoint).Result;
        
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(OK));
            
            Assert.That(response.Headers.Contains("X-Digest-A"), Is.True);
            Assert.That(response.Headers.Contains("X-Digest-B"), Is.True);
            
            Assert.That(response.Headers.GetValues("X-Digest-A").First(), Is.EqualTo(serverDigest));
            Assert.That(response.Headers.GetValues("X-Digest-B").First(), Is.EqualTo(clientDigest));
        });
    }
    
    [Test]
    public void PspDigestIsCorrect()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddEndpointGroup<TestEndpoints>();
        context.Server.Value.Server.AddMiddleware<DigestMiddleware>();

        const string endpoint = "/LITTLEBIGPLANETPS3_XML/test";
        const string expectedResultStr = "test";
        
        using MemoryStream blankMs = new();
        using MemoryStream expectedResultMs = new(Encoding.ASCII.GetBytes(expectedResultStr));
        
        string serverDigest = DigestMiddleware.CalculateDigest(endpoint, expectedResultMs, "", null, null);
        string clientDigest = DigestMiddleware.CalculateDigest(endpoint, blankMs, "", 205, 5);

        context.Http.DefaultRequestHeaders.Add("X-Digest-A", clientDigest);
        context.Http.DefaultRequestHeaders.Add("X-data-v", "5");
        context.Http.DefaultRequestHeaders.Add("X-exe-v", "205");
        HttpResponseMessage response =  context.Http.GetAsync(endpoint).Result;
        
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(OK));
            
            Assert.That(response.Headers.Contains("X-Digest-A"), Is.True);
            Assert.That(response.Headers.Contains("X-Digest-B"), Is.True);
            
            Assert.That(response.Headers.GetValues("X-Digest-A").First(), Is.EqualTo(serverDigest));
            Assert.That(response.Headers.GetValues("X-Digest-B").First(), Is.EqualTo(clientDigest));
        });
    }

    [Test]
    public void FailsWhenDigestIsBad()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddEndpointGroup<TestEndpoints>();
        context.Server.Value.Server.AddMiddleware<DigestMiddleware>();
        
        context.Http.DefaultRequestHeaders.Add("X-Digest-A", "asdf");
        HttpResponseMessage response =  context.Http.GetAsync("/LITTLEBIGPLANETPS3_XML/eula").Result;
        
        Assert.Pass(); // TODO: we have no way of detecting a failed digest check
    }
} */
