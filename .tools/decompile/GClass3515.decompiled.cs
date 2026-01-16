using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using UnityEngine;

public abstract class GClass3515 : SearchContentOperation
{
	[CompilerGenerated]
	public class Class2508
	{
		public Callback callback;

		public void method_0(Task task)
		{
			callback.Succeed();
		}
	}

	[NonSerialized]
	public bool Bool_0;

	[NonSerialized]
	public const int Int_0 = 2;

	[NonSerialized]
	public Profile Profile_0;

	[NonSerialized]
	public IPlayerSearchController IplayerSearchController_0;

	[NonSerialized]
	public bool Bool_1;

	[NonSerialized]
	public CancellationTokenSource CancellationTokenSource_0 = new CancellationTokenSource();

	public bool Boolean_0 => CancellationTokenSource_0.IsCancellationRequested;

	public GClass3515(ushort id, TraderControllerClass controller, IPlayerSearchController searchController, Profile profile, SearchableItemItemClass item)
		: base(id, controller, item)
	{
		Profile_0 = profile;
		IplayerSearchController_0 = searchController;
		Bool_1 = !IplayerSearchController_0.IsSearched(Item);
		Bool_0 = Bool_1 && UnityEngine.Random.Range(0f, 1f) < Profile_0.SkillsInfo.AttentionEliteLuckySearchValue;
	}

	public override void ExecuteInternal(Callback callback)
	{
		GStruct155 gStruct = InteractionsHandlerClass.CheckItemForLocked(Item);
		if (gStruct.Failed)
		{
			callback(GClass1617.ToResult(gStruct));
			return;
		}
		method_4().ContinueWith(delegate
		{
			callback.Succeed();
		}, TaskContinuationOptions.ExecuteSynchronously).HandleExceptions();
	}

	public async Task method_4()
	{
		await method_5();
		if (!Boolean_0)
		{
			await method_6();
		}
	}

	public async Task method_5()
	{
		if (!Bool_1)
		{
			return;
		}
		if (!Bool_0)
		{
			await Task.Delay(2000).Await(CancellationTokenSource_0.Token);
			if (Boolean_0)
			{
				return;
			}
		}
		SearchItem();
	}

	public async Task method_6()
	{
		if (!IplayerSearchController_0.ContainsUnknownItems(Item))
		{
			return;
		}
		bool flag = GClass3113.GetOwner(Item.Parent).RootItem is InventoryEquipment;
		IInventoryProfileSkillInfo skillsInfo = Profile_0.SkillsInfo;
		float num = (flag ? (1f + skillsInfo.AttentionLootSpeedValue + skillsInfo.SearchBuffSpeedValue) : (1f + skillsInfo.AttentionLootSpeedValue));
		Item unknownItem;
		while (method_7(out unknownItem))
		{
			await Task.Delay((int)((Bool_0 ? 0f : ((float)UnityEngine.Random.Range(1, 3) / num)) * 1000f)).Await(CancellationTokenSource_0.Token);
			if (!Boolean_0)
			{
				if (!method_7(out var unknownItem2))
				{
					break;
				}
				DiscoverItem(unknownItem2);
				continue;
			}
			return;
		}
		IplayerSearchController_0.OnItemFullySearched();
	}

	public bool method_7(out Item unknownItem)
	{
		foreach (Item firstLevelItem in GClass3380.GetFirstLevelItems(Item))
		{
			if (!IplayerSearchController_0.IsItemKnown(firstLevelItem))
			{
				unknownItem = firstLevelItem;
				return true;
			}
		}
		unknownItem = null;
		return false;
	}

	public virtual void SearchItem()
	{
		IplayerSearchController_0.SetItemAsSearched(Item);
	}

	public virtual void DiscoverItem(Item item)
	{
		IplayerSearchController_0.SetItemAsKnown(item, raiseEvents: true);
	}

	public override void Terminate()
	{
		if (Boolean_0)
		{
			Debug.LogError("Search has already been terminated.");
			return;
		}
		CancellationTokenSource_0.Cancel(throwOnFirstException: false);
		CancellationTokenSource_0.Dispose();
	}

	public override void Dispose()
	{
		if (!Boolean_0)
		{
			CancellationTokenSource_0.Cancel(throwOnFirstException: false);
			CancellationTokenSource_0.Dispose();
		}
	}
}
