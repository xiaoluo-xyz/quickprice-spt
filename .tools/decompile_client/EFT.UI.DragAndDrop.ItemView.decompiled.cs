using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Comfort.Common;
using Diz.Binding;
using Diz.LanguageExtensions;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFT.UI.DragAndDrop;

public abstract class ItemView : AssetPoolObject, IDragHandler, IEventSystemHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, GInterface489, IDisposable
{
	[Serializable]
	[CompilerGenerated]
	public class Class3352
	{
		public static readonly Class3352 class3352_0 = new Class3352();

		public static Func<bool, bool, bool> func_0;

		public static Func<bool, bool, bool, bool, bool> func_1;

		public static Func<bool, bool, bool, bool, bool, bool, bool, bool> func_2;

		public bool method_0(bool dragged, bool removed)
		{
			if (!dragged)
			{
				return !removed;
			}
			return false;
		}

		public bool method_1(bool added, bool removed, bool examined, bool loaded)
		{
			return added || removed || examined || loaded;
		}

		public bool method_2(bool filtered, bool dragged, bool added, bool drained, bool removed, bool searched, bool loadAmmo)
		{
			if (!filtered && !dragged && !added && !drained && !removed && !searched)
			{
				return !loadAmmo;
			}
			return false;
		}
	}

	[CompilerGenerated]
	public class Class3353
	{
		public float minAlpha;

		public float maxAlpha;

		public float method_0(bool dragDisabled, Error removeError)
		{
			if (!dragDisabled && (removeError == null || removeError is Slot.GClass1576))
			{
				return maxAlpha;
			}
			return minAlpha;
		}
	}

	[CompilerGenerated]
	public class Class3354
	{
		public ItemView itemView_0;

		public GInterface436 favoriteContext;

		public void method_0()
		{
			itemView_0.ItemContext.OnUpdate -= itemView_0.UpdateInfo;
			itemView_0.ItemContext.OnDragStateChange -= itemView_0.method_3;
			itemView_0.ItemContext.OnCheckAccept -= itemView_0.CheckAcceptHandler;
		}

		public void method_1(bool drained)
		{
			itemView_0._drainLoader.SetActive(drained);
		}

		public void method_2(float transparency)
		{
			itemView_0.CanvasGroup.alpha = transparency;
		}

		public void method_3(bool isInteractive)
		{
			itemView_0.CanvasGroup.blocksRaycasts = isInteractive;
		}

		public void method_4(Item item1)
		{
			itemView_0.method_2(item1, favoriteContext);
		}
	}

	[CompilerGenerated]
	public class Class3355
	{
		public RepairKitsItemClass repairKit;

		public bool method_0(RepairableComponent subRepairable)
		{
			return repairKit.CanRepair(subRepairable.Item);
		}
	}

	[CompilerGenerated]
	public class Class3356
	{
		public Item item;

		public GClass3391 address;

		public bool method_0(KeyValuePair<EBoundItem, Item> pair)
		{
			return pair.Value == item;
		}

		public bool method_1(KeyValuePair<EBoundItem, Slot> pair)
		{
			return pair.Value == address.Slot;
		}
	}

	protected const int HIGHLIGHT_ALPHA = 50;

	protected const int BACKGROUND_ALPHA = 77;

	private const float float_0 = 0.3f;

	[SerializeField]
	protected ItemViewAnimation Animator;

	[SerializeField]
	protected Image MainImage;

	[SerializeField]
	protected Image ColorPanel;

	[SerializeField]
	protected Image _border;

	[SerializeField]
	private GameObject _drainLoader;

	[SerializeField]
	private GameObject _iconLoader;

	[SerializeField]
	protected CanvasGroup CanvasGroup;

	[SerializeField]
	protected GameObject InsuredItemBorder;

	[SerializeField]
	private float _mainImageAlpha;

	[SerializeField]
	private Image _favoriteImage;

	[SerializeField]
	private Image _unFavoriteImage;

	[SerializeField]
	protected ItemViewBottomPanel BottomPanel;

