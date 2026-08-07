namespace RefFriendlyQuests;

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Web.Models.Configs;
using SPTarkov.Server.Web.Services;

[Injectable(InjectionType.Singleton)]
public class ConfigProvider(RefFriendlyModConfig config) : IConfigEditorConfigProvider
{
    public IEnumerable<ConfigEditorConfigRegistration> GetConfigs()
    {
        var metadata = new ModMetadata();
        yield return ConfigEditorConfigRegistration.Create(
        metadata.ModGuid,
        metadata.Name,
        config,
        Path.Combine("user", "mods", "acidphantasm-reffriendlyquests", "config.json")
        );
    }
}