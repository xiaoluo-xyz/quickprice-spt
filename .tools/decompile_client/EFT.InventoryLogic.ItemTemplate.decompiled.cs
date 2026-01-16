using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using JetBrains.Annotations;
using JsonType;
using Newtonsoft.Json;

namespace EFT.InventoryLogic;

public class ItemTemplate : IUnlootableComponentTemplate, IAnimationVariantsComponentTemplate, IItemTemplate
{
	public class GClass1868 : GClass1867<ItemTemplate, string>
	{
		public string _id;

		public string _name;

		public NodeType _type;

		public FlatItemsDataClass[] _items;

		public override string TypeToKey(Type serializedType)
		{
			return TemplateIdToObjectMappingsClass.BackwardTypeTable[serializedType];
		}

		public override Type KeyToType(string serializedType)
		{
			Type value;
			switch (_type)
			{
			case NodeType.Node:
				if (!TemplateIdToObjectMappingsClass.TemplateTypeTable.TryGetValue(_id, out value))
				{
					throw new Exception("No C# type for taxonomy node with id " + _id + " found. Node name: " + _name);
				}
				break;
			default:
				throw new ArgumentException("_type has unexpected value: " + _type);
			case NodeType.Item:
				if (!TemplateIdToObjectMappingsClass.TemplateTypeTable.TryGetValue(serializedType, out value))
				{
					throw new Exception("No C# type for taxonomy item with id " + serializedType + " found. Item name: " + _name);
				}
				break;
			}
			return value;
		}

		public override ItemTemplate Deserialize(JsonReader reader, Type objectType, JsonSerializer serializer)
		{
			ItemTemplate itemTemplate = base.Deserialize(reader, objectType, serializer);
			itemTemplate._id = _id;
			itemTemplate._name = _name;
			itemTemplate._type = _type;
			if (!GClass1673.IsNullOrEmpty(_parent))
			{
				itemTemplate.ParentId = _parent;
			}
			return itemTemplate;
		}
	}

	public string Name;

	public string ShortName;

	public string Description;

	public float Weight;

	public bool ExaminedByDefault;

	public float ExamineTime;

	public bool QuestItem;

	public TaxonomyColor BackgroundColor;

	public int Width;

	public int Height;

	public int ExtraSizeLeft;

	public int ExtraSizeRight;

	public int ExtraSizeUp;

	public int ExtraSizeDown;

	public bool ExtraSizeForceAdd;

	public int StackMaxSize;

	public int StackObjectsCount;

	public int CreditsPrice;

	public string ItemSound;

	public ResourceKey Prefab;

	public ResourceKey UsePrefab;

	public ELootRarity Rarity;

	public EItemDropSoundType DropSoundType;

	public float SpawnChance;

	public bool NotShownInSlot;

	public int LootExperience;

	public bool HideEntrails;

	public int ExamineExperience;

	public int RepairCost;

	public int RepairSpeed;

	public bool MergesWithChildren;

	public bool CanSellOnRagfair;

	public bool CanRequireOnRagfair;

	public string[] ConflictingItems;

	public int AnimationVariantsNumber;

	public float RagFairCommissionModifier = 1f;

	public bool IsAlwaysAvailableForInsurance;

	public bool InsuranceDisabled;

	public int DiscardLimit = -1;

	public bool Unlootable;

	public bool IsUnremovable;

	public bool IsSpecialSlotOnly;

	public bool IsSecretExitRequirement;

	public bool LeftHandItem;

	public string UnlootableFromSlot;

	public EPlayerSideMask UnlootableFromSide;

	[NonSerialized]
	public List<IItemComponent> ReadonlyComponents;

	[NonSerialized]
	public List<ItemTemplate> Children_1;

	[NonSerialized]
	[JsonIgnore]
	public IReadOnlyList<ItemTemplate> CompatibleItems_1;

	public string ShortNameLocalizationKey => string.Concat(_id, " ShortName");

