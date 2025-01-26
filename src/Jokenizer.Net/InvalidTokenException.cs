using System;

namespace Jokenizer.Net;

public class InvalidTokenException(string message) : Exception(message);
