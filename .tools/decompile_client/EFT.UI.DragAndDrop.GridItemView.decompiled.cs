using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using TMPro;
using UI.DragAndDrop.ItemViews;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFT.UI.DragAndDrop;

public class GridItemView : ItemView, IOnItemAdded, GInterface179, IOnItemRemoved, GInterface182, GInterface183, GInterface184, GInterface185, GInterface186, GInterface187, GInterface188, GInterface189
{
	public enum EItemValueFormat
	{
		OneValue,
		TwoValues,
		Other
	}

	[Serializable]
	[CompilerGenerated]
	public class Class3348
	{
		public static readonly Class3348 class3348_0 = new Class3348();

		public static Func<BuffComponent, int> func_0;

		public static Func<Slot, Item> func_1;

		public int method_0(BuffComponent subBuff)
		{
			if (subBuff.Rarity != EBuffRarity.Rare)
			{
				return 1;
			}
			return -1;
		}

		public Item method_1(Slot x)
		{
			return x.ContainedItem;
		}
	}

	[CompilerGenerated]
	public class Class3349
	{
		public RectTransform customObject;

		public void method_0()
		{
			UnityEngine.Object.Destroy(customObject.gameObject);
		}
	}

	[CompilerGenerated]
	public class Class3350
	{
		public GridItemView gridItemView_0;

		public Weapon weapon;

		public GClass3455 selectableContext;

		public FilterPanel filterPanel;

		public void method_0()
		{
			gridItemView_0.HoverTrigger.OnHoverStart -= gridItemView_0.method_17;
			gridItemView_0.HoverTrigger.OnHoverEnd -= gridItemView_0.method_16;
		}

		public void method_1()
		{
			gridItemView_0.ItemContext.OnInventoryError -= gridItemView_0.method_12;
		}

		public void method_2()
		{
			weapon.MalfState.OnStateChanged -= gridItemView_0.UpdateInfo;
		}

		public void method_3()
		{
			gridItemView_0.ItemOwner.UnregisterView(gridItemView_0);
		}

		public void method_4()
		{
			gridItemView_0.ItemController.UnregisterView(gridItemView_0);
		}

		public void method_5()
		{
			gridItemView_0.ItemController.ExamineEvent -= gridItemView_0.method_24;
		}

		public void method_6()
		{
			selectableContext.OnSelected -= gridItemView_0.ChangeSelectedStatus;
		}

		public void method_7()
		{
			filterPanel.CurrentFilterChanged -= gridItemView_0.method_11;
		}
	}

	private const float float_2 = 40f;

	private const float float_3 = 12f;

	private static Color color_0 = Color.green;

	private static Color color_1 = Color.yellow;

	protected const int OVERLAP_LENGTH = 6;

	[SerializeField]
	private Image _unsearchedBackground;

	[SerializeField]
	private Image _secureIcon;

	[SerializeField]
	private Image _lockedIcon;

	[SerializeField]
	private Image _togglableIcon;

	[SerializeField]
	private Image _tagColor;

	[SerializeField]
	private Image _missingLayout;

	[SerializeField]
	protected TextMeshProUGUI Caption;

	[SerializeField]
	private TextMeshProUGUI TagName;

	[SerializeField]
	private BindPanel _bindPanel;

	[SerializeField]
	private RectTransform _infoPanel;

	[SerializeField]
	private GameObject _resizeRectPanelTemplate;

	[SerializeField]
	private WishlistGridView _wishlistView;

	[SerializeField]
	private GameObject _pinBackground;

	[SerializeField]
	private GameObject _lockIcon;

	public const float TOOLTIP_DELAY = 0.6f;

	private RectTransform rectTransform_1;

	private FilterPanel filterPanel_0;

	private InsuranceCompanyClass insuranceCompanyClass;

	private bool bool_5;

	private bool bool_6;

	private bool bool_7;

	private GClass3455 gclass3455_0;

	private GClass2067 gclass2067_0;

	private string string_0 = string.Empty;

	public ItemViewStats ItemViewStats => BottomPanel.ItemViewStats;

	public TextMeshProUGUI TextMeshProUGUI_0 => BottomPanel.ItemInscription;

	public TextMeshProUGUI ItemValue => BottomPanel.ItemValue;

	public PointerEventsProxy PointerEventsProxy_0 => BottomPanel.ValuePointerEventsProxy;

	public virtual string ValueFormat => "<color={2}>{0}</color>/{1}";

	public ItemClass ItemClass => ItemClass.FindOrCreate(base.Item);

	public Image SecureIcon => _secureIcon;

	public Image LockedIcon => _lockedIcon;

	public Image TogglableIcon => _togglableIcon;

	public override bool IsInteractable => true;

	public string CurrentItemValue
	{
		get
		{
			return string_0;
		}
		set
		{
			string_0 = value;
			ItemValue.SetText(value);
		}
	}

	public override void OnBeingExaminedChanged(bool isBeingExamined)
	{
		base.OnBeingExaminedChanged(isBeingExamined);
		SetInscriptionVisibility(!isBeingExamined);
		SetValueVisibility(!isBeingExamined);
		Caption.gameObject.SetActive(!isBeingExamined);
	}

	public static GridItemView Create(Item item, ItemContextAbstractClass sourceContext, ItemRotation rotation, TraderControllerClass itemController, IItemOwner itemOwner, [CanBeNull] FilterPanel filterPanel, [CanBeNull] IContainer container, [CanBeNull] ItemUiContext itemUiContext, InsuranceCompanyClass insurance, GClass2067 wishlistManager)
	{
		GridItemView gridItemView = ItemViewFactory.CreateFromPool<GridItemView>("grid_layout");
		gridItemView.NewGridItemView(item, sourceContext, rotation, itemController, itemOwner, filterPanel, container, itemUiContext, insurance, wishlistManager);
		gridItemView.Init();
		return gridItemView;
	}

