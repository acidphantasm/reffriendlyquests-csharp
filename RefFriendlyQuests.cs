using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace RefFriendlyQuests;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 14)]
public class RefFriendlyQuests(
    TemplateTable templateTable,
    LocaleTable localeTable,
    TradersTable tradersTable,
    ModHelper modHelper,
    ItemHelper itemHelper,
    RefFriendlyModConfig config)
    : IOnLoad
{
    private Dictionary<MongoId, QuestConditionTypes> _fixedQuestData = null!;
    
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
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _fixedQuestData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, QuestConditionTypes>>(pathToMod, "db/quests.json");

        AddWeaponsToQuestsIfMissing();
        EditQuests();
        FixLocales();
        if (config.ChangeLoyaltyLevelRequirements) ChangeLoyalty();
        if (config.AddLegaMedalRewards) AddLegaMedalRewards();
        MultiplyGpCoin();
        
        return Task.CompletedTask;
    }

    private void AddWeaponsToQuestsIfMissing()
    {
        var items = templateTable.Items;
        foreach (var item in items)
        {
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.ASSAULT_CARBINE))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[0].Counter?.Conditions?[0].Weapon?.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.ASSAULT_RIFLE))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[1].Counter?.Conditions?[0].Weapon?.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.MACHINE_GUN))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[2].Counter?.Conditions?[0].Weapon?.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.MARKSMAN_RIFLE))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[3].Counter?.Conditions?[0].Weapon?.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.SHOTGUN))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[4].Counter?.Conditions?[0].Weapon?.Add(item.Key);
                continue;
            }
            if (itemHelper.IsOfBaseclass(item.Key, BaseClasses.SMG))
            {
                _fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[5].Counter?.Conditions?[0].Weapon?.Add(item.Key);
            }
        }
    }
    
    private void EditQuests()
    {
        var quests = templateTable.Quests;
        
        foreach (var quest in _refQuestsToEdit)
        {
            quests[quest].Conditions.AvailableForFinish = _fixedQuestData[quest].AvailableForFinish;
        }
    }

    private void FixLocales()
    {
        var globalLocale = localeTable.Global;
        foreach (var (_, lazyLoadedLocales) in globalLocale)
        {
            lazyLoadedLocales.AddTransformer(localeData =>
            {
                if (localeData is not null)
                {
                    localeData[_fixedQuestData["68341eb25619c8e2a9031501"].AvailableForFinish?[0].Id!] = "Eliminate 10 PMCs";
                    localeData[_fixedQuestData["68341f6fe2e7ef70a3060a0a"].AvailableForFinish?[0].Id!] = "Eliminate 15 PMCs";
                    localeData[_fixedQuestData["6834202a186efa3c5b07f9a2"].AvailableForFinish?[0].Id!] = "Eliminate 20 PMCs";
                    localeData[_fixedQuestData["683421515619c8e2a9031511"].AvailableForFinish?[0].Id!] = "Eliminate 25 PMCs";
                    localeData[_fixedQuestData["68342265a8d674b5740b31f0"].AvailableForFinish?[0].Id!] = "Eliminate 50 PMCs";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[0].Id!] = "Eliminate any 10 targets with Assault Carbines";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[1].Id!] = "Eliminate any 10 targets with Assault Rifles";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[2].Id!] = "Eliminate any 10 targets with LMGs";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[3].Id!] = "Eliminate any 10 targets with Marksman Rifles";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[4].Id!] = "Eliminate any 10 targets with Shotguns";
                    localeData[_fixedQuestData["68342446a8d674b5740b31fc"].AvailableForFinish?[5].Id!] = "Eliminate any 10 targets with SMGs";
                }
                return localeData;
            });
        }
    }
    
    private void ChangeLoyalty()
    {
        var refBase = tradersTable.GetTrader(Traders.REF)?.Base;
        foreach (var loyaltyLevel in refBase?.LoyaltyLevels ?? [])
        {
            if (loyaltyLevel.MinStanding is null or 0)
                continue;

            loyaltyLevel.MinStanding = Math.Round(loyaltyLevel.MinStanding.Value * config.LoyaltyLevelMultiplier, 2);
        }
    }
    
    private void AddLegaMedalRewards()
    {
        var quests = templateTable.Quests;
        
        foreach (var quest in _refQuests)
        {
            var successRewards = quests[quest].Rewards?["Success"];
            var alreadyHasLega = false;
            
            foreach (var reward in successRewards ?? [])
            {
                if (reward.Items is not null)
                {
                    if (reward.Items[0].Template == ItemTpl.BARTER_LEGA_MEDAL) alreadyHasLega = true;
                }
            }

            if (!alreadyHasLega)
            {
                successRewards?.Add(new Reward()
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
        var quests = templateTable.Quests;

        foreach (var quest in _refQuests)
        {
            var rewards = quests[quest].Rewards;
            if (rewards is null)
                continue;

            var successRewards = rewards["Success"];
            var index = successRewards.FindIndex(0, x => x.Items?[0].Template == ItemTpl.MONEY_GP_COIN);
            if (index == -1)
                continue;

            var stackCount = successRewards[index].Items?[0].Upd?.StackObjectsCount;
            if (stackCount is null)
                continue;

            var newStackCount = Math.Round(stackCount.Value * config.GpCoinMultiplier);

            successRewards[index].Items![0].Upd = new Upd { StackObjectsCount = newStackCount };
            successRewards[index].Value = newStackCount;
        }
    }
}