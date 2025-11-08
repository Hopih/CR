namespace WordGuesserServer;

public enum SessionState
{
    WaitingForPlayer,   // после Create — ждём второго игрока
    Ready,              // оба игрока подключены, Host может задать слово
    WordSet,            // слово задано, игрок может угадывать
    Guessing,           // игрок делает попытки
    RoundEnd,           // слово отгадано или попытки кончились
    Swapping,           // смена ролей
    Finished            // матч завершён (3 победы)
}