	public static readonly Color DefaultSelectedColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 77);

	protected Color SelectedColor = DefaultSelectedColor;

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_0 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_1 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_2 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_3 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_4 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_5 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_6 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_7 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_8 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_9 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<bool> gclass1643_10 = new global::BindableStateClass<bool>();

	[CompilerGenerated]
	private readonly global::BindableStateClass<Error> gclass1643_11 = new global::BindableStateClass<Error>();

	public IContainer Container;

	private bool bool_3 = true;

	[NonSerialized]
	public bool IsConflicting;

	protected ItemUiContext ItemUiContext;

	private IBindable<bool> ibindable_0;

	private IBindable<bool> ibindable_1;

	protected IBindable<bool> IsInteractive;

	private bool bool_4;

	protected bool HighlightedGlobally;

	protected bool IsKilled;

	protected ItemRotation _itemRotation;

	protected Color OriginalBackgroundColor;

	protected Color BackgroundColor;

	protected TraderControllerClass ItemController;

	protected IItemOwner ItemOwner;

	protected bool IsTeammateDogtag;

	private float float_1;

	private RectTransform rectTransform_0;

	protected readonly CompositeDisposableClass CompositeDisposable = new CompositeDisposableClass();

	private GClass929 gclass929_0;

	private Vector2? nullable_0;

	private Action action_0;

	private PointerEventData pointerEventData_0;

	private Color? nullable_1;

	[CompilerGenerated]
	private ItemContextAbstractClass itemContextAbstractClass;

	[CompilerGenerated]
	private Item item_0;

	[CompilerGenerated]
	private DraggedItemView draggedItemView_0;

	public QuestItemViewPanel QuestItemViewPanel_0 => BottomPanel.QuestsItemViewPanel;

	public GameObject InsuredIcon => BottomPanel.InsuredIcon;

	public Image RepairBuffIcon => BottomPanel.RepairBuffIcon;

	public HoverTrigger HoverTrigger => BottomPanel.HoverTrigger;

	public global::BindableStateClass<bool> IsBeingDragged
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_0;
		}
	}

	public global::BindableStateClass<bool> IsBeingRemoved
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_1;
		}
	}

	public global::BindableStateClass<bool> IsBeingAdded
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_2;
		}
	}

	public global::BindableStateClass<bool> IsBeingDrained
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_3;
		}
	}

	public global::BindableStateClass<bool> IsFilteredOut
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_4;
		}
	}

	public global::BindableStateClass<bool> IsDisabledDrag
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_5;
		}
	}

	public global::BindableStateClass<bool> IsBeingSearched
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_6;
		}
	}

	public global::BindableStateClass<bool> IsBeingExamined
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_7;
		}
	}

	public global::BindableStateClass<bool> IsBeingLoadedMagazine
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_8;
		}
	}

	public global::BindableStateClass<bool> IsBeingUnloadedMagazine
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_9;
		}
	}

	public global::BindableStateClass<bool> IsBeingLoadedAmmo
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_10;
		}
	}

	public global::BindableStateClass<Error> RemoveError
	{
		[CompilerGenerated]
		get
		{
			return gclass1643_11;
		}
	}

	public virtual bool PropagateClicks => false;

	GameObject GInterface489.GameObject => base.gameObject;

	Transform GInterface489.Transform => base.transform;

	public ItemContextAbstractClass ItemContext
	{
		[CompilerGenerated]
		get
		{
			return itemContextAbstractClass;
		}
		[CompilerGenerated]
		set
		{
			itemContextAbstractClass = value;
		}
	}

	public bool BeingDragged => IsBeingDragged.Value;

	public Color BorderColor
	{
		set
		{
			if (!(_border == null))
			{
				if (!nullable_1.HasValue)
				{
					nullable_1 = _border.color;
				}
				_border.color = value;
			}
		}
	}

	public Vector2? IconScale
	{
		get
		{
			return nullable_0;
		}
		set
		{
			nullable_0 = value;
			UpdateScale();
		}
	}

	public Item Item
	{
		[CompilerGenerated]
		get
		{
			return item_0;
		}
		[CompilerGenerated]
		set
		{
			item_0 = value;
		}
	}

	public bool IsSearched
	{
		get
		{
			return bool_3;
		}
		set
		{
			bool_3 = value;
			ItemContext.Searched = bool_3;
		}
	}

	public abstract bool IsInteractable { get; }

	public virtual ItemRotation ItemRotation
	{
		get
		{
			return _itemRotation;
		}
		set
		{
			_itemRotation = value;
			MainImage.transform.rotation = ((value == ItemRotation.Horizontal) ? ItemViewFactory.HorizontalRotation : ItemViewFactory.VerticalRotation);
			if (RectTransform.anchorMin == RectTransform.anchorMax)
			{
				RectTransform.sizeDelta = ItemViewFactory.GetCellPixelSize(Item.CalculateRotatedSize(_itemRotation));
			}
		}
	}

	public RectTransform RectTransform
	{
		get
		{
			if (rectTransform_0 == null)
			{
				rectTransform_0 = (RectTransform)base.transform;
			}
			return rectTransform_0;
		}
	}

	public virtual IBindable<float> Transparency
	{
		get
		{
			float minAlpha = 0.6f;
			float maxAlpha = 1f;
			if (Item is ArmorPlateItemClass)
			{
				minAlpha = 1f;
			}
			return GClass1641.Combine(IsDisabledDrag, RemoveError, (bool dragDisabled, Error removeError) => (!dragDisabled && (removeError == null || removeError is Slot.GClass1576)) ? maxAlpha : minAlpha);
		}
	}

	public virtual bool Examined
	{
		get
		{
			if (ItemController != null)
			{
				return ItemController.Examined(Item);
			}
			return true;
		}
	}

	[CanBeNull]
	public DraggedItemView DraggedItemView
	{
		[CompilerGenerated]
		get
		{
			return draggedItemView_0;
		}
		[CompilerGenerated]
		set
		{
			draggedItemView_0 = value;
		}
	}

	public global::ItemInfoInteractionsAbstractClass<EItemInfoButton> NewContextInteractions => ItemUiContext.GetItemContextInteractions(ItemContext, null);

	public void NewItemView(Item item, ItemContextAbstractClass sourceItemContext, ItemRotation rotation, TraderControllerClass itemController, IContainer container, IItemOwner itemOwner, [CanBeNull] ItemUiContext itemUiContext)
	{
		ItemController = itemController;
		ItemOwner = itemOwner;
		ItemUiContext = itemUiContext;
		Item = item;
		Container = container;
		ItemContext = CreateNewItemContext(sourceItemContext);
		RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		IsKilled = false;
		ItemRotation = rotation;
		IsBeingExamined.Value = false;
		IsBeingLoadedMagazine.Value = false;
		IsBeingUnloadedMagazine.Value = false;
		IsBeingLoadedAmmo.Value = false;
		IsBeingSearched.Value = false;
		IsBeingDragged.Value = false;
		IsBeingDrained.Value = false;
		IsFilteredOut.Value = false;
		IsBeingRemoved.Value = false;
		IsBeingAdded.Value = false;
		HighlightedGlobally = false;
		bool_4 = false;
		IconScale = null;
		ItemContext.OnUpdate += UpdateInfo;
		ItemContext.OnDragStateChange += method_3;
		ItemContext.OnCheckAccept += CheckAcceptHandler;
		CompositeDisposable.AddDisposable(delegate
		{
			ItemContext.OnUpdate -= UpdateInfo;
			ItemContext.OnDragStateChange -= method_3;
			ItemContext.OnCheckAccept -= CheckAcceptHandler;
		});
		ibindable_1 = GClass1641.Combine(IsBeingDragged, IsBeingRemoved, (bool dragged, bool removed) => !dragged && !removed);
		ibindable_0 = GClass1641.Combine(IsBeingAdded, IsBeingRemoved, IsBeingExamined, IsBeingLoadedAmmo, (bool added, bool removed, bool examined, bool loaded) => added || removed || examined || loaded);
		InitInteractiveBinding();
		CompositeDisposable.BindState(IsBeingDragged, Animator.SetDragState);
		CompositeDisposable.BindState(IsBeingDrained, delegate(bool drained)
		{
			_drainLoader.SetActive(drained);
		});
		CompositeDisposable.BindState(IsBeingSearched, Animator.SetSearchedState);
		CompositeDisposable.BindState(IsBeingExamined, OnBeingExaminedChanged);
		CompositeDisposable.BindState(ibindable_1, UpdateInfoVisibility);
		CompositeDisposable.BindState(Transparency, delegate(float transparency)
		{
			CanvasGroup.alpha = transparency;
		});
		CompositeDisposable.BindState(IsInteractive, delegate(bool isInteractive)
		{
			CanvasGroup.blocksRaycasts = isInteractive;
		});
		CompositeDisposable.BindState(ibindable_0, Animator.SetBlinkingState);
		CompositeDisposable.BindState(IsFilteredOut, method_1);
		if (_favoriteImage != null)
		{
			_favoriteImage.gameObject.SetActive(value: false);
		}
		if (_unFavoriteImage != null)
		{
			_unFavoriteImage.gameObject.SetActive(value: false);
		}
		ItemContextAbstractClass itemContext = ItemContext;
		GInterface436 favoriteContext = itemContext as GInterface436;
		if (favoriteContext != null)
		{
			CompositeDisposable.SubscribeEvent(favoriteContext.FavoriteItems.OnFavoriteItemsChanged, delegate(Item item2)
			{
				method_2(item2, favoriteContext);
			});
			method_2(Item, favoriteContext);
		}
		_ = ItemUiContext == null;
	}

	public void method_1(bool filterState)
	{
		if (BottomPanel.AlphaFakeObject != null)
		{
			BottomPanel.AlphaFakeObject.SetActive(filterState);
		}
	}

	public void method_2(Item item, GInterface436 favoriteComponent)
	{
		if (!(_favoriteImage == null) && !(_unFavoriteImage == null) && favoriteComponent != null && Item == item && favoriteComponent.ShouldSetFavoriteIcon(item, out var isFavorite))
		{
			_favoriteImage.gameObject.SetActive(isFavorite);
			_unFavoriteImage.gameObject.SetActive(!isFavorite);
		}
	}

	public bool CanInteract(ItemContextClass dragItemContext)
	{
		Item item = Item;
		Item item2 = dragItemContext.Item;
		RepairKitsItemClass repairKit = item2 as RepairKitsItemClass;
		if (repairKit == null)
		{
			return false;
		}
		if (!GClass3380.GetItemComponentsInChildren<RepairableComponent>(item).Any((RepairableComponent subRepairable) => repairKit.CanRepair(subRepairable.Item)))
		{
			return false;
		}
		if (item.CurrentAddress is GClass3391 { Slot: GClass3125 { Locked: not false } })
		{
			return false;
		}
		GClass3462 itemContext = new GClass3462(ItemContext.CreateChild(item), repairKit);
		if (ItemUiContext.GetItemContextInteractions(itemContext, null).IsInteractionAvailable(EItemInfoButton.Repair).Succeed)
		{
			return true;
		}
		return false;
	}

	public void method_3(bool isBeingDragged)
	{
		IsBeingDragged.Value = isBeingDragged;
		if (!isBeingDragged)
		{
			Container.DragCancelled();
		}
	}

	public virtual void CheckAcceptHandler(ItemContextClass dragItemContext)
	{
		if (Container == null || Container.Equals(null))
		{
			Container = null;
			CompositeDisposable.Dispose();
		}
	}

	public virtual ItemContextAbstractClass CreateNewItemContext(ItemContextAbstractClass sourceContext)
	{
		return sourceContext.CreateChild(Item);
	}

	public virtual void InitInteractiveBinding()
	{
		IsInteractive = GClass1641.Combine(IsFilteredOut, IsBeingDragged, IsBeingAdded, IsBeingDrained, IsBeingRemoved, IsBeingSearched, IsBeingLoadedAmmo, (bool filtered, bool dragged, bool added, bool drained, bool removed, bool searched, bool loadAmmo) => !filtered && !dragged && !added && !drained && !removed && !searched && !loadAmmo);
	}

	public void Rotate()
	{
		ItemRotation = (ItemRotation)((int)(_itemRotation + 1) % 2);
	}

	public virtual void OnBeingExaminedChanged(bool isBeingExamined)
	{
	}

	public virtual void UpdateRemoveError(bool ignoreMalfunctions = true)
	{
		if (Item.CurrentAddress == null)
		{
			RemoveError.Value = null;
			return;
		}
		if (ItemContext.Error != null)
		{
			RemoveError.Value = ItemContext.Error;
			return;
		}
		Error error = InteractionsHandlerClass.Remove(Item, ItemController, simulate: true).Error;
		if (error is InteractionsHandlerClass.GClass1591 gClass)
		{
			if (!ignoreMalfunctions && ItemController is GInterface415 gInterface && !gInterface.HasKnownMalfunction(gClass.Weapon))
			{
				gInterface.ExamineMalfunction(gClass.Weapon);
			}
			else
			{
				error = null;
			}
		}
		RemoveError.Value = error;
	}

	public virtual void UpdateInfoVisibility(bool isVisible)
	{
		if (_border != null)
		{
			_border.gameObject.SetActive(isVisible);
		}
		ColorPanel.gameObject.SetActive(isVisible);
	}

	public void Init()
	{
		UpdateStaticInfo();
		UpdateInfo();
		method_5();
		method_6();
		SetQuestItemViewPanel();
		RegisterItemView();
		base.gameObject.SetActive(value: true);
	}

	public virtual void RegisterItemView()
	{
		if (!(ItemUiContext == null) && ItemContext.ViewType != EItemViewType.Empty)
		{
			ItemUiContext.RegisterView(ItemContext);
			CompositeDisposable.AddDisposable(delegate
			{
				ItemUiContext.UnregisterView(ItemContext);
			});
		}
	}

	public virtual void RefreshIcon()
	{
		if (action_0 != null)
		{
			action_0();
			action_0 = null;
		}
		gclass929_0 = ItemViewFactory.LoadItemIcon(Item);
		action_0 = gclass929_0.Changed.Bind(method_4);
	}

	public virtual void UpdateStaticInfo()
	{
		RefreshIcon();
		BackgroundColor = GClass1409.ToColor(Item.BackgroundColor);
		BackgroundColor.a = 0.3019608f;
		UpdateColor();
		OriginalBackgroundColor = BackgroundColor;
		Animator.Init(OriginalBackgroundColor);
	}

	public void SetQuestItemViewPanel()
	{
		if (QuestItemViewPanel_0 == null)
		{
			return;
		}
		if (ItemController is InventoryController { Profile: Profile profile })
		{
			if (ItemContext.HasViewComponent(EItemViewComponent.QuestItem))
			{
				QuestItemViewPanel_0.Show(profile, Item, (ItemUiContext != null) ? ItemUiContext.Tooltip : null);
			}
			else
			{
				QuestItemViewPanel_0.HideGameObject();
			}
		}
		else
		{
			QuestItemViewPanel_0.HideGameObject();
		}
	}

	public void method_4()
	{
		if (!(base.gameObject == null) && !(_iconLoader == null))
		{
			_iconLoader.SetActive(gclass929_0.Sprite == null);
			MainImage.gameObject.SetActive(gclass929_0.Sprite != null && IsSearched);
			MainImage.sprite = gclass929_0.Sprite;
			MainImage.SetNativeSize();
			LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform_0);
			UpdateScale();
		}
	}

	public virtual void UpdateInfo()
	{
	}

	public void method_5()
	{
		if (ItemController != null)
		{
			IsTeammateDogtag = !ItemController.CanMoveDogtag(Item);
			IsDisabledDrag.Value = IsTeammateDogtag;
		}
	}

	public void method_6()
	{
		if (ItemController != null)
		{
			IsDisabledDrag.Value = !ItemController.CanMoveCompoundItem(Item);
		}
	}

	public virtual void OnPointerDown(PointerEventData eventData)
	{
		if (method_7(eventData))
		{
			UpdateRemoveError(ignoreMalfunctions: false);
		}
	}

	public virtual void OnBeginDrag([NotNull] PointerEventData eventData)
	{
		if (method_7(eventData))
		{
			Singleton<GUISounds>.Instance.PlayItemSound(Item.ItemSound, EInventorySoundType.pickup);
			IsBeingDragged.Value = true;
			DraggedItemView = DraggedItemView.Create(ItemContext, ItemRotation, Examined ? Color.white : new Color(0f, 0f, 0f, 0.85f), ItemUiContext);
			((RectTransform)DraggedItemView.transform).position = base.transform.position;
			Container.DragStarted();
		}
	}

	public bool method_7(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && Container != null && Container.CanDrag(ItemContext) && IsSearched && !IsTeammateDogtag && RemoveError.Value == null)
		{
			return DraggedItemView == null;
		}
		return false;
	}

	public virtual void OnDrag([NotNull] PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && !(DraggedItemView == null))
		{
			pointerEventData_0 = eventData;
			DraggedItemView.OnDrag(eventData);
			if (IsInteractable)
			{
				ItemContextAbstractClass itemUnderCursor = eventData.pointerEnter?.GetComponentInParent<ItemView>()?.ItemContext;
				IContainer containerUnderCursor = eventData.pointerEnter?.GetComponentInParent<IContainer>();
				DraggedItemView.UpdateTargetUnderCursor(containerUnderCursor, itemUnderCursor);
			}
		}
	}

	public virtual void OnEndDrag([NotNull] PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			ItemContextAbstractClass itemContextAbstractClass = eventData.pointerEnter?.GetComponentInParent<ItemView>()?.ItemContext;
			if (itemContextAbstractClass != null)
			{
				itemContextAbstractClass.InventoryError = null;
			}
			pointerEventData_0 = null;
			if (!(DraggedItemView == null))
			{
				ItemContextClass itemContext = DraggedItemView.ItemContext;
				DraggedItemView.Kill();
				UnityEngine.Object.DestroyImmediate(DraggedItemView.gameObject);
				DraggedItemView = null;
				method_8(itemContext, eventData);
			}
		}
	}

	public void OnApplicationFocus(bool hasFocus)
	{
		if (!hasFocus && pointerEventData_0 != null)
		{
			OnEndDrag(pointerEventData_0);
		}
	}

	public void method_8(ItemContextClass dragItemContext, PointerEventData eventData)
	{
		ItemContextAbstractClass itemContextAbstractClass = ((eventData.pointerEnter == null) ? null : eventData.pointerEnter.GetComponentInParent<ItemView>()?.ItemContext);
		IContainer container = ((eventData.pointerEnter == null) ? null : eventData.pointerEnter.GetComponentInParent<IContainer>());
		dragItemContext.DragCancelled();
		bool flag = container == Container;
		if (container != null)
		{
			bool flag2 = true;
			if (Container is SlotView)
			{
				flag2 = !flag;
			}
			else if (Container is QuickSlotView)
			{
				flag2 = !flag && container is QuickSlotView;
			}
			if (flag2 &= container.CanAccept(dragItemContext, itemContextAbstractClass, out var _))
			{
				container.AcceptItem(dragItemContext, itemContextAbstractClass).HandleExceptions();
				return;
			}
		}
		if (itemContextAbstractClass != null && dragItemContext.Item is RepairKitsItemClass repairKit)
		{
			GClass3462 itemContext = new GClass3462(itemContextAbstractClass, repairKit);
			ItemUiContext.GetItemContextInteractions(itemContext, null).ExecuteInteraction(EItemInfoButton.Repair);
		}
	}

	public virtual void Update()
	{
		if (pointerEventData_0 != null && !Input.GetMouseButton(0))
		{
			OnEndDrag(pointerEventData_0);
		}
		if (IsSearched)
		{
			if (IsBeingDrained.Value)
			{
				UpdateInfo();
			}
			if (Math.Abs(float_1 - _mainImageAlpha) > 0.01f)
			{
				float_1 = _mainImageAlpha;
				Color color = MainImage.color;
				color.a = _mainImageAlpha;
				MainImage.color = color;
			}
		}
	}

	public virtual void OnPointerEnter([NotNull] PointerEventData eventData)
	{
		ItemUiContext.RegisterCurrentItemContext(ItemContext);
		Highlight(highlight: true);
	}

	public virtual void OnPointerExit([NotNull] PointerEventData eventData)
	{
		ItemUiContext.UnregisterCurrentItemContext(ItemContext);
		Highlight(highlight: false);
	}

	public void Highlight(bool highlight)
	{
		bool_4 = highlight;
		UpdateColor();
	}

	public void UpdateColor()
	{
		ColorPanel.color = ((bool_4 || HighlightedGlobally) ? SelectedColor : BackgroundColor);
	}

	public void SetBeingExaminedState(GEventArgs6 activeEvent)
	{
		bool flag = activeEvent.Status == CommandStatus.Begin;
		IsBeingExamined.Value = flag;
		UpdateRemoveError();
		if (flag)
		{
			Animator.StartExamination(activeEvent.ExamineTime);
		}
		else
		{
			Animator.StopExamination();
		}
	}

	public void SetLoadMagazineStatus(GEventArgs7 activeEvent)
	{
		bool flag = activeEvent.Status == CommandStatus.Begin;
		IsBeingLoadedMagazine.Value = flag;
		if (flag)
		{
			Animator.StartLoading(activeEvent.LoadTime, activeEvent.LoadCount);
		}
		else
		{
			Animator.StopLoading();
		}
	}

	public void SetUnloadMagazineStatus(GEventArgs8 activeEvent)
	{
		bool flag = activeEvent.Status == CommandStatus.Begin;
		IsBeingUnloadedMagazine.Value = flag;
		if (flag)
		{
			Animator.StartLoading(activeEvent.UnloadTime, activeEvent.UnloadCount, activeEvent.StartCount);
		}
		else
		{
			Animator.StopLoading();
		}
	}

	public void SetInventoryCheckMagazineStatus(float time, bool value)
	{
		IsBeingLoadedMagazine.Value = value;
		if (value)
		{
			Animator.StartLoading(time, 1);
		}
		else
		{
			Animator.StopLoading();
		}
	}

	public void SetLoadAmmoStatus(GEventArgs1 activeEvent)
	{
		IsBeingLoadedAmmo.Value = activeEvent.Status == CommandStatus.Begin;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		bool flag = eventData.button == PointerEventData.InputButton.Left && eventData.clickCount % 2 == 0;
		OnClick(eventData.button, eventData.pressPosition, flag);
		if (PropagateClicks && !flag)
		{
			ExecuteEvents.ExecuteHierarchy(base.transform.parent.gameObject, eventData, ExecuteEvents.pointerClickHandler);
		}
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
	{
		//ILSpy generated this explicit interface implementation from .override directive in OnPointerClick
		this.OnPointerClick(eventData);
	}

	public virtual void OnClick(PointerEventData.InputButton button, Vector2 position, bool doubleClick)
	{
		if (ItemUiContext == null || !IsSearched)
		{
			return;
		}
		bool flag = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		bool flag2 = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
		bool flag3 = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		global::ItemInfoInteractionsAbstractClass<EItemInfoButton> newContextInteractions = NewContextInteractions;
		switch (button)
		{
		case PointerEventData.InputButton.Left:
		{
			if (!(flag || flag3) && doubleClick)
			{
				bool flag4 = ItemController is Player.PlayerInventoryController;
				bool flag5 = Singleton<SharedGameSettingsClass>.Instance.Game.Settings.ItemQuickUseMode.Value switch
				{
					GClass1085.EItemQuickUseMode.Disabled => false, 
					GClass1085.EItemQuickUseMode.InRaidOnly => flag4, 
					GClass1085.EItemQuickUseMode.InRaidAndInLobby => true, 
					_ => throw new ArgumentOutOfRangeException(), 
				};
				if ((Item is FoodDrinkItemClass || Item is MedsItemClass) && flag5)
				{
					if (!newContextInteractions.ExecuteInteraction(EItemInfoButton.Use))
					{
						newContextInteractions.ExecuteInteraction(EItemInfoButton.UseAll);
					}
					break;
				}
				if (newContextInteractions.ExecuteInteraction(Item.IsContainer ? EItemInfoButton.Open : EItemInfoButton.Inspect))
				{
					break;
				}
			}
			SimpleTooltip tooltip = ItemUiContext.Tooltip;
			if (flag || flag3)
			{
				GStruct153 gStruct = (flag ? ItemUiContext.QuickFindAppropriatePlace(ItemContext, ItemController) : ItemUiContext.QuickMoveToSortingTable(ItemContext, ItemController));
				if (gStruct.Failed || !ItemController.CanExecute(gStruct.Value))
				{
					break;
				}
				if (gStruct.Value is GInterface427 { ItemsDestroyRequired: not false } gInterface)
				{
					NotificationManagerClass.DisplayWarningNotification(new GClass1583(Item, gInterface.ItemsToDestroy).GetLocalizedDescription());
					break;
				}
				string itemSound = Item.ItemSound;
				ItemController.RunNetworkTransaction(gStruct.Value);
				if (tooltip != null)
				{
					tooltip.Close();
				}
				Singleton<GUISounds>.Instance.PlayItemSound(itemSound, EInventorySoundType.pickup);
			}
			else if (flag2)
			{
				newContextInteractions.ExecuteInteraction(EItemInfoButton.Equip);
				if (tooltip != null)
				{
					tooltip.Close();
				}
			}
			else if (IsBeingLoadedMagazine.Value || IsBeingUnloadedMagazine.Value)
			{
				ItemController.StopProcesses();
			}
			break;
		}
		case PointerEventData.InputButton.Right:
			ShowContextMenu(position);
			break;
		case PointerEventData.InputButton.Middle:
			if (!ExecuteMiddleClick())
			{
				newContextInteractions.ExecuteInteraction(EItemInfoButton.CheckMagazine);
			}
			break;
		}
	}

	public bool ExecuteMiddleClick()
	{
		if (Item.CurrentAddress == null)
		{
			return false;
		}
		global::ItemInfoInteractionsAbstractClass<EItemInfoButton> newContextInteractions = NewContextInteractions;
		if (!newContextInteractions.ExecuteInteraction(EItemInfoButton.Examine) && !newContextInteractions.ExecuteInteraction(EItemInfoButton.Fold) && !newContextInteractions.ExecuteInteraction(EItemInfoButton.Unfold) && !newContextInteractions.ExecuteInteraction(EItemInfoButton.TurnOn))
		{
			return newContextInteractions.ExecuteInteraction(EItemInfoButton.TurnOff);
		}
		return true;
	}

	public void ShowContextMenu(Vector2 position)
	{
		ItemUiContext.ShowContextMenu(ItemContext, position);
	}

	public virtual void UpdateScale()
	{
		if (MainImage.gameObject.activeSelf)
		{
			if (nullable_0.HasValue)
			{
				Vector2 sizeDelta = MainImage.rectTransform.sizeDelta;
				float num = nullable_0.Value.x / sizeDelta.x;
				float num2 = nullable_0.Value.y / sizeDelta.y;
				float num3 = ((!(num > 1f) || num2 <= 1f) ? Mathf.Min(num, num2) : 1f);
				MainImage.rectTransform.localScale = new Vector3(num3, num3, 1f);
			}
			else
			{
				MainImage.rectTransform.localScale = Vector3.one;
			}
		}
	}

	public virtual void ValidateWishlistView()
	{
	}

	public static EBoundItem? GetBindingForItem([CanBeNull] TraderControllerClass itemController, Item item)
	{
		if (!(itemController is InventoryController inventoryController))
		{
			return null;
		}
		inventoryController.FastAccess.BoundItems.FirstOrDefault((KeyValuePair<EBoundItem, Item> pair) => pair.Value == item).Deconstruct(out var key, out var value);
		EBoundItem value2 = key;
		if (value != null)
		{
			return value2;
		}
		GClass3391 address = item.CurrentAddress as GClass3391;
		if (address == null)
		{
			return null;
		}
		inventoryController.FastAccess.BoundCells.FirstOrDefault((KeyValuePair<EBoundItem, Slot> pair) => pair.Value == address.Slot).Deconstruct(out key, out var value3);
		EBoundItem value4 = key;
		if (value3 != null)
		{
			return value4;
		}
		return null;
	}

	public virtual void Kill()
	{
		base.gameObject.SetActive(value: false);
		if (nullable_1.HasValue)
		{
			BorderColor = nullable_1.Value;
		}
		CompositeDisposable.Dispose();
		Animator.Stop();
		action_0?.Invoke();
		action_0 = null;
		RemoveError.Value = null;
		if (DraggedItemView != null)
		{
			DraggedItemView.Kill();
			UnityEngine.Object.DestroyImmediate(DraggedItemView.gameObject);
			DraggedItemView = null;
		}
		if (ItemUiContext != null)
		{
			ItemUiContext.UnregisterCurrentItemContext(ItemContext);
			ItemUiContext = null;
		}
		ItemContext?.Dispose();
		ItemContext = null;
		ItemController = null;
		ItemOwner = null;
		Container = null;
		Item = null;
		ReturnToPool();
		IsKilled = true;
	}

	public void Dispose()
	{
		Kill();
	}

	void IDisposable.Dispose()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Dispose
		this.Dispose();
	}

	public ItemView()
	{
	}

	[CompilerGenerated]
	public void method_9()
	{
		ItemUiContext.UnregisterView(ItemContext);
	}
}
