// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

namespace Melia.Shared.Const
{
	/// <summary>
	/// Used in BC_MESSAGE.
	/// </summary>
	public enum MsgType : byte
	{
		Text = 0,
		UsernameOrPasswordIncorrect1 = 1, // Username?
		UsernameOrPasswordIncorrect2 = 2, // Password?
		DoubleLogin = 3, // Disconnects gracefully
		WrongVerificationCode = 4,
		ServerVersionDoesNotMatch = 5,
		NameAlreadyExists = 6,
		CannotCreateCharacter = 7,
		CharacterNameTooShort = 8, // "The character name must be at least %s characters.
								   // 9
		CannotDeleteCharacter1 = 10,
		CreateCharFail = 11,
		CharacterNameTooLong = 12, // "The character name must be less than %s characters.
		CannotDeleteCharacter2 = 13,
		TeamDoesNotExist = 14,
		CharacterDoesNotExist = 15,
		CannotStartGame = 16,
		NewJobAdded = 17,
		// 18
		TeamMemberCapExceeded = 19,
		InsufficientCharacterPoints = 20,
		AccountBlocked1 = 21,
		AccountBlocked2 = 22,
		InventoryFull = 23,
		StartingMapNotAvailable = 24,
		UnableToDeletePartyLeader = 25,
		NotCBTUser = 26,
		RatedEsrbTeen = 27,
		InvalidIpf = 29, // Client IPF files are invalid.

		// ...
	}

	/// <summary>
	/// Constant strings that may be sent with the packet `ZC_ADDON_MSG`.
	/// </summary>
	public static class AddonMessage
	{
		/// <summary>
		/// Used for quitting the game, transitioning from the zone to the barrack, and possibly other reasons.
		/// </summary>
		public const string EXPIREDITEM_ALERT_OPEN = "EXPIREDITEM_ALERT_OPEN";

		public const string RESET_STAT_UP = "RESET_STAT_UP";
		public const string RESET_SKL_UP = "RESET_SKL_UP";
		public const string FAIL_SHOP_BUY = "FAIL_SHOP_BUY";

		public const string RESET_ABILITY_UP = "RESET_ABILITY_UP";
		public const string SUCCESS_BUY_ABILITY_POINT = "SUCCESS_BUY_ABILITY_POINT";
		public const string RESET_ABILITY_ACTIVE = "RESET_ABILITY_ACTIVE";

		/// <summary>
		/// Opens the event banner.
		/// </summary>
		public const string DO_OPEN_EVENTBANNER_UI = "DO_OPEN_EVENTBANNER_UI";
		public const string SET_CHAT_MACRO_DEFAULT = "SET_CHAT_MACRO_DEFAULT";
		public const string FISHING_SUCCESS_COUNT = "FISHING_SUCCESS_COUNT";

		/// <summary>
		/// The UI for first time players showing how to move using the keyboard.
		/// </summary>
		public const string KEYBOARD_TUTORIAL = "KEYBOARD_TUTORIAL";
	}

	/// <summary>
	/// Constant strings sent with `BC_NORMAL_Run`.
	/// </summary>
	public static class BarrackMessage
	{
		public const string THEMA_BUY_SUCCESS = "THEMA_BUY_SUCCESS";
	}
}
