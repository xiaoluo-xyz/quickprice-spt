using EFT.InventoryLogic;
using Newtonsoft.Json;

public class KeycardItemClass : KeyItemClass
{
	[JsonProperty("Keycard")]
	[GAttribute26]
	public KeycardComponent KeycardComponent;

	public KeycardItemClass(string id, KeycardTemplateClass template)
		: base(id, template)
	{
		Components.Add(KeycardComponent = new KeycardComponent(this, template));
	}
}
