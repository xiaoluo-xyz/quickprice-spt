using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Comfort.Common;
using Diz.LanguageExtensions;
using JetBrains.Annotations;
using UnityEngine;

namespace EFT.InventoryLogic;

[Serializable]
public class Weapon : CompoundItem, IWeapon
{
	[Flags]
	public enum EFireMode : byte
	{
		fullauto = 0,
		single = 1,
		doublet = 2,
		burst = 3,
		doubleaction = 4,
		semiauto = 5,
		grenadeThrowing = 6,
		greanadePlanting = 7
	}

	public enum EReloadMode
	{
		ExternalMagazine,
		InternalMagazine,
		OnlyBarrel,
		ExternalMagazineWithInternalReloadSupport
	}

	public enum EMalfunctionState
	{
		None,
		Misfire,
		Jam,
		HardSlide,
		SoftSlide,
		Feed
	}

	[Flags]
	public enum EMalfunctionSource
	{
		Durability = 0,
		Ammo = 1,
		Magazine = 2,
		Overheat = 4,
		ConsoleCommand = 8,
		Effect = 0x10
	}

	public class WeaponMalfunctionStateClass
	{
		[Serializable]
		[CompilerGenerated]
		public class Class2391
		{
			public static readonly Class2391 class2391_0 = new Class2391();

			public static Func<MongoID, string> func_0;

			public static Func<MongoID, string> func_1;

			public string method_0(MongoID id)
			{
				return id.ToString();
			}

			public string method_1(MongoID id)
			{
				return id.ToString();
			}
		}

		public AmmoItemClass AmmoToFire;

		public AmmoItemClass AmmoWillBeLoadedToChamber;

		public AmmoItemClass MalfunctionedAmmo;

		public float LastShotOverheat;

		public float LastShotTime;

		public float LastMalfunctionTime;

		public float OverheatBarrelMoveMult;

		public Vector2 OverheatBarrelMoveDir;

		public float OverheatFirerateMult;

		public bool OverheatFirerateMultInited;

		public bool SlideOnOverheatReached;

		public bool AutoshotChanceInited;

		public float AutoshotTime;

		[NonSerialized]
		public EMalfunctionState State_1;

		[NonSerialized]
		public List<string> PlayersWhoKnowAboutMalfunction_1 = new List<string>(3);

		[NonSerialized]
		public List<string> PlayersWhoKnowMalfType_1 = new List<string>(3);

		[NonSerialized]
		public Dictionary<string, EMalfunctionSource> PlayersReducedMalfChances_1 = new Dictionary<string, EMalfunctionSource>(3);

		public IEnumerable<string> PlayersWhoKnowAboutMalfunction => PlayersWhoKnowAboutMalfunction_1;

		public IEnumerable<string> PlayersWhoKnowMalfType => PlayersWhoKnowMalfType_1;

		public IDictionary<string, EMalfunctionSource> PlayersReducedMalfChances => PlayersReducedMalfChances_1;

		public bool IsAnyMalfExceptMisfire
		{
			get
			{
				if (State_1 != EMalfunctionState.Feed && State_1 != EMalfunctionState.Jam && State_1 != EMalfunctionState.HardSlide)
				{
					return State_1 == EMalfunctionState.SoftSlide;
				}
				return true;
			}
		}

		public EMalfunctionState State
		{
			get
			{
				return State_1;
			}
			set
			{
				if (State_1 != value)
				{
					State_1 = value;
					this.OnStateChanged?.Invoke();
				}
			}
		}

		[field: NonSerialized]
		public EMalfunctionSource Source { get; set; }

		public event Action OnStateChanged;

		public void ChangeStateSilent(EMalfunctionState state)
		{
			State_1 = state;
		}

		public bool IsKnownMalfunction(string profileId)
		{
			if (State != EMalfunctionState.None)
			{
				return PlayersWhoKnowAboutMalfunction_1.Contains(profileId);
			}
			return false;
		}

		public bool IsKnownMalfType(string profileId)
		{
			if (State != EMalfunctionState.None)
			{
				return PlayersWhoKnowMalfType_1.Contains(profileId);
			}
			return false;
		}

		public void AddPlayerWhoKnowMalfunction(string playerId, bool clearRest = false)
		{
			if (clearRest)
			{
				PlayersWhoKnowAboutMalfunction_1.Clear();
			}
			else if (PlayersWhoKnowAboutMalfunction_1.Contains(playerId))
			{
				return;
			}
			PlayersWhoKnowAboutMalfunction_1.Add(playerId);
			this.OnStateChanged?.Invoke();
		}

		public void AddPlayerWhoKnowMalfType(string playerId)
		{
			if (!PlayersWhoKnowMalfType_1.Contains(playerId))
			{
				PlayersWhoKnowMalfType_1.Add(playerId);
				if (!PlayersWhoKnowAboutMalfunction_1.Contains(playerId))
				{
					PlayersWhoKnowAboutMalfunction_1.Add(playerId);
				}
				this.OnStateChanged?.Invoke();
			}
		}

		public void ClearPlayersWhoKnow()
		{
			PlayersWhoKnowAboutMalfunction_1.Clear();
			PlayersWhoKnowMalfType_1.Clear();
			this.OnStateChanged?.Invoke();
		}

		public void Repair()
		{
			State_1 = EMalfunctionState.None;
			ClearPlayersWhoKnow();
		}

		public bool HasMalfReduceChance(string profileId, EMalfunctionSource malfSource)
		{
			if (PlayersReducedMalfChances_1.TryGetValue(profileId, out var value))
			{
				return value.HasFlag(malfSource);
			}
			return false;
		}

		public void AddMalfReduceChance(string profileId, EMalfunctionSource malfSource)
		{
			if (!PlayersReducedMalfChances_1.ContainsKey(profileId))
			{
				PlayersReducedMalfChances_1.Add(profileId, malfSource);
			}
			else if ((PlayersReducedMalfChances_1[profileId] & malfSource) != malfSource)
			{
				PlayersReducedMalfChances_1[profileId] |= malfSource;
			}
		}

		public void CopyFrom(GClass1917 descriptor, ItemFactoryClass itemFactory)
		{
			State_1 = (EMalfunctionState)descriptor.Malfunction;
			LastShotOverheat = descriptor.LastShotOverheat;
			SlideOnOverheatReached = descriptor.SlideOnOverheatReached;
			LastShotTime = descriptor.LastShotTime;
			PlayersWhoKnowAboutMalfunction_1.Clear();
			PlayersWhoKnowAboutMalfunction_1.AddRange(descriptor.PlayersWhoKnowAboutMalfunction.Select((MongoID id) => id.ToString()));
			PlayersWhoKnowMalfType_1.Clear();
			PlayersWhoKnowMalfType_1.AddRange(descriptor.PlayersWhoKnowMalfType.Select((MongoID id) => id.ToString()));
			PlayersReducedMalfChances_1.Clear();
			foreach (var (mongoID2, value) in descriptor.PlayersReducedMalfChances)
			{
				PlayersReducedMalfChances_1.Add(mongoID2, (EMalfunctionSource)value);
			}
			MongoID? ammoToFireTemplateId = descriptor.AmmoToFireTemplateId;
			MongoID? ammoWillBeLoadedToChamberTemplateId = descriptor.AmmoWillBeLoadedToChamberTemplateId;
			MongoID? ammoMalfunctionedTemplateId = descriptor.AmmoMalfunctionedTemplateId;
			if (ammoToFireTemplateId.HasValue)
			{
				AmmoToFire = (AmmoItemClass)itemFactory.CreateItem(MongoID.Generate(), ammoToFireTemplateId.Value, null);
			}
			if (ammoWillBeLoadedToChamberTemplateId.HasValue)
			{
				AmmoWillBeLoadedToChamber = (AmmoItemClass)itemFactory.CreateItem(MongoID.Generate(), ammoWillBeLoadedToChamberTemplateId.Value, null);
			}
			if (ammoMalfunctionedTemplateId.HasValue)
			{
				MalfunctionedAmmo = (AmmoItemClass)itemFactory.CreateItem(MongoID.Generate(), ammoMalfunctionedTemplateId.Value, null);
			}
			this.OnStateChanged?.Invoke();
		}
	}

