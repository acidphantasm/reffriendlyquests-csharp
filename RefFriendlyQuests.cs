using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace _refFriendlyQuests;

public record ModMetadata : AbstractModMetadata
{
    /// <summary>
    /// Any string can be used for a modId, but it should ideally be unique and not easily duplicated
    /// a 'bad' ID would be: "mymod", "mod1", "questmod"
    /// It is recommended (but not mandatory) to use the reverse domain name notation,
    /// see: https://docs.oracle.com/javase/tutorial/java/package/namingpkgs.html
    /// </summary>
    public override string ModGuid { get; init; } = "com.acidphantasm.reffriendlyquests";
    public override string Name { get; init; } = "Ref Friendly Quests";
    public override string Author { get; init; } = "acidphantasm";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.3");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.10");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "MIT";
}

// We want to load after PostDBModLoader is complete, so we set our type priority to that, plus 1.
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 69420)]
public class RefFriendlyQuests(
    ISptLogger<RefFriendlyQuests> logger,
    DatabaseService databaseService,
    ModHelper modHelper,
    ItemHelper itemHelper)
    : IOnLoad
{
    private ModConfig? _modConfig;
    private Dictionary<MongoId, QuestConditionTypes>? _fixedQuestData;
    
    private readonly List<MongoId> _refQuests =
    [
        "6834158f2f0e2a7eb90b62c8", // easy money p2
        "675c15fbf7da9792a4059871", // provide viewership
        "68341846186efa3c5b07f989", // balancing p1
        "68341a0b2f0e2a7eb90b62d4", // balancing p2
        "68341b407559f4e6d50bc0ce", // surprise
        "68341c4babec72d95d0c1260", // create a distraction p1
        "68341d7d7559f4e6d50bc0db", // create a distraction p2
        "68341eb25619c8e2a9031501", // to great heights p1 - arena -> 10 pmc
        "68341f6fe2e7ef70a3060a0a", // to great heights p2 - arena -> 15 pmc
        "6834202a186efa3c5b07f9a2", // to great heights p3 - arena -> 25 pmc
        "683421515619c8e2a9031511", // to great heights p4 - arena -> 50 pmc
        "68342265a8d674b5740b31f0", // to great heights p5 - arena -> 75 pmc
        "6834233fecd5cf3a440d855b", // against the conscience p1
        "68342446a8d674b5740b31fc", // against the conscience p2 - arena -> 50 any with each weapon type
        "6834254f2f0e2a7eb90b62ef"  // decisions
    ];

    private readonly List<MongoId> _refQuestsToEdit =
    [
        "68341eb25619c8e2a9031501", // to great heights p1 - arena -> 10 pmc
        "68341f6fe2e7ef70a3060a0a", // to great heights p2 - arena -> 15 pmc
        "6834202a186efa3c5b07f9a2", // to great heights p3 - arena -> 25 pmc
        "683421515619c8e2a9031511", // to great heights p4 - arena -> 50 pmc
        "68342265a8d674b5740b31f0", // to great heights p5 - arena -> 75 pmc
        "68342446a8d674b5740b31fc", // against the conscience p2 - arena -> 50 any with each weapon type
    ];
    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _modConfig = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config.json");
        _fixedQuestData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, QuestConditionTypes>>(pathToMod, "db/quests.json");

        AddWeaponsToQuestsIfMissing();
        EditQuests();
        FixLocales();
        if (_modConfig.ChangeLoyalty4RepRequirements) ChangeLoyalty();
        if (_modConfig.AddLegaMedalRewards) AddLegaMedalRewards();
        MultiplyGpCoin();
        
        return Task.CompletedTask;
    }

    private void AddWeaponsToQuestsIfMissing()
    {
        var items = databaseService.GetItems();
        foreach (var item in items)
        {
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.ASSAULT_CARBINE))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[0].Counter.Conditions[0].Weapon.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.ASSAULT_RIFLE))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[1].Counter.Conditions[0].Weapon.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.MACHINE_GUN))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[2].Counter.Conditions[0].Weapon.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.MARKSMAN_RIFLE))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[3].Counter.Conditions[0].Weapon.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.SHOTGUN))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[4].Counter.Conditions[0].Weapon.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.SMG))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[5].Counter.Conditions[0].Weapon.Add(item.Key);
            }
        }
    }
    
    private void EditQuests()
    {
        var quests = databaseService.GetQuests();
        
        foreach (var quest in _refQuestsToEdit)
        {
            quests[quest].Conditions.AvailableForFinish = _fixedQuestData[quest].AvailableForFinish;
        }
    }

    private void FixLocales()
    {
        var globalLocale = databaseService.GetLocales().Global;
        foreach ((string locale, var lazyLoadedLocales) in globalLocale)
        {
            lazyLoadedLocales.AddTransformer(localeData =>
            {
                if (localeData is not null)
                {
                    localeData[_fixedQuestData["68341eb25619c8e2a9031501"].AvailableForFinish[0].Id] = "Eliminate 10 PMCs";
                    localeData[_fixedQuestData["68341f6fe2e7ef70a3060a0a"].AvailableForFinish[0].Id] = "Eliminate 15 PMCs";
                    localeData[_fixedQuestData["6834202a186efa3c5b07f9a2"].AvailableForFinish[0].Id] = "Eliminate 25 PMCs";
                    localeData[_fixedQuestData["683421515619c8e2a9031511"].AvailableForFinish[0].Id] = "Eliminate 50 PMCs";
                    localeData[_fixedQuestData["68342265a8d674b5740b31f0"].AvailableForFinish[0].Id] = "Eliminate 75 PMCs";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[0].Id] = "Eliminate any 50 targets with Assault Carbines";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[1].Id] = "Eliminate any 50 targets with Assault Rifles";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[2].Id] = "Eliminate any 50 targets with LMGs";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[3].Id] = "Eliminate any 50 targets with Marksman Rifles";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[4].Id] = "Eliminate any 50 targets with Shotguns";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish[5].Id] = "Eliminate any 50 targets with SMGs";
                }
                return localeData;
            });
        }
    }
    
    private void ChangeLoyalty()
    {
        var refBase = databaseService.GetTrader(Traders.REF).Base;
        refBase.LoyaltyLevels[3].MinStanding = 1.0;
    }
    
    private void AddLegaMedalRewards()
    {
        var quests = databaseService.GetQuests();
        
        foreach (var quest in _refQuests)
        {
            var successRewards = quests[quest].Rewards["Success"];
            var alreadyHasLega = false;
            
            foreach (var reward in successRewards)
            {
                if (reward.Items is not null)
                {
                    if (reward.Items[0].Template == ItemTpl.BARTER_LEGA_MEDAL) alreadyHasLega = true;
                }
            }

            if (!alreadyHasLega)
            {
                successRewards.Add(new Reward()
                {
                    AvailableInGameEditions = [],
                    FindInRaid = false,
                    GameMode = [
                        "regular",
                        "pve"
                    ],
                    Id = "68341d7d7559f4e6d50bc0e7",
                    IsEncoded = false,
                    IsHidden = false,
                    Items = 
                    [
                        new()
                        {
                            Id = "68a9695194f6582e59140ee9",
                            Template = "6656560053eaaa7a23349c86",
                            Upd = new()
                            {
                                StackObjectsCount = 1
                            },
                        }
                    ],
                    Target = "68a9695194f6582e59140ee9",
                    Type = RewardType.Item,
                    Unknown = false,
                    Value = 1
                });
            }
        }
    }
    
    private void MultiplyGpCoin()
    {
        var quests = databaseService.GetQuests();

        foreach (var quest in _refQuests)
        {
            var list = quests[quest].Rewards["Success"];
            var index = list.FindIndex(0, x => x.Items?[0].Template == ItemTpl.MONEY_GP_COIN);
            if (index != -1)
            {
                var stackCount = quests[quest].Rewards["Success"][index].Items[0].Upd.StackObjectsCount;
                var newStackCount = (double)Math.Round(stackCount.Value * _modConfig.GpCoinMultiplier);

                list[index].Items[0].Upd = new Upd()
                {
                    StackObjectsCount = newStackCount
                };
                list[index].Value = newStackCount;
            }
        }
    }
}

public class ModConfig
{
    public bool ChangeLoyalty4RepRequirements { get; set; }
    public bool AddLegaMedalRewards { get; set; }
    public double GpCoinMultiplier { get; set; }
}