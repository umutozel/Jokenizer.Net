using System;

namespace Jokenizer.Net;

public class InvalidSyntaxException(string message) : Exception(message);