	[Serializable]
	[CompilerGenerated]
	public class Class2392
	{
		public static readonly Class2392 class2392_0 = new Class2392();

		public static Func<EItemAttributeDisplayType> func_0;

		public static Func<EItemAttributeDisplayType> func_1;

		public static Func<EItemAttributeDisplayType> func_2;

		public static Func<EItemAttributeDisplayType> func_3;

		public static Func<EItemAttributeDisplayType> func_4;

		public static Func<EItemAttributeDisplayType> func_5;

		public static Func<EItemAttributeDisplayType> func_6;

		public static Func<EItemAttributeDisplayType> func_7;

		public static Func<EItemAttributeDisplayType> func_8;

		public static Func<EItemAttributeDisplayType> func_9;

		public static Func<Mod, bool> func_10;

		public static Func<Mod, float> func_11;

		public static Func<Mod, float> func_12;

		public static Func<Mod, bool> func_13;

		public static Func<Mod, float> func_14;

		public static Func<Mod, float> func_15;

		public static Func<Mod, float> func_16;

		public static Func<Slot, bool> func_17;

		public static Func<Slot, bool> func_18;

		public static Func<Mod, int> func_19;

		public static Func<Mod, bool> func_20;

		public static Func<Mod, float> func_21;

		public static Func<Slot, bool> func_22;

		public static Func<Slot, Item> func_23;

		public static Predicate<Slot> predicate_0;

		public static Func<Slot, bool> func_24;

		public static Func<Slot, bool> func_25;

		public EItemAttributeDisplayType method_0()
		{
			return EItemAttributeDisplayType.FullBar;
		}

		public EItemAttributeDisplayType method_1()
		{
			return EItemAttributeDisplayType.FullBar;
		}

		public EItemAttributeDisplayType method_2()
		{
			return EItemAttributeDisplayType.FullBar;
		}

		public EItemAttributeDisplayType method_3()
		{
			return EItemAttributeDisplayType.FullBar;
		}

		public EItemAttributeDisplayType method_4()
		{
			return EItemAttributeDisplayType.FullBar;
		}

