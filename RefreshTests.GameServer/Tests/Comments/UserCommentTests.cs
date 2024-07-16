using Refresh.GameServer.Authentication;
using Refresh.GameServer.Types.Comments;
using Refresh.GameServer.Types.Lists;
using Refresh.GameServer.Types.UserData;
using RefreshTests.GameServer.Extensions;

namespace RefreshTests.GameServer.Tests.Comments;

public class UserCommentTests : GameServerTest
{
    [Test]
    public void PostAndDeleteUserComment()
    {
        using TestContext context = this.GetServer();
        GameUser user1 = context.CreateUser();
        GameUser user2 = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user1);

        GameComment comment = new()
        {
            Author = user1,
            Content = "This is a test comment!",
        };

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/postUserComment/{user2.Username}", new StringContent(comment.AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));

        response = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/userComments/{user2.Username}").Result;
        SerializedCommentList userComments = response.Content.ReadAsXML<SerializedCommentList>();
        Assert.That(userComments.Items, Has.Count.EqualTo(1));
        Assert.That(userComments.Items[0].Content, Is.EqualTo(comment.Content));
        
        response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteUserComment/{user2.Username}?commentId={userComments.Items[0].SequentialId}", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
        
        response = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/userComments/{user2.Username}").Result;
        userComments = response.Content.ReadAsXML<SerializedCommentList>();
        Assert.That(userComments.Items, Has.Count.EqualTo(0));
    }
    
    [Test]
    public void CantPostTooLongUserComment()
    {
        using TestContext context = this.GetServer();
        GameUser user1 = context.CreateUser();
        GameUser user2 = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user1);

        GameComment comment = new()
        {
            Author = user1,
            Content = new string('S', 5000),
        };

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/postUserComment/{user2.Username}", new StringContent(comment.AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }

    [Test]
    public void CantUserCommentToInvalidUser()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        GameComment comment = new()
        {
            Author = user,
            Content = "This is a test comment",
        };

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/postUserComment/I_AM_NOT_REAL", new StringContent(comment.AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(NotFound));
    }
    
    [Test]
    public void CantGetUserCommentsOfInvalidUser()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/userComments/I_AM_NOT_REAL").Result;
        Assert.That(response.StatusCode, Is.EqualTo(NotFound));
    }
    
    [Test]
    public void CantDeleteInvalidCommentId()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteUserComment/I_AM_NOT_REAL?commentId=BAD", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }
    
    [Test]
    public void CantDeleteCommentForInvalidUser()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteUserComment/I_AM_NOT_REAL?commentId=1", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(NotFound));
    }
    
    [Test]
    public void CantDeleteNonExistantComment()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();

        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteUserComment/{user.Username}?commentId=1", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }
    
    [Test]
    public void CantDeleteAnotherUsersComment()
    {
        using TestContext context = this.GetServer();
        GameUser user1 = context.CreateUser();
        GameUser user2 = context.CreateUser();

        using HttpClient client1 = context.GetAuthenticatedClient(TokenType.Game, user1);
        using HttpClient client2 = context.GetAuthenticatedClient(TokenType.Game, user2);

        GameComment comment = new()
        {
            Author = user1,
            Content = "This is a test comment!",
        };

        HttpResponseMessage response = client1.PostAsync($"/LITTLEBIGPLANETPS3_XML/postUserComment/{user2.Username}", new StringContent(comment.AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));

        response = client1.GetAsync($"/LITTLEBIGPLANETPS3_XML/userComments/{user2.Username}").Result;
        SerializedCommentList userComments = response.Content.ReadAsXML<SerializedCommentList>();
        Assert.That(userComments.Items, Has.Count.EqualTo(1));
        Assert.That(userComments.Items[0].Content, Is.EqualTo(comment.Content));
        
        response = client2.PostAsync($"/LITTLEBIGPLANETPS3_XML/deleteUserComment/{user2.Username}?commentId={userComments.Items[0].SequentialId}", new ByteArrayContent(Array.Empty<byte>())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(Unauthorized));
    }
    
    [Test]
    public void RateProfileComment()
    {
        using TestContext context = this.GetServer();
        GameUser user = context.CreateUser();
        GameComment comment = context.Database.PostCommentToProfile(user, user, "This is a test comment!");
        
        CommentTests.RateComment(context, user, comment, $"/LITTLEBIGPLANETPS3_XML/rateUserComment/{user.Username}", $"/LITTLEBIGPLANETPS3_XML/userComments/{user.Username}");
    }
}