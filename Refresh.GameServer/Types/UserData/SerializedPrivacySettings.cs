using System.Xml.Serialization;

namespace Refresh.GameServer.Types.UserData;

[XmlType("privacySettings")]
[XmlRoot("privacySettings")]
public class SerializedPrivacySettings
{
    // These are marked as nullable because the game sends these two options as separate requests, one for level visibility, one for profile visibility
    [XmlElement("levelVisibility")]
    public Visibility? LevelVisibility { get; set; }
    [XmlElement("profileVisibility")]
    public Visibility? ProfileVisibility { get; set; }
}