		public EItemAttributeDisplayType method_5()
		{
			return EItemAttributeDisplayType.FullBar;
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

		public bool method_10(Mod mod)
		{
			return !(mod is StockItemClass);
		}

		public float method_11(Mod mod)
		{
			return mod.Template.Recoil;
		}

		public float method_12(Mod mod)
		{
			return mod.Template.Recoil;
		}

		public bool method_13(Mod mod)
		{
			return mod is StockItemClass;
		}

		public float method_14(Mod mod)
		{
			return mod.Template.Recoil;
		}

		public float method_15(Mod mod)
		{
			return mod.Template.Ergonomics;
		}

		public float method_16(Mod x)
		{
			return x.SightingRange;
		}

		public bool method_17(Slot x)
		{
			return x.ID.Contains("mod_tactical");
		}

		public bool method_18(Slot x)
		{
			return x.ContainedItem == null;
		}

		public int method_19(Mod mod)
		{
			return mod.Accuracy;
		}

		public bool method_20(Mod mod)
		{
			return mod is StockItemClass;
		}

		public float method_21(Mod mod)
		{
			return mod.Template.Velocity;
		}

		public bool method_22(Slot x)
		{
			return !x.ID.StartsWith("chamber");
		}

		public Item method_23(Slot slot)
		{
			return slot.ContainedItem;
		}

		public bool method_24(Slot x)
		{
			return x.ID == EWeaponModType.mod_magazine.ToString();
		}

		public bool method_25(Slot slot)
		{
			return slot.ContainedItem is LauncherItemClass;
		}

		public bool method_26(Slot slot)
		{
			return slot.ContainedItem is LauncherItemClass;
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	public struct Struct893
	{
		public TraderControllerClass itemController;
	}

	public const string WEAPON_CLASS_PISTOL = "pistol";

	public const float MOA_ON_100_METERS = 2.9089f;

	[NonSerialized]
	public const float ERGONOMICS_MIN = 0f;

	[NonSerialized]
	public const float ERGONOMICS_MAX = 100f;

	[GAttribute26]
	public readonly RepairableComponent Repairable;

	[GAttribute26]
	public readonly FoldableComponent Foldable;

	[GAttribute26]
	public readonly FireModeComponent FireMode;

	[GAttribute26]
	public BuffComponent Buff;

	[NonSerialized]
	public static IEnumerable<EItemInfoButton> WeaponInteractions = new List<EItemInfoButton>
	{
		EItemInfoButton.Modding,
		EItemInfoButton.EditBuild,
		EItemInfoButton.ApplyMagPreset,
		EItemInfoButton.Reload,
		EItemInfoButton.Load,
		EItemInfoButton.Unload,
		EItemInfoButton.UnloadAmmo,
		EItemInfoButton.Equip,
		EItemInfoButton.Unequip,
		EItemInfoButton.Disassemble
	};

	[NonSerialized]
	public Vector3[] OpticCalibrationPoints;

	[NonSerialized]
	public GClass3735 OpticTrajectoryInfosForAGS;

	[NonSerialized]
	public Slot MagSlotCache;

	[NonSerialized]
	public int LastModsCalculateFrame;

	[NonSerialized]
	public IEnumerable<Mod> Mods_1;

	public global::BindableStateClass<int> AimIndex = new global::BindableStateClass<int>();

	public bool CompatibleAmmo;

	public bool Armed;

	public bool IsUnderBarrelDeviceActive;

	public bool CylinderHammerClosed;

	[NonSerialized]
	public AmmoTemplate[] ShellsInChambers_1;

	public const float OVERHEAT_PROBLEMS_START = 100f;

	[NonSerialized]
	public static List<Mod> PreallocatedMods = new List<Mod>(63);

	public EReloadMode ReloadMode
	{
		get
		{
			MagazineItemClass currentMagazine = GetCurrentMagazine();
			EReloadMode reloadMode = GetTemplate<WeaponTemplate>().ReloadMode;
			if (currentMagazine == null)
			{
				return reloadMode;
			}
			EReloadMode reloadMagType = currentMagazine.ReloadMagType;
			if (reloadMagType == reloadMode)
			{
				return reloadMode;
			}
			if (reloadMode != EReloadMode.InternalMagazine || reloadMagType != EReloadMode.ExternalMagazine)
			{
				throw new NotImplementedException($"Not supported weapon-magazine reload modes. Weapon reload mode '{reloadMode}', mag reload mode '{reloadMagType}'");
			}
			return EReloadMode.ExternalMagazineWithInternalReloadSupport;
		}
	}

	public string WeapClass => GetTemplate<WeaponTemplate>().weapClass;

	public EFireMode[] WeapFireType => GetTemplate<WeaponTemplate>().weapFireType;

	public float RecoilForceBack => GetTemplate<WeaponTemplate>().RecoilForceBack;

	public bool IsBoltCatch => GetTemplate<WeaponTemplate>().isBoltCatch;

	public bool MustBoltBeOpennedForExternalReload => GetTemplate<WeaponTemplate>().MustBoltBeOpennedForExternalReload;

	public bool MustBoltBeOpennedForInternalReload => GetTemplate<WeaponTemplate>().MustBoltBeOpennedForInternalReload;

	public bool BoltAction => GetTemplate<WeaponTemplate>().BoltAction;

	public float DoubleActionAccuracyPenalty => GetTemplate<WeaponTemplate>().DoubleActionAccuracyPenalty;

	public bool CompactHandling => GetTemplate<WeaponTemplate>().CompactHandling;

	public bool ManualBoltCatch => GetTemplate<WeaponTemplate>().ManualBoltCatch;

	public float SightingRange => GetTemplate<WeaponTemplate>().SightingRange;

	public int FireRate => GetTemplate<WeaponTemplate>().bFirerate;

	public bool AllowJam => GetTemplate<WeaponTemplate>().AllowJam;

	public bool AllowFeed => GetTemplate<WeaponTemplate>().AllowFeed;

	public bool AllowMisfire => GetTemplate<WeaponTemplate>().AllowMisfire;

	public bool AllowSlide => GetTemplate<WeaponTemplate>().AllowSlide;

	public bool AllowOverheat => GetTemplate<WeaponTemplate>().AllowOverheat;

	public bool AllowMalfunction
	{
		get
		{
			if (!AllowJam && !AllowFeed && !AllowMisfire)
			{
				return AllowSlide;
			}
			return true;
		}
	}

	public float BaseMalfunctionChance => GetTemplate<WeaponTemplate>().BaseMalfunctionChance;

	public float DurabilityBurnRatio => GetTemplate<WeaponTemplate>().DurabilityBurnRatio;

	public float HeatFactorGun => GetTemplate<WeaponTemplate>().HeatFactorGun;

	public float CoolFactorGun => GetTemplate<WeaponTemplate>().CoolFactorGun;

	public float HeatFactorByShot => GetTemplate<WeaponTemplate>().HeatFactorByShot;

	public bool IsFlareGun => GetTemplate<WeaponTemplate>().IsFlareGun;

	public bool IsOneOff => GetTemplate<WeaponTemplate>().IsOneoff;

	public bool IsGrenadeLauncher => GetTemplate<WeaponTemplate>().IsGrenadeLauncher;

	public bool NoFiremodeOnBoltcatch => GetTemplate<WeaponTemplate>().NoFiremodeOnBoltcatch;

	public string AmmoCaliber => GetTemplate<WeaponTemplate>().ammoCaliber.Replace("Caliber", string.Empty);

	public bool IsStationaryWeapon => GetTemplate<WeaponTemplate>().IsStationaryWeapon;

	public bool IsBeltMachineGun => GetTemplate<WeaponTemplate>().IsBeltMachineGun;

	public bool BlockLeftStance => GetTemplate<WeaponTemplate>().BlockLeftStance;

	public bool WithAnimatorAiming => GetTemplate<WeaponTemplate>().WithAnimatorAiming;

	public bool IsMountable => GetTemplate<WeaponTemplate>().IsMountable;

	public bool UseAltMountBone => GetTemplate<WeaponTemplate>().UseAltMountBone;

	public bool CanUnloadAmmoByPlayer => GetTemplate<WeaponTemplate>().CanUnloadAmmoByPlayer;

	public int SingleFireRate
	{
		get
		{
			if (GetTemplate<WeaponTemplate>().SingleFireRate <= 0)
			{
				UnityEngine.Debug.LogErrorFormat($"ALERT! SingleFireRate value is {GetTemplate<WeaponTemplate>().SingleFireRate} for {GetTemplate<WeaponTemplate>()._name}, setting it to {240}");
				GetTemplate<WeaponTemplate>().SingleFireRate = 240;
			}
			return Mathf.Max(GetTemplate<WeaponTemplate>().SingleFireRate, 240);
		}
	}

	public bool CanQueueSecondShot => GetTemplate<WeaponTemplate>().CanQueueSecondShot;

	[field: NonSerialized]
	public Slot[] Chambers { get; set; }

	public Slot FirstFreeChamberSlot
	{
		get
		{
			int num = 0;
			while (true)
			{
				if (num < Chambers.Length)
				{
					if (Chambers[num].ContainedItem == null)
					{
						break;
					}
					num++;
					continue;
				}
				return null;
			}
			return Chambers[num];
		}
	}

	public Slot FirstLoadedChamberSlot
	{
		get
		{
			int num = 0;
			while (true)
			{
				if (num < Chambers.Length)
				{
					if (Chambers[num].ContainedItem != null)
					{
						break;
					}
					num++;
					continue;
				}
				return null;
			}
			return Chambers[num];
		}
	}

	public Slot[] FreeChambersForLoading
	{
		get
		{
			List<Slot> list = new List<Slot>();
			for (int i = 0; i < Chambers.Length; i++)
			{
				if (Chambers[i].ContainedItem == null || Chambers[i].ContainedItem is AmmoItemClass { IsUsed: not false })
				{
					list.Add(Chambers[i]);
				}
			}
			return list.ToArray();
		}
	}

	public int FreeChamberSlotsCount => FreeChambersForLoading.Length;

	public bool IsMultiBarrel => Chambers.Length > 1;

	public bool SupportsInternalReload
	{
		get
		{
			if (ReloadMode != EReloadMode.InternalMagazine)
			{
				return ReloadMode == EReloadMode.ExternalMagazineWithInternalReloadSupport;
			}
			return true;
		}
	}

	public bool Folded => GetFoldable()?.Folded ?? false;

	public float DeviationCurve => GClass3380.GetItemComponentsInChildren<BarrelComponent>(this).FirstOrDefault()?.Template.DeviationCurve ?? Template.DeviationCurve;

	public float Single_0
	{
		get
		{
			float num = GClass3380.GetItemComponentsInChildren<BarrelComponent>(this).FirstOrDefault()?.Template.DeviationMax ?? Template.DeviationMax;
			if (num != 0f)
			{
				return num;
			}
			return 100f;
		}
	}

	public float RecoilBase => Template.RecoilForceUp + Template.RecoilForceBack;

	public float RecoilDelta => (Folded ? Mods.Where((Mod mod) => !(mod is StockItemClass)).Sum((Mod mod) => mod.Template.Recoil) : Mods.Sum((Mod mod) => mod.Template.Recoil)) / 100f;

	public float StockRecoilDelta => (Folded ? 0f : Mods.Where((Mod mod) => mod is StockItemClass).Sum((Mod mod) => mod.Template.Recoil)) / 100f;

	public float RecoilTotal => RecoilBase + RecoilBase * RecoilDelta;

	public float ErgonomicsTotal => Template.Ergonomics * (1f + ErgonomicsDelta);

	public float ErgonomicsDelta => Mods.Sum((Mod mod) => mod.Template.Ergonomics) / Mathf.Max(1f, Template.Ergonomics);

	public int EmptyTacticalSlotCount => (from x in AllSlots
		where x.ID.Contains("mod_tactical")
		where x.ContainedItem == null
		select x).Count();

	public bool CanReloadFast => Template.isFastReload;

	public bool CanLoadAmmoToChamber => Template.isChamberLoad;

	public float ShotgunDispersionBase => GClass3380.GetItemComponentsInChildren<BarrelComponent>(this).FirstOrDefault()?.Template.ShotgunDispersion ?? ((float)Template.ShotgunDispersion);

	public float TotalShotgunDispersion => ShotgunDispersionBase * (1f + CenterOfImpactDelta);

	public float CenterOfImpactBase => GClass3380.GetItemComponentsInChildren<BarrelComponent>(this).FirstOrDefault()?.Template.CenterOfImpact ?? Template.CenterOfImpact;

	public float CenterOfImpactDelta => (float)(-Mods.Sum((Mod mod) => mod.Accuracy)) / 100f;

	public float StockDoubleActionAccuracyPenaltyMult => Mods.FirstOrDefault((Mod mod) => mod is StockItemClass)?.DoubleActionAccuracyPenaltyMult ?? 1f;

	public float TotalAccuracy => GetTotalCenterOfImpact(includeAmmo: true);

	public float VelocityBase => CurrentAmmoTemplate?.InitialSpeed ?? 0f;

	[CanBeNull]
	public AmmoTemplate CurrentAmmoTemplate
	{
		get
		{
			Slot slot = Chambers.FirstOrDefault();
			AmmoItemClass ammoItemClass = ((slot == null) ? null : (slot.ContainedItem as AmmoItemClass));
			if (ammoItemClass != null)
			{
				return ammoItemClass.Template as AmmoTemplate;
			}
			MagazineItemClass currentMagazine = GetCurrentMagazine();
			if (currentMagazine != null && currentMagazine.Cartridges != null)
			{
				Item item = currentMagazine.FirstRealAmmo();
				if (item != null)
				{
					return item.Template as AmmoTemplate;
				}
			}
			AmmoTemplate defAmmoTemplate = Template.DefAmmoTemplate;
			if (defAmmoTemplate != null)
			{
				return defAmmoTemplate;
			}
			return null;
		}
	}

	public float VelocityDelta => (Mods.Sum((Mod mod) => mod.Template.Velocity) + Template.Velocity) / 100f;

	public float SpeedFactor => 1f + VelocityDelta;

	public float TotalVelocity => VelocityBase * SpeedFactor;

	public override IEnumerable<IContainer> Containers
	{
		get
		{
			foreach (IContainer container in base.Containers)
			{
				yield return container;
			}
			Slot[] chambers = Chambers;
			for (int i = 0; i < chambers.Length; i++)
			{
				yield return chambers[i];
			}
		}
	}

	public override IEnumerable<EItemInfoButton> ItemInteractionButtons => base.ItemInteractionButtons.Concat(WeaponInteractions);

	public Vector3[] OpticCalibrationPointsForAll => OpticCalibrationPoints;

	bool IWeapon.IsUnderbarrelWeapon => false;

	Item IWeapon.Item => this;

	WeaponTemplate IWeapon.WeaponTemplate => Template;

	public override bool CanBePickedUp => !Template.IsStationaryWeapon;

	public new WeaponTemplate Template => GetTemplate<WeaponTemplate>();

	public EFireMode SelectedFireMode => FireMode.FireMode;

	public bool HasChambers => Chambers.Length != 0;

	public int ChamberAmmoCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < Chambers.Length; i++)
			{
				num += ((Chambers[i].ContainedItem is AmmoItemClass { IsUsed: false }) ? 1 : 0);
			}
			return num;
		}
	}

