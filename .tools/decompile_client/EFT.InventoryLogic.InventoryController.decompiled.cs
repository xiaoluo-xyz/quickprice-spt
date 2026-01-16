using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace EFT.InventoryLogic;

public class InventoryController : GClass3384, GInterface415, IContainer, IItemContainer
{
	[Serializable]
	[CompilerGenerated]
	public class Class2401
	{
		public static readonly Class2401 class2401_0 = new Class2401();

		public static Func<BackendConfigSettingsClass.GClass1737, string> func_0;

		public static Func<BackendConfigSettingsClass.GClass1737, int> func_1;

		public static Func<Item, MongoID> func_2;

		public static Func<Item, int> func_3;

		public static Func<Item, int> func_4;

		public string method_0(BackendConfigSettingsClass.GClass1737 restriction)
		{
			return restriction.TemplateId;
		}

		public int method_1(BackendConfigSettingsClass.GClass1737 restriction)
		{
			return restriction.MaxInRaid;
		}

		public MongoID method_2(Item selecting)
		{
			return selecting.TemplateId;
		}

		public int method_3(Item x)
		{
			return x.StackObjectsCount;
		}

		public int method_4(Item x)
		{
			return x.StackObjectsCount;
		}
	}

	[CompilerGenerated]
	public class Class2402
	{
		public InventoryController inventoryController_0;

		public Slot parentSlot;

		public Slot method_0(EquipmentSlot slotType)
		{
			return inventoryController_0.Inventory.Equipment.GetSlot(slotType);
		}

		public bool method_1(Slot slot)
		{
			return slot == parentSlot;
		}
	}

	[CompilerGenerated]
	public class Class2403
	{
		public Item item;

		public bool method_0(KeyValuePair<EBoundItem, Item> x)
		{
			return x.Value == item;
		}
	}

	[CompilerGenerated]
	public class Class2404
	{
		public InventoryController inventoryController_0;

		public ExamineOperationClass operation;

		public Callback callback;

		public Callback callback_0;

		public void method_0(IResult examineResult)
		{
			if (!examineResult.Failed)
			{
				inventoryController_0.vmethod_1(operation, delegate(IResult result)
				{
					inventoryController_0.Examining = false;
					callback?.Invoke(result);
				});
			}
		}

		public void method_1(IResult result)
		{
			inventoryController_0.Examining = false;
			callback?.Invoke(result);
		}
	}

	[CompilerGenerated]
	public class Class2405
	{
		public Item first;

		public bool method_0(Item x)
		{
			return x.TemplateId == first.TemplateId;
		}
	}

	[CompilerGenerated]
	public class Class2406
	{
		public MagazineItemClass magazine;

		public bool status;

		public void method_0(IResult arg)
		{
			Debug.Log($"> Change checked status on ({GClass2348.Localized(magazine.ShortName)}) to: {status}");
		}
	}

	[NonSerialized]
	public static EquipmentSlot[] HiddenFromSelf = new EquipmentSlot[1] { EquipmentSlot.Dogtag };

	[NonSerialized]
	public static EquipmentSlot[] HiddenFromOthers = new EquipmentSlot[1] { EquipmentSlot.SecuredContainer };

	[NonSerialized]
	public static EquipmentSlot[] AnimatedSlots = new EquipmentSlot[1] { EquipmentSlot.Backpack };

	[NonSerialized]
	public Dictionary<string, int> RestrictionsInRaid;

	[NonSerialized]
	public new bool Examined;

	[NonSerialized]
	public bool Examining;

	public virtual Item ItemInHands => null;

	public FastAccess FastAccess => Inventory.FastAccess;

	[field: NonSerialized]
	public Inventory Inventory { get; set; }

	[field: NonSerialized]
	public virtual IInventoryProfileInfo Profile { get; }

	public override CompoundItem Root => Inventory.Stash;

	[CanBeNull]
	[field: NonSerialized]
	public GClass3248 QuestStashItem { get; }

	[CanBeNull]
	[field: NonSerialized]
	public GClass3248 QuestRaidItem { get; }

	public IEnumerable<CompoundItem> EquipmentItems
	{
		get
		{
			Slot[] slots = Inventory.Equipment.Slots;
			for (int i = 0; i < slots.Length; i++)
			{
				if (slots[i].ContainedItem is CompoundItem compoundItem)
				{
					yield return compoundItem;
				}
			}
		}
	}

