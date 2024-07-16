using Refresh.GameServer.Authentication;
using Refresh.GameServer.Types.Levels;
using Refresh.GameServer.Types.News;
using Refresh.GameServer.Types.UserData;
using RefreshTests.GameServer.Extensions;

namespace RefreshTests.GameServer.Tests.Users;

public class ActivityEndpointsTests : GameServerTest
{
    [Test]
    public void GetNews()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        
        //Team pick a level, so that it appears in the news list
        context.Database.AddTeamPickToLevel(level);

        HttpResponseMessage message = client.GetAsync("/LITTLEBIGPLANETPS3_XML/news").Result;
        Assert.That(message.StatusCode, Is.EqualTo(OK));

        GameNewsResponse response = message.Content.ReadAsXML<GameNewsResponse>();
        Assert.That(response.Subcategory.Items, Has.Count.EqualTo(1));
        Assert.That(response.Subcategory.Items[0].Subject, Is.EqualTo("Team Pick"));
    }

    [Test]
    public void GetRecentActivity()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage message = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/stream").Result;
        Assert.That(message.StatusCode, Is.EqualTo(OK));
        
        //TODO: once we figure out how to parse ActivityPage here, lets do that instead of this mess
        string response = message.Content.ReadAsStringAsync().Result;
        
        //Ensure that the response contains a first login event, and a user
        Assert.That(response, Contains.Substring("<event type=\"firstlogin\">"));
        Assert.That(response, Contains.Substring("<user type=\"user\">"));
    }
    
    [Test]
    public void CantGetRecentActivityWithInvalidTimestamp()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage message = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/stream?timestamp=HAHA").Result;
        Assert.That(message.StatusCode, Is.EqualTo(BadRequest));
        
        message = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/stream?endTimestamp=HAHAHA").Result;
        Assert.That(message.StatusCode, Is.EqualTo(BadRequest));
    }
}