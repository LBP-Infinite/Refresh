using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Listener.Protocol;
using Bunkum.Protocols.Http;
using Refresh.GameServer.Database;
using Refresh.GameServer.Types.Report;

namespace Refresh.GameServer.Endpoints.Game; 

public class ReportingEndpoints : EndpointGroup 
{
    [GameEndpoint("grief", HttpMethods.Post, ContentType.Xml)]
    public Response UploadReport(RequestContext context, GameDatabaseContext database, GameReport body)
    {
        if ((body.LevelId != 0 && database.GetLevelById(body.LevelId) == null) || body.Players is { Length: > 4 } || body.ScreenElements is { Player.Length: > 4 })
        {
            return BadRequest;
        }

        database.AddGriefReport(body);
        
        return OK;
    }
}