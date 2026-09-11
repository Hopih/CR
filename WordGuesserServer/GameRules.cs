using WordGuesser.Protocol;

namespace WordGuesserServer;

public static class GameRules
{
    public const int AttemptsPerRound = 5;
    public const int WinningScore = 3;

    public static bool IsWord(string word) => word.Length > 0 && word.All(c =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= 'а' and <= 'я' or >= 'А' and <= 'Я' or 'ё' or 'Ё');

    public static CommandResponse SetWord(GameSession session, string word)
    {
        if (session.State != SessionState.Ready) return CommandResponse.InvalidState;
        if (!IsWord(word)) return CommandResponse.InvalidWord;
        session.GuessedWord = word.ToUpperInvariant();
        session.TryCount = AttemptsPerRound;
        session.State = SessionState.Guessing;
        return CommandResponse.Ok;
    }

    public static CommandResponse Guess(GameSession session, string guess, out string mask, out bool guessed)
    {
        mask = "";
        guessed = false;
        if (session.State != SessionState.Guessing || session.GuessedWord == null || session.TryCount <= 0)
            return CommandResponse.InvalidState;
        if (!IsWord(guess) || guess.Length != session.GuessedWord.Length) return CommandResponse.InvalidGuess;
        guess = guess.ToUpperInvariant();
        var word = session.GuessedWord;
        mask = new string(word.Select((c, i) => c == guess[i] ? c : '*').ToArray());
        guessed = guess == word;
        session.TryCount--;
        if (guessed || session.TryCount == 0) session.State = SessionState.RoundEnd;
        return CommandResponse.Ok;
    }
}
