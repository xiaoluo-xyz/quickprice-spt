using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Repair;
using SPTarkov.Server.Core.Models.Eft.Trade;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace SPTarkov.Server.Core.Services;

[Injectable(InjectionType.Singleton, null, int.MaxValue)]
public class RepairService
{
	protected readonly RepairConfig RepairConfig;

	public RepairService(ISptLogger<RepairService> logger, RandomUtil randomUtil, DatabaseService databaseService, ItemHelper itemHelper, TraderHelper traderHelper, PaymentService paymentService, ProfileHelper profileHelper, RepairHelper repairHelper, ServerLocalisationService serverLocalisationService, ConfigServer configServer, WeightedRandomHelper weightedRandomHelper)
	{
		<logger>P = logger;
		<randomUtil>P = randomUtil;
		<databaseService>P = databaseService;
		<itemHelper>P = itemHelper;
		<traderHelper>P = traderHelper;
		<paymentService>P = paymentService;
		<profileHelper>P = profileHelper;
		<repairHelper>P = repairHelper;
		<serverLocalisationService>P = serverLocalisationService;
		<weightedRandomHelper>P = weightedRandomHelper;
		RepairConfig = configServer.GetConfig<RepairConfig>();
		base..ctor();
	}

	/// <summary>
	///     Use trader to repair an items durability
	/// </summary>
	/// <param name="sessionID">Session id</param>
	/// <param name="pmcData">Profile to find item to repair in</param>
	/// <param name="repairItemDetails">Details of the item to repair</param>
	/// <param name="traderId">Trader being used to repair item</param>
	/// <returns>RepairDetails object</returns>
	public virtual RepairDetails RepairItemByTrader(MongoId sessionID, PmcData pmcData, RepairItem repairItemDetails, MongoId traderId)
	{
		Item item = pmcData.Inventory.Items.FirstOrDefault((Item item2) => item2.Id == repairItemDetails.Id);
		if ((object)item == null)
		{
			<logger>P.Error(<serverLocalisationService>P.GetText("repair-unable_to_find_item_in_inventory_cant_repair", (object?)repairItemDetails.Id));
		}
		double? repairPriceCoefficient = <traderHelper>P.GetLoyaltyLevel(traderId, pmcData).RepairPriceCoefficient;
		TraderRepair? obj = <traderHelper>P.GetTrader(traderId, sessionID)?.Repair;
		if ((object)obj == null)
		{
			<logger>P.Error(<serverLocalisationService>P.GetText("repair-unable_to_find_trader_details_by_id", (object?)traderId));
		}
		double? quality = obj.Quality;
		double? num = ((repairPriceCoefficient <= 0.0) ? new double?(1.0) : (repairPriceCoefficient / 100.0 + 1.0));
		Dictionary<MongoId, TemplateItem> items = <databaseService>P.GetItems();
		TemplateItem templateItem = items[item.Template];
		bool flag = templateItem.Properties.ArmorMaterial.HasValue;
		<repairHelper>P.UpdateItemDurability(item, templateItem, flag, repairItemDetails.Count.Value, useRepairKit: false, quality.Value, quality != 0.0 && RepairConfig.ApplyRandomizeDurabilityLoss);
		int? repairCost = items[item.Template].Properties.RepairCost;
		if (!repairCost.HasValue)
		{
			<logger>P.Error(<serverLocalisationService>P.GetText("repair-unable_to_find_item_repair_cost", (object?)item.Template));
		}
		double value = Math.Round((double)repairCost.Value * repairItemDetails.Count.Value * num.Value * RepairConfig.PriceMultiplier);
		if (<logger>P.IsLogEnabled(LogLevel.Debug))
		{
			<logger>P.Debug($"item base repair cost: {repairCost}");
			<logger>P.Debug($"price multiplier: {RepairConfig.PriceMultiplier}");
			<logger>P.Debug($"repair cost: {value}");
		}
		return new RepairDetails
		{
			RepairCost = value,
			RepairedItem = item,
			RepairedItemIsArmor = flag,
			RepairAmount = repairItemDetails.Count,
			RepairedByKit = false
		};
	}

