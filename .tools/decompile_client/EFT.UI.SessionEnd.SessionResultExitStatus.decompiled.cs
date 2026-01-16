using System;
using System.Runtime.CompilerServices;
using System.Threading;
using EFT.Hideout;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI.Screens;
using UnityEngine;

namespace EFT.UI.SessionEnd;

public class SessionResultExitStatus : EftScreen<SessionResultExitStatus.GClass3899, SessionResultExitStatus>
{
	public class GClass3899 : CurrentScreenSingletonClass.GClass3861<GClass3899, SessionResultExitStatus>
	{
		[CompilerGenerated]
		private Action action_2;

		[CompilerGenerated]
		private Action action_3;

		public readonly Profile ActiveProfile;

		public readonly ESideType PlayerSide;

		public readonly ExitStatus ExitStatus;

		public readonly TimeSpan RaidTime;

		public readonly bool IsOnline;

		public readonly ISession Session;

		public readonly LastPlayerStateClass LastPlayerState;

		public override EEftScreenType ScreenType => EEftScreenType.ExitStatus;

		public override bool MainEnvironment => false;

		public override bool KeyScreen => true;

		public override EStateSwitcher TaskBarButtonsAvailability => EStateSwitcher.Disabled;

		public override EStateSwitcher MenuChatBarVisibility => EStateSwitcher.Disabled;

		public event Action OnShowNextScreen
		{
			[CompilerGenerated]
			add
			{
				Action action = action_2;
				Action action2;
				do
				{
					action2 = action;
					Action value2 = (Action)Delegate.Combine(action2, value);
					action = Interlocked.CompareExchange(ref action_2, value2, action2);
				}
				while ((object)action != action2);
			}
			[CompilerGenerated]
			remove
			{
				Action action = action_2;
				Action action2;
				do
				{
					action2 = action;
					Action value2 = (Action)Delegate.Remove(action2, value);
					action = Interlocked.CompareExchange(ref action_2, value2, action2);
				}
				while ((object)action != action2);
			}
		}

		public event Action OnGoToMainMenu
		{
			[CompilerGenerated]
			add
			{
				Action action = action_3;
				Action action2;
				do
				{
					action2 = action;
					Action value2 = (Action)Delegate.Combine(action2, value);
					action = Interlocked.CompareExchange(ref action_3, value2, action2);
				}
				while ((object)action != action2);
			}
			[CompilerGenerated]
			remove
			{
				Action action = action_3;
				Action action2;
				do
				{
					action2 = action;
					Action value2 = (Action)Delegate.Remove(action2, value);
					action = Interlocked.CompareExchange(ref action_3, value2, action2);
				}
				while ((object)action != action2);
			}
		}

		public GClass3899(Profile activeProfile, LastPlayerStateClass lastPlayerState, ESideType side, ExitStatus exitStatus, TimeSpan raidTime, ISession session, bool isOnline)
		{
			ActiveProfile = activeProfile;
			LastPlayerState = lastPlayerState;
			PlayerSide = side;
			IsOnline = isOnline;
			ExitStatus = exitStatus;
			RaidTime = raidTime;
			Session = session;
		}

		public void ShowNextScreen()
		{
			action_2?.Invoke();
		}

		public void GoToMainMenu()
		{
			action_3?.Invoke();
		}
	}

	[Serializable]
	[CompilerGenerated]
	public class Class3245
	{
		public static readonly Class3245 class3245_0 = new Class3245();

		public static Func<string> func_0;

		public string method_0()
		{
			return "WatchProfile";
		}
	}

	private const string string_0 = "WatchProfile";

	[SerializeField]
	private DefaultUIButton _nextButton;

	[SerializeField]
	private DefaultUIButton _mainMenuButton;

	[SerializeField]
	private PlayerLevelPanel _levelPanel;

	[SerializeField]
	private PlayerNamePanel _namePanel;

	[SerializeField]
	private PlayerNamePanel _killerNamePanel;

	[SerializeField]
	private CustomTextMeshProUGUI _bodyPartLabel;

	[SerializeField]
	private PlayerModelView _playerModelView;

	[SerializeField]
	private GameObject _survivedPanel;

	[SerializeField]
	private GameObject _leftPanel;

	[SerializeField]
	private GameObject _missingPanel;

	[SerializeField]
	private GameObject _killedPanel;

	[SerializeField]
	private GameObject _runnerPanel;

	[SerializeField]
	private GameObject _warningPanel;

	[SerializeField]
	private CustomTextMeshProUGUI _warningCaption;

	[SerializeField]
	private CustomTextMeshProUGUI _warningDescription;

	[SerializeField]
	private CustomTextMeshProUGUI _raidTime;

	[SerializeField]
	private CustomTextMeshProUGUI _experience;

	[SerializeField]
	private ReportPanel _reportPanel;

	[SerializeField]
	private ComplementaryButton _showProfileButton;

	private ISession iSession;

	private Profile profile_0;

	public void Awake()
	{
		_showProfileButton.SetTooltipMessages(() => "WatchProfile", null);
		_nextButton.OnClick.AddListener(ScreenController.ShowNextScreen);
		_mainMenuButton.OnClick.AddListener(ScreenController.GoToMainMenu);
	}