	public GridItemView NewGridItemView(Item item, ItemContextAbstractClass sourceContext, ItemRotation rotation, TraderControllerClass itemController, IItemOwner itemOwner, [CanBeNull] FilterPanel filterPanel, [CanBeNull] IContainer container, [CanBeNull] ItemUiContext itemUiContext, InsuranceCompanyClass insurance, [CanBeNull] GClass2067 wishlistManger = null)
	{
		bool_6 = GClass2340.InRaid;
		bool_7 = false;
		gclass2067_0 = wishlistManger;
		NewItemView(item, sourceContext, rotation, itemController, container, itemOwner, itemUiContext);
		IsConflicting = false;
		base.IsSearched = !(item.CurrentAddress?.Container is GInterface215) || itemController.SearchController.IsItemKnown(item);
		base.RectTransform.anchorMin = new Vector2(0f, 0f);
		base.RectTransform.anchorMax = new Vector2(0f, 0f);
		insuranceCompanyClass = insurance;
		filterPanel_0 = filterPanel;
		method_18(enable: false);
		if (insuranceCompanyClass != null)
		{
			CompositeDisposable.SubscribeEvent(insurance.OnItemInsured, ChangeInsuredStatus);
			ChangeInsuredStatus(ItemClass);
			if (base.HoverTrigger != null && insurance.Insured(base.Item.Id))
			{
				if (ItemClass.InsurerId != null)
				{
					base.HoverTrigger.OnHoverStart += method_17;
					base.HoverTrigger.OnHoverEnd += method_16;
					CompositeDisposable.AddDisposable(delegate
					{
						base.HoverTrigger.OnHoverStart -= method_17;
						base.HoverTrigger.OnHoverEnd -= method_16;
					});
				}
				else
				{
					insurance.Logger.LogError("<b>Insurance.</b> [Insurer] on {0} is null.", GClass2348.Localized(base.Item.ShortName));
				}
			}
		}
		if (_wishlistView != null)
		{
			_wishlistView.Show(wishlistManger, item.TemplateId, Examined);
		}
		ChangeRepairBuffStatus();
		base.ItemContext.OnInventoryError += method_12;
		CompositeDisposable.AddDisposable(delegate
		{
			base.ItemContext.OnInventoryError -= method_12;
		});
		if (bool_6 && method_30(item, out var weapon))
		{
			weapon.MalfState.OnStateChanged += UpdateInfo;
			CompositeDisposable.AddDisposable(delegate
			{
				weapon.MalfState.OnStateChanged -= UpdateInfo;
			});
		}
		if (ItemOwner != null)
		{
			ItemOwner.RegisterView(this);
			CompositeDisposable.AddDisposable(delegate
			{
				ItemOwner.UnregisterView(this);
			});
		}
		if (ItemController != ItemOwner && ItemController != null)
		{
			ItemController.RegisterView(this);
			CompositeDisposable.AddDisposable(delegate
			{
				ItemController.UnregisterView(this);
			});
		}
		if (ItemController != null)
		{
			ItemController.ExamineEvent += method_24;
			CompositeDisposable.AddDisposable(delegate
			{
				ItemController.ExamineEvent -= method_24;
			});
		}
		bool flag = true;
		foreach (GEventArgs2 item2 in ItemController.SelectEvents<GEventArgs2>(base.Item))
		{
			if (item2.Status != CommandStatus.Succeed)
			{
				flag = false;
				break;
			}
		}
		SetItemBinding(flag ? ItemView.GetBindingForItem(ItemController, base.Item) : ((EBoundItem?)null));
		CompositeDisposable.BindState(base.IsBeingDragged, method_15);
		method_21();
		if (_missingLayout != null)
		{
			_missingLayout.gameObject.SetActive(value: false);
		}
		if (base.ItemContext is GInterface435 gInterface && _missingLayout != null)
		{
			_missingLayout.gameObject.SetActive(gInterface.HasMissingChildren);
		}
		ItemContextAbstractClass itemContext = base.ItemContext;
		GClass3455 selectableContext = itemContext as GClass3455;
		if (selectableContext != null)
		{
			switch (selectableContext.ViewType)
			{
			case EItemViewType.Empty:
			case EItemViewType.Inventory:
			case EItemViewType.ScavInventory:
			case EItemViewType.TradingPlayer:
			case EItemViewType.TransferPlayer:
			case EItemViewType.TransferTrader:
			case EItemViewType.HideoutAreaStash:
			case EItemViewType.InventoryWithoutDiscard:
			case EItemViewType.InventoryDuringMatching:
				gclass3455_0 = selectableContext;
				ChangeSelectedStatus(selectableContext.IsSelected);
				selectableContext.OnSelected += ChangeSelectedStatus;
				CompositeDisposable.AddDisposable(delegate
				{
					selectableContext.OnSelected -= ChangeSelectedStatus;
				});
				break;
			}
		}
		if (PointerEventsProxy_0 != null)
		{
			CompositeDisposable.SubscribeEvent(PointerEventsProxy_0.OnPointerEnter, method_34);
			CompositeDisposable.SubscribeEvent(PointerEventsProxy_0.OnPointerExit, method_35);
		}
		if (filterPanel == null)
		{
			return this;
		}
		method_11();
		filterPanel.CurrentFilterChanged += method_11;
		CompositeDisposable.AddDisposable(delegate
		{
			filterPanel.CurrentFilterChanged -= method_11;
		});
		return this;
	}

