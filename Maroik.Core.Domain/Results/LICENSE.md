# ErrorOr — third-party notice

The code in this folder (`Maroik.Core.Domain/Results`, with its `Interfaces/` and `Extensions/`) and its tests
(`Maroik.Core.Domain.Tests/Results`) come from ErrorOr 2.1.1 (<https://github.com/error-or/error-or>, tag `v2.1.1`, commit
`03984d5775704d2bd3e17df35f9362ce75375c2d`), the source of the `ErrorOr` NuGet package, which Maroik no longer references.
It was adapted to this repository's rules (namespace `Maroik.Core.Domain.Results`, one type per file, the interfaces and
extension classes in their own folders, net10.0 only; the tests use xUnit's `Assert` instead of FluentAssertions) and is
maintained here. Later upstream fixes applied here: `60b20e0` (`ErrorOr<TValue>.GetHashCode` no longer throws for a success holding
`null`, including `default(ErrorOr<TValue>)`).

MIT License

Copyright (c) 2022 Amichai Mantinband

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
