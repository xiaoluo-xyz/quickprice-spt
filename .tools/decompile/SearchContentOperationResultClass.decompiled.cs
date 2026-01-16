using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;

public class SearchContentOperationResultClass : GClass3515
{
	public SearchContentOperationResultClass(ushort id, TraderControllerClass controller, IPlayerSearchController searchController, Profile profile, SearchableItemItemClass item)
		: base(id, controller, searchController, profile, item)
	{
	}

	public static void smethod_4()
	{
		Singleton<GUISounds>.Instance.PlayItemSound("looting_luck2", EInventorySoundType.other);
	}

	public static void smethod_5(Item item)
	{
		Singleton<GUISounds>.Instance.PlayItemSound(item.ItemSound, EInventorySoundType.drop);
	}

	public override void ExecuteInternal(Callback callback)
	{
		if (Bool_0)
		{
			smethod_4();
		}
		base.ExecuteInternal(callback);
	}

	public override void DiscoverItem(Item item)
	{
		base.DiscoverItem(item);
		if (!Bool_0)
		{
			smethod_5(item);
		}
	}
}
