using EFT.InventoryLogic;

public class KeyTemplateClass : ItemTemplate, GInterface394
{
	public int MaximumNumberOfUsage;

	int GInterface394.MaximumNumberOfUsage => MaximumNumberOfUsage;

	string GInterface394.KeyId => base._id;
}