	public int ShellsInWeaponCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < ShellsInChambers.Length; i++)
			{
				num += ((ShellsInChambers[i] != null) ? 1 : 0);
			}
			return num;
		}
	}

	public int ShellsInChamberCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < Chambers.Length; i++)
			{
				num += ((Chambers[i].ContainedItem is AmmoItemClass { IsUsed: not false }) ? 1 : 0);
			}
			return num;
		}
	}

	public IEnumerable<Mod> Mods
	{
		get
		{
			if (LastModsCalculateFrame == Time.frameCount)
			{
				return Mods_1;
			}
			Mods_1 = AllSlots.Select((Slot slot) => slot.ContainedItem).OfType<Mod>();
			LastModsCalculateFrame = Time.frameCount;
			return Mods_1;
		}
	}

	public override IEnumerable<Slot> AllSlots => base.AllSlotsWithoutArrayInside.Concat(Chambers);

	public AmmoTemplate[] ShellsInChambers
	{
		get
		{
			if (GetCurrentMagazine() is CylinderMagazineItemClass cylinderMagazineItemClass && ShellsInChambers_1.Length < cylinderMagazineItemClass.MaxCount)
			{
				ShellsInChambers_1 = new AmmoTemplate[cylinderMagazineItemClass.MaxCount];
			}
			return ShellsInChambers_1;
		}
		set
		{
			ShellsInChambers_1 = value;
		}
	}

	public bool HasShellsInChamberBarrelOnlyWeapon
	{
		get
		{
			AmmoTemplate[] shellsInChambers = ShellsInChambers;
			int num = 0;
			while (true)
			{
				if (num < shellsInChambers.Length)
				{
					if (shellsInChambers[num] != null)
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
	}

	[field: NonSerialized]
	public WeaponMalfunctionStateClass MalfState { get; } = new WeaponMalfunctionStateClass();

	public event Func<EMalfunctionState, bool> OnMalfunctionValidate;

	public FoldableComponent GetFoldable()
	{
		return GClass3380.GetItemComponentsInChildren<FoldableComponent>(this).FirstOrDefault();
	}

	public Weapon(string id, WeaponTemplate template)
		: base(id, template)
	{
		Components.Add(Repairable = new RepairableComponent(this, template));
		if (template.Foldable || template.Retractable)
		{
			Components.Add(Foldable = new FoldableComponent(this, template));
		}
		Components.Add(Buff = new BuffComponent(this));
		Components.Add(FireMode = new FireModeComponent(this, template));
		Chambers = Array.ConvertAll(template.Chambers, (Slot x) => new Slot(x, this));
		ShellsInChambers = new AmmoTemplate[Chambers.Length];
		Attributes.Add(new GClass3378(EItemAttributeId.Ergonomics)
		{
			Name = GClass3374.GetName(EItemAttributeId.Ergonomics),
			Range = new Vector2(0f, 100f),
			Base = () => Mathf.RoundToInt(Template.Ergonomics),
			Delta = () => ErgonomicsDelta,
			StringValue = () => Mathf.Clamp(ErgonomicsTotal, 0f, 100f).ToString("0.##"),
			DisplayType = () => EItemAttributeDisplayType.FullBar
		});
		Attributes.Add(new GClass3378(EItemAttributeId.CenterOfImpact)
		{
			Name = GClass3374.GetName(EItemAttributeId.CenterOfImpact),
			Base = () => CenterOfImpactBase,
			Delta = delegate
			{
				float num = method_9(Repairable.TemplateDurability);
				float num2 = (GetBarrelDeviation() - num) / (Single_0 - num);
				return CenterOfImpactDelta + num2;
			},
			StringValue = () => (GetTotalCenterOfImpact(includeAmmo: true) * GetBarrelDeviation() * 100f / 2.9089f).ToString("0.0#") + " " + GClass2348.Localized("moa"),
			Range = new Vector2(BarrelTemplateClass.MaxCenterOfImpact + 0.1f, 0.001f),
			DisplayType = () => EItemAttributeDisplayType.FullBar,
			LessIsGood = true
		});
		Attributes.Add(new GClass3378(EItemAttributeId.SightingRange)
		{
			Name = GClass3374.GetName(EItemAttributeId.SightingRange),
			Base = () => GetSightingRange(),
			StringValue = () => (!(GetSightingRange() > 0f)) ? "N/A" : GetSightingRange().ToString(),
			Range = new Vector2(0f, 5000f),
			DisplayType = () => EItemAttributeDisplayType.FullBar
		});
		Attributes.Add(new GClass3378(EItemAttributeId.RecoilUp)
		{
			Name = GClass3374.GetName(EItemAttributeId.RecoilUp),
			Range = new Vector2(0f, 1000f),
			LessIsGood = true,
			Base = () => Template.RecoilForceUp,
			Delta = () => RecoilDelta,
			StringValue = () => Mathf.RoundToInt(Template.RecoilForceUp + Template.RecoilForceUp * RecoilDelta).ToString(),
			DisplayType = () => EItemAttributeDisplayType.FullBar
		});
		Attributes.Add(new GClass3378(EItemAttributeId.RecoilBack)
		{
			Name = GClass3374.GetName(EItemAttributeId.RecoilBack),
			Range = new Vector2(0f, 1000f),
			LessIsGood = true,
			Base = () => Template.RecoilForceBack,
			Delta = () => RecoilDelta,
			StringValue = () => Mathf.RoundToInt(Template.RecoilForceBack + Template.RecoilForceBack * RecoilDelta).ToString(),
			DisplayType = () => EItemAttributeDisplayType.FullBar
		});
		Attributes.Add(new GClass3378(EItemAttributeId.Velocity)
		{
			Name = GClass3374.GetName(EItemAttributeId.Velocity),
			Range = new Vector2(0f, 1500f),
			Base = () => VelocityBase,
			Delta = () => VelocityDelta,
			StringValue = () => TotalVelocity.ToString("0") + " " + GClass2348.Localized("m/s"),
			DisplayType = () => EItemAttributeDisplayType.FullBar
		});
		SafelyAddAttributeToList(new GClass3378(EItemAttributeId.WeaponFireType)
		{
			Name = GClass3374.GetName(EItemAttributeId.WeaponFireType),
			Base = () => WeapFireType.Length,
			StringValue = () => GClass2066.CastToStringValue(WeapFireType, ", "),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		SafelyAddAttributeToList(new GClass3378(EItemAttributeId.AmmoCaliber)
		{
			Name = GClass3374.GetName(EItemAttributeId.AmmoCaliber),
			Base = () => AmmoCaliber.Length,
			StringValue = () => GClass2348.Localized(AmmoCaliber),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		SafelyAddAttributeToList(new GClass3378(EItemAttributeId.FireRate)
		{
			Name = GClass3374.GetName(EItemAttributeId.FireRate),
			Base = () => Template.bFirerate,
			StringValue = () => Template.bFirerate + " " + GClass2348.Localized("rpm"),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		SafelyAddAttributeToList(new GClass3378(EItemAttributeId.EffectiveDist)
		{
			Name = GClass3374.GetName(EItemAttributeId.EffectiveDist),
			Base = () => Template.bEffDist,
			StringValue = () => Template.bEffDist + " " + GClass2348.Localized("meters"),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
	}

	public float GetSightingRange()
	{
		float a = 0f;
		if (Mods.Count() > 0)
		{
			a = Mods.Max((Mod x) => x.SightingRange);
		}
		return Mathf.Max(a, SightingRange);
	}

	public float GetTotalCenterOfImpact(bool includeAmmo)
	{
		float num = CenterOfImpactBase * (1f + CenterOfImpactDelta);
		if (!includeAmmo)
		{
			return num;
		}
		return num * (CurrentAmmoTemplate?.AmmoFactor ?? 1f);
	}

	public float GetBarrelDeviation()
	{
		return method_9(Repairable.Durability) * (float)Buff.WeaponSpread;
	}

	public float method_9(float durability)
	{
		float deviationCurve = DeviationCurve;
		float num = 2f * deviationCurve;
		float num2 = ((100f - num == 0f) ? (durability / num) : ((0f - deviationCurve + Mathf.Sqrt((0f - num + 100f) * durability + deviationCurve * deviationCurve)) / (0f - num + 100f)));
		float num3 = 1f - num2;
		float single_ = Single_0;
		return num3 * num3 * single_ + 2f * num2 * num3 * deviationCurve + num2 * num2;
	}

	public bool IsModSuitable(Mod mod)
	{
		Slot[] array = AllSlots.Where((Slot x) => !x.ID.StartsWith("chamber")).ToArray();
		int num = 0;
		while (true)
		{
			if (num < array.Length)
			{
				if (GClass3124.CanAccept(array[num], mod))
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

	public override bool TryFindItem(string itemId, out Item item)
	{
		if (!method_5(itemId, out item) && !GClass3373.TryFindItem(Chambers, itemId, out item))
		{
			return base.TryFindItem(itemId, out item);
		}
		return true;
	}

	public override IContainer GetContainer(string containerId)
	{
		int num = 0;
		while (true)
		{
			if (num < Chambers.Length)
			{
				if (Chambers[num].ID == containerId)
				{
					break;
				}
				num++;
				continue;
			}
			return base.GetContainer(containerId);
		}
		return Chambers[num];
	}

	[OnDeserializing]
	public void method_10(StreamingContext context)
	{
		Buff.DisableComponent();
	}

	public void CreateOpticCalibrationPoints(SightComponent sight)
	{
		for (int i = 0; i < sight.ScopesCount; i++)
		{
			if (sight.OpticCalibrationPoints == null)
			{
				sight.OpticCalibrationPoints = new Vector3[sight.ScopesCount][];
			}
			if (sight.GetScopeCalibrationDistances(i) != null)
			{
				if (sight.OpticCalibrationPoints == null)
				{
					RecalculateOpticCalibrationPoints();
				}
			}
			else if (sight.OpticCalibrationPoints[i] == null)
			{
				sight.OpticCalibrationPoints[i] = new Vector3[0];
			}
		}
	}

	public void RecalculateOpticCalibrationPoints()
	{
		SightComponent[] array = GClass3380.GetComponents<SightComponent>(GClass3380.GetAllItemsFromCollection(this)).ToArray();
		foreach (SightComponent sightComponent in array)
		{
			if (sightComponent.OpticCalibrationPoints == null)
			{
				sightComponent.OpticCalibrationPoints = new Vector3[sightComponent.ScopesCount][];
			}
			for (int j = 0; j < sightComponent.ScopesCount; j++)
			{
				method_11(sightComponent, j);
			}
		}
	}

	public void method_11(SightComponent sight, int scopeIndex)
	{
		List<int> list = new List<int>();
		int[] scopeCalibrationDistances = sight.GetScopeCalibrationDistances(scopeIndex);
		if (scopeCalibrationDistances != null)
		{
			foreach (int item in scopeCalibrationDistances)
			{
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		list.Sort();
		AmmoTemplate defAmmoTemplate = Template.DefAmmoTemplate;
		if (defAmmoTemplate != null)
		{
			OpticCalibrationPoints = CreateOpticCalibrationData(list.ToArray(), defAmmoTemplate, SpeedFactor, 0.001f);
			Vector3[] array = new Vector3[scopeCalibrationDistances.Length];
			for (int j = 0; j < scopeCalibrationDistances.Length; j++)
			{
				int item2 = scopeCalibrationDistances[j];
				int num = list.IndexOf(item2);
				array[j] = OpticCalibrationPoints[num];
			}
			sight.OpticCalibrationPoints[scopeIndex] = array;
		}
		else
		{
			UnityEngine.Debug.LogError("cant find default ammo template for: " + base.Name);
		}
	}

	public Vector3[] CreateOpticCalibrationData(int[] opticCalibrationDistances, AmmoTemplate ammoTemplate, float speedFactor, float deltaTime)
	{
		Vector3[] array = new Vector3[opticCalibrationDistances.Length];
		Vector3 zero = Vector3.zero;
		Vector3 forward = Vector3.forward;
		float num = ammoTemplate.InitialSpeed * speedFactor;
		Vector3 vector = forward * num;
		Vector3 currentPosition = zero;
		Vector3 velocity = vector;
		float num2 = 0f;
		float num3 = 0f;
		EftBulletClass.FormTrajectory(zero, vector, ammoTemplate.BulletMassGram, ammoTemplate.BulletDiameterMilimeters, ammoTemplate.BallisticCoeficient, out var trajectoryInfo);
		for (int i = 0; i < opticCalibrationDistances.Length; i++)
		{
			float num4 = opticCalibrationDistances[i] * opticCalibrationDistances[i];
			while (num2 < num4)
			{
				EftBulletClass.PredictedTrajectoryCalculation(out currentPosition, out velocity, trajectoryInfo, num3);
				num2 = Vector3.SqrMagnitude(zero - currentPosition);
				num3 += deltaTime;
			}
			array[i] = currentPosition;
		}
		return array;
	}

	public Vector3 CalculateShotDirectionForIron(Vector3 localPosition, float CachedSpeedFactor, int calibrationRange)
	{
		AmmoTemplate defAmmoTemplate = Template.DefAmmoTemplate;
		float x = defAmmoTemplate.InitialSpeed * CachedSpeedFactor;
		EftBulletClass.FormTrajectory(Vector3.zero, new Vector3(x, 0f, 0f), defAmmoTemplate.BulletMassGram, defAmmoTemplate.BulletDiameterMilimeters, defAmmoTemplate.BallisticCoeficient, out var trajectoryInfo);
		float num = 0f;
		int num2 = 0;
		int num3 = trajectoryInfo.MaxAllowedLength - 1;
		while (num2 <= num3)
		{
			int num4 = (num2 + num3) / 2;
			if (!(trajectoryInfo[num4].position.magnitude < (float)calibrationRange) || trajectoryInfo[num4 + 1].position.magnitude <= (float)calibrationRange)
			{
				if (trajectoryInfo[num4].position.magnitude > (float)calibrationRange && trajectoryInfo[num4 + 1].position.magnitude > (float)calibrationRange)
				{
					num3 = num4 - 1;
				}
				else
				{
					num2 = num4 + 1;
				}
				continue;
			}
			num = trajectoryInfo[num4].position.y;
			break;
		}
		trajectoryInfo.Reset();
		num2 = 0;
		num3 = trajectoryInfo.MaxAllowedLength - 1;
		while (num2 <= num3)
		{
			int num5 = (num2 + num3) / 2;
			if (!(trajectoryInfo[num5].position.x < (float)calibrationRange) || trajectoryInfo[num5 + 1].position.x <= (float)calibrationRange)
			{
				if (trajectoryInfo[num5].position.x > (float)calibrationRange && trajectoryInfo[num5 + 1].position.x > (float)calibrationRange)
				{
					num3 = num5 - 1;
				}
				else
				{
					num2 = num5 + 1;
				}
				continue;
			}
			num = trajectoryInfo[num5].position.y;
			break;
		}
		return new Vector3(0f, -1f, (localPosition.y - num) / localPosition.z);
	}

	public Vector3 ZeroLevelPosition(Vector3 direction, Vector3 startPosition, float height, out float time)
	{
		AmmoTemplate defAmmoTemplate = Template.DefAmmoTemplate;
		float initialSpeed = defAmmoTemplate.InitialSpeed;
		Vector3 vector = direction * initialSpeed;
		Vector3 velocity = vector;
		float num = EftBulletClass.TimeToLevel(height, vector.y);
		EftBulletClass.FormTrajectory(startPosition, vector, defAmmoTemplate.BulletMassGram, defAmmoTemplate.BulletDiameterMilimeters, defAmmoTemplate.BallisticCoeficient, out OpticTrajectoryInfosForAGS);
		time = num;
		EftBulletClass.PredictedTrajectoryCalculation(out var currentPosition, out velocity, OpticTrajectoryInfosForAGS, num);
		return currentPosition;
	}

	public int GetCurrentMagazineCount_1()
	{
		return GetCurrentMagazineCount();
	}

	int IWeapon.GetCurrentMagazineCount()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetCurrentMagazineCount_1
		return this.GetCurrentMagazineCount_1();
	}

	public override int GetHashSum()
	{
		int num = base.GetHashSum();
		if (Chambers == null)
		{
			return num;
		}
		if (Foldable != null)
		{
			num = num * 27 + Foldable.Folded.GetHashCode();
		}
		for (int i = 0; i < Chambers.Length; i++)
		{
			num = num * 23 + Chambers[i].GetHashSum();
		}
		return num;
	}

	public int GetModsHashSumWithoutMag()
	{
		int num = 0;
		Slot[] slots = Slots;
		foreach (Slot slot in slots)
		{
			if (slot.ContainedItem != null && !(slot.ContainedItem is MagazineItemClass) && !(slot.ContainedItem is AmmoItemClass))
			{
				num += slot.ContainedItem.GetHashSum();
			}
		}
		return num;
	}

	[CanBeNull]
	public T GetFirstOrDefaultMod<T>()
	{
		return Mods.OfType<T>().FirstOrDefault();
	}

	public void GetShellsIndexes(List<int> shellsIndexes)
	{
		shellsIndexes.Clear();
		for (int i = 0; i < ShellsInChambers_1.Length; i++)
		{
			if (ShellsInChambers_1[i] != null)
			{
				shellsIndexes.Add(i);
			}
		}
	}

	public int GetShellsInWeaponCount()
	{
		int num = 0;
		for (int i = 0; i < ShellsInChambers_1.Length; i++)
		{
			if (ShellsInChambers_1[i] != null)
			{
				num++;
			}
		}
		return num;
	}

	[CanBeNull]
	public Slot GetMagazineSlot()
	{
		return MagSlotCache ?? (MagSlotCache = Array.Find(Slots, (Slot x) => x.ID == EWeaponModType.mod_magazine.ToString()));
	}

	public bool HasMagazineWithBelt()
	{
		return GetCurrentMagazine()?.IsMagazineWithBelt ?? false;
	}

	public override MagazineItemClass GetCurrentMagazine()
	{
		return (MagazineItemClass)(GetMagazineSlot()?.ContainedItem);
	}

	public int GetCurrentMagazineCount()
	{
		return GetCurrentMagazine()?.Count ?? 0;
	}

	public int GetMaxMagazineCount()
	{
		return GetCurrentMagazine()?.MaxCount ?? 0;
	}

	[CanBeNull]
	public LauncherItemClass GetUnderbarrelWeapon()
	{
		return AllSlots.FirstOrDefault((Slot slot) => slot.ContainedItem is LauncherItemClass)?.ContainedItem as LauncherItemClass;
	}

	[CanBeNull]
	public Slot GetLauncherSlot()
	{
		return AllSlots.FirstOrDefault((Slot slot) => slot.ContainedItem is LauncherItemClass);
	}

	public override GStruct153 Apply([NotNull] TraderControllerClass itemController, [NotNull] Item item, int count, bool simulate)
	{
		Struct893 struct893_ = default(Struct893);
		struct893_.itemController = itemController;
		if (!struct893_.itemController.Examined(item))
		{
			return new GClass1551(item);
		}
		if (!struct893_.itemController.Examined(this))
		{
			return new GClass1551(this);
		}
		Slot magazineSlot = GetMagazineSlot();
		Error error;
		if (item is MagazineItemClass item2 && magazineSlot != null)
		{
			if (!GClass3124.CanAccept(magazineSlot, item2))
			{
				return new Slot.GClass1579(item2, magazineSlot);
			}
			ItemAddress itemAddress = magazineSlot.CreateItemAddress();
			IResult result = smethod_1(itemAddress, ref struct893_);
			if (result.Failed)
			{
				return new GClass1522(result.Error);
			}
			GStruct154<GClass3411> gStruct = InteractionsHandlerClass.Move(item2, itemAddress, struct893_.itemController, simulate);
			if (gStruct.Succeeded)
			{
				return gStruct;
			}
			error = gStruct.Error;
			Item containedItem = magazineSlot.ContainedItem;
			if (!GClass842.DisabledForNow && containedItem != null && GClass3396.CanSwap(item2, magazineSlot))
			{
				return new GStruct153((IRaiseEvents)null);
			}
		}
		else
		{
			if (item is AmmoItemClass && IsMultiBarrel)
			{
				GStruct153 result2 = base.Apply(struct893_.itemController, item, count, simulate);
				if (result2.Succeeded)
				{
					return result2;
				}
				return result2.Error;
			}
			GStruct153 result3 = base.Apply(struct893_.itemController, item, count, simulate);
			if (result3.Succeeded)
			{
				return result3;
			}
			error = result3.Error;
			if (!(item is AmmoItemClass ammoItemClass) || !(magazineSlot?.ContainedItem is MagazineItemClass magazineItemClass) || !SupportsInternalReload || magazineItemClass.MaxCount <= magazineItemClass.Count || GClass3380.Contains(this, ammoItemClass))
			{
				return error;
			}
			IResult result4 = smethod_1(magazineSlot.CreateItemAddress(), ref struct893_);
			if (result4.Failed)
			{
				return new GClass1522(result4.Error);
			}
			GStruct153 result5 = magazineItemClass.ApplyWithoutRestrictions(struct893_.itemController, ammoItemClass, count, simulate);
			if (result5.Succeeded)
			{
				return result5;
			}
			error = result5.Error;
		}
		return error;
	}

	public void OnShot(float ammoBurnRatio, float ammoHeatFactor, float skillWeaponTreatmentFactor, BackendConfigSettingsClass.GClass1739 overheatSettings, float pastTime)
	{
		if (AllowOverheat)
		{
			float modsCoolFactor;
			float currentOverheat = GetCurrentOverheat(pastTime, overheatSettings, out modsCoolFactor);
			float modsHeatFactor;
			float shotOverheat = GetShotOverheat(ammoHeatFactor, overheatSettings.ModHeatFactor, out modsHeatFactor);
			MalfState.OverheatBarrelMoveDir = GetCurrentOverheatBarrelMove(pastTime, overheatSettings.BarrelMoveRndDuration);
			MalfState.LastShotOverheat = Mathf.Clamp(currentOverheat + shotOverheat, overheatSettings.MinOverheat, overheatSettings.MaxOverheat);
			MalfState.LastShotTime = pastTime;
		}
		else
		{
			MalfState.LastShotOverheat = 0f;
		}
		float num = Mathf.Clamp(MalfState.LastShotOverheat, overheatSettings.OverheatProblemsStart, overheatSettings.MaxOverheat) / 100f;
		float overheatFactor = Mathf.Lerp(overheatSettings.DurReduceMinMult, overheatSettings.DurReduceMaxMult, num - 1f);
		MalfState.OverheatBarrelMoveMult = (AllowOverheat ? Mathf.Lerp(0f, overheatSettings.BarrelMoveMaxMult, num - 1f) : 0f);
		if (Repairable.Durability > 0f)
		{
			Repairable.Durability -= GetDurabilityLossOnShot(ammoBurnRatio, overheatFactor, skillWeaponTreatmentFactor, out var _);
			Repairable.Durability = Mathf.Max(Repairable.Durability, 0f);
			Buff.TryDisableComponent(Repairable.Durability);
		}
		if (Repairable.Durability < 0f)
		{
			Repairable.Durability = 0f;
		}
		if (MalfState.LastShotOverheat >= overheatSettings.OverheatProblemsStart && Repairable.MaxDurability / 100f >= overheatSettings.OverheatWearLimit)
		{
			num -= 1f;
			float num2 = 0f;
			num2 = ((!(Mathf.Abs(MalfState.LastShotOverheat - overheatSettings.MaxOverheat) <= Mathf.Epsilon)) ? Mathf.Lerp(overheatSettings.MinWearOnOverheat, overheatSettings.MaxWearOnOverheat, num) : UnityEngine.Random.Range(overheatSettings.MinWearOnMaxOverheat, overheatSettings.MaxWearOnMaxOverheat));
			if (Repairable.Durability < Repairable.MaxDurability - num2)
			{
				Repairable.MaxDurability = Mathf.Max(Repairable.MaxDurability - num2, overheatSettings.OverheatWearLimit * 100f);
			}
		}
		if (overheatSettings.EnableSlideOnMaxOverheat)
		{
			if (!MalfState.SlideOnOverheatReached && MalfState.LastShotOverheat >= overheatSettings.StartSlideOverheat)
			{
				MalfState.SlideOnOverheatReached = true;
			}
			if (MalfState.LastShotOverheat <= overheatSettings.FixSlideOverheat)
			{
				MalfState.SlideOnOverheatReached = false;
			}
		}
		if (MalfState.LastShotOverheat > overheatSettings.FirerateOverheatBorder)
		{
			if (!MalfState.OverheatFirerateMultInited)
			{
				MalfState.OverheatFirerateMultInited = true;
				MalfState.OverheatFirerateMult = UnityEngine.Random.Range(overheatSettings.FirerateReduceMinMult, overheatSettings.FirerateReduceMaxMult);
			}
		}
		else
		{
			MalfState.OverheatFirerateMult = 0f;
			MalfState.OverheatFirerateMultInited = false;
		}
		if (MalfState.LastShotOverheat >= overheatSettings.AutoshotMinOverheat)
		{
			if (!MalfState.AutoshotChanceInited)
			{
				float num3 = UnityEngine.Random.Range(0.05f, overheatSettings.AutoshotPossibilityDuration);
				bool flag = UnityEngine.Random.Range(0f, 1f) <= overheatSettings.AutoshotChance;
				MalfState.AutoshotTime = (flag ? (MalfState.LastShotTime + num3) : (-1f));
				MalfState.AutoshotChanceInited = true;
			}
		}
		else
		{
			MalfState.AutoshotChanceInited = false;
			MalfState.AutoshotTime = -1f;
		}
	}

	public bool CanQuickdrawPistolAfterMalf(float pastTime, BackendConfigSettingsClass.GClass1738 malfSettings)
	{
		return pastTime - MalfState.LastMalfunctionTime < malfSettings.TimeToQuickdrawPistol;
	}

	public float GetCurrentOverheat(float pastTime, BackendConfigSettingsClass.GClass1739 overheatSettings, out float modsCoolFactor)
	{
		if (MalfState.LastShotOverheat <= 0f)
		{
			modsCoolFactor = 1f;
			return 0f;
		}
		modsCoolFactor = Template.CoolFactorGunMods;
		PreallocatedMods.Clear();
		GClass3380.GetAllItemsNonAlloc(this, PreallocatedMods);
		foreach (Mod preallocatedMod in PreallocatedMods)
		{
			modsCoolFactor *= preallocatedMod.CoolFactor;
		}
		PreallocatedMods.Clear();
		float num = modsCoolFactor * overheatSettings.ModCoolFactor;
		float num2 = pastTime - MalfState.LastShotTime;
		float lastShotOverheat = MalfState.LastShotOverheat;
		return Mathf.Clamp(lastShotOverheat - num2 * lastShotOverheat * CoolFactorGun * num / (lastShotOverheat - Mathf.Pow(lastShotOverheat / overheatSettings.MaxOverheat + overheatSettings.MaxOverheatCoolCoef * 0.0338f + 0.2925f, 9.667f - overheatSettings.MaxOverheatCoolCoef * -0.0668f)), overheatSettings.MinOverheat, overheatSettings.MaxOverheat);
	}

	public float GetCurrentOverheat(float pastTime, BackendConfigSettingsClass.GClass1739 overheatSettings, List<Mod> weaponCurrentMods, out float modsCoolFactor)
	{
		if (MalfState.LastShotOverheat <= 0f)
		{
			modsCoolFactor = 1f;
			return 0f;
		}
		modsCoolFactor = Template.CoolFactorGunMods;
		foreach (Mod weaponCurrentMod in weaponCurrentMods)
		{
			modsCoolFactor *= weaponCurrentMod.CoolFactor;
		}
		float num = modsCoolFactor * overheatSettings.ModCoolFactor;
		float num2 = pastTime - MalfState.LastShotTime;
		float lastShotOverheat = MalfState.LastShotOverheat;
		return Mathf.Clamp(lastShotOverheat - num2 * lastShotOverheat * CoolFactorGun * num / (lastShotOverheat - Mathf.Pow(lastShotOverheat / overheatSettings.MaxOverheat + overheatSettings.MaxOverheatCoolCoef * 0.0338f + 0.2925f, 9.667f - overheatSettings.MaxOverheatCoolCoef * -0.0668f)), overheatSettings.MinOverheat, overheatSettings.MaxOverheat);
	}

	public Vector2 GetCurrentOverheatBarrelMove(float pastTime, float timeToChangeDir)
	{
		if (!(pastTime - MalfState.LastShotTime < timeToChangeDir))
		{
			return UnityEngine.Random.insideUnitCircle;
		}
		return MalfState.OverheatBarrelMoveDir;
	}

	public float GetDurabilityLossOnShot(float ammoBurnRatio, float overheatFactor, float skillWeaponTreatmentFactor, out float modsBurnRatio)
	{
		modsBurnRatio = 1f;
		foreach (Mod mod in Mods)
		{
			modsBurnRatio *= mod.DurabilityBurnModificator;
		}
		return (float)Repairable.TemplateDurability / Template.OperatingResource * DurabilityBurnRatio * (modsBurnRatio * ammoBurnRatio) * overheatFactor * (1f - skillWeaponTreatmentFactor);
	}

	public float GetShotOverheat(float ammoHeatFactor, float globalModHeatFactor, out float modsHeatFactor)
	{
		modsHeatFactor = ammoHeatFactor;
		foreach (Mod mod in Mods)
		{
			modsHeatFactor *= mod.HeatFactor;
		}
		return Template.HeatFactorByShot * modsHeatFactor * Template.HeatFactorGun * globalModHeatFactor;
	}

	public bool ValidateMalfunction(EMalfunctionState malfState)
	{
		if (this.OnMalfunctionValidate == null)
		{
			return true;
		}
		return this.OnMalfunctionValidate(malfState);
	}

	[CompilerGenerated]
	public Slot method_12(Slot x)
	{
		return new Slot(x, this);
	}

	[CompilerGenerated]
	public float method_13()
	{
		return Mathf.RoundToInt(Template.Ergonomics);
	}

	[CompilerGenerated]
	public float method_14()
	{
		return ErgonomicsDelta;
	}

	[CompilerGenerated]
	public string method_15()
	{
		return Mathf.Clamp(ErgonomicsTotal, 0f, 100f).ToString("0.##");
	}

	[CompilerGenerated]
	public float method_16()
	{
		return CenterOfImpactBase;
	}

	[CompilerGenerated]
	public float method_17()
	{
		float num = method_9(Repairable.TemplateDurability);
		float num2 = (GetBarrelDeviation() - num) / (Single_0 - num);
		return CenterOfImpactDelta + num2;
	}

	[CompilerGenerated]
	public string method_18()
	{
		return (GetTotalCenterOfImpact(includeAmmo: true) * GetBarrelDeviation() * 100f / 2.9089f).ToString("0.0#") + " " + GClass2348.Localized("moa");
	}

	[CompilerGenerated]
	public float method_19()
	{
		return GetSightingRange();
	}

	[CompilerGenerated]
	public string method_20()
	{
		if (!(GetSightingRange() > 0f))
		{
			return "N/A";
		}
		return GetSightingRange().ToString();
	}

	[CompilerGenerated]
	public float method_21()
	{
		return Template.RecoilForceUp;
	}

	[CompilerGenerated]
	public float method_22()
	{
		return RecoilDelta;
	}

	[CompilerGenerated]
	public string method_23()
	{
		return Mathf.RoundToInt(Template.RecoilForceUp + Template.RecoilForceUp * RecoilDelta).ToString();
	}

	[CompilerGenerated]
	public float method_24()
	{
		return Template.RecoilForceBack;
	}

	[CompilerGenerated]
	public float method_25()
	{
		return RecoilDelta;
	}

	[CompilerGenerated]
	public string method_26()
	{
		return Mathf.RoundToInt(Template.RecoilForceBack + Template.RecoilForceBack * RecoilDelta).ToString();
	}

	[CompilerGenerated]
	public float method_27()
	{
		return VelocityBase;
	}

	[CompilerGenerated]
	public float method_28()
	{
		return VelocityDelta;
	}

	[CompilerGenerated]
	public string method_29()
	{
		return TotalVelocity.ToString("0") + " " + GClass2348.Localized("m/s");
	}

	[CompilerGenerated]
	public float method_30()
	{
		return WeapFireType.Length;
	}

	[CompilerGenerated]
	public string method_31()
	{
		return GClass2066.CastToStringValue(WeapFireType, ", ");
	}

	[CompilerGenerated]
	public float method_32()
	{
		return AmmoCaliber.Length;
	}

	[CompilerGenerated]
	public string method_33()
	{
		return GClass2348.Localized(AmmoCaliber);
	}

	[CompilerGenerated]
	public float method_34()
	{
		return Template.bFirerate;
	}

	[CompilerGenerated]
	public string method_35()
	{
		return Template.bFirerate + " " + GClass2348.Localized("rpm");
	}

	[CompilerGenerated]
	public float method_36()
	{
		return Template.bEffDist;
	}

	[CompilerGenerated]
	public string method_37()
	{
		return Template.bEffDist + " " + GClass2348.Localized("meters");
	}

	[CompilerGenerated]
	[DebuggerHidden]
	public IEnumerable<IContainer> method_38()
	{
		return base.Containers;
	}

	[CompilerGenerated]
	public static IResult smethod_1(ItemAddress itemAddress, ref Struct893 struct893_0)
	{
		if (struct893_0.itemController is InventoryController inventoryController && inventoryController.Inventory.Equipment.ContainerSlots.Contains(itemAddress.Container) && struct893_0.itemController.SelectEvents(null).Any())
		{
			return new FailedResult("Inventory/PlayerIsBusy");
		}
		return SuccessfulResult.New;
	}
}
