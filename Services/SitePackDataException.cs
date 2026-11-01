namespace Material_Tracker.Services;

public sealed class SitePackDataException(string message, Exception? innerException = null)
    : Exception(message, innerException);