	public string NameLocalizationKey => string.Concat(_id, " Name");

	public string DescriptionLocalizationKey => string.Concat(_id, " Description");

	public virtual IEnumerable<ResourceKey> AllResources
	{
		get
		{
			if (!string.IsNullOrEmpty(Prefab.path))
			{
				yield return Prefab;
				if (!string.IsNullOrEmpty(UsePrefab.path))
				{
					yield return UsePrefab;
				}
			}
		}
	}

	public ExtraSize ExtraSize => new ExtraSize
	{
		Left = ((!ExtraSizeForceAdd) ? ExtraSizeLeft : 0),
		Right = ((!ExtraSizeForceAdd) ? ExtraSizeRight : 0),
		Up = ((!ExtraSizeForceAdd) ? ExtraSizeUp : 0),
		Down = ((!ExtraSizeForceAdd) ? ExtraSizeDown : 0),
		ForcedLeft = (ExtraSizeForceAdd ? ExtraSizeLeft : 0),
		ForcedRight = (ExtraSizeForceAdd ? ExtraSizeRight : 0),
		ForcedUp = (ExtraSizeForceAdd ? ExtraSizeUp : 0),
		ForcedDown = (ExtraSizeForceAdd ? ExtraSizeDown : 0)
	};

	string IUnlootableComponentTemplate.SlotName => UnlootableFromSlot;

	EPlayerSideMask IUnlootableComponentTemplate.Side => UnlootableFromSide;

	int IAnimationVariantsComponentTemplate.AnimationVariantsNumber => AnimationVariantsNumber;

	[field: NonSerialized]
	public MongoID _id { get; set; }

	[field: NonSerialized]
	public string _name { get; set; }

	[JsonProperty("_parent")]
	[field: NonSerialized]
	public MongoID? ParentId { get; set; }

	[field: NonSerialized]
	public NodeType _type { get; set; }

	public string StringId => _id.ToString();

	[JsonIgnore]
	public IReadOnlyList<ItemTemplate> CompatibleItems
	{
		get
		{
			if (CompatibleItems_1 != null)
			{
				return CompatibleItems_1;
			}
			Dictionary<ItemTemplate, ItemTemplate[]> allChildrenDict = new Dictionary<ItemTemplate, ItemTemplate[]>();
			ItemFactoryClass instance = Singleton<ItemFactoryClass>.Instance;
			IEnumerable<ItemTemplate> allCompatibleTemplates = GClass1408.GetAllCompatibleTemplates(new ItemTemplate[1] { instance.ItemTemplates[_id] }, allChildrenDict);
			CompatibleItems_1 = allCompatibleTemplates.ToList();
			return CompatibleItems_1;
		}
	}

	[JsonIgnore]
	public IReadOnlyList<ItemTemplate> Children => Children_1;

	[JsonIgnore]
	[field: NonSerialized]
	public ItemTemplate Parent { get; set; }

	[JsonConstructor]
	public ItemTemplate()
	{
	}

	public ItemTemplate(MongoID id)
	{
		_id = id;
	}

	public virtual void OnInit()
	{
	}

	public void AddChild(ItemTemplate template)
	{
		if (Children_1 == null)
		{
			Children_1 = new List<ItemTemplate>(3);
		}
		Children_1.Add(template);
		template.Parent = this;
	}

	public bool IsChildOf(string parentTemplateId)
	{
		ItemTemplate parent = Parent;
		while (true)
		{
			if (parent != null)
			{
				if (parent._id == (MongoID)parentTemplateId)
				{
					break;
				}
				parent = parent.Parent;
				continue;
			}
			return false;
		}
		return true;
	}

	public virtual List<IItemComponent> CreateReadonlyComponentsCollection()
	{
		return new List<IItemComponent>();
	}

	[NotNull]
	public List<IItemComponent> GetReadonlyComponents()
	{
		return ReadonlyComponents ?? (ReadonlyComponents = CreateReadonlyComponentsCollection());
	}
}
