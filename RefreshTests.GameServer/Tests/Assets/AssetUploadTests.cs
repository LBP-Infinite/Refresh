using System.Security.Cryptography;
using Refresh.GameServer.Authentication;
using Refresh.GameServer.Services;
using Refresh.GameServer.Types.Assets;
using Refresh.GameServer.Types.Lists;
using Refresh.GameServer.Types.Roles;
using Refresh.GameServer.Types.UserData;
using RefreshTests.GameServer.Extensions;

namespace RefreshTests.GameServer.Tests.Assets;

public class AssetUploadTests : GameServerTest
{
    private const string MissingHash = "6e4d252f247e3aa99ef846df8c65493393e79f4f";
    
    [TestCase(false)]
    [TestCase(true)]
    public void CanUploadAsset(bool psp)
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        if(psp)
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");

        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
    }
    
    [TestCase(false)]
    [TestCase(true)]
    public void CannotUploadAssetPastFillingFilesizeQuota(bool psp)
    {
        using TestContext context = this.GetServer();
        
        context.Server.Value.GameServerConfig.UserFilesizeQuota = 8;
        
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        if(psp)
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");

        ReadOnlySpan<byte> data1 = "TEX a"u8;
        ReadOnlySpan<byte> data2 = "TEX b"u8;
        
        string hash1 = BitConverter.ToString(SHA1.HashData(data1))
            .Replace("-", "")
            .ToLower();
        
        string hash2 = BitConverter.ToString(SHA1.HashData(data2))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash1, new ByteArrayContent(data1.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
        
        response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash2, new ByteArrayContent(data2.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(RequestEntityTooLarge));
    }
    
    [TestCase(false)]
    [TestCase(true)]
    public void CannotUploadAssetWhenBlocked(bool psp)
    {
        using TestContext context = this.GetServer();

        context.Server.Value.GameServerConfig.BlockAssetUploads = true;
            
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        if(psp)
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");

        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(Unauthorized));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TrustedCanUploadAssetWhenBlocked(bool psp)
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        context.Server.Value.GameServerConfig.BlockAssetUploads = true;
        context.Server.Value.GameServerConfig.BlockAssetUploadsForTrustedUsers = false;
        
        GameUser user = context.CreateUser();
        context.Database.SetUserRole(user, GameUserRole.Trusted);
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        if(psp)
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");
        
        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
    }
    
    [TestCase(false)]
    [TestCase(true)]
    public void AdminCanUploadAssetWhenBlocked(bool psp)
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        context.Server.Value.GameServerConfig.BlockAssetUploads = true;
        context.Server.Value.GameServerConfig.BlockAssetUploadsForTrustedUsers = true;
        
        GameUser user = context.CreateUser();
        context.Database.SetUserRole(user, GameUserRole.Admin);
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        if(psp)
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");
        
        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
    }
    
    [Test]
    public void TrustedCanUploadAssetWithSafetyLevel()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        context.Server.Value.GameServerConfig.MaximumAssetSafetyLevel = AssetSafetyLevel.Safe;
        context.Server.Value.GameServerConfig.MaximumAssetSafetyLevelForTrustedUsers = AssetSafetyLevel.SafeMedia;
        
        GameUser user = context.CreateUser();
        context.Database.SetUserRole(user, GameUserRole.Trusted);
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        
        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
    }
    
    [Test]
    public void NormalUserCantUploadAssetWithSafetyLevel()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        context.Server.Value.GameServerConfig.MaximumAssetSafetyLevel = AssetSafetyLevel.Safe;
        context.Server.Value.GameServerConfig.MaximumAssetSafetyLevelForTrustedUsers = AssetSafetyLevel.SafeMedia;
        
        GameUser user = context.CreateUser();
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        
        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(Unauthorized));
    }
    
    [Test]
    public void PspCantUploadMediaAssetWhileBlocked()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        context.Server.Value.GameServerConfig.MaximumAssetSafetyLevel = AssetSafetyLevel.Safe;
        
        GameUser user = context.CreateUser();
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");
        
        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(Unauthorized));
    }
    
    [Test]
    public void PspCanUploadNormalAssetWhileBlocked()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        context.Server.Value.GameServerConfig.MaximumAssetSafetyLevel = AssetSafetyLevel.Safe;
        
        GameUser user = context.CreateUser();
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");
        
        ReadOnlySpan<byte> data = "LVLb "u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
    }
    
    [Test]
    public void CantUploadAssetWithInvalidHash()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        ReadOnlySpan<byte> data = "TEX a"u8;
        
        HttpResponseMessage response = client.PostAsync($"/LITTLEBIGPLANETPS3_XML/upload/{MissingHash}", new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }
    
    [Test]
    public void CantUploadBlockedAsset()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        ReadOnlySpan<byte> data = "FSHbthsa"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(Unauthorized));
    }
    
    [Test]
    public void DataStoreWriteFailReturnsInternalServerError()
    {
        using TestContext context = this.GetServer(true, new WriteFailingDataStore());
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(InternalServerError));
    }
    
    [Test]
    public void CantUploadDuplicateAssets()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
        
        response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(Conflict));
    }
    
    [Test]
    public void CantUploadTooLarge()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        //create 5mb array
        ReadOnlySpan<byte> data = new byte[5000000];
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(RequestEntityTooLarge));
    }
    
    [Test]
    public void InvalidAssetHashUploadFails()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        ReadOnlySpan<byte> data = "TEX a"u8;
        
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/I_AM_NOT_REAL", new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }
    
    [TestCase(false)]
    [TestCase(true)]
    public void CanRetrieveAsset(bool psp)
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        if (psp) 
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");

        ReadOnlySpan<byte> data = "TEX a"u8;
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();

        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
        
        response = client.GetAsync("/LITTLEBIGPLANETPS3_XML/r/" + hash).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
        
        byte[] returnedData = response.Content.ReadAsByteArrayAsync().Result;
        
        Assert.That(data.SequenceEqual(returnedData), Is.True);
    }
    
    [TestCase(false)]
    [TestCase(true)]
    public void CheckForMissingAssets(bool psp)
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        if(psp) 
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("LBPPSP CLIENT");

        ReadOnlySpan<byte> data = "TEX a"u8;
        string hash = BitConverter.ToString(SHA1.HashData(data))
            .Replace("-", "")
            .ToLower();
        
        //Check the list initially, should have 1 item
        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/filterResources", new StringContent(new SerializedResourceList(new[] { hash }).AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));

        SerializedResourceList missingList = response.Content.ReadAsXML<SerializedResourceList>();
        Assert.That(missingList.Items, Has.Count.EqualTo(1));
        Assert.That(missingList.Items[0], Is.EqualTo(hash));
        
        //Upload an asset
        response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/upload/" + hash, new ByteArrayContent(data.ToArray())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));
        
        //Check the list after uploading, should now only have 0 items returned
        response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/filterResources", new StringContent(new SerializedResourceList(new[] { hash }).AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(OK));

        missingList = response.Content.ReadAsXML<SerializedResourceList>();
        Assert.That(missingList.Items, Has.Count.EqualTo(0));
    }
    
    [Test]
    public void CantRetrieveMissingAsset()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/r/{MissingHash}").Result;
        Assert.That(response.StatusCode, Is.EqualTo(NotFound));
    }

    [Test]
    public void DataStoreReadFailReturnsInternalServerError()
    {
        using TestContext context = this.GetServer(true, new ReadFailingDataStore());
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        HttpResponseMessage response = client.GetAsync($"/LITTLEBIGPLANETPS3_XML/r/{MissingHash}").Result;
        Assert.That(response.StatusCode, Is.EqualTo(InternalServerError));
    }
    
    [Test]
    public void InvalidHashFails()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);
        
        HttpResponseMessage response = client.GetAsync("/LITTLEBIGPLANETPS3_XML/r/asdf").Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
        
        response = client.GetAsync("/LITTLEBIGPLANETPS3_XML/r/..%2Frpc.json").Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }
    
    [Test]
    public void CantCheckForInvalidMissingAssets()
    {
        using TestContext context = this.GetServer();
        context.Server.Value.Server.AddService<ImportService>();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.Game, user);

        //Check the list initially, should have 1 item
        HttpResponseMessage response = client.PostAsync("/LITTLEBIGPLANETPS3_XML/filterResources", new StringContent(new SerializedResourceList(new[] { "I_AM_NOT_HASH" }).AsXML())).Result;
        Assert.That(response.StatusCode, Is.EqualTo(BadRequest));
    }
}