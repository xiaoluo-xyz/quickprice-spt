namespace EFT.InventoryLogic;

public class KeycardComponent : GClass3379
{
	public readonly GInterface394 Template;

	public readonly KeyComponent Key;

	public KeycardComponent(Item item, GInterface394 template)
		: base(item)
	{
		Key = item.GetItemComponent<KeyComponent>();
		Template = template;
	}
}
