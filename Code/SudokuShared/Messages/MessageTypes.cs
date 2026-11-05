
namespace SudokuShared.Messages;

public static class MessageTypes
{
    // Ghép trận
    public const string JOIN_QUEUE = "JOIN_QUEUE";
    public const string WAITING = "WAITING";
    public const string MATCH_FOUND = "MATCH_FOUND";
    public const string GAME_STARTED = "GAME_STARTED";

    // Trong trận
    public const string SUBMIT_MOVE = "SUBMIT_MOVE";
    public const string MOVE_RESULT = "MOVE_RESULT";
    public const string BOARD_UPDATE = "BOARD_UPDATE";
    public const string TIME_UPDATE = "TIME_UPDATE";
    public const string GAME_OVER = "GAME_OVER";
    public const string GIVE_UP = "GIVE_UP";

    // Khán giả
    public const string SPECTATE_MATCH = "SPECTATE_MATCH";
    public const string SPECTATOR_UPDATE = "SPECTATOR_UPDATE";

    // Kết nối lại
    public const string RESUME_MATCH = "RESUME_MATCH";
    public const string MATCH_PAUSED = "MATCH_PAUSED";
    public const string MATCH_RESUMED = "MATCH_RESUMED";

    // Lỗi
    public const string ERROR = "ERROR";
}