	public override void Init(ResourceTypeStruct resourceType, bool isStub = false)
	{
		base.Init(resourceType, isStub);
		method_10();
	}

	public void method_10()
	{
		if (ItemValue.text != CurrentItemValue)
		{
			ItemValue.text = CurrentItemValue;
		}
	}

	public void SetValueVisibility(bool visible)
	{
		ItemValue.gameObject.SetActive(visible);
	}

	public virtual void SetInscription(string inscription)
	{
		TextMeshProUGUI_0.text = inscription;
	}

	public virtual void SetInscriptionVisibility(bool visible)
	{
		TextMeshProUGUI_0.gameObject.SetActive(visible);
	}

	public override void OnClick(PointerEventData.InputButton button, Vector2 position, bool doubleClick)
	{
		if (button == PointerEventData.InputButton.Left && !doubleClick)
		{
			GClass3455 gClass = gclass3455_0;
			if (gClass != null && gClass.IsActive(out var _))
			{
				gclass3455_0.ToggleSelection();
				return;
			}
		}
		base.OnClick(button, position, doubleClick);
	}

	public virtual void ChangeSelectedStatus(bool selected)
	{
		if (_missingLayout != null)
		{
			_missingLayout.gameObject.SetActive(selected);
		}
	}

	public void method_11()
	{
		base.IsFilteredOut.Value = !filterPanel_0.IsFilteredSingleItem(base.Item);
	}

	public void method_12(InventoryError error)
	{
		InteractionsHandlerClass.GClass1605 gClass = error as InteractionsHandlerClass.GClass1605;
		bool flag = gClass != null && gClass.ConflictingItem == base.Item;
		if (bool_5 == flag)
		{
			return;
		}
		bool_5 = flag;
		if (bool_5)
		{
			if (rectTransform_1 == null)
			{
				rectTransform_1 = (RectTransform)UnityEngine.Object.Instantiate(_resizeRectPanelTemplate, base.transform, worldPositionStays: false).transform;
				rectTransform_1.gameObject.SetActive(bool_5);
			}
			rectTransform_1.sizeDelta = ItemViewFactory.GetCellPixelSize(GClass707.Rotate(((GClass3393)gClass.ConflictingItem.Parent).LocationInGrid.r, gClass.NewSize));
			base.transform.SetAsLastSibling();
		}
		else if (rectTransform_1 != null)
		{
			UnityEngine.Object.Destroy(rectTransform_1.gameObject);
		}
		UpdateInfo();
	}

	public void method_13(ItemContextClass dragItemContext)
	{
		if (Container.CanAccept(dragItemContext, base.ItemContext, out var _))
		{
			if (base.Item is CompoundItem compoundItem && compoundItem.Grids.Any())
			{
				SelectedColor = color_1;
			}
			else if (base.Item.StackMaxSize > 1)
			{
				SelectedColor = color_1;
			}
			else
			{
				SelectedColor = color_0;
			}
		}
		else
		{
			if (!CanInteract(dragItemContext))
			{
				method_14();
				return;
			}
			SelectedColor = color_0;
		}
		SelectedColor.a = 10f / 51f;
		HighlightedGlobally = true;
		UpdateColor();
	}

	public void method_14()
	{
		SelectedColor = ItemView.DefaultSelectedColor;
		HighlightedGlobally = false;
		UpdateColor();
	}

	public override void CheckAcceptHandler(ItemContextClass dragItemContext)
	{
		base.CheckAcceptHandler(dragItemContext);
		if (Container != null && base.ItemContext.IsPreviewHighlightAvailable)
		{
			if (dragItemContext == null)
			{
				method_14();
			}
			else if (!base.Item.Equals(dragItemContext.Item) && !GClass3380.IsChildOf(base.Item, dragItemContext.Item))
			{
				method_13(dragItemContext);
			}
		}
	}

	public void method_15(bool dragged)
	{
		Animator.SetDragState(dragged);
	}

	public void method_16(PointerEventData eventData)
	{
		ItemUiContext.Tooltip.Close();
		ShowTooltip();
	}

	public void method_17(PointerEventData eventData)
	{
		HideTooltip();
		ItemUiContext.Tooltip.Show("<color=#dd831a><b>" + GClass2348.Localized("Insured by") + "</b></color> <color=white>" + GClass2348.Localized(Singleton<BackendConfigSettingsClass>.Instance.TradersSettings[ItemClass.InsurerId].Nickname) + "</color>");
	}

	public void ChangeInsuredStatus(ItemClass item)
	{
		if (!(item.Id != ItemClass.Id))
		{
			method_18(insuranceCompanyClass.Insured(base.Item.Id));
		}
	}

	public void method_18(bool enable)
	{
		if (InsuredItemBorder != null)
		{
			InsuredItemBorder.SetActive(enable);
		}
		if (base.InsuredIcon != null)
		{
			base.InsuredIcon.SetActive(enable);
		}
	}

	public void ChangeRepairBuffStatus()
	{
		if (!(base.RepairBuffIcon == null))
		{
			if (method_19(out var buff) && Examined)
			{
				base.RepairBuffIcon.sprite = EFTHardSettings.Instance.StaticIcons.GetAttributeIcon(buff.BuffAttributeId);
				base.RepairBuffIcon.SetNativeSize();
				base.RepairBuffIcon.gameObject.SetActive(value: true);
			}
			else
			{
				base.RepairBuffIcon.gameObject.SetActive(value: false);
			}
		}
	}

