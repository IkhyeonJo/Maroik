# MVC Grid — third-party notice

The code in this folder (`Maroik.Website/Grids`) and its tests (`Maroik.Website.Tests/Grids`) come from
NonFactors AspNetCore.Grid 8.0.0 (<https://gitlab.com/NonFactors/AspNetCore.Grid>, tag `v8.0.0`), the source of the
`NonFactors.Grid.Core.Mvc6` / `NonFactors.Grid.Mvc6` NuGet packages, which Maroik no longer references. It was adapted to
this repository's rules (namespaces, folders, analyzers; `IHtmlGrid.RenderAsync` instead of `IHtmlContent`) and is
maintained here. The client side of the same release — `wwwroot/{admin,user,anonymous}/js/mvc-grid`,
`css/mvc-grid` and `Views/Shared/MvcGrid` — is covered by the same licence.

MIT License

Copyright © NonFactors

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
