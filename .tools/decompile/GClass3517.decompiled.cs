using System;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;

public class GClass3517 : SearchContentOperation, GInterface441
{
	[NonSerialized]
	public Callback Callback_0;

	public GClass3517(ushort id, TraderControllerClass itemController, SearchableItemItemClass item)
		: base(id, itemController, item)
	{
	}

	public override void ExecuteInternal(Callback callback)
	{
		Callback_0 = callback;
	}

	public override void Terminate()
	{
		Callback_0.Succeed();
	}

	public static void PlayInstantSearchSound()
	{
		Singleton<GUISounds>.Instance.PlayItemSound("looting_luck2", EInventorySoundType.other);
	}

	public static void PlayDiscoverSound(Item item)
	{
		Singleton<GUISounds>.Instance.PlayItemSound(item.ItemSound, EInventorySoundType.drop);
	}
}