	/// <summary>
	/// </summary>
	/// <param name="sessionID">Session id</param>
	/// <param name="pmcData">Profile to take money from</param>
	/// <param name="repairedItemId">Repaired item id</param>
	/// <param name="repairCost">Cost to repair item in roubles</param>
	/// <param name="traderId">Id of the trader who repaired the item / who is paid</param>
	/// <param name="output">Client response</param>
	public virtual void PayForRepair(MongoId sessionID, PmcData pmcData, string repairedItemId, double repairCost, MongoId traderId, ItemEventRouterResponse output)
	{
		ProcessBuyTradeRequestData processBuyTradeRequestData = new ProcessBuyTradeRequestData();
		int num = 1;
		List<IdWithCount> list = new List<IdWithCount>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<IdWithCount> span = CollectionsMarshal.AsSpan(list);
		int index = 0;
		span[index] = new IdWithCount
		{
			Count = Math.Round(repairCost),
			Id = Money.ROUBLES
		};
		processBuyTradeRequestData.SchemeItems = list;
		processBuyTradeRequestData.TransactionId = traderId;
		processBuyTradeRequestData.Action = "SptRepair";
		processBuyTradeRequestData.Type = string.Empty;
		processBuyTradeRequestData.ItemId = MongoId.Empty();
		processBuyTradeRequestData.Count = 0;
		processBuyTradeRequestData.SchemeId = 0;
		ProcessBuyTradeRequestData request = processBuyTradeRequestData;
		<paymentService>P.PayMoney(pmcData, request, sessionID, output);
	}

	/// <summary>
	///     Add skill points to profile after repairing an item
	/// </summary>
	/// <param name="sessionId">Session id</param>
	/// <param name="repairDetails">Details of item repaired, cost/item</param>
	/// <param name="pmcData">Profile to add points to</param>
	public virtual void AddRepairSkillPoints(MongoId sessionId, RepairDetails repairDetails, PmcData pmcData)
	{
		if ((repairDetails.RepairedByKit ?? false) && <itemHelper>P.IsOfBaseclass(repairDetails.RepairedItem.Template, BaseClasses.WEAPON))
		{
			double weaponRepairSkillPoints = GetWeaponRepairSkillPoints(repairDetails);
			if (weaponRepairSkillPoints > 0.0)
			{
				<logger>P.Debug($"Added: {weaponRepairSkillPoints} WEAPON_TREATMENT points to skill");
				<profileHelper>P.AddSkillPointsToPlayer(pmcData, SkillTypes.WeaponTreatment, weaponRepairSkillPoints, useSkillProgressRateMultiplier: true);
			}
		}
		if ((repairDetails.RepairedByKit ?? false) && <itemHelper>P.IsOfBaseclasses(repairDetails.RepairedItem.Template, new global::<>z__ReadOnlyArray<MongoId>(new MongoId[2]
		{
			BaseClasses.ARMOR_PLATE,
			BaseClasses.BUILT_IN_INSERTS
		})))
		{
			KeyValuePair<bool, TemplateItem> item = <itemHelper>P.GetItem(repairDetails.RepairedItem.Template);
			if (!item.Key)
			{
				<logger>P.Error(<serverLocalisationService>P.GetText("repair-unable_to_find_item_in_db", (object?)repairDetails.RepairedItem.Template));
				return;
			}
			SkillTypes skillTypes = ((item.Value.Properties.ArmorType == "Heavy") ? SkillTypes.HeavyVests : SkillTypes.LightVests);
			if (!repairDetails.RepairPoints.HasValue)
			{
				<logger>P.Error(<serverLocalisationService>P.GetText("repair-item_has_no_repair_points", (object?)repairDetails.RepairedItem.Template));
			}
			double? num = repairDetails.RepairPoints * RepairConfig.ArmorKitSkillPointGainPerRepairPointMultiplier;
			<logger>P.Debug($"Added: {num} {skillTypes} skill");
			<profileHelper>P.AddSkillPointsToPlayer(pmcData, skillTypes, num ?? 0.0);
		}
		double intellectGainedFromRepair = GetIntellectGainedFromRepair(repairDetails);
		if (intellectGainedFromRepair > 0.0)
		{
			<logger>P.Debug($"Added: {intellectGainedFromRepair} intellect skill");
			<profileHelper>P.AddSkillPointsToPlayer(pmcData, SkillTypes.Intellect, intellectGainedFromRepair);
		}
	}