	public event Action CriticalInventoryErrorHappened;

	public event Action<int> OnAmmoLoaded;

	public event Action<int> OnAmmoUnloaded;

	public event Action OnMagazineCheck;

	public event Action OnProfileUpdate;

	public event Action<Weapon> ExamineMalfunctionEvent;

	public InventoryController(IInventoryProfileInfo profile, bool examined)
		: base(profile.InventoryInfo.Equipment, profile.Side, profile.ProfileId, profile.Nickname, canBeLocalized: false)
	{
		Profile = profile;
		this.Examined = examined;
		QuestStashItem = profile.InventoryInfo.QuestStashItems;
		QuestRaidItem = profile.InventoryInfo.QuestRaidItems;
		if (QuestStashItem != null && QuestStashItem.CurrentAddress == null)
		{
			QuestStashItem.CurrentAddress = CreateItemAddress();
		}
		if (QuestRaidItem != null && QuestRaidItem.CurrentAddress == null)
		{
			QuestRaidItem.CurrentAddress = CreateItemAddress();
		}
		BackendConfigSettingsClass.GClass1737[] restrictionsInRaid = Singleton<BackendConfigSettingsClass>.Instance.RestrictionsInRaid;
		RestrictionsInRaid = ((restrictionsInRaid != null) ? restrictionsInRaid.ToDictionary((BackendConfigSettingsClass.GClass1737 restriction) => restriction.TemplateId, (BackendConfigSettingsClass.GClass1737 restriction) => restriction.MaxInRaid) : new Dictionary<string, int>());
		ReplaceInventory(profile.InventoryInfo);
	}

