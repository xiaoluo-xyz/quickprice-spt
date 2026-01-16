using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JsonType;
using UnityEngine;

namespace EFT.InventoryLogic;

public class AmmoTemplate : StackableItemTemplateClass, GInterface386, IItemTemplate
{
	[Serializable]
	[CompilerGenerated]
	public class Class2332
	{
		public static readonly Class2332 class2332_0 = new Class2332();

		public static Func<EItemAttributeDisplayType> func_0;

		public static Func<EItemAttributeDisplayType> func_1;

		public static Func<float> func_2;

		public static Func<EItemAttributeDisplayType> func_3;

		public static Func<EItemAttributeDisplayType> func_4;

		public static Func<EItemAttributeDisplayType> func_5;

		public static Func<EItemAttributeDisplayType> func_6;

		public static Func<EItemAttributeDisplayType> func_7;

		public static Func<EItemAttributeDisplayType> func_8;

		public static Func<EItemAttributeDisplayType> func_9;

		public EItemAttributeDisplayType method_0()
		{
			return EItemAttributeDisplayType.Compact;
		}

		public EItemAttributeDisplayType method_1()
		{
			return EItemAttributeDisplayType.CompactWithTooltip;
		}

		public float method_2()
		{
			return 0f;
		}

		public EItemAttributeDisplayType method_3()
		{
			return EItemAttributeDisplayType.Compact;
		}

		public EItemAttributeDisplayType method_4()
		{
			return EItemAttributeDisplayType.Compact;
		}

		public EItemAttributeDisplayType method_5()
		{
			return EItemAttributeDisplayType.Compact;
		}

		public EItemAttributeDisplayType method_6()
		{
			return EItemAttributeDisplayType.Compact;
		}

		public EItemAttributeDisplayType method_7()
		{
			return EItemAttributeDisplayType.Compact;
		}

		public EItemAttributeDisplayType method_8()
		{
			return EItemAttributeDisplayType.Compact;
		}

		public EItemAttributeDisplayType method_9()
		{
			return EItemAttributeDisplayType.Compact;
		}
	}

	[NonSerialized]
	public static float MaxMalfMisfireChance;

	[NonSerialized]
	public static float MaxMalfFeedChance;

	[NonSerialized]
	public static string[] MalfChancesKeys = new string[6] { "Malfunction/NoneChance", "Malfunction/VeryLowChance", "Malfunction/LowChance", "Malfunction/MediumChance", "Malfunction/HighChance", "Malfunction/VeryHighChance" };

	public string ammoType;

	public int Damage;

	public int ammoAccr;

	public int ammoRec;

	public int ammoDist;

	public int buckshotBullets;

	public int PenetrationPower = 40;

	public float PenetrationPowerDiviation;

	public int ammoHear;

	public string ammoSfx;

	public float MisfireChance;

	public int MinFragmentsCount = 2;

	public int MaxFragmentsCount = 3;

	public int ammoShiftChance;

	public string casingName;

	public float casingEjectPower;

	public float casingMass;

	public string casingSounds;

	public int ProjectileCount = 1;

	public float InitialSpeed = 700f;

	public float PenetrationDamageMod = 0.1f;

	public float PenetrationChanceObstacle = 0.2f;

	public float RicochetChance = 0.1f;

	public float FragmentationChance = 0.03f;

	public float BallisticCoeficient = 1f;

	public bool Tracer;

	public TaxonomyColor TracerColor;

	public float TracerDistance;

	public int ArmorDamage;

	public string Caliber;

	public float StaminaBurnPerDamage;

	public bool HasGrenaderComponent;

	public float FuzeArmTimeSec;

	public float MinExplosionDistance;

	public float MaxExplosionDistance;

	public int FragmentsCount;

	public string FragmentType;

	public string ExplosionType;

	public bool ShowHitEffectOnExplode;

	public float ExplosionStrength;

	public bool ShowBullet;

	public float AmmoLifeTimeSec = 2f;

	public float MalfMisfireChance;

	public float MalfFeedChance;

	public Vector3 ArmorDistanceDistanceDamage;

	public Vector3 Contusion;

	public Vector3 Blindness;

	public float LightBleedingDelta;

	public float HeavyBleedingDelta;

	public bool IsLightAndSoundShot;

	public float LightAndSoundShotAngle;

	public float LightAndSoundShotSelfContusionTime;

	public float LightAndSoundShotSelfContusionStrength;