	protected virtual double GetIntellectGainedFromRepair(RepairDetails repairDetails)
	{
		if (repairDetails.RepairedByKit ?? false)
		{
			double num = (<itemHelper>P.IsOfBaseclass(repairDetails.RepairedItem.Template, BaseClasses.WEAPON) ? RepairConfig.RepairKitIntellectGainMultiplier.Weapon : RepairConfig.RepairKitIntellectGainMultiplier.Armor);
			if (!repairDetails.RepairPoints.HasValue)
			{
				<logger>P.Error(<serverLocalisationService>P.GetText("repair-item_has_no_repair_points", (object?)repairDetails.RepairedItem.Template));
			}
			return Math.Min(repairDetails.RepairPoints.Value * num, RepairConfig.MaxIntellectGainPerRepair.Kit);
		}
		return Math.Min(repairDetails.RepairAmount.Value / 10.0, RepairConfig.MaxIntellectGainPerRepair.Trader);
	}

	/// <summary>
	///     Return an approximation of the amount of skill points live would return for the given repairDetails
	/// </summary>
	/// <param name="repairDetails">The repair details to calculate skill points for</param>
	/// <returns>The number of skill points to reward the user</returns>
	protected virtual double GetWeaponRepairSkillPoints(RepairDetails repairDetails)
	{
		Random random = new Random();
		double pointGainMultiplier = RepairConfig.WeaponTreatment.PointGainMultiplier;
		double num = Math.Ceiling(Math.Ceiling(repairDetails.RepairAmount.Value / 2.0) * pointGainMultiplier / 2.0) * 2.0 * 2.0;
		if ((double)random.Next() <= RepairConfig.WeaponTreatment.CritFailureChance)
		{
			num -= RepairConfig.WeaponTreatment.CritFailureAmount;
		}
		if ((double)random.Next() <= RepairConfig.WeaponTreatment.CritSuccessChance)
		{
			num += RepairConfig.WeaponTreatment.CritSuccessAmount;
		}
		return Math.Max(num, 0.0);
	}

	/// <summary>
	/// </summary>
	/// <param name="sessionId">Session id</param>
	/// <param name="pmcData">Profile to update repaired item in</param>
	/// <param name="repairKits">List of Repair kits to use</param>
	/// <param name="itemToRepairId">Item id to repair</param>
	/// <param name="output">ItemEventRouterResponse</param>
	/// <returns>Details of repair, item/price</returns>
	public virtual RepairDetails RepairItemByKit(MongoId sessionId, PmcData pmcData, List<RepairKitsInfo> repairKits, MongoId itemToRepairId, ItemEventRouterResponse output)
	{
		Item item = pmcData.Inventory.Items.FirstOrDefault((Item x) => x.Id == itemToRepairId);
		if ((object)item == null)
		{
			<logger>P.Error(<serverLocalisationService>P.GetText("repair-item_not_found_unable_to_repair", (object?)itemToRepairId));
		}
		Dictionary<MongoId, TemplateItem> items = <databaseService>P.GetItems();
		TemplateItem templateItem = items[item.Template];
		bool flag = templateItem.Properties.ArmorMaterial.HasValue;
		double? repairAmount = (double?)repairKits[0].Count / GetKitDivisor(templateItem, flag, pmcData);
		bool applyMaxDurabilityDegradation = ShouldRepairKitApplyDurabilityLoss(pmcData, RepairConfig.ApplyRandomizeDurabilityLoss);
		<repairHelper>P.UpdateItemDurability(item, templateItem, flag, repairAmount.Value, useRepairKit: true, 1.0, applyMaxDurabilityDegradation);
		foreach (RepairKitsInfo repairKit in repairKits)
		{
			Item item2 = pmcData.Inventory.Items.FirstOrDefault((Item item3) => item3.Id == repairKit.Id);
			if ((object)item2 == null)
			{
				<logger>P.Error(<serverLocalisationService>P.GetText("repair-repair_kit_not_found_in_inventory", (object?)repairKit.Id));
			}
			TemplateItem repairKitDetails = items[item2.Template];
			float? count = repairKit.Count;
			AddMaxResourceToKitIfMissing(repairKitDetails, item2);
			item2.Upd.RepairKit.Resource -= count;
			output.ProfileChanges[sessionId].Items.ChangedItems.Add(item2);
		}
		return new RepairDetails
		{
			RepairPoints = repairKits[0].Count,
			RepairedItem = item,
			RepairedItemIsArmor = flag,
			RepairAmount = repairAmount,
			RepairedByKit = true
		};
	}