	public void ReportProfileUpdate()
	{
		try
		{
			this.OnProfileUpdate?.Invoke();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public override IContainer FindContainer(GClass1949 container)
	{
		MongoID? parentId = container.ParentId;
		string containerId = container.ContainerId;
		IContainer container2 = null;
		GClass3248 questRaidItem = QuestRaidItem;
		if (questRaidItem != null)
		{
			MongoID? mongoID = parentId;
			if (GClass3112.TryFindContainer(questRaidItem, containerId, mongoID.HasValue ? ((string)mongoID.GetValueOrDefault()) : null, out container2))
			{
				goto IL_007a;
			}
		}
		GClass3248 questStashItem = QuestStashItem;
		if (questStashItem != null)
		{
			MongoID? mongoID = parentId;
			if (GClass3112.TryFindContainer(questStashItem, containerId, mongoID.HasValue ? ((string)mongoID.GetValueOrDefault()) : null, out container2))
			{
				goto IL_007a;
			}
		}
		return base.FindContainer(container);
		IL_007a:
		return container2;
	}

	public GStruct156<IEnumerable<DestroyedItemsStruct>> FindDestroyedItems(IEnumerable<GClass1955> itemsData)
	{
		if (itemsData == null)
		{
			return default(GStruct156<IEnumerable<DestroyedItemsStruct>>);
		}
		List<DestroyedItemsStruct> list = null;
		foreach (var (text2, numberToDestroy, numberToPreserve) in itemsData)
		{
			if (list == null)
			{
				list = new List<DestroyedItemsStruct>();
			}
			GStruct156<Item> gStruct = FindItemById(text2);
			if (!gStruct.Failed)
			{
				list.Add(new DestroyedItemsStruct(gStruct.Value, numberToDestroy, numberToPreserve));
				continue;
			}
			return gStruct.Error;
		}
		return (GStruct156<IEnumerable<DestroyedItemsStruct>>)(IEnumerable<DestroyedItemsStruct>)list;
	}

	public virtual GStruct156<Item> FindItemById(MongoID itemId, bool checkDistance = true, bool checkOwnership = true)
	{
		throw new NotImplementedException();
	}

	public void ReplaceInventory(Inventory newInventory)
	{
		if (Inventory != null)
		{
			base.RemoveItemEvent -= Inventory.UpdateTotalWeight;
			base.AddItemEvent -= Inventory.UpdateTotalWeight;
			base.RefreshItemEvent -= Inventory.UpdateTotalWeight;
			Inventory = null;
		}
		Inventory = newInventory;
		if (Profile.Side == EPlayerSide.Savage)
		{
			Inventory.Equipment.GetSlot(EquipmentSlot.ArmBand).Deleted = true;
		}
		base.RemoveItemEvent += Inventory.UpdateTotalWeight;
		base.AddItemEvent += Inventory.UpdateTotalWeight;
		base.RefreshItemEvent += Inventory.UpdateTotalWeight;
		if (Inventory.Stash != null && Inventory.Stash.CurrentAddress == null)
		{
			Inventory.Stash.CurrentAddress = CreateItemAddress();
		}
	}

	public override bool TryFindItem(string itemId, out Item item)
	{
		item = null;
		GClass3248 questRaidItem = QuestRaidItem;
		if (questRaidItem == null || !questRaidItem.TryFindItem(itemId, out item))
		{
			GClass3248 questStashItem = QuestStashItem;
			if (questStashItem == null || !questStashItem.TryFindItem(itemId, out item))
			{
				return base.TryFindItem(itemId, out item);
			}
		}
		return true;
	}

	public override bool TryFindItem(Func<Item, bool> predicate, out Item result)
	{
		if (QuestRaidItem != null && QuestRaidItem.TryFindItem(predicate, out result))
		{
			return true;
		}
		if (QuestStashItem != null && QuestStashItem.TryFindItem(predicate, out result))
		{
			return true;
		}
		return base.TryFindItem(predicate, out result);
	}

	[CanBeNull]
	public virtual GClass3393 FindQuestGridToPickUp(Item item)
	{
		return Inventory.QuestRaidItems.Grid.FindLocationForItem(item);
	}

	public virtual bool IsInventoryBlocked()
	{
		return Locked;
	}

	public virtual bool IsAllowedToSeeSlot(Slot slot, EquipmentSlot slotName)
	{
		IItemOwner owner = GClass3113.GetOwner(slot.ParentItem.Parent);
		if (slot.Deleted)
		{
			return false;
		}
		if (owner != this)
		{
			return !HiddenFromOthers.Contains(slotName);
		}
		return !HiddenFromSelf.Contains(slotName);
	}

	public virtual bool IsAllowedToSeeEquipmentSlot(Slot slot, EquipmentSlot slotName)
	{
		IItemOwner owner = GClass3113.GetOwner(slot.ParentItem.Parent);
		if (slot.Deleted)
		{
			return false;
		}
		if (owner != this)
		{
			return !HiddenFromOthers.Contains(slotName);
		}
		if (Side == EPlayerSide.Savage)
		{
			return !HiddenFromSelf.Contains(slotName);
		}
		return true;
	}

	public virtual bool IsAllowedToSort(CompoundItem item)
	{
		return false;
	}

	public virtual bool HasCultistAmulet(out CultistAmuletItemClass amulet)
	{
		amulet = null;
		return false;
	}

	public bool IsAtBindablePlace(Item item)
	{
		if (item.CurrentAddress != null && !(item.Parent is GClass3390))
		{
			Slot parentSlot = item.Parent.Container.ParentItem.CurrentAddress?.Container as Slot;
			CompoundItem compoundItem = item as CompoundItem;
			if (Inventory.FastAccessSlots.Select((EquipmentSlot slotType) => Inventory.Equipment.GetSlot(slotType)).Any((Slot slot) => slot == parentSlot) && (compoundItem == null || !compoundItem.MissingVitalParts.Any()) && Examined(item))
			{
				if (!(item is Weapon) && !(item is ThrowWeapItemClass) && item.GetItemComponent<KnifeComponent>() == null && !(item is MedsItemClass) && !(item is FoodDrinkItemClass) && !(item is PortableRangeFinderItemClass) && !(item is CompassItemClass))
				{
					return item is RadioTransmitterItemClass;
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public bool IsAnimatedSlot(ItemAddress itemAddress)
	{
		if (itemAddress == null)
		{
			return false;
		}
		EquipmentSlot[] animatedSlots = AnimatedSlots;
		int num = 0;
		while (true)
		{
			if (num < animatedSlots.Length)
			{
				EquipmentSlot slotName = animatedSlots[num];
				if (Inventory.Equipment.GetSlot(slotName) == itemAddress.Container)
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

	public bool IsAtReachablePlace(Item item)
	{
		if (item.CurrentAddress == null)
		{
			return false;
		}
		IContainer container = item.Parent.Container;
		if ((Inventory.Stash == null || container != Inventory.Stash.Grid) && (!(item is CompoundItem compoundItem) || !compoundItem.MissingVitalParts.Any()) && Inventory.GetItemsInSlots(Inventory.BindAvailableSlotsExtended).Contains(item) && Examined(item))
		{
			if (!(item is Weapon) && !(item is ThrowWeapItemClass) && item.GetItemComponent<KnifeComponent>() == null && !(item is MedsItemClass) && !(item is FoodDrinkItemClass) && !(item is PortableRangeFinderItemClass) && !(item is CompassItemClass))
			{
				return item is RadioTransmitterItemClass;
			}
			return true;
		}
		return false;
	}

	public async Task<bool> method_27(ETraderServiceType serviceType, AbstractQuestControllerClass questController, string subServiceId = null)
	{
		if (!Singleton<BackendConfigSettingsClass>.Instance.ServicesData.TryGetValue(serviceType, out var value))
		{
			GClass865.Log($"[PurchaseTraderService] Available services not found: {serviceType}");
			return false;
		}
		if (!Profile.TradersInfo.TryGetValue(value.TraderId, out var value2))
		{
			GClass865.Log($"[PurchaseTraderService] Traders info not found: {value.TraderId}");
			return false;
		}
		if (!value2.IsServiceAvailableForPurchase(serviceType))
		{
			GClass865.Log($"[PurchaseTraderService] Service not available for purchase: {serviceType}");
			return false;
		}
		GStruct154<GClass3532> gStruct = InteractionsHandlerClass.PurchaseTraderService(value, subServiceId, questController, this, simulate: true);
		IResult obj = await TryRunNetworkTransaction(gStruct);
		if (obj.Failed)
		{
			Debug.LogError(gStruct.Error);
		}
		if (!obj.Succeed)
		{
			GClass865.Log("[PurchaseTraderService] Operation not succeed");
		}
		return obj.Succeed;
	}

	public override bool IsRootAddressIsStash(ItemAddress location)
	{
		foreach (Item allParentItem in GClass3380.GetAllParentItems(location))
		{
			if (allParentItem == Inventory.Stash)
			{
				return true;
			}
		}
		return false;
	}

	public virtual Task<IResult> UnloadMagazine(MagazineItemClass magazine, bool equipmentBlocked)
	{
		return UnloadAmmoInstantly(magazine, equipmentBlocked);
	}

	public virtual bool CheckedMagazine(MagazineItemClass magazine)
	{
		return true;
	}

	public virtual void InventoryCheckMagazine(MagazineItemClass magazine, bool notify)
	{
		if (!CheckedMagazine(magazine))
		{
			Profile.CheckMagazines(magazine.Id, 0);
		}
	}

	public async Task<IResult> UnloadAmmoInstantly<TContainer>(TContainer ammoContainer, bool equipmentBlocked) where TContainer : Item, IAmmoContainer
	{
		AmmoItemClass ammoItemClass = ammoContainer.Cartridges.Last as AmmoItemClass;
		if (ammoItemClass == null)
		{
			return new GClass1562(ammoContainer).ToResult();
		}
		string itemSound = ammoItemClass.ItemSound;
		bool flag = false;
		GStruct153 gStruct = default(GStruct153);
		IEnumerable<CompoundItem> targets;
		if (Inventory.Stash != null)
		{
			IEnumerable<CompoundItem> enumerable2;
			if (!equipmentBlocked)
			{
				IEnumerable<CompoundItem> enumerable = new CompoundItem[2] { Inventory.Equipment, Inventory.Stash };
				enumerable2 = enumerable;
			}
			else
			{
				IEnumerable<CompoundItem> enumerable = GClass1518.ToEnumerable(Inventory.Stash);
				enumerable2 = enumerable;
			}
			targets = enumerable2;
		}
		else
		{
			targets = GClass1518.ToEnumerable(Inventory.Equipment);
		}
		while (ammoItemClass != null)
		{
			gStruct = InteractionsHandlerClass.QuickFindAppropriatePlace(ammoItemClass, this, targets, InteractionsHandlerClass.EMoveItemOrder.UnloadAmmo, simulate: true);
			if (gStruct.Failed)
			{
				break;
			}
			flag = true;
			IResult result = await TryRunNetworkTransaction(gStruct);
			if (!result.Failed)
			{
				ammoItemClass = ammoContainer.Cartridges.Last as AmmoItemClass;
				continue;
			}
			gStruct = new GClass1522(result.Error);
			break;
		}
		if (flag && Singleton<GUISounds>.Instantiated)
		{
			Singleton<GUISounds>.Instance.PlayItemSound(itemSound, EInventorySoundType.drop);
		}
		if (!gStruct.Succeeded)
		{
			return GClass1617.ToResult(gStruct);
		}
		if (ammoContainer is AmmoBox)
		{
			TaskCompletionSource<IResult> taskCompletionSource = new TaskCompletionSource<IResult>();
			ThrowItem(ammoContainer, downDirection: false, taskCompletionSource.SetResult);
			return await taskCompletionSource.Task;
		}
		ammoContainer.RaiseRefreshEvent();
		return GClass1617.ToResult(gStruct);
	}

	public void UnbindItem(EBoundItem boundItemIndex, Callback callback = null)
	{
		Inventory.FastAccess.BoundItems.TryGetValue(boundItemIndex, out var value);
		if (value == null)
		{
			TraderControllerClass.RaiseCallback(new FailedResult("no item bound to " + boundItemIndex), callback, "Unbind");
			return;
		}
		GStruct154<GClass3432> gStruct = GClass3432.Run(this, value, boundItemIndex, simulate: true);
		if (gStruct.Failed)
		{
			TraderControllerClass.RaiseCallback(GClass1617.ToResult(gStruct), callback, "Unbind");
		}
		else
		{
			vmethod_1(new GClass3504(method_12(), this, gStruct.Value), callback);
		}
	}

	public GStruct154<GClass3432> UnbindItemDirect(Item item, bool simulate)
	{
		KeyValuePair<EBoundItem, Item> keyValuePair = Inventory.FastAccess.BoundItems.FirstOrDefault((KeyValuePair<EBoundItem, Item> x) => x.Value == item);
		if (keyValuePair.Value == null)
		{
			return new GClass1522("Can't unbind item");
		}
		return GClass3432.Run(this, item, keyValuePair.Key, simulate);
	}

	public virtual void SetupItem(Item item, string zone, Vector3 position, Quaternion rotation, float setupTime, Callback callback = null)
	{
	}

	public virtual void PlantTripwire(ThrowWeapItemClass grenade, PlantingKitsItemClass plantingKit, Vector3 fromPosition, Vector3 toPosition, Callback callback = null)
	{
	}

	public override void Examine(Item item, Callback callback = null)
	{
		if (Examined(item))
		{
			return;
		}
		if (Examining)
		{
			NotificationManagerClass.DisplayWarningNotification(GClass2348.Localized("You can't examine two items at the same time"));
			return;
		}
		Examining = true;
		ExamineOperationClass operation = new ExamineOperationClass(method_12(), this, Profile, item);
		operation.FakeExecute(delegate(IResult examineResult)
		{
			if (!examineResult.Failed)
			{
				vmethod_1(operation, delegate(IResult result)
				{
					Examining = false;
					callback?.Invoke(result);
				});
			}
		});
	}

	public override void OnAmmoLoadedCall(int count)
	{
		this.OnAmmoLoaded?.Invoke(count);
	}

	public override void OnAmmoUnloadedCall(int count)
	{
		this.OnAmmoUnloaded?.Invoke(count);
	}

	public override void OnMagazineCheckCall()
	{
		this.OnMagazineCheck?.Invoke();
	}

	public override bool CheckedChamber(Weapon weapon)
	{
		return Profile.IsCheckedChambers(weapon.Id);
	}

	public override void CheckChamber(Weapon weapon, bool status)
	{
		if (status)
		{
			if (!Profile.IsCheckedChambers(weapon.Id))
			{
				Profile.CheckChamber(weapon.Id);
			}
		}
		else if (Profile.IsCheckedChambers(weapon.Id))
		{
			Profile.UnCheckChamber(weapon.Id);
		}
		base.CheckChamber(weapon, status);
	}

	public override bool IsLimitedAtAddress(string templateId, ItemAddress address, out int limit)
	{
		InventoryEquipment parentEquipment;
		if (RestrictionsInRaid.TryGetValue(templateId, out limit) && limit >= 0)
		{
			return GClass3380.IsEquipmentAddress(Inventory, address, out parentEquipment);
		}
		return false;
	}

	public override bool IsLimitedAtAddress(Item item, [CanBeNull] ItemAddress address, out int limit)
	{
		if (address == null)
		{
			return IsLimitedAtAddress(item.TemplateId, item.CurrentAddress, out limit);
		}
		if (GClass3380.IsEquipmentAddress(Inventory, item.CurrentAddress, out var parentEquipment) && GClass3380.IsChildOf(address, parentEquipment))
		{
			limit = -1;
			return false;
		}
		return IsLimitedAtAddress(item.TemplateId, address, out limit);
	}

	public override bool CheckOverLimit(IEnumerable<Item> items, [CanBeNull] ItemAddress to, bool useItemCountInEquipment, out InteractionsHandlerClass.GClass1609 error)
	{
		foreach (IGrouping<MongoID, Item> item in from selecting in items
			group selecting by selecting.TemplateId)
		{
			Item first = item.First();
			if (!IsLimitedAtAddress(first, to, out var limit) || limit < 0)
			{
				continue;
			}
			int num = limit;
			if (useItemCountInEquipment)
			{
				InventoryEquipment inventoryEquipment = GClass3380.GetAllParentItems(to).OfType<InventoryEquipment>().FirstOrDefault();
				if (inventoryEquipment != null)
				{
					IEnumerable<Item> source = from x in GClass3380.GetAllItemsFromCollection(inventoryEquipment)
						where x.TemplateId == first.TemplateId
						select x;
					num -= source.Sum((Item x) => x.StackObjectsCount);
				}
			}
			if (item.Sum((Item x) => x.StackObjectsCount) > num)
			{
				error = new InteractionsHandlerClass.GClass1609(first, limit, num);
				return false;
			}
		}
		error = null;
		return true;
	}

	public IResult CheckInRaidItemsRestrictions()
	{
		if (!CheckOverLimit(Inventory.GetPlayerItems(EPlayerItems.Equipment), null, useItemCountInEquipment: false, out var error))
		{
			return new FailedResult(error.GetLocalizedDescription());
		}
		return SuccessfulResult.New;
	}

	public virtual void NotifyMagazineChecked(string name)
	{
	}

	public void SetMagazineCheckedStatus(MagazineItemClass magazine, bool status, int skill, bool useOperation = true, bool notify = true)
	{
		if (useOperation)
		{
			vmethod_1(new CheckMagazineOperationClass(method_12(), this, status, skill, magazine, Profile), delegate
			{
				Debug.Log($"> Change checked status on ({GClass2348.Localized(magazine.ShortName)}) to: {status}");
			});
		}
		else
		{
			CheckMagazineOperationClass.CheckMagazine(this, magazine, Profile, status, skill, notify);
		}
	}

	public override bool Examined(Item item)
	{
		if (this.Examined)
		{
			return true;
		}
		if (!item.ExaminedByDefault)
		{
			return Profile.Examined(item.TemplateId);
		}
		return true;
	}

	public bool HasKnownMalfunction(Weapon weapon)
	{
		return weapon.MalfState.IsKnownMalfunction(Profile.ProfileId);
	}

	bool GInterface415.HasKnownMalfunction(Weapon weapon)
	{
		//ILSpy generated this explicit interface implementation from .override directive in HasKnownMalfunction
		return this.HasKnownMalfunction(weapon);
	}

	public bool HasKnownMalfType_1(Weapon weapon)
	{
		return HasKnownMalfType(weapon);
	}

	bool GInterface415.HasKnownMalfType(Weapon weapon)
	{
		//ILSpy generated this explicit interface implementation from .override directive in HasKnownMalfType_1
		return this.HasKnownMalfType_1(weapon);
	}

	public bool HasKnownMalfType(Weapon weapon)
	{
		return weapon.MalfState.IsKnownMalfType(Profile.ProfileId);
	}

	public virtual void ExamineMalfunction(Weapon weapon, bool clearRest = false)
	{
		if (!weapon.MalfState.IsKnownMalfunction(Profile.ProfileId))
		{
			weapon.MalfState.AddPlayerWhoKnowMalfunction(Profile.ProfileId, clearRest);
			this.ExamineMalfunctionEvent?.Invoke(weapon);
			if (Profile.SkillsInfo.IsTroubleFixingExamineMalfElite)
			{
				ExamineMalfunctionType(weapon);
			}
			GClass3509 operation = new GClass3509(method_12(), this, Profile.ProfileId, weapon);
			vmethod_1(operation, null);
		}
	}

	public virtual void ExamineMalfunctionType(Weapon weapon)
	{
		if (!weapon.MalfState.IsKnownMalfType(Profile.ProfileId))
		{
			weapon.MalfState.AddPlayerWhoKnowMalfType(Profile.ProfileId);
			GClass3508 operation = new GClass3508(method_12(), this, Profile.ProfileId, weapon);
			vmethod_1(operation, null);
		}
	}

	public virtual void CallUnknownMalfunctionStartRepair(Weapon weapon)
	{
	}

	public virtual void CallMalfunctionRepaired(Weapon weapon)
	{
	}

	public virtual void ProcessFastWeaponSwitchAvailability()
	{
	}

	public bool Examined(string item)
	{
		return Profile.Examined(item);
	}

	public virtual Task<bool> TryPurchaseTraderService(ETraderServiceType serviceType, AbstractQuestControllerClass questController, string subServiceId = null)
	{
		return method_27(serviceType, questController, subServiceId);
	}

	public void RechamberWeapon(Weapon weapon, Callback callback = null)
	{
		GStruct154<GClass3408> gStruct = InteractionsHandlerClass.SimulateRechamberWeapon(this, weapon);
		if (gStruct.Failed)
		{
			if (callback != null)
			{
				callback(GClass1617.ToResult(gStruct));
			}
			else
			{
				Logger.LogError(gStruct.Error.ToString());
			}
		}
		else
		{
			vmethod_1(new GClass3505(method_12(), this, weapon, gStruct.Value), callback);
		}
	}

	public virtual void GetTraderServicesDataFromServer(string traderId)
	{
		throw new NotImplementedException();
	}

	[Obsolete("Use GetReachableItemsOfTypeNonAlloc instead")]
	public IEnumerable<TItem> GetReachableItemsOfType<TItem>(Predicate<TItem> predicate = null) where TItem : Item
	{
		List<TItem> list = new List<TItem>();
		GetReachableItemsOfTypeNonAlloc(list, predicate);
		return list;
	}

	public void GetReachableItemsOfTypeNonAlloc<TItem>([NotNull] IList<TItem> preAllocatedList, Predicate<TItem> predicate = null) where TItem : Item
	{
		GetAcceptableItemsNonAlloc(Inventory.FastAccessSlots, preAllocatedList, predicate);
	}

	public virtual void GetAcceptableItemsNonAlloc<TItem>([NotNull] EquipmentSlot[] equipmentSlots, [NotNull] IList<TItem> preAllocatedList, Predicate<TItem> predicate = null, Predicate<GClass3248> goDeeperPredicate = null) where TItem : Item
	{
		InventoryEquipment equipment = Inventory.Equipment;
		foreach (EquipmentSlot slotName in equipmentSlots)
		{
			if (!(equipment.GetSlot(slotName).ContainedItem is GClass3248 gClass) || (goDeeperPredicate != null && !goDeeperPredicate(gClass)))
			{
				continue;
			}
			foreach (IContainer container in gClass.Containers)
			{
				foreach (Item item in container.Items)
				{
					if (item is TItem val && (predicate == null || predicate(val)))
					{
						preAllocatedList.Add(val);
					}
				}
			}
		}
	}

	public new int GetHashSum()
	{
		return Inventory.CreateInventoryHashSum();
	}

	int IContainer.GetHashSum()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetHashSum
		return this.GetHashSum();
	}

	public void RaiseCriticalError()
	{
		this.CriticalInventoryErrorHappened?.Invoke();
	}

	public void RaiseEvent(GEventArgs9 args)
	{
		method_19(args);
		foreach (IOnSetInHands item in HashSet_0.OfType<IOnSetInHands>().ToList())
		{
			GClass1913.SafeOnSetInHandsInvoke(item, args);
		}
	}

	public void RaiseEvent(GEventArgs10 args)
	{
		method_19(args);
		foreach (GInterface191 item in HashSet_0.OfType<GInterface191>().ToList())
		{
			GClass1913.SafeOnRemoveFromHandsInvoke(item, args);
		}
	}

	public void RaiseEvent(GEventArgs4 args)
	{
		method_19(args);
	}

	public void RaiseEvent(GEventArgs5 args)
	{
		method_19(args);
	}

	public override void RaiseBindItemEvent(GEventArgs11 args)
	{
		method_19(args);
		foreach (GInterface188 item in HashSet_0.OfType<GInterface188>().ToList())
		{
			GClass1913.SafeOnBindItemInvoke(item, args);
		}
	}

	public override void RaiseUnbindItemEvent(GEventArgs12 args)
	{
		method_19(args);
		foreach (GInterface189 item in HashSet_0.OfType<GInterface189>().ToList())
		{
			GClass1913.SafeOnUnbindItemInvoke(item, args);
		}
	}
}