	public float DurabilityBurnModificator = 1f;

	public float HeatFactor = 1f;

	public float BulletMassGram;

	public float BulletDiameterMilimeters;

	public bool RemoveShellAfterFire;

	public string airDropTemplateId;

	public FlareEventType[] FlareTypes;

	[NonSerialized]
	public List<ItemAttributeClass> CachedQualities;

	[NonSerialized]
	public SonicBulletSoundPlayer.SonicType? CachedSonicType;

	public float AmmoFactor
	{
		get
		{
			if (ammoAccr <= 0)
			{
				return (100f + (float)Mathf.Abs(ammoAccr)) / 100f;
			}
			return 100f / (float)(100 + ammoAccr);
		}
	}

	public float ArmorDamagePortion => (float)ArmorDamage / 100f;

	float GInterface386.FuzeArmTimeSec => FuzeArmTimeSec;

	float GInterface386.MinExplosionDistance => MinExplosionDistance;

	float GInterface386.MaxExplosionDistance => MaxExplosionDistance;

	int GInterface386.FragmentsCount => FragmentsCount;

	string GInterface386.FragmentType => FragmentType;

	string GInterface386.ExplosionType => ExplosionType;

	float GInterface386.ExplosionStrength => ExplosionStrength;

	bool GInterface386.ShowHitEffectOnExplode => ShowHitEffectOnExplode;

	Vector3 GInterface386.ArmorDistanceDistanceDamage => ArmorDistanceDistanceDamage;

	Vector3 GInterface386.Contusion => Contusion;

	Vector3 GInterface386.Blindness => Blindness;

	public bool GrenadeComponentIsDummy
	{
		get
		{
			if (Blindness.y < float.Epsilon && Contusion.y < float.Epsilon)
			{
				return MaxExplosionDistance < float.Epsilon;
			}
			return false;
		}
	}

	public string AirDropTemplateId => airDropTemplateId;

	public bool ContainsType(FlareEventType flareEventType)
	{
		if (FlareTypes == null)
		{
			return false;
		}
		FlareEventType[] flareTypes = FlareTypes;
		int num = 0;
		while (true)
		{
			if (num < flareTypes.Length)
			{
				if (flareTypes[num] == flareEventType)
				{
					break;
				}
				num++;
				continue;
			}
			return false;
		}
		return true;
	}

	public override void OnInit()
	{
		MaxMalfFeedChance = Math.Max(MalfFeedChance, MaxMalfFeedChance);
		MaxMalfMisfireChance = Math.Max(MalfMisfireChance, MaxMalfMisfireChance);
	}