	public bool method_19(out BuffComponent buff)
	{
		buff = null;
		buff = (from subBuff in method_20()
			orderby (subBuff.Rarity != EBuffRarity.Rare) ? 1 : (-1)
			select subBuff).FirstOrDefault();
		return buff != null;
	}

	public IEnumerable<BuffComponent> method_20()
	{
		foreach (BuffComponent itemComponentsInChild in GClass3380.GetItemComponentsInChildren<BuffComponent>(base.Item))
		{
			if (itemComponentsInChild.IsActive)
			{
				yield return itemComponentsInChild;
			}
		}
	}

	public void OnRefreshItem_1(GEventArgs18 eventArgs)
	{
		OnRefreshItem(eventArgs);
	}

	void GInterface186.OnRefreshItem(GEventArgs18 eventArgs)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnRefreshItem_1
		this.OnRefreshItem_1(eventArgs);
	}

	public virtual void OnRefreshItem(GEventArgs18 eventArgs)
	{
		if (eventArgs.Item == base.Item && !(MainImage == null))
		{
			ItemRotation = ItemRotation;
			base.IsBeingDragged.Value = false;
			UpdateInfo();
			if (eventArgs.RefreshIcon)
			{
				RefreshIcon();
			}
			method_21();
			UpdateRemoveError();
		}
	}

	public void method_21()
	{
		if (_tagColor == null || TagName == null)
		{
			return;
		}
		TagComponent itemComponent = base.Item.GetItemComponent<TagComponent>();
		if (itemComponent == null)
		{
			_tagColor.gameObject.SetActive(value: false);
			return;
		}
		string text = itemComponent.Name;
		if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text.Trim()))
		{
			_tagColor.gameObject.SetActive(value: true);
			_tagColor.color = EditTagWindow.GetColor(itemComponent.Color);
			TagName.text = text;
			method_22().HandleExceptions();
		}
		else
		{
			_tagColor.gameObject.SetActive(value: false);
		}
	}

	public async Task method_22()
	{
		TagName.gameObject.SetActive(value: false);
		await Task.Yield();
		RectTransform rectTransform = _tagColor.rectTransform;
		float num = base.RectTransform.sizeDelta.x - Caption.renderedWidth - 2f;
		if (num < 40f)
		{
			rectTransform.sizeDelta = new Vector2(base.RectTransform.sizeDelta.x, rectTransform.sizeDelta.y);
			return;
		}
		TagName.gameObject.SetActive(value: true);
		float x = Mathf.Clamp(TagName.preferredWidth + 12f, 40f, num);
		rectTransform.sizeDelta = new Vector2(x, _tagColor.rectTransform.sizeDelta.y);
	}

	public void OnItemAdded_1(GEventArgs2 eventArgs)
	{
		OnItemAdded(eventArgs);
	}

	void IOnItemAdded.OnItemAdded(GEventArgs2 eventArgs)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnItemAdded_1
		this.OnItemAdded_1(eventArgs);
	}

	public virtual void OnItemAdded(GEventArgs2 eventArgs)
	{
		if (eventArgs.Status == CommandStatus.Succeed)
		{
			method_23(eventArgs.To);
			SetItemBinding(ItemView.GetBindingForItem(ItemController, base.Item));
		}
	}

	public void OnItemRemoved_1(GEventArgs3 eventArgs)
	{
		OnItemRemoved(eventArgs);
	}

	void IOnItemRemoved.OnItemRemoved(GEventArgs3 eventArgs)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnItemRemoved_1
		this.OnItemRemoved_1(eventArgs);
	}

	public virtual void OnItemRemoved(GEventArgs3 eventArgs)
	{
		if (eventArgs.Status == CommandStatus.Succeed)
		{
			method_23(eventArgs.From);
		}
		if (eventArgs.Item == base.Item && eventArgs.Status == CommandStatus.Succeed)
		{
			UpdateInfo();
		}
	}

	public void OnMagazineChange(MagazineItemClass magazine)
	{
		if (magazine == base.Item)
		{
			UpdateInfo();
		}
	}

	void GInterface182.OnMagazineChange(MagazineItemClass magazine)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnMagazineChange
		this.OnMagazineChange(magazine);
	}

	public void OnInventoryMagazineCheck(MagazineItemClass magazine, float speed, bool status)
	{
		if (magazine == base.Item)
		{
			SetInventoryCheckMagazineStatus(speed, status);
		}
	}

	void GInterface185.OnInventoryMagazineCheck(MagazineItemClass magazine, float speed, bool status)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnInventoryMagazineCheck
		this.OnInventoryMagazineCheck(magazine, speed, status);
	}

	public void method_23([CanBeNull] ItemAddress location)
	{
		if (!(this == null) && GClass3380.IsChildOf(location, base.Item))
		{
			RefreshIcon();
			UpdateInfo();
			method_21();
			method_28();
		}
	}

	public void OnUnbindItem(GEventArgs12 eventArgs)
	{
		if (eventArgs.Status == CommandStatus.Succeed && eventArgs.Item == base.Item)
		{
			SetItemBinding(null);
		}
	}

	void GInterface189.OnUnbindItem(GEventArgs12 eventArgs)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnUnbindItem
		this.OnUnbindItem(eventArgs);
	}

	public void OnBindItem(GEventArgs11 eventArgs)
	{
		if (eventArgs.Status == CommandStatus.Succeed && eventArgs.Item == base.Item)
		{
			SetItemBinding(eventArgs.Index);
		}
	}

	void GInterface188.OnBindItem(GEventArgs11 eventArgs)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnBindItem
		this.OnBindItem(eventArgs);
	}

	public void OnDrain(GEventArgs13 eventArgs)
	{
		if (eventArgs.Item == base.Item)
		{
			base.IsBeingDrained.Value = eventArgs.Status == CommandStatus.Begin;
		}
	}

	void GInterface187.OnDrain(GEventArgs13 eventArgs)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnDrain
		this.OnDrain(eventArgs);
	}

	public void method_24(GEventArgs6 eventArgs)
	{
		if (eventArgs.Item == base.Item)
		{
			SetBeingExaminedState(eventArgs);
		}
		if (!(eventArgs.Item.TemplateId != base.Item.TemplateId))
		{
			if (_wishlistView != null)
			{
				_wishlistView.SetExamined(examined: true);
			}
			UpdateStaticInfo();
			UpdateInfo();
		}
	}

	public void OnLoadMagazine(GEventArgs7 eventArgs)
	{
		if (GClass3380.GetRootMergedItem(eventArgs.TargetItem) == base.Item)
		{
			SetLoadMagazineStatus(eventArgs);
		}
		if (eventArgs.Item == base.Item)
		{
			SetLoadAmmoStatus(eventArgs);
		}
	}

	void GInterface183.OnLoadMagazine(GEventArgs7 eventArgs)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnLoadMagazine
		this.OnLoadMagazine(eventArgs);
	}

	public void OnUnloadMagazine(GEventArgs8 eventArgs)
	{
		if (eventArgs.FromItem == base.Item)
		{
			SetUnloadMagazineStatus(eventArgs);
		}
		if (eventArgs.TargetItem == base.Item)
		{
			SetLoadAmmoStatus(eventArgs);
		}
	}

	void GInterface184.OnUnloadMagazine(GEventArgs8 eventArgs)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnUnloadMagazine
		this.OnUnloadMagazine(eventArgs);
	}

	[CanBeNull]
	public virtual string GetErrorText()
	{
		Error error = base.RemoveError.Value;
		if (base.ItemContext is GInterface435 { HasMissingChildren: not false } gInterface)
		{
			error = gInterface.Error;
		}
		if (error != null)
		{
			if (error is InteractionsHandlerClass.GClass1606)
			{
				return "<sprite name=\"LockedIcon\"> " + method_26();
			}
			string text = ((error is GClass1540 gClass) ? gClass.GetLocalizedDescription() : base.RemoveError.Value.ToString());
			return "<color=red>" + text + "</color>";
		}
		return null;
	}

	public void method_25(ArmorPlateItemClass armorPlate, GClass3125 armorSlot)
	{
		string errorText = GetErrorText();
		string text = GClass3132.FormatArmorPlateTooltip(armorPlate, armorSlot, errorText);
		ItemUiContext.Tooltip.Show(text, null, 0.6f);
	}

	public virtual void ShowTooltip()
	{
		if (base.Item is ArmorPlateItemClass armorPlate && base.Item.CurrentAddress is GClass3391 { Slot: GClass3125 slot })
		{
			method_25(armorPlate, slot);
			return;
		}
		string errorText = GetErrorText();
		if (errorText != null)
		{
			ItemUiContext.Tooltip.Show(errorText);
			return;
		}
		if (base.Item is Weapon weapon)
		{
			if (ItemController is GInterface415 gInterface && gInterface.HasKnownMalfunction(weapon))
			{
				if (!gInterface.HasKnownMalfType(weapon))
				{
					ItemUiContext.Tooltip.Show("<color=red>" + GClass2541.GetLocalizedDescription(withKey: false) + "</color>");
				}
				else
				{
					ItemUiContext.Tooltip.Show("<color=red>" + GClass2540.GetLocalizedDescription(weapon.MalfState.State, withKey: false) + "</color>");
				}
				return;
			}
			if (!weapon.CompatibleAmmo)
			{
				MagazineItemClass currentMagazine = weapon.GetCurrentMagazine();
				ItemUiContext.Tooltip.Show("<color=red>" + string.Format(GClass2348.Localized("Ammo ({0}) is not compatible. Need: {1}") + "</color>", "<color=white>" + ((currentMagazine != null) ? GClass2348.Localized(currentMagazine.Cartridges.Last.Name) : string.Empty) + "</color>", "<color=white>" + weapon.AmmoCaliber + "</color>"));
				return;
			}
		}
		if (IsTeammateDogtag)
		{
			ItemUiContext.Tooltip.Show(GClass2348.Localized("You cannot take off a dogtag from a friend or group member"));
			return;
		}
		string text = method_26();
		if (base.Item is RandomLootContainerItemClass && bool_6)
		{
			text = text + "\n\n<color=red>" + GClass2348.Localized("UI/Inventory/CantUnpackInRaid") + "</color>";
		}
		ItemUiContext.Tooltip.Show(text, null, 0.6f);
	}

	public string method_26()
	{
		if (!Examined)
		{
			return GClass2348.Localized("Unknown item");
		}
		return Singleton<ItemFactoryClass>.Instance.BriefItemName(base.Item, GClass2348.Localized(base.Item.Name));
	}

	public virtual void HideTooltip()
	{
		if (ItemUiContext.Tooltip.Displayed)
		{
			ItemUiContext.Tooltip.Close();
		}
		if (ItemUiContext.MultiLineTooltip.Displayed)
		{
			ItemUiContext.MultiLineTooltip.Close();
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		if (base.IsSearched)
		{
			ShowTooltip();
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.ItemContext.InventoryError = null;
		base.OnPointerExit(eventData);
		HideTooltip();
	}

	public override void UpdateStaticInfo()
	{
		base.UpdateStaticInfo();
		SetInscription(string.Empty);
		SetInscriptionVisibility(visible: false);
		ItemViewStats.SetStaticInfo(base.Item, Examined);
	}

	public static string GetStackColor(Item item)
	{
		string result = "#b6c1c7";
		if (!(item is MoneyItemClass))
		{
			if (item is AmmoItemClass)
			{
				result = "#dadabc";
			}
		}
		else
		{
			result = "#8db500";
		}
		return result;
	}

	public string method_27(ItemAddress itemAddress)
	{
		if (itemAddress is GClass3391 { Slot: GClass3125 slot } && base.Item is ArmorPlateItemClass)
		{
			return slot.LocalizedName();
		}
		return GClass2348.Localized(base.Item.ShortName);
	}

	public override void UpdateInfo()
	{
		if (base.Item == null)
		{
			return;
		}
		base.IsSearched = !(base.Item.CurrentAddress?.Container is GInterface215) || ItemController.SearchController.IsItemKnown(base.Item);
		if (!base.IsSearched)
		{
			for (int i = 0; i < base.transform.childCount; i++)
			{
				base.transform.GetChild(i).gameObject.SetActive(value: false);
			}
			_unsearchedBackground.gameObject.SetActive(value: true);
			return;
		}
		if ((object)_unsearchedBackground != null)
		{
			_unsearchedBackground.gameObject.SetActive(value: false);
		}
		if (_pinBackground != null)
		{
			_pinBackground.gameObject.SetActive(base.Item.PinLockState != EItemPinLockState.Free);
		}
		if (_lockIcon != null)
		{
			_lockIcon.gameObject.SetActive(base.Item.PinLockState == EItemPinLockState.Locked);
		}
		bool flag = base.ItemContext.HasViewComponent(EItemViewComponent.ResourceInfo);
		MedKitComponent itemComponent = base.Item.GetItemComponent<MedKitComponent>();
		FoodDrinkComponent itemComponent2 = base.Item.GetItemComponent<FoodDrinkComponent>();
		DogtagComponent itemComponent3 = base.Item.GetItemComponent<DogtagComponent>();
		ResourceComponent itemComponent4 = base.Item.GetItemComponent<ResourceComponent>();
		KeyComponent itemComponent5 = base.Item.GetItemComponent<KeyComponent>();
		SideEffectComponent itemComponent6 = base.Item.GetItemComponent<SideEffectComponent>();
		RepairKitsItemClass repairKitsItemClass = base.Item as RepairKitsItemClass;
		RepairableComponent[] array = GClass3380.GetItemComponentsInChildren<RepairableComponent>(base.Item).ToArray();
		bool examined = Examined;
		ArmorComponent component;
		ArmorHolderComponent component2;
		bool flag2 = array.Any() && (base.Item.TryGetItemComponent<ArmorComponent>(out component) || base.Item.TryGetItemComponent<ArmorHolderComponent>(out component2));
		float num = 0f;
		float num2 = 0f;
		bool_7 = method_36();
		method_28();
		UpdateItemValue(string.Empty);
		SetCountValue();
		SetQuestItemViewPanel();
		LockableComponent itemComponent7 = base.Item.GetItemComponent<LockableComponent>();
		_lockedIcon.gameObject.SetActive(itemComponent7?.Locked ?? false);
		TogglableComponent togglableComponent = (base.ItemContext.HasViewComponent(EItemViewComponent.TogglableComponent) ? base.Item.GetItemComponent<TogglableComponent>() : null);
		_togglableIcon.gameObject.SetActive(togglableComponent != null);
		if (togglableComponent != null)
		{
			_togglableIcon.sprite = (togglableComponent.On ? EFTHardSettings.Instance.StaticIcons.TogglableOn : EFTHardSettings.Instance.StaticIcons.TogglableOff);
		}
		if (ItemViewFactory.IsSecureContainer(base.Item) && !base.IsBeingExamined.Value)
		{
			_secureIcon.gameObject.SetActive(value: true);
			_lockedIcon.gameObject.SetActive(value: false);
		}
		else
		{
			_secureIcon.gameObject.SetActive(value: false);
		}
		if (base.Item is Weapon weapon)
		{
			MagazineItemClass currentMagazine = weapon.GetCurrentMagazine();
			bool flag3 = currentMagazine?.IsAmmoCompatible(weapon.Chambers) ?? false;
			weapon.CompatibleAmmo = flag3 || currentMagazine == null;
			string inscription = (examined ? weapon.AmmoCaliber : "?");
			SetInscription(inscription);
			int num3 = 0;
			int num4 = 0;
			EItemValueFormat format = EItemValueFormat.TwoValues;
			string color = "red";
			if (currentMagazine != null)
			{
				num3 = currentMagazine.Count;
				num4 = currentMagazine.MaxCount;
			}
			else if (weapon.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
			{
				num3 = weapon.Chambers.Select((Slot x) => x.ContainedItem).OfType<AmmoItemClass>().Count();
				num4 = weapon.Chambers.Length;
			}
			if (num4 > 0)
			{
				color = (((float)num3 / (float)num4 < 0.15f) ? "red" : "#dadabc");
				format = EItemValueFormat.Other;
			}
			SetItemValue(format, examined, color, num3, num4);
		}
		else if (flag2)
		{
			RepairableComponent[] array2 = array;
			foreach (RepairableComponent repairableComponent in array2)
			{
				num += repairableComponent.Durability;
				num2 += repairableComponent.MaxDurability;
			}
			if (num2 > Mathf.Epsilon)
			{
				SetItemValue(EItemValueFormat.TwoValues, examined, (num <= 0f) ? "#ff0000" : "#ffffff", Mathf.Round(num), Mathf.Round(num2));
			}
		}
		else if (itemComponent6 != null && GClass855.Positive(itemComponent6.Value))
		{
			SetItemValue(EItemValueFormat.TwoValues, examined, (itemComponent6.Value / itemComponent6.MaxResource < 0.15f) ? "red" : "#009225", (int)itemComponent6.Value, itemComponent6.MaxResource, "#009225");
		}
		else if (base.Item is MagazineItemClass magazineItemClass)
		{
			SetItemValue(EItemValueFormat.TwoValues, examined, "#dadabc", magazineItemClass.Count, magazineItemClass.MaxCount);
		}
		else if (base.Item is AmmoBox ammoBox)
		{
			SetItemValue(EItemValueFormat.TwoValues, examined, "#dadabc", ammoBox.Count, ammoBox.MaxCount);
		}
		else if (itemComponent != null)
		{
			SetItemValue(EItemValueFormat.TwoValues, examined, "#ff5335", Mathf.RoundToInt(itemComponent.HpResource), Mathf.RoundToInt(itemComponent.MaxHpResource));
		}
		else if (itemComponent2 != null && itemComponent2.MaxResource > 1f)
		{
			SetItemValue(EItemValueFormat.TwoValues, examined, "#ff5335", Mathf.RoundToInt(itemComponent2.HpPercent), Mathf.RoundToInt(itemComponent2.MaxResource));
		}
		else if (itemComponent3 != null)
		{
			int level = itemComponent3.Level;
			string inscription2 = ((level > 0) ? level.ToString() : string.Empty);
			SetInscription(inscription2);
		}
		else if (itemComponent4 != null && itemComponent4.MaxResource > 0f)
		{
			SetItemValue(EItemValueFormat.TwoValues, examined, "#dadabc", (int)itemComponent4.Value, itemComponent4.MaxResource);
		}
		else if (itemComponent5 != null)
		{
			int maximumNumberOfUsage = itemComponent5.Template.MaximumNumberOfUsage;
			if (maximumNumberOfUsage > 0)
			{
				int num6 = maximumNumberOfUsage - itemComponent5.NumberOfUsages;
				SetItemValue(EItemValueFormat.TwoValues, examined, "#ff5335", Mathf.RoundToInt(num6), Mathf.RoundToInt(maximumNumberOfUsage));
			}
		}
		else if (repairKitsItemClass != null)
		{
			float resource = repairKitsItemClass.Resource;
			float num7 = (float)repairKitsItemClass.MaxRepairResource / 2f;
			SetItemValue(EItemValueFormat.TwoValues, examined, (Math.Ceiling(resource) <= 0.0) ? "#ff0000" : "#ffffff", (resource > num7) ? Math.Floor(resource) : Math.Ceiling(resource), repairKitsItemClass.MaxRepairResource);
		}
		bool flag4 = (base.Item is CompoundItem compoundItem && compoundItem.MissingVitalParts.Any()) || base.Item is Weapon { CompatibleAmmo: false } || method_33(base.Item);
		if (!base.IsBeingLoadedAmmo.Value && !base.IsBeingLoadedMagazine.Value && !base.IsBeingUnloadedMagazine.Value)
		{
			if (!examined)
			{
				MainImage.color = new Color(0f, 0f, 0f, 0.75f);
				BackgroundColor = new Color32(0, 0, 0, 100);
			}
			else if (!flag4 && !bool_5 && !IsConflicting)
			{
				if (method_19(out var buff) && buff.IsActive)
				{
					MainImage.color = Color.white;
					BackgroundColor = new Color32(209, 153, 45, 32);
				}
				else
				{
					MainImage.color = Color.white;
					BackgroundColor = OriginalBackgroundColor;
				}
				ChangeRepairBuffStatus();
			}
			else
			{
				MainImage.color = Color.red;
				BackgroundColor = new Color32(83, 0, 0, 63);
			}
		}
		Animator.SetExaminedState(examined);
		SetValueVisibility(CurrentItemValue.Length > 0 && flag);
		SetInscriptionVisibility(!GClass1673.IsNullOrEmpty(BottomPanel.ItemInscription.text));
		if (!base.IsBeingLoadedMagazine.Value && !base.IsBeingUnloadedMagazine.Value)
		{
			UpdateColor();
		}
	}

	public void method_28()
	{
		if (base.Item != null)
		{
			Caption.text = method_29(base.Item.CurrentAddress);
		}
	}

	public string method_29(ItemAddress nextAddress)
	{
		string empty = string.Empty;
		if (base.Item == null)
		{
			return empty;
		}
		DogtagComponent itemComponent = base.Item.GetItemComponent<DogtagComponent>();
		empty = ((itemComponent == null || string.IsNullOrEmpty(itemComponent.Nickname)) ? (Examined ? Singleton<ItemFactoryClass>.Instance.BriefItemName(base.Item, method_27(nextAddress)) : "???") : (Examined ? GClass856.SubstringIfNecessary(itemComponent.Nickname, 20) : "???"));
		return "<color=#b6c1c7> " + empty + " </color>";
	}

	public virtual void SetCountValue()
	{
		if (base.Item.UnlimitedCount)
		{
			SetItemValue(EItemValueFormat.OneValue, display: true, GetStackColor(base.Item), GClass2348.Localized("A LOT"));
		}
		else if (base.Item.StackObjectsCount != 1)
		{
			SetItemValue(EItemValueFormat.OneValue, display: true, GetStackColor(base.Item), base.Item.StackObjectsCount);
		}
	}

	public bool method_30(Item item, out Weapon weapon)
	{
		if (!(item is Weapon weapon2))
		{
			if (!(item is MagazineItemClass item2))
			{
				if (!(item is AmmoItemClass ammoItemClass))
				{
					weapon = null;
					return false;
				}
				ItemAddress currentAddress = ammoItemClass.CurrentAddress;
				weapon = ((currentAddress != null) ? GClass3380.GetRootItem(currentAddress) : null) as Weapon;
				return weapon != null;
			}
			weapon = GClass3380.GetRootItem(item2) as Weapon;
			return weapon != null;
		}
		weapon = weapon2;
		return true;
	}

	public bool method_31(MagazineItemClass magazine, GInterface415 malfunctionController)
	{
		if (!(GClass3380.GetRootItem(magazine) is Weapon weapon))
		{
			return false;
		}
		if (weapon.MalfState.State == Weapon.EMalfunctionState.Feed)
		{
			return malfunctionController.HasKnownMalfunction(weapon);
		}
		return false;
	}

	public bool method_32(AmmoItemClass ammo, GInterface415 malfunctionController)
	{
		ItemAddress currentAddress = ammo.CurrentAddress;
		if (!(((currentAddress != null) ? GClass3380.GetRootItem(currentAddress) : null) is Weapon weapon))
		{
			return false;
		}
		if (weapon.MalfState.State != Weapon.EMalfunctionState.None)
		{
			return malfunctionController.HasKnownMalfunction(weapon);
		}
		return false;
	}

	public bool method_33(Item item)
	{
		if (!(ItemController is GInterface415 gInterface))
		{
			return false;
		}
		if (!(item is Weapon weapon))
		{
			if (!(item is MagazineItemClass magazine))
			{
				if (!(item is AmmoItemClass ammo))
				{
					return false;
				}
				return method_32(ammo, gInterface);
			}
			return method_31(magazine, gInterface);
		}
		return gInterface.HasKnownMalfunction(weapon);
	}

	public virtual void SetItemValue(EItemValueFormat format, bool display, string color, object arg1, [CanBeNull] object arg2 = null, [CanBeNull] string color2 = null)
	{
		if (base.Item is Weapon && arg2 != null && (int)arg2 == 0)
		{
			UpdateItemValue(string.Empty);
			return;
		}
		if (!display)
		{
			arg1 = "?";
			arg2 = "?";
		}
		else if (ItemController is Player.PlayerInventoryController playerInventoryController)
		{
			MagazineItemClass currentMagazine = base.Item.GetCurrentMagazine();
			if (currentMagazine != null && !base.Item.UnlimitedCount)
			{
				Profile profile = playerInventoryController.Profile;
				int skill = Mathf.Max(profile.MagDrillsMastering, profile.CheckedMagazineSkillLevel(currentMagazine.Id), currentMagazine.CheckOverride);
				UpdateItemValue(currentMagazine.GetAmmoCountByLevel((int)arg1, (int)arg2, skill, color, playerInventoryController.CheckedMagazine(currentMagazine), GClass3373.IsItemEquipped(playerInventoryController, base.Item), ValueFormat));
				return;
			}
		}
		switch (format)
		{
		default:
			throw new ArgumentOutOfRangeException("format", format, null);
		case EItemValueFormat.OneValue:
			UpdateItemValue($"<color={color}>{arg1}</color>");
			break;
		case EItemValueFormat.TwoValues:
			UpdateItemValue($"<color={color}>{arg1}</color><color={color2 ?? color}>/{arg2}</color>");
			break;
		case EItemValueFormat.Other:
			UpdateItemValue(string.Format(ValueFormat, arg1, arg2, color));
			break;
		}
	}

	public void UpdateItemValue(string newValue)
	{
		CurrentItemValue = newValue;
	}

	public void method_34()
	{
		if (bool_7)
		{
			GClass3843 multiLineInfo = new GClass3843(base.Item as CompoundItem, onlyMoveAble: false);
			ItemUiContext.Instance.Tooltip.Close();
			ItemUiContext.Instance.MultiLineTooltip.Show(multiLineInfo);
		}
	}

	public void method_35()
	{
		if (bool_7)
		{
			ItemUiContext.Instance.MultiLineTooltip.Close();
		}
	}

	public bool method_36()
	{
		if (Examined && base.Item.TryGetItemComponent<ArmorHolderComponent>(out var component))
		{
			return component.ArmorPlates.Any();
		}
		return false;
	}

	public override void UpdateInfoVisibility(bool isVisible)
	{
		base.UpdateInfoVisibility(isVisible);
		_infoPanel.gameObject.SetActive(isVisible);
	}

	public virtual void SetItemBinding(EBoundItem? slotName)
	{
		if (!(_bindPanel == null))
		{
			if (slotName.HasValue)
			{
				_bindPanel.Show(slotName.Value);
			}
			else
			{
				_bindPanel.Hide();
			}
		}
	}

	public override void ValidateWishlistView()
	{
		if (_wishlistView != null)
		{
			_wishlistView.Show(gclass2067_0, base.Item.TemplateId, Examined);
		}
	}

	public void AddCustomObjectToInfoPanel(RectTransform customObject, Vector2 anchoredPosition)
	{
		customObject.SetParent(_infoPanel, worldPositionStays: false);
		customObject.anchoredPosition = anchoredPosition;
		CompositeDisposable.AddDisposable(delegate
		{
			UnityEngine.Object.Destroy(customObject.gameObject);
		});
	}

	public override void Kill()
	{
		method_14();
		if (ItemUiContext != null)
		{
			if (ItemUiContext.Tooltip != null)
			{
				ItemUiContext.Tooltip.Close();
			}
			if (ItemUiContext.MultiLineTooltip != null)
			{
				ItemUiContext.MultiLineTooltip.Close();
			}
		}
		gclass3455_0 = null;
		base.Kill();
	}
}