	public override void Show(GClass3899 controller)
	{
		Show(controller.ActiveProfile, controller.LastPlayerState, controller.PlayerSide, controller.ExitStatus, controller.RaidTime, controller.Session, controller.IsOnline);
	}

	public void Show(Profile activeProfile, LastPlayerStateClass lastPlayerState, ESideType side, ExitStatus exitStatus, TimeSpan raidTime, ISession session, bool isOnline)
	{
		GClass4062.ReleaseBeginSample("SessionResultExitStatus.Show", "Show");
		ShowGameObject();
		iSession = session;
		profile_0 = activeProfile;
		_levelPanel.Set(profile_0.Info.Level, side);
		_namePanel.Set(profile_0);
		GClass788 aggressor = profile_0.EftStats.Aggressor;
		bool flag = aggressor != null && exitStatus == ExitStatus.Killed;
		_killerNamePanel.gameObject.SetActive(flag);
		_bodyPartLabel.gameObject.SetActive(flag);
		if (flag)
		{
			string text = string.Empty;
			if (aggressor.ProfileId != profile_0.Id)
			{
				text = ((!(aggressor.ProfileId == "66f3fad50ec64d74847d049d")) ? GClass945.GetCorrectedNickname(aggressor) : GClass2348.Localized(aggressor.Name));
			}
			if (aggressor.Side == EPlayerSide.Savage && !string.IsNullOrEmpty(aggressor.MainProfileNickname))
			{
				string text2 = aggressor.MainProfileNickname;
				if (aggressor.Category == EMemberCategory.UniqueId)
				{
					Color iconColor = GClass861.Load<ChatSpecialIconSettings>("ChatSpecialIconSettings").GetDataByMemberCategory(aggressor.Category).IconColor;
					text2 = "<color=#" + ColorUtility.ToHtmlStringRGBA(iconColor) + ">" + text2 + "</color>";
				}
				text = text + " (" + text2 + ")";
			}
			string text3 = GClass2348.Localized($"Collider Type {aggressor.ColliderType}").ToLower();
			_bodyPartLabel.text = "(" + text3 + ")";
			_killerNamePanel.Set(aggressor.Side != EPlayerSide.Savage, aggressor.Category, text, 23, aggressor.PrestigeLevel);
			if (iSession.ReportAvailable && !string.IsNullOrEmpty(aggressor.ProfileId) && aggressor.ProfileId != "66f3fad50ec64d74847d049d")
			{
				_reportPanel.Show(iSession, aggressor.AccountId);
			}
			if (!string.IsNullOrEmpty(aggressor.ProfileId) && aggressor.ProfileId != "66f3fad50ec64d74847d049d")
			{
				_showProfileButton.Show(delegate
				{
					method_3();
				});
			}
		}
		_survivedPanel.SetActive(exitStatus == ExitStatus.Survived);
		_leftPanel.SetActive(exitStatus == ExitStatus.Left);
		_missingPanel.SetActive(exitStatus == ExitStatus.MissingInAction);
		_killedPanel.SetActive(exitStatus == ExitStatus.Killed);
		_runnerPanel.SetActive(exitStatus == ExitStatus.Runner);
		_warningPanel.SetActive(exitStatus != ExitStatus.Survived && (isOnline || session.SessionMode == ESessionMode.Pve));
		switch (exitStatus)
		{
		case ExitStatus.Left:
			_warningCaption.text = GClass2348.Localized("Attention! You’ve left the raid and lost everything you brought or found in it.");
			_warningDescription.text = GClass2348.Localized("When you leave the raid you don’t get anything and also receive Left the Action exit status.");
			break;
		case ExitStatus.Runner:
			_warningCaption.text = GClass2348.Localized("Attention! You’ve completed the raid way too early.");
			_warningDescription.text = GClass2348.Localized("Not enough experience gained. Therefore, you have received the Ran Through exit status.");
			break;
		case ExitStatus.Killed:
		case ExitStatus.MissingInAction:
			_warningCaption.text = GClass2348.Localized("Attention! All items, brought by you into the raid or found in it, have been lost.");
			_warningDescription.text = GClass2348.Localized("Insured items can be recovered, if they were not picked up or used by anyone else.");
			break;
		}
		_raidTime.text = $"{raidTime.Hours:D2}:{raidTime.Minutes:D2}:{raidTime.Seconds:D2}";
		_experience.text = GClass3854.ToThousandsString(activeProfile.EftStats.TotalSessionExperience);
		if (lastPlayerState != null)
		{
			_playerModelView.Show(lastPlayerState).HandleExceptions();
		}
		else
		{
			_playerModelView.Show(activeProfile).HandleExceptions();
		}
		UI.AddDisposable(_playerModelView);
		UI.AddDisposable(_reportPanel.Close);
	}

	public void method_3()
	{
		ItemUiContext.Instance.ShowPlayerProfileScreen(profile_0.EftStats.Aggressor.AccountId, EItemViewType.OtherPlayerProfileSimple).HandleExceptions();
	}

	public override ETranslateResult TranslateCommand(ECommand command)
	{
		return InputNode.GetDefaultBlockResult(command);
	}

	[CompilerGenerated]
	public void method_4(bool arg)
	{
		method_3();
	}
}
