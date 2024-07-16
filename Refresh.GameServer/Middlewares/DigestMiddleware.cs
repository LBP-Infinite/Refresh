using System.Diagnostics;
using System.Security.Cryptography;
using Bunkum.Listener.Request;
using Bunkum.Core.Database;
using Bunkum.Core.Endpoints.Middlewares;
using Refresh.Common.Extensions;
using Refresh.GameServer.Endpoints;
using System;
using System.IO;

namespace Refresh.GameServer.Middlewares;




public class digestfilechachethingy1
{
    private static string _cachedContent = null;
    
    public static string ReadFileWithCache()
    {
        // Check if the content is already cached
        if (_cachedContent != null)
        {
            return _cachedContent;
        }

		
		Console.WriteLine("First time reading digestkey1.txt");
		_cachedContent = File.ReadAllText("digestkey1.txt");

        return _cachedContent;
    }
}

public class digestfilechachethingy2
{
    private static string _cachedContent = null;
    
    public static string ReadFileWithCache()
    {
        // Check if the content is already cached
        if (_cachedContent != null)
        {
            return _cachedContent;
        }

		
		Console.WriteLine("First time reading digestkey2.txt");
		_cachedContent = File.ReadAllText("digestkey2.txt");

        return _cachedContent;
    }
}

public class DigestMiddleware : IMiddleware
{
    // Should be 19 characters (or less maybe?)
    // Length was taken from PS3 and PS4 digest keys
    // private const string DigestKey = "CustomServerDigest";

    public static string CalculateDigest(string DigestKey, string url, Stream body, string auth, short? exeVersion, short? dataVersion)
    {
        using MemoryStream ms = new();
        
        if (!url.StartsWith($"{GameEndpointAttribute.BaseRoute}upload/"))
        {
            // get request body
            body.CopyTo(ms);
            body.Seek(0, SeekOrigin.Begin);
        }
        
        ms.WriteString(auth);
        ms.WriteString(url);
        if (exeVersion.HasValue)
        {
            byte[] bytes = BitConverter.GetBytes(exeVersion.Value);
            if(!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            ms.Write(bytes);
        } 
        if (dataVersion.HasValue)
        {
            byte[] bytes = BitConverter.GetBytes(dataVersion.Value);
            if(!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            ms.Write(bytes);
        }  
        ms.WriteString(DigestKey);

        ms.Position = 0;
        using SHA1 sha = SHA1.Create();
        string digestResponse = Convert.ToHexString(sha.ComputeHash(ms)).ToLower();

        return digestResponse;
    }
    
    // Referenced from Project Lighthouse
    // https://github.com/LBPUnion/ProjectLighthouse/blob/d16132f67f82555ef636c0dabab5aabf36f57556/ProjectLighthouse.Servers.GameServer/Middlewares/DigestMiddleware.cs
    // https://github.com/LBPUnion/ProjectLighthouse/blob/19ea44e0e2ff5f2ebae8d9dfbaf0f979720bd7d9/ProjectLighthouse/Helpers/CryptoHelper.cs#L35
    private string VerifyDigestRequest(ListenerContext context, short? exeVersion, short? dataVersion)
    {
        string url = context.Uri.AbsolutePath;
        string auth = context.Cookies["MM_AUTH"] ?? string.Empty;

        bool isUpload = url.StartsWith($"{GameEndpointAttribute.BaseRoute}upload/");

        MemoryStream body = isUpload ? new MemoryStream(0) : context.InputStream;
        string digestHeader = isUpload ? "X-Digest-B" : "X-Digest-A";
        string clientDigest = context.RequestHeaders[digestHeader] ?? string.Empty;

        string expectedDigest = CalculateDigest(digestfilechachethingy1.ReadFileWithCache(), url, body, auth, isUpload ? null : exeVersion, isUpload ? null : dataVersion);
        
        
        if (clientDigest == expectedDigest) {	
			context.ResponseHeaders["X-Digest-B"] = expectedDigest;
			return digestfilechachethingy1.ReadFileWithCache();
        }
		string expectedDigest2 = CalculateDigest(digestfilechachethingy2.ReadFileWithCache(), url, body, auth, isUpload ? null : exeVersion, isUpload ? null : dataVersion);
        if (clientDigest == expectedDigest2) {	
			context.ResponseHeaders["X-Digest-B"] = expectedDigest2;
			return digestfilechachethingy2.ReadFileWithCache();
        }
        context.ResponseHeaders["X-Digest-B"] = expectedDigest2;
		return "false";
    }
    
    private void SetDigestResponse(string DigestKey,ListenerContext context)
    {
        string url = context.Uri.AbsolutePath;
        string auth = context.Cookies["MM_AUTH"] ?? string.Empty;
    
        string digestResponse = CalculateDigest(DigestKey,url, context.ResponseStream, auth, null, null);
        
        context.ResponseHeaders["X-Digest-A"] = digestResponse;
    }

    public void HandleRequest(ListenerContext context, Lazy<IDatabaseContext> database, Action next)
    {
        //If this isn't an LBP endpoint, dont do digest
        if (!context.Uri.AbsolutePath.StartsWith(GameEndpointAttribute.BaseRoute))
        {
            next();
            return;
        }

        short? exeVersion = null;
        short? dataVersion = null;
        if (short.TryParse(context.RequestHeaders["X-Exe-V"], out short exeVer))
        {
            exeVersion = exeVer;
        }
        if (short.TryParse(context.RequestHeaders["X-Data-V"], out short dataVer))
        {
            dataVersion = dataVer;
        }

        string new_digesty = this.VerifyDigestRequest(context, exeVersion, dataVersion);
        Debug.Assert(context.InputStream.Position == 0); // should be at position 0 before we pass down the pipeline
        
        next();

        // should be at position 0 before we try to set digest
        context.ResponseStream.Seek(0, SeekOrigin.Begin);
        this.SetDigestResponse(new_digesty,context);
    }
}
