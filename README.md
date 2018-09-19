# Jokenizer.Net
C# Expression parser, inspired from jokenizer project.

[![Build status](https://ci.appveyor.com/api/projects/status/ytmg0iw1qmynl3fd?svg=true)](https://ci.appveyor.com/project/umutozel/jokenizer-net)
[![Coverage Status](https://coveralls.io/repos/github/umutozel/Jokenizer.Net/badge.svg?branch=master)](https://coveralls.io/github/umutozel/Jokenizer.Net?branch=master)
[![NuGet Badge](https://buildstats.info/nuget/Jokenizer.Net)](https://www.nuget.org/packages/Jokenizer.Net/)
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
*/
```

# License
Jokenizer is under the [MIT License](LICENSE).
