using System;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace SPTarkov.Server.Core.Helpers;

[Injectable(InjectionType.Scoped, null, int.MaxValue)]
public class RepairHelper(ISptLogger<RepairHelper> logger, RandomUtil randomUtil, DatabaseService databaseService, ICloner cloner)
{
	/// <summary>
	///     Alter an items durability after a repair by trader/repair kit
	/// </summary>
	/// <param name="itemToRepair">item to update durability details</param>
	/// <param name="itemToRepairDetails">db details of item to repair</param>
	/// <param name="isArmor">Is item being repaired a piece of armor</param>
	/// <param name="amountToRepair">how many unit of durability to repair</param>
	/// <param name="useRepairKit">Is item being repaired with a repair kit</param>
	/// <param name="traderQualityMultiplier">Trader quality value from traders base json</param>
	/// <param name="applyMaxDurabilityDegradation">should item have max durability reduced</param>
	public virtual void UpdateItemDurability(Item itemToRepair, TemplateItem itemToRepairDetails, bool isArmor, double amountToRepair, bool useRepairKit, double traderQualityMultiplier, bool applyMaxDurabilityDegradation = true)
	{
		if (logger.IsLogEnabled(LogLevel.Debug))
		{
			logger.Debug($"Adding {amountToRepair} to {itemToRepairDetails.Name} using kit: {useRepairKit}");
		}
		double? num = cloner.Clone(itemToRepair.Upd.Repairable.MaxDurability);
		double? num2 = cloner.Clone(itemToRepair.Upd.Repairable.Durability);
		double? num3 = cloner.Clone(itemToRepair.Upd.Repairable.MaxDurability);
		double? num4 = num2 + amountToRepair;
		double? num5 = num3 + amountToRepair;
		if (num5 > num)
		{
			num5 = num;
		}
		if (num4 > num)
		{
			num4 = num;
		}
		itemToRepair.Upd.Repairable = new UpdRepairable
		{
			Durability = num4,
			MaxDurability = num5
		};
		if (applyMaxDurabilityDegradation)
		{
			double num6 = (isArmor ? GetRandomisedArmorRepairDegradationValue(itemToRepairDetails.Properties.ArmorMaterial.Value, useRepairKit, num3.GetValueOrDefault(), traderQualityMultiplier) : GetRandomisedWeaponRepairDegradationValue(itemToRepairDetails.Properties, useRepairKit, num3.GetValueOrDefault(), traderQualityMultiplier));
			itemToRepair.Upd.Repairable.MaxDurability -= num6;
			if (itemToRepair.Upd.Repairable.Durability > itemToRepair.Upd.Repairable.MaxDurability)
			{
				itemToRepair.Upd.Repairable.Durability = itemToRepair.Upd.Repairable.MaxDurability;
			}
		}
		if ((object)itemToRepair.Upd.FaceShield != null)
		{
			UpdFaceShield? faceShield = itemToRepair.Upd.FaceShield;
			if ((object)faceShield != null && faceShield.Hits > 0)
			{
				itemToRepair.Upd.FaceShield.Hits = 0;
			}
		}
	}

	/// <summary>
	///     Repairing armor reduces the total durability value slightly, get a randomised (to 2dp) amount based on armor material
	/// </summary>
	/// <param name="material">What material is the armor being repaired made of</param>
	/// <param name="isRepairKit">Was a repair kit used</param>
	/// <param name="armorMax">Max amount of durability item can have</param>
	/// <param name="traderQualityMultiplier">Different traders produce different loss values</param>
	/// <returns>Amount to reduce max durability by</returns>
	protected virtual double GetRandomisedArmorRepairDegradationValue(ArmorMaterial material, bool isRepairKit, double armorMax, double traderQualityMultiplier)
	{
		if (!databaseService.GetGlobals().Configuration.ArmorMaterials.TryGetValue(material, out ArmorType value))
		{
			logger.Error($"Unable to find armor with a type of: {material}");
		}
		double min = (isRepairKit ? value.MinRepairKitDegradation : value.MinRepairDegradation);
		double max = (isRepairKit ? value.MaxRepairKitDegradation : value.MaxRepairDegradation);
		return Math.Round(randomUtil.GetDouble(min, max) * armorMax * traderQualityMultiplier, 2);
	}

	/// <summary>
	///     Repairing weapons reduces the total durability value slightly, get a randomised (to 2dp) amount
	/// </summary>
	/// <param name="itemProperties">Weapon properties</param>
	/// <param name="isRepairKit">Was a repair kit used</param>
	/// <param name="weaponMax">Max amount of durability item can have</param>
	/// <param name="traderQualityMultiplier">Different traders produce different loss values</param>
	/// <returns>Amount to reduce max durability by</returns>
	protected virtual double GetRandomisedWeaponRepairDegradationValue(TemplateItemProperties itemProperties, bool isRepairKit, double weaponMax, double traderQualityMultiplier)
	{
		double? num = (isRepairKit ? itemProperties.MinRepairKitDegradation : itemProperties.MinRepairDegradation);
		double? num2 = (isRepairKit ? itemProperties.MaxRepairKitDegradation : itemProperties.MaxRepairDegradation);
		if (num2 == 0.0)
		{
			num2 = itemProperties.MaxRepairDegradation;
		}
		return Math.Round(randomUtil.GetDouble(num.Value, num2.Value) * weaponMax * traderQualityMultiplier, 2);
	}
}
