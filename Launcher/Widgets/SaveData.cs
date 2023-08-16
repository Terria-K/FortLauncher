using TeuJson;
using TeuJson.Attributes;

namespace FortLauncher;

public partial class SaveData : ISerialize, IDeserialize 
{
    [TeuObject]
    public string CurrentClientPath = "";

    [TeuObject]
    public string CurrentInstaller;

    [TeuObject]
    public string LaunchArguments = "";

    [TeuObject]
    public List<Client> Clients = new();
    
    public SaveData() {}
}