# Jokenizer.Net
C# Expression parser, inspired from jokenizer project.

[![Build and Test](https://github.com/umutozel/Jokenizer.Net/actions/workflows/build.yml/badge.svg)](https://github.com/umutozel/Jokenizer.Net/actions/workflows/build.yml)
[![codecov](https://codecov.io/gh/umutozel/Jokenizer.Net/graph/badge.svg?token=FzxwO5q0gr)](https://codecov.io/gh/umutozel/Jokenizer.Net)
[![NuGet Badge](https://img.shields.io/nuget/v/Jokenizer.Net.svg)](https://www.nuget.org/packages/Jokenizer.Net/)
![NuGet Downloads](https://img.shields.io/nuget/dt/Jokenizer.Net.svg)
[![GitHub issues](https://img.shields.io/github/issues/umutozel/Jokenizer.Net.svg)](https://github.com/umutozel/Jokenizer.Net/issues)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/umutozel/Jokenizer.Net/master/LICENSE)

[![GitHub stars](https://img.shields.io/github/stars/umutozel/jokenizer.net.svg?style=social&label=Star)](https://github.com/umutozel/jokenizer.net)
[![GitHub forks](https://img.shields.io/github/forks/umutozel/jokenizer.net.svg?style=social&label=Fork)](https://github.com/umutozel/jokenizer.net)

Jokenizer.Net is just a simple library to parse C# expressions and evaluate them with variables and parameters.

# Installation
```
dotnet add package Jokenizer.Net
```

# Let's try it out

```csharp
var expression = Tokenizer.Parse("(a, b) => a < b");
var func = Evaluator.ToFunc<int, int, bool>(expression);
var result = func(1, 2);    // true
```

# License
Jokenizer is under the [MIT License](LICENSE).
