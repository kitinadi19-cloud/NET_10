namespace SudokuShared.Messages;

public static class ProtocolConstants
{
    public const int DefaultPort = 8888;
    public const int MatchSeconds = 600;      // 10 phút / trận
    public const int ReconnectSeconds = 60;   // chờ người chơi quay lại 1 phút
}

public static class MessageTypes
{
    // ===== Client -> Server =====
    public const string Login = "LOGIN";
    public const string JoinQueue = "JOIN_QUEUE";
    public const string LeaveQueue = "LEAVE_QUEUE";
    public const string SubmitMove = "SUBMIT_MOVE";
    public const string Surrender = "SURRENDER";
    public const string ListPlayers = "LIST_PLAYERS";
    public const string ListMatches = "LIST_MATCHES";
    public const string Challenge = "CHALLENGE";
    public const string ChallengeResponse = "CHALLENGE_RESPONSE";
    public const string SpectateMatch = "SPECTATE_MATCH";
    public const string LeaveSpectate = "LEAVE_SPECTATE";
    public const string Ping = "PING";

    // ===== Server -> Client =====
    public const string LoginOk = "LOGIN_OK";
    public const string Ok = "OK";
    public const string Waiting = "WAITING";
    public const string MatchFound = "MATCH_FOUND";
    public const string GameStarted = "GAME_STARTED";
    public const string MoveResult = "MOVE_RESULT";
    public const string BoardUpdate = "BOARD_UPDATE";
    public const string TimeUpdate = "TIME_UPDATE";
    public const string GamePaused = "GAME_PAUSED";
    public const string GameResumed = "GAME_RESUMED";
    public const string GameOver = "GAME_OVER";
    public const string PlayerList = "PLAYER_LIST";
    public const string MatchList = "MATCH_LIST";
    public const string SpectatorUpdate = "SPECTATOR_UPDATE";
    public const string ChallengeReceived = "CHALLENGE_RECEIVED";
    public const string ChallengeDeclined = "CHALLENGE_DECLINED";
    public const string Pong = "PONG";
    public const string Error = "ERROR";
}

public static class MatchStates
{
    public const string Playing = "PLAYING";
    public const string Paused = "PAUSED";
    public const string Finished = "FINISHED";
}