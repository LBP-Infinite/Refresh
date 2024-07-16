using Refresh.GameServer.Authentication;
using Refresh.GameServer.Types.Comments;
using Refresh.GameServer.Types.Levels;
using Refresh.GameServer.Types.Lists;
using Refresh.GameServer.Types.UserData;
using RefreshTests.GameServer.Extensions;

namespace RefreshTests.GameServer.Tests.Comments;

public class LevelCommentTests : GameServerTest
{
    [Test]
    public void PostAndDeleteLevelComment()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        GameComment comment = new()
        {
            Author = user,
            Content = "This is a test comment!",
        };

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/postComment/user/{level.LevelId}", new StringContent(comment.AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));

        response = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/comments/user/{level.LevelId}").Result;
        SerializedCommentList userComments = response.Content.ReadAsXML<SerializedCommentList>();
        Assert.That(userComments.Items, Has.Count.EqualTo(1));
        Assert.That(userComments.Items[0].Content, Is.EqualTo(comment.Content));
        
        response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteComment/user/{level.LevelId}?commentId={userComments.Items[0].SequentialId}", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
        
        response = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/comments/user/{level.LevelId}").Result;
        userComments = response.Content.ReadAsXML<SerializedCommentList>();
        Assert.That(userComments.Items, Has.Count.EqualTo(0));
    }
    
    [Test]
    public void CantPostTooLongLevelComment()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        GameComment comment = new()
        {
            Author = user,
            Content = new string('S', 5000),
        };

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/postComment/user/{level.LevelId}", new StringContent(comment.AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }

    [Test]
    public void CantPostCommentToInvalidLevel()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        GameComment comment = new()
        {
            Author = user,
            Content = "This is a test comment",
        };

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/postComment/user/I_AM_NOT_REAL", new StringContent(comment.AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(NotFound));
    }
    
    [Test]
    public void CantGetLevelCommentsOfInvalidLevel()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/comments/user/I_AM_NOT_REAL").Result;
        Assert.That(response.StatusCode, Is.EqualTo(NotFound));
    }
    
    [Test]
    public void CantDeleteInvalidLevelCommentId()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteComment/user/{level.LevelId}?commentId=BAD", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }
    
    [Test]
    public void CantDeleteCommentForInvalidLevel()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteComment/user/I_AM_NOT_REAL?commentId=1", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(NotFound));
    }
    
    [Test]
    public void CantDeleteNonExistentLevelComment()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteComment/user/{level.LevelId}?commentId=1", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }
    
    [Test]
    public void CantDeleteAnotherUsersComment()
    {
        using TestContext context = this.GetServer();
        GameUser user1 = context.CreateUser();
        GameUser user2 = context.CreateUser();
        GameLevel level = context.CreateLevel(user1);

        using HttpClient client1 = context.GetAuthenticatedClient(TokenType.Game, user1);
        using HttpClient client2 = context.GetAuthenticatedClient(TokenType.Game, user2);

        GameComment comment = new()
        {
            Author = user1,
            Content = "This is a test comment!",
        };

        HttpResponseMessage response = client1.PostAsync($"/LITTLEBIGPLANETPS3_XML/postComment/user/{level.LevelId}", new StringContent(comment.AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));

        response = client1.GetAsync($"/LITTLEBIGPLANETPS3_XML/comments/user/{level.LevelId}").Result;
        SerializedCommentList userComments = response.Content.ReadAsXML<SerializedCommentList>();
        Assert.That(userComments.Items, Has.Count.EqualTo(1));
        Assert.That(userComments.Items[0].Content, Is.EqualTo(comment.Content));
        
        response = client2.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteComment/user/{level.LevelId}?commentId={userComments.Items[0].SequentialId}", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(Unauthorized));
    }

    [Test]
    public void RateUserLevelComment()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);
        GameComment comment = context.Database.PostCommentToLevel(level, user, "This is a test comment!");
        
        CommentTests.RateComment(context, user, comment, $"/LITTLEBIGPLANETPS3_XML/rateComment/user/{level.LevelId}", $"/LITTLEBIGPLANETPS3_XML/comments/user/{level.LevelId}");
    }
    
    [Test]
    public void RateDeveloperLevelComment()
    {
        const int levelId = 1;
        
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.Database.GetStoryLevelById(levelId);
        GameComment comment = context.Database.PostCommentToLevel(level, user, "This is a test comment!");
        
        CommentTests.RateComment(context, user, comment, $"/LITTLEBIGPLANETPS3_XML/rateComment/developer/{level.LevelId}", $"/LITTLEBIGPLANETPS3_XML/comments/developer/{level.LevelId}");
    }
}