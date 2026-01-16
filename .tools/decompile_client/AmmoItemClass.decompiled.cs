using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EFT.InventoryLogic;
using JsonType;
using UnityEngine;

public class AmmoItemClass : StackableItemItemClass
{
	public float buckshotDispersion;

	public SonicBulletSoundPlayer.SonicType SonicType = SonicBulletSoundPlayer.SonicType.Sonic545;

	[DefaultValue(false)]
	[GAttribute25]
	public bool IsUsed;

	public override bool ImportantForCheckSpawnedInSession => false;

	public override bool SpawnedInSession
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public string Caliber => GetTemplate<AmmoTemplate>().Caliber.Replace("Caliber", string.Empty);

	public float GetBulletSpeed => GetTemplate<AmmoTemplate>().InitialSpeed;

	public float DurabilityBurnModificator => GetTemplate<AmmoTemplate>().DurabilityBurnModificator;

	public float HeatFactor => GetTemplate<AmmoTemplate>().HeatFactor;

	public float BulletMassGram => GetTemplate<AmmoTemplate>().BulletMassGram;

	public float BulletDiameterMilimeters => GetTemplate<AmmoTemplate>().BulletDiameterMilimeters;

	public AmmoTemplate AmmoTemplate => Template as AmmoTemplate;

	public override IEnumerable<EItemInfoButton> ItemInteractionButtons => base.ItemInteractionButtons.Append(EItemInfoButton.ApplyMagPreset);

	public float AmmoFactor => GetTemplate<AmmoTemplate>().AmmoFactor;

	public string ammoType => GetTemplate<AmmoTemplate>().ammoType;

	public int Damage => GetTemplate<AmmoTemplate>().Damage;

	public int ammoAccr => GetTemplate<AmmoTemplate>().ammoAccr;

	public int ammoRec => GetTemplate<AmmoTemplate>().ammoRec;

	public int ArmorDamage => GetTemplate<AmmoTemplate>().ArmorDamage;

	public float ArmorDamagePortion => GetTemplate<AmmoTemplate>().ArmorDamagePortion;

	public int ammoDist => GetTemplate<AmmoTemplate>().ammoDist;

	public int buckshotBullets => GetTemplate<AmmoTemplate>().buckshotBullets;

	public int PenetrationPower => GetTemplate<AmmoTemplate>().PenetrationPower;

	public float PenetrationPowerDiviation => GetTemplate<AmmoTemplate>().PenetrationPowerDiviation;

	public int ammoHear => GetTemplate<AmmoTemplate>().ammoHear;

	public string ammoSfx => GetTemplate<AmmoTemplate>().ammoSfx;

	public float MisfireChance => GetTemplate<AmmoTemplate>().MisfireChance;

	public int MinFragmentsCount => GetTemplate<AmmoTemplate>().MinFragmentsCount;

	public int MaxFragmentsCount => GetTemplate<AmmoTemplate>().MaxFragmentsCount;

	public int ammoShiftChance => GetTemplate<AmmoTemplate>().ammoShiftChance;

	public string casingName => GetTemplate<AmmoTemplate>().casingName;

	public float casingEjectPower => GetTemplate<AmmoTemplate>().casingEjectPower;

	public float casingMass => GetTemplate<AmmoTemplate>().casingMass;

	public string casingSounds => GetTemplate<AmmoTemplate>().casingSounds;

	public int ProjectileCount => GetTemplate<AmmoTemplate>().ProjectileCount;

	public float InitialSpeed => GetTemplate<AmmoTemplate>().InitialSpeed;

	public float PenetrationDamageMod => GetTemplate<AmmoTemplate>().PenetrationDamageMod;

	public float PenetrationChanceObstacle => GetTemplate<AmmoTemplate>().PenetrationChanceObstacle;

	public float RicochetChance => GetTemplate<AmmoTemplate>().RicochetChance;

	public float FragmentationChance => GetTemplate<AmmoTemplate>().FragmentationChance;

	public float BallisticCoeficient => GetTemplate<AmmoTemplate>().BallisticCoeficient;

	public bool Tracer => GetTemplate<AmmoTemplate>().Tracer;

	public TaxonomyColor TracerColor => GetTemplate<AmmoTemplate>().TracerColor;

	public float TracerDistance => GetTemplate<AmmoTemplate>().TracerDistance;

	public float StaminaBurnRate => GetTemplate<AmmoTemplate>().StaminaBurnPerDamage;

	public float HeavyBleedingDelta => GetTemplate<AmmoTemplate>().HeavyBleedingDelta;

	public float LightBleedingDelta => GetTemplate<AmmoTemplate>().LightBleedingDelta;

	public bool ShowBullet => GetTemplate<AmmoTemplate>().ShowBullet;

	public float AmmoLifeTimeSec => GetTemplate<AmmoTemplate>().AmmoLifeTimeSec;

	public float MalfMisfireChance => GetTemplate<AmmoTemplate>().MalfMisfireChance;

	public float MalfFeedChance => GetTemplate<AmmoTemplate>().MalfFeedChance;

	public AmmoItemClass(string id, AmmoTemplate template)
		: base(id, template)
	{
		Attributes = template.GetCachedReadonlyQualities();
		SonicType = template.GetCachedSonicType();
	}

	public static GStruct154<GInterface433> ApplyToAmmo(Item from, Item to, int count, TraderControllerClass itemController, bool simulate)
	{
		int count2 = Mathf.Min(from.StackMaxSize, from.StackObjectsCount, to.StackMaxSize - to.StackObjectsCount, count);
		GStruct154<GClass3425> source = InteractionsHandlerClass.TransferMaxStackCount(from, to, count2, itemController, simulate: true);
		if (source.Failed)
		{
			return source.Error;
		}
		if (source.Value.Count == 0)
		{
			return new GClass1522("Nothing to tranfer");
		}
		if (from.StackObjectsCount == source.Value.Count)
		{
			return GClass1617.Cast<GClass3417, GInterface433>(InteractionsHandlerClass.Merge(from, to, itemController, simulate));
		}
		if (!simulate)
		{
			source.Value.Execute();
		}
		return GClass1617.Cast<GClass3425, GInterface433>(source);
	}

	public static GStruct154<GInterface433> ApplyToAddress(Item from, ItemAddress toLocation, int count, TraderControllerClass itemController, bool simulate)
	{
		int num = Mathf.Min(from.StackMaxSize, from.StackObjectsCount, count);
		if (num == from.StackObjectsCount)
		{
			return GClass1617.Cast<GClass3411, GInterface433>(InteractionsHandlerClass.Move(from, toLocation, itemController, simulate));
		}
		return GClass1617.Cast<GClass3424, GInterface433>(InteractionsHandlerClass.SplitMax(from, num, toLocation, itemController, itemController, simulate));
	}

	public AmmoItemClass Clone(string id)
	{
		return new AmmoItemClass(id, (AmmoTemplate)Template)
		{
			IsUsed = IsUsed
		};
	}
}
