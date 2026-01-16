using System;
using System.Runtime.CompilerServices;

namespace EFT.InventoryLogic;

public class KeyComponent : GClass3379, IRelativeComponent, IItemComponent
{
	[Serializable]
	[CompilerGenerated]
	public class Class2298
	{
		public static readonly Class2298 class2298_0 = new Class2298();

		public static Func<EItemAttributeDisplayType> func_0;

		public static Func<EItemAttributeDisplayType> func_1;

		public EItemAttributeDisplayType method_0()
		{
			return EItemAttributeDisplayType.Compact;
		}

		public EItemAttributeDisplayType method_1()
		{
			return EItemAttributeDisplayType.Compact;
		}
	}

	[CompilerGenerated]
	public class Class2299
	{
		public KeyComponent keyComponent_0;

		public GInterface394 template;

		public int maximumUsages;

		public string method_0()
		{
			return keyComponent_0.method_0(maximumUsages);
		}

		public string method_1()
		{
			int num = template.MaximumNumberOfUsage - keyComponent_0.NumberOfUsages;
			bool flag = maximumUsages > 1 && num == 1;
			return $"{(flag ? string.Format(RedColorFormat, num) : num.ToString())}/{template.MaximumNumberOfUsage}";
		}
	}

	[NonSerialized]
	public const string String_0 = "keycard_single";

	[NonSerialized]
	public const string String_1 = "keycard_reusable";

	[NonSerialized]
	public const string String_2 = "keycard_unlimited";

	[GAttribute25]
	public int NumberOfUsages;

	public readonly GInterface394 Template;

	public static readonly string RedColorFormat = "<color=#C40000FF>{0}</color>";

	public static readonly string WhiteColorFormat = "<color=#FFFFFFFF>{0}</color>";

	public static readonly string CyanColorFormat = "<color=#54C1FFFF>{0}</color>";

	public float RelativeValue => 1f - (float)NumberOfUsages / (float)Template.MaximumNumberOfUsage;

	public KeyComponent(Item item, GInterface394 template)
		: base(item)
	{
		KeyComponent keyComponent_0 = this;
		Template = template;
		int maximumUsages = template.MaximumNumberOfUsage;
		item.Attributes.Add(new ItemAttributeClass(EItemAttributeId.Keys)
		{
			Name = GClass3374.GetName(EItemAttributeId.Keys),
			StringValue = () => keyComponent_0.method_0(maximumUsages),
			DisplayType = () => EItemAttributeDisplayType.Compact
		});
		if (template.MaximumNumberOfUsage > 0)
		{
			item.Attributes.Add(new ItemAttributeClass(EItemAttributeId.KeyUses)
			{
				Name = GClass3374.GetName(EItemAttributeId.KeyUses),
				StringValue = delegate
				{
					int num = template.MaximumNumberOfUsage - keyComponent_0.NumberOfUsages;
					bool flag = maximumUsages > 1 && num == 1;
					return $"{(flag ? string.Format(RedColorFormat, num) : num.ToString())}/{template.MaximumNumberOfUsage}";
				},
				DisplayType = () => EItemAttributeDisplayType.Compact
			});
		}
	}

	public string method_0(int usage)
	{
		if (usage != 1)
		{
			if (usage <= 1)
			{
				return string.Format(CyanColorFormat, GClass2348.Localized("keycard_unlimited"));
			}
			return GClass2348.Localized("keycard_reusable");
		}
		return string.Format(RedColorFormat, GClass2348.Localized("keycard_single"));
	}
}