	public List<ItemAttributeClass> GetCachedReadonlyQualities()
	{
		if (CachedQualities != null)
		{
			return CachedQualities;
		}
		CachedQualities = new List<ItemAttributeClass>(6);
		SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.MaxAmmoDamage)
		{
			Name = GClass3374.GetName(EItemAttributeId.MaxAmmoDamage),
			Base = () => Damage,
			StringValue = () => Damage.ToString(),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.AmmoPenetrationPower)
		{
			Name = GClass3374.GetName(EItemAttributeId.AmmoPenetrationPower),
			Base = () => PenetrationPower,
			StringValue = () => PenetrationPower.ToString(),
			MultiLineInfo = () => new GClass3843(PenetrationPower),
			DisplayType = () => EItemAttributeDisplayType.CompactWithTooltip
		});
		CachedQualities.Add(new ItemAttributeClass(EItemAttributeId.Caliber)
		{
			Name = GClass3374.GetName(EItemAttributeId.Caliber),
			Base = () => 0f,
			StringValue = () => GClass2348.Localized(Caliber),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		if (ProjectileCount > 1)
		{
			SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.AmmoProjectileCount)
			{
				Name = GClass3374.GetName(EItemAttributeId.AmmoProjectileCount),
				Base = () => ProjectileCount,
				StringValue = () => ProjectileCount.ToString(),
				DisplayType = () => EItemAttributeDisplayType.Compact
			});
		}
		SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.BulletSpeed)
		{
			Name = GClass3374.GetName(EItemAttributeId.BulletSpeed),
			Base = () => InitialSpeed,
			StringValue = () => InitialSpeed + " " + GClass2348.Localized("m/s"),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.CenterOfImpact)
		{
			Name = GClass3374.GetName(EItemAttributeId.CenterOfImpact),
			Base = () => ammoAccr,
			StringValue = () => ammoAccr.ToString("F1") + "%",
			DisplayType = () => EItemAttributeDisplayType.Compact,
			LabelVariations = EItemAttributeLabelVariations.Colored
		});
		SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.Recoil)
		{
			Name = GClass3374.GetName(EItemAttributeId.Recoil),
			Base = () => ammoRec,
			StringValue = () => ammoRec.ToString(),
			DisplayType = () => EItemAttributeDisplayType.Compact,
			LabelVariations = EItemAttributeLabelVariations.Colored,
			LessIsGood = true
		});
		SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.HeavyBleedingDelta)
		{
			Name = GClass3374.GetName(EItemAttributeId.HeavyBleedingDelta),
			Base = () => HeavyBleedingDelta,
			StringValue = () => HeavyBleedingDelta.ToString("P0"),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.LightBleedingDelta)
		{
			Name = GClass3374.GetName(EItemAttributeId.LightBleedingDelta),
			Base = () => LightBleedingDelta,
			StringValue = () => LightBleedingDelta.ToString("P0"),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		return CachedQualities;
	}

	public void SafelyAddQualityToList(ItemAttributeClass itemAttribute)
	{
		if (!GClass855.IsZero(itemAttribute.Base()))
		{
			CachedQualities.Add(itemAttribute);
		}
	}

	public SonicBulletSoundPlayer.SonicType GetCachedSonicType()
	{
		if (CachedSonicType.HasValue)
		{
			return CachedSonicType.Value;
		}
		if (!casingSounds.Contains("pistol") && !casingSounds.Contains("40mm"))
		{
			if (casingSounds.Contains("762"))
			{
				CachedSonicType = SonicBulletSoundPlayer.SonicType.Sonic762;
			}
			else if (!casingSounds.Contains("shotgun") && !casingSounds.Contains("50cal"))
			{
				CachedSonicType = SonicBulletSoundPlayer.SonicType.Sonic545;
			}
			else
			{
				CachedSonicType = SonicBulletSoundPlayer.SonicType.SonicShotgun;
			}
		}
		else
		{
			CachedSonicType = SonicBulletSoundPlayer.SonicType.Sonic9;
		}
		return CachedSonicType.Value;
	}

	public override List<IItemComponent> CreateReadonlyComponentsCollection()
	{
		List<IItemComponent> list = base.CreateReadonlyComponentsCollection();
		if (HasGrenaderComponent)
		{
			list.Add(new ExplosiveItemComponentClass(this));
		}
		return list;
	}

	[CompilerGenerated]
	public float method_0()
	{
		return Damage;
	}

	[CompilerGenerated]
	public string method_1()
	{
		return Damage.ToString();
	}

	[CompilerGenerated]
	public float method_2()
	{
		return PenetrationPower;
	}

	[CompilerGenerated]
	public string method_3()
	{
		return PenetrationPower.ToString();
	}

	[CompilerGenerated]
	public GClass3843 method_4()
	{
		return new GClass3843(PenetrationPower);
	}

	[CompilerGenerated]
	public string method_5()
	{
		return GClass2348.Localized(Caliber);
	}

	[CompilerGenerated]
	public float method_6()
	{
		return ProjectileCount;
	}

	[CompilerGenerated]
	public string method_7()
	{
		return ProjectileCount.ToString();
	}

	[CompilerGenerated]
	public float method_8()
	{
		return InitialSpeed;
	}

	[CompilerGenerated]
	public string method_9()
	{
		return InitialSpeed + " " + GClass2348.Localized("m/s");
	}

	[CompilerGenerated]
	public float method_10()
	{
		return ammoAccr;
	}

	[CompilerGenerated]
	public string method_11()
	{
		return ammoAccr.ToString("F1") + "%";
	}

	[CompilerGenerated]
	public float method_12()
	{
		return ammoRec;
	}

	[CompilerGenerated]
	public string method_13()
	{
		return ammoRec.ToString();
	}

	[CompilerGenerated]
	public float method_14()
	{
		return HeavyBleedingDelta;
	}

	[CompilerGenerated]
	public string method_15()
	{
		return HeavyBleedingDelta.ToString("P0");
	}

	[CompilerGenerated]
	public float method_16()
	{
		return LightBleedingDelta;
	}

	[CompilerGenerated]
	public string method_17()
	{
		return LightBleedingDelta.ToString("P0");
	}
}
