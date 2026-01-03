using Newtonsoft.Json;

namespace MMXOnline;

public class ChatEntry {
	public string message = "";
	public int? alliance;
	public int ownerTeam;
	public float time;
	public string sender;
	public bool alwaysShow;
	public bool isSpectator;

	// For JSON.
	#nullable disable
	public ChatEntry() { }
	#nullable enable

	public ChatEntry(
		string message, string sender, int? alliance,
		bool alwaysShow, bool isSpectator = false,
		int ownerTeam = -1
	) {
		this.message = message;
		this.sender = sender;
		this.alliance = alliance;
		this.alwaysShow = alwaysShow;
		this.isSpectator = isSpectator;
		this.ownerTeam = ownerTeam;
	}

	public string getDisplayMessage() {
		if (string.IsNullOrEmpty(sender)) {
			return message;
		}
		string teamMsgPart = "";
		if (isSpectator) {
			teamMsgPart = "(spectator)";
		}
		else if (alliance != null) {
			GameMode gamemode = Global.level.gameMode;
			if (alliance.Value >= 0 && alliance.Value < gamemode.teamNames.Length) {
				teamMsgPart = $"({gamemode.teamNames[alliance.Value].ToLower()})";
			} else {
				teamMsgPart = "(team)";
			}
		}
		
		return sender + teamMsgPart + ": " + message;
	}

	public void sendRpc() {
		var json = JsonConvert.SerializeObject(this);
		Global.serverClient?.rpc(RPC.sendChatMessage, json);
	}
}
