using EFT.InventoryLogic;
using Newtonsoft.Json;

public class KeyItemClass : Item
{
	[JsonProperty("Key")]
	[GAttribute26]
	public KeyComponent KeyComponent;

	public KeyItemClass(string id, KeyTemplateClass template)
		: base(id, template)
	{
		Components.Add(KeyComponent = new KeyComponent(this, template));
	}
}