	/// <summary>
	///     Calculate value repairkit points need to be divided by to get the durability points to be added to an item
	/// </summary>
	/// <param name="itemToRepairDetails">Item to repair details</param>
	/// <param name="isArmor">Is the item being repaired armor</param>
	/// <param name="pmcData">Player profile</param>
	/// <returns>Number to divide kit points by</returns>
	protected virtual double GetKitDivisor(TemplateItem itemToRepairDetails, bool isArmor, PmcData pmcData)
	{
		Globals globals = <databaseService>P.GetGlobals();
		Config configuration = globals.Configuration;
		RepairSettings repairSettings = configuration.RepairSettings;
		double repairPointsCostReduction = configuration.SkillsSettings.Intellect.RepairPointsCostReduction;
		double num = pmcData.GetSkillFromProfile(SkillTypes.Intellect)?.Progress ?? 0.0;
		double num2 = repairPointsCostReduction * Math.Truncate(num / 100.0);
		if (isArmor)
		{
			double durabilityPointCostArmor = repairSettings.DurabilityPointCostArmor;
			double bonusMultiplierValue = GetBonusMultiplierValue(BonusType.RepairArmorBonus, pmcData);
			double num3 = 1.0 - (bonusMultiplierValue - 1.0) - num2;
			ArmorMaterial value = itemToRepairDetails.Properties.ArmorMaterial.Value;
			configuration.ArmorMaterials.TryGetValue(value, out ArmorType value2);
			double num4 = 1.0 + value2.Destructibility;
			int value3 = itemToRepairDetails.Properties.ArmorClass.Value;
			double armorClassDivisor = globals.Configuration.RepairSettings.ArmorClassDivisor;
			double num5 = 1.0 + (double)value3 / armorClassDivisor;
			return durabilityPointCostArmor * num3 * num4 * num5;
		}
		double num6 = GetBonusMultiplierValue(BonusType.RepairWeaponBonus, pmcData) - 1.0;
		double num7 = 1.0 - num6 - num2;
		return globals.Configuration.RepairSettings.DurabilityPointCostGuns * num7;
	}

	/// <summary>
	///     Get the bonus multiplier for a skill from a player profile
	/// </summary>
	/// <param name="skillBonus">Bonus to get multiplier of</param>
	/// <param name="pmcData">Player profile to look in for skill</param>
	/// <returns>Multiplier value</returns>
	protected virtual double GetBonusMultiplierValue(BonusType skillBonus, PmcData pmcData)
	{
		IEnumerable<Bonus> enumerable = pmcData?.Bonuses?.Where((Bonus b) => b.Type == skillBonus);
		double result = 1.0;
		if (enumerable != null)
		{
			double num = enumerable.Sum((Bonus x) => x.Value.GetValueOrDefault());
			result = 1.0 + num / 100.0;
		}
		return result;
	}

	/// <summary>
	///     Should a repair kit apply total durability loss on repair
	/// </summary>
	/// <param name="pmcData">Player profile</param>
	/// <param name="applyRandomizeDurabilityLoss">Value from repair config</param>
	/// <returns>True if loss should be applied</returns>
	protected virtual bool ShouldRepairKitApplyDurabilityLoss(PmcData pmcData, bool applyRandomizeDurabilityLoss)
	{
		bool flag = applyRandomizeDurabilityLoss;
		if (flag && <profileHelper>P.HasEliteSkillLevel(SkillTypes.Charisma, pmcData))
		{
			flag = <randomUtil>P.GetChance100(50.0);
		}
		return flag;
	}

