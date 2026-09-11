namespace WordGuesser.Protocol;

public enum CommandResponse
{
    Ok,
    PlayerNotJoined,
    IncorrectSender,
    CommandIncorrect,
    PackageIncorrect,
    SessionNotFound,
    AlreadyInSession,
    SessionFull,
    InvalidState,
    InvalidWord,
    InvalidGuess
}