	/// <summary>
	///     Update repair kits Resource object if it doesn't exist
	/// </summary>
	/// <param name="repairKitDetails">Repair kit details from db</param>
	/// <param name="repairKitInInventory">Repair kit to update</param>
	protected virtual void AddMaxResourceToKitIfMissing(TemplateItem repairKitDetails, Item repairKitInInventory)
	{
		int? maxRepairResource = repairKitDetails.Properties.MaxRepairResource;
		if ((object)repairKitInInventory.Upd == null)
		{
			if (<logger>P.IsLogEnabled(LogLevel.Debug))
			{
				<logger>P.Debug($"Repair kit: {repairKitInInventory.Id} in inventory lacks upd object, adding");
			}
			repairKitInInventory.Upd = new Upd
			{
				RepairKit = new UpdRepairKit
				{
					Resource = maxRepairResource
				}
			};
		}
		if (!(repairKitInInventory.Upd.RepairKit?.Resource).HasValue)
		{
			repairKitInInventory.Upd.RepairKit = new UpdRepairKit
			{
				Resource = maxRepairResource
			};
		}
	}

	/// <summary>
	///     Chance to apply buff to an item (Armor/weapon) if repaired by armor kit
	/// </summary>
	/// <param name="repairDetails">Repair details of item</param>
	/// <param name="pmcData">Player profile</param>
	public virtual void AddBuffToItem(RepairDetails repairDetails, PmcData pmcData)
	{
		if ((repairDetails.RepairedByKit ?? false) && ShouldBuffItem(repairDetails, pmcData))
		{
			if (<itemHelper>P.IsOfBaseclasses(repairDetails.RepairedItem.Template, new global::<>z__ReadOnlyArray<MongoId>(new MongoId[4]
			{
				BaseClasses.ARMOR,
				BaseClasses.VEST,
				BaseClasses.HEADWEAR,
				BaseClasses.ARMOR_PLATE
			})))
			{
				SPTarkov.Server.Core.Models.Spt.Config.BonusSettings armor = RepairConfig.RepairKit.Armor;
				AddBuff(armor, repairDetails.RepairedItem);
			}
			else if (<itemHelper>P.IsOfBaseclass(repairDetails.RepairedItem.Template, BaseClasses.WEAPON))
			{
				SPTarkov.Server.Core.Models.Spt.Config.BonusSettings weapon = RepairConfig.RepairKit.Weapon;
				AddBuff(weapon, repairDetails.RepairedItem);
			}
		}
	}

	/// <summary>
	///     Add random buff to item
	/// </summary>
	/// <param name="itemConfig">weapon/armor config</param>
	/// <param name="item">Item to repair</param>
	public virtual void AddBuff(SPTarkov.Server.Core.Models.Spt.Config.BonusSettings itemConfig, Item item)
	{
		string weightedValue = <weightedRandomHelper>P.GetWeightedValue(itemConfig.RarityWeight);
		string weightedValue2 = <weightedRandomHelper>P.GetWeightedValue(itemConfig.BonusTypeWeight);
		Dictionary<string, BonusValues> obj = ((weightedValue == "Rare") ? itemConfig.Rare : itemConfig.Common);
		MinMax<double> valuesMinMax = obj[weightedValue2].ValuesMinMax;
		double value = <randomUtil>P.GetDouble(valuesMinMax.Min, valuesMinMax.Max);
		MinMax<int> activeDurabilityPercentMinMax = obj[weightedValue2].ActiveDurabilityPercentMinMax;
		double percent = <randomUtil>P.GetDouble(activeDurabilityPercentMinMax.Min, activeDurabilityPercentMinMax.Max);
		item.Upd.Buff = new UpdBuff
		{
			Rarity = weightedValue,
			BuffType = Enum.Parse<RepairBuffType>(weightedValue2),
			Value = value,
			ThresholdDurability = <randomUtil>P.GetPercentOfValue(percent, item.Upd.Repairable.Durability.Value, 0)
		};
	}

	/// <summary>
	///     Check if item should be buffed by checking the item type and relevant player skill level
	/// </summary>
	/// <param name="repairDetails">Item that was repaired</param>
	/// <param name="pmcData">Player profile</param>
	/// <returns>True if item should have buff applied</returns>
	protected virtual bool ShouldBuffItem(RepairDetails repairDetails, PmcData pmcData)
	{
		Globals globals = <databaseService>P.GetGlobals();
		KeyValuePair<bool, TemplateItem> item = <itemHelper>P.GetItem(repairDetails.RepairedItem.Template);
		if (!item.Key)
		{
			return false;
		}
		TemplateItem value = item.Value;
		SkillTypes? itemSkillType = GetItemSkillType(value);
		if (!itemSkillType.HasValue)
		{
			return false;
		}
		if (itemSkillType == SkillTypes.WeaponTreatment)
		{
			CommonSkill? skillFromProfile = pmcData.GetSkillFromProfile(SkillTypes.WeaponTreatment);
			if ((object)skillFromProfile != null && skillFromProfile.Progress < 1000.0)
			{
				return false;
			}
		}
		if (new HashSet<SkillTypes>
		{
			SkillTypes.LightVests,
			SkillTypes.HeavyVests
		}.Contains(itemSkillType.Value))
		{
			CommonSkill? skillFromProfile2 = pmcData.GetSkillFromProfile(itemSkillType.Value);
			if ((object)skillFromProfile2 != null && skillFromProfile2.Progress < 1000.0)
			{
				return false;
			}
		}
		Dictionary<string, object> allPropertiesAsDictionary = globals.Configuration.SkillsSettings.GetAllPropertiesAsDictionary();
		BuffSettings buffSettings = null;
		switch (itemSkillType)
		{
		case SkillTypes.LightVests:
		case SkillTypes.HeavyVests:
			buffSettings = ((ArmorSkills)allPropertiesAsDictionary[itemSkillType.ToString()]).BuffSettings;
			break;
		case SkillTypes.WeaponTreatment:
			buffSettings = ((WeaponTreatment)allPropertiesAsDictionary[itemSkillType.ToString()]).BuffSettings;
			break;
		default:
			<logger>P.Error($"Unhandled buff type: {itemSkillType}");
			break;
		}
		double commonBuffMinChanceValue = buffSettings.CommonBuffMinChanceValue;
		double commonBuffChanceLevelBonus = buffSettings.CommonBuffChanceLevelBonus;
		double receivedDurabilityMaxPercent = buffSettings.ReceivedDurabilityMaxPercent;
		double num = Math.Truncate((pmcData.GetSkillFromProfile(itemSkillType.Value)?.Progress ?? 0.0) / 100.0);
		if (!repairDetails.RepairPoints.HasValue)
		{
			<logger>P.Error(<serverLocalisationService>P.GetText("repair-item_has_no_repair_points", (object?)repairDetails.RepairedItem.Template));
		}
		double durabilityMultiplier = GetDurabilityMultiplier(receivedDurabilityMaxPercent, (repairDetails.RepairPoints / value.Properties.MaxDurability).Value);
		double num2 = commonBuffMinChanceValue + commonBuffChanceLevelBonus * num * durabilityMultiplier;
		return (double)new Random().Next() <= num2;
	}

	/// <summary>
	///     Based on item, what underlying skill does this item use for buff settings
	/// </summary>
	/// <param name="itemTemplate">Item to check for skill</param>
	/// <returns>Skill name</returns>
	protected virtual SkillTypes? GetItemSkillType(TemplateItem itemTemplate)
	{
		if (<itemHelper>P.IsOfBaseclasses(itemTemplate.Id, new global::<>z__ReadOnlyArray<MongoId>(new MongoId[4]
		{
			BaseClasses.ARMOR,
			BaseClasses.VEST,
			BaseClasses.HEADWEAR,
			BaseClasses.ARMOR_PLATE
		})))
		{
			string armorType = itemTemplate.Properties.ArmorType;
			if (armorType == "Light")
			{
				return SkillTypes.LightVests;
			}
			if (armorType == "Heavy")
			{
				return SkillTypes.HeavyVests;
			}
		}
		if (<itemHelper>P.IsOfBaseclass(itemTemplate.Id, BaseClasses.WEAPON))
		{
			return SkillTypes.WeaponTreatment;
		}
		if (<itemHelper>P.IsOfBaseclass(itemTemplate.Id, BaseClasses.KNIFE))
		{
			return SkillTypes.Melee;
		}
		return null;
	}

	/// <summary>
	///     Ensure multiplier is between 1 and 0.01
	/// </summary>
	/// <param name="receiveDurabilityMaxPercent">Max durability percent</param>
	/// <param name="receiveDurabilityPercent">current durability percent</param>
	/// <returns>durability multiplier value</returns>
	protected virtual double GetDurabilityMultiplier(double receiveDurabilityMaxPercent, double receiveDurabilityPercent)
	{
		double num = Math.Max(0.01, receiveDurabilityMaxPercent);
		return Math.Clamp(receiveDurabilityPercent / num, 0.01, 1.0);
	}
}
