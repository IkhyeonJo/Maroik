using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Encodings.Web;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IHtmlHelper"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeHtmlHelper : IHtmlHelper
{
    /// <summary>The value <see cref="IHtmlHelper.Html5DateRenderingMode"/> answers; settable by a test.</summary>
    public Html5DateRenderingMode Html5DateRenderingMode { get; set; }

    /// <summary>The value <see cref="IHtmlHelper.IdAttributeDotReplacement"/> answers; settable by a test.</summary>
    public string IdAttributeDotReplacement { get; set; } = default!;

    /// <summary>The value <see cref="IHtmlHelper.MetadataProvider"/> answers; settable by a test.</summary>
    public IModelMetadataProvider MetadataProvider { get; set; } = default!;

    /// <summary>The value <see cref="IHtmlHelper.ViewBag"/> answers; settable by a test.</summary>
    public object ViewBag { get; set; } = default!;

    /// <summary>The value <see cref="IHtmlHelper.ViewContext"/> answers; settable by a test.</summary>
    public ViewContext ViewContext { get; set; } = default!;

    /// <summary>The value <see cref="IHtmlHelper.ViewData"/> answers; settable by a test.</summary>
    public ViewDataDictionary ViewData { get; set; } = default!;

    /// <summary>The value <see cref="IHtmlHelper.TempData"/> answers; settable by a test.</summary>
    public ITempDataDictionary TempData { get; set; } = default!;

    /// <summary>The value <see cref="IHtmlHelper.UrlEncoder"/> answers; settable by a test.</summary>
    public UrlEncoder UrlEncoder { get; set; } = default!;

    /// <summary>What <c>ActionLink</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, string, string, string, object, object, IHtmlContent>? OnActionLink { get; set; }

    /// <summary>The arguments of every <c>ActionLink</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string LinkText, string ActionName, string ControllerName, string Protocol, string Hostname, string Fragment, object RouteValues, object HtmlAttributes)> ActionLinkCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent ActionLink(string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes)
    {
        lock (ActionLinkCalls) ActionLinkCalls.Add((linkText, actionName, controllerName, protocol, hostname, fragment, routeValues, htmlAttributes));
        if (OnActionLink is { } on)
        {
            return on(linkText, actionName, controllerName, protocol, hostname, fragment, routeValues, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>AntiForgeryToken</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IHtmlContent>? OnAntiForgeryToken { get; set; }

    /// <summary>How many times <c>AntiForgeryToken</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int AntiForgeryTokenCallCount => Volatile.Read(ref _antiForgeryTokenCallCount);

    /// <summary>Backs <see cref="AntiForgeryTokenCallCount"/>.</summary>
    private int _antiForgeryTokenCallCount;

    /// <inheritdoc/>
    public IHtmlContent AntiForgeryToken()
    {
        Interlocked.Increment(ref _antiForgeryTokenCallCount);
        if (OnAntiForgeryToken is { } on)
        {
            return on();
        }

        return default!;
    }

    /// <summary>What <c>BeginForm</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, object, FormMethod, bool?, object, MvcForm>? OnBeginForm { get; set; }

    /// <summary>The arguments of every <c>BeginForm</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string ActionName, string ControllerName, object RouteValues, FormMethod Method, bool? Antiforgery, object HtmlAttributes)> BeginFormCalls { get; } = [];

    /// <inheritdoc/>
    public MvcForm BeginForm(string actionName, string controllerName, object routeValues, FormMethod method, bool? antiforgery, object htmlAttributes)
    {
        lock (BeginFormCalls) BeginFormCalls.Add((actionName, controllerName, routeValues, method, antiforgery, htmlAttributes));
        if (OnBeginForm is { } on)
        {
            return on(actionName, controllerName, routeValues, method, antiforgery, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>BeginRouteForm</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object, FormMethod, bool?, object, MvcForm>? OnBeginRouteForm { get; set; }

    /// <summary>The arguments of every <c>BeginRouteForm</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string RouteName, object RouteValues, FormMethod Method, bool? Antiforgery, object HtmlAttributes)> BeginRouteFormCalls { get; } = [];

    /// <inheritdoc/>
    public MvcForm BeginRouteForm(string routeName, object routeValues, FormMethod method, bool? antiforgery, object htmlAttributes)
    {
        lock (BeginRouteFormCalls) BeginRouteFormCalls.Add((routeName, routeValues, method, antiforgery, htmlAttributes));
        if (OnBeginRouteForm is { } on)
        {
            return on(routeName, routeValues, method, antiforgery, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>CheckBox</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool?, object, IHtmlContent>? OnCheckBox { get; set; }

    /// <summary>The arguments of every <c>CheckBox</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, bool? IsChecked, object HtmlAttributes)> CheckBoxCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent CheckBox(string expression, bool? isChecked, object htmlAttributes)
    {
        lock (CheckBoxCalls) CheckBoxCalls.Add((expression, isChecked, htmlAttributes));
        if (OnCheckBox is { } on)
        {
            return on(expression, isChecked, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>Display</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, object, IHtmlContent>? OnDisplay { get; set; }

    /// <summary>The arguments of every <c>Display</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, string TemplateName, string HtmlFieldName, object AdditionalViewData)> DisplayCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent Display(string expression, string templateName, string htmlFieldName, object additionalViewData)
    {
        lock (DisplayCalls) DisplayCalls.Add((expression, templateName, htmlFieldName, additionalViewData));
        if (OnDisplay is { } on)
        {
            return on(expression, templateName, htmlFieldName, additionalViewData);
        }

        return default!;
    }

    /// <summary>What <c>DisplayName</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnDisplayName { get; set; }

    /// <summary>The arguments of every <c>DisplayName</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> DisplayNameCalls { get; } = [];

    /// <inheritdoc/>
    public string DisplayName(string expression)
    {
        lock (DisplayNameCalls) DisplayNameCalls.Add(expression);
        if (OnDisplayName is { } on)
        {
            return on(expression);
        }

        return default!;
    }

    /// <summary>What <c>DisplayText</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnDisplayText { get; set; }

    /// <summary>The arguments of every <c>DisplayText</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> DisplayTextCalls { get; } = [];

    /// <inheritdoc/>
    public string DisplayText(string expression)
    {
        lock (DisplayTextCalls) DisplayTextCalls.Add(expression);
        if (OnDisplayText is { } on)
        {
            return on(expression);
        }

        return default!;
    }

    /// <summary>What <c>DropDownList</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IEnumerable<SelectListItem>, string, object, IHtmlContent>? OnDropDownList { get; set; }

    /// <summary>The arguments of every <c>DropDownList</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, IEnumerable<SelectListItem> SelectList, string OptionLabel, object HtmlAttributes)> DropDownListCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent DropDownList(string expression, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes)
    {
        lock (DropDownListCalls) DropDownListCalls.Add((expression, selectList, optionLabel, htmlAttributes));
        if (OnDropDownList is { } on)
        {
            return on(expression, selectList, optionLabel, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>Editor</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, object, IHtmlContent>? OnEditor { get; set; }

    /// <summary>The arguments of every <c>Editor</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, string TemplateName, string HtmlFieldName, object AdditionalViewData)> EditorCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent Editor(string expression, string templateName, string htmlFieldName, object additionalViewData)
    {
        lock (EditorCalls) EditorCalls.Add((expression, templateName, htmlFieldName, additionalViewData));
        if (OnEditor is { } on)
        {
            return on(expression, templateName, htmlFieldName, additionalViewData);
        }

        return default!;
    }

    /// <summary>What <c>Encode</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<object, string>? OnEncode { get; set; }

    /// <summary>The arguments of every <c>Encode</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<object> EncodeCalls { get; } = [];

    /// <inheritdoc/>
    public string Encode(object value)
    {
        lock (EncodeCalls) EncodeCalls.Add(value);
        if (OnEncode is { } on)
        {
            return on(value);
        }

        return default!;
    }

    /// <summary>What <c>Encode</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnEncode_Value { get; set; }

    /// <summary>The arguments of every <c>Encode</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> Encode_ValueCalls { get; } = [];

    /// <inheritdoc/>
    public string Encode(string value)
    {
        lock (Encode_ValueCalls) Encode_ValueCalls.Add(value);
        if (OnEncode_Value is { } on)
        {
            return on(value);
        }

        return default!;
    }

    /// <summary>What <c>EndForm</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action? OnEndForm { get; set; }

    /// <summary>How many times <c>EndForm</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int EndFormCallCount => Volatile.Read(ref _endFormCallCount);

    /// <summary>Backs <see cref="EndFormCallCount"/>.</summary>
    private int _endFormCallCount;

    /// <inheritdoc/>
    public void EndForm()
    {
        Interlocked.Increment(ref _endFormCallCount);
        OnEndForm?.Invoke();
    }

    /// <summary>What <c>FormatValue</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<object, string, string>? OnFormatValue { get; set; }

    /// <summary>The arguments of every <c>FormatValue</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(object Value, string Format)> FormatValueCalls { get; } = [];

    /// <inheritdoc/>
    public string FormatValue(object value, string format)
    {
        lock (FormatValueCalls) FormatValueCalls.Add((value, format));
        if (OnFormatValue is { } on)
        {
            return on(value, format);
        }

        return default!;
    }

    /// <summary>What <c>GenerateIdFromName</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnGenerateIdFromName { get; set; }

    /// <summary>The arguments of every <c>GenerateIdFromName</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> GenerateIdFromNameCalls { get; } = [];

    /// <inheritdoc/>
    public string GenerateIdFromName(string fullName)
    {
        lock (GenerateIdFromNameCalls) GenerateIdFromNameCalls.Add(fullName);
        if (OnGenerateIdFromName is { } on)
        {
            return on(fullName);
        }

        return default!;
    }

    /// <inheritdoc/>
    IEnumerable<SelectListItem> IHtmlHelper.GetEnumSelectList<TEnum>() => throw new NotSupportedException("GetEnumSelectList is not faked.");

    /// <summary>What <c>GetEnumSelectList</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Type, IEnumerable<SelectListItem>>? OnGetEnumSelectListWithEnumType { get; set; }

    /// <summary>The arguments of every <c>GetEnumSelectList</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<Type> GetEnumSelectListWithEnumTypeCalls { get; } = [];

    /// <inheritdoc/>
    public IEnumerable<SelectListItem> GetEnumSelectList(Type enumType)
    {
        lock (GetEnumSelectListWithEnumTypeCalls) GetEnumSelectListWithEnumTypeCalls.Add(enumType);
        if (OnGetEnumSelectListWithEnumType is { } on)
        {
            return on(enumType);
        }

        return [];
    }

    /// <summary>What <c>Hidden</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object, object, IHtmlContent>? OnHidden { get; set; }

    /// <summary>The arguments of every <c>Hidden</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, object Value, object HtmlAttributes)> HiddenCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent Hidden(string expression, object value, object htmlAttributes)
    {
        lock (HiddenCalls) HiddenCalls.Add((expression, value, htmlAttributes));
        if (OnHidden is { } on)
        {
            return on(expression, value, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>Id</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnId { get; set; }

    /// <summary>The arguments of every <c>Id</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> IdCalls { get; } = [];

    /// <inheritdoc/>
    public string Id(string expression)
    {
        lock (IdCalls) IdCalls.Add(expression);
        if (OnId is { } on)
        {
            return on(expression);
        }

        return default!;
    }

    /// <summary>What <c>Label</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, object, IHtmlContent>? OnLabel { get; set; }

    /// <summary>The arguments of every <c>Label</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, string LabelText, object HtmlAttributes)> LabelCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent Label(string expression, string labelText, object htmlAttributes)
    {
        lock (LabelCalls) LabelCalls.Add((expression, labelText, htmlAttributes));
        if (OnLabel is { } on)
        {
            return on(expression, labelText, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>ListBox</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IEnumerable<SelectListItem>, object, IHtmlContent>? OnListBox { get; set; }

    /// <summary>The arguments of every <c>ListBox</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, IEnumerable<SelectListItem> SelectList, object HtmlAttributes)> ListBoxCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent ListBox(string expression, IEnumerable<SelectListItem> selectList, object htmlAttributes)
    {
        lock (ListBoxCalls) ListBoxCalls.Add((expression, selectList, htmlAttributes));
        if (OnListBox is { } on)
        {
            return on(expression, selectList, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>Name</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnName { get; set; }

    /// <summary>The arguments of every <c>Name</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> NameCalls { get; } = [];

    /// <inheritdoc/>
    public string Name(string expression)
    {
        lock (NameCalls) NameCalls.Add(expression);
        if (OnName is { } on)
        {
            return on(expression);
        }

        return default!;
    }

    /// <summary>What <c>PartialAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object, ViewDataDictionary, Task<IHtmlContent>>? OnPartialAsync { get; set; }

    /// <summary>The arguments of every <c>PartialAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string PartialViewName, object Model, ViewDataDictionary ViewData)> PartialAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IHtmlContent> PartialAsync(string partialViewName, object model, ViewDataDictionary viewData)
    {
        lock (PartialAsyncCalls) PartialAsyncCalls.Add((partialViewName, model, viewData));
        if (OnPartialAsync is { } on)
        {
            return on(partialViewName, model, viewData);
        }

        return Task.FromResult<IHtmlContent>(default!);
    }

    /// <summary>What <c>Password</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object, object, IHtmlContent>? OnPassword { get; set; }

    /// <summary>The arguments of every <c>Password</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, object Value, object HtmlAttributes)> PasswordCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent Password(string expression, object value, object htmlAttributes)
    {
        lock (PasswordCalls) PasswordCalls.Add((expression, value, htmlAttributes));
        if (OnPassword is { } on)
        {
            return on(expression, value, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>RadioButton</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object, bool?, object, IHtmlContent>? OnRadioButton { get; set; }

    /// <summary>The arguments of every <c>RadioButton</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, object Value, bool? IsChecked, object HtmlAttributes)> RadioButtonCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent RadioButton(string expression, object value, bool? isChecked, object htmlAttributes)
    {
        lock (RadioButtonCalls) RadioButtonCalls.Add((expression, value, isChecked, htmlAttributes));
        if (OnRadioButton is { } on)
        {
            return on(expression, value, isChecked, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>Raw</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IHtmlContent>? OnRaw { get; set; }

    /// <summary>The arguments of every <c>Raw</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> RawCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent Raw(string value)
    {
        lock (RawCalls) RawCalls.Add(value);
        if (OnRaw is { } on)
        {
            return on(value);
        }

        return default!;
    }

    /// <summary>What <c>Raw</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<object, IHtmlContent>? OnRaw_Value { get; set; }

    /// <summary>The arguments of every <c>Raw</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<object> Raw_ValueCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent Raw(object value)
    {
        lock (Raw_ValueCalls) Raw_ValueCalls.Add(value);
        if (OnRaw_Value is { } on)
        {
            return on(value);
        }

        return default!;
    }

    /// <summary>What <c>RenderPartialAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object, ViewDataDictionary, Task>? OnRenderPartialAsync { get; set; }

    /// <summary>The arguments of every <c>RenderPartialAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string PartialViewName, object Model, ViewDataDictionary ViewData)> RenderPartialAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task RenderPartialAsync(string partialViewName, object model, ViewDataDictionary viewData)
    {
        lock (RenderPartialAsyncCalls) RenderPartialAsyncCalls.Add((partialViewName, model, viewData));
        if (OnRenderPartialAsync is { } on)
        {
            return on(partialViewName, model, viewData);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>RouteLink</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, string, string, object, object, IHtmlContent>? OnRouteLink { get; set; }

    /// <summary>The arguments of every <c>RouteLink</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string LinkText, string RouteName, string Protocol, string HostName, string Fragment, object RouteValues, object HtmlAttributes)> RouteLinkCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent RouteLink(string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes)
    {
        lock (RouteLinkCalls) RouteLinkCalls.Add((linkText, routeName, protocol, hostName, fragment, routeValues, htmlAttributes));
        if (OnRouteLink is { } on)
        {
            return on(linkText, routeName, protocol, hostName, fragment, routeValues, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>TextArea</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, int, int, object, IHtmlContent>? OnTextArea { get; set; }

    /// <summary>The arguments of every <c>TextArea</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, string Value, int Rows, int Columns, object HtmlAttributes)> TextAreaCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent TextArea(string expression, string value, int rows, int columns, object htmlAttributes)
    {
        lock (TextAreaCalls) TextAreaCalls.Add((expression, value, rows, columns, htmlAttributes));
        if (OnTextArea is { } on)
        {
            return on(expression, value, rows, columns, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>TextBox</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object, string, object, IHtmlContent>? OnTextBox { get; set; }

    /// <summary>The arguments of every <c>TextBox</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, object Value, string Format, object HtmlAttributes)> TextBoxCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent TextBox(string expression, object value, string format, object htmlAttributes)
    {
        lock (TextBoxCalls) TextBoxCalls.Add((expression, value, format, htmlAttributes));
        if (OnTextBox is { } on)
        {
            return on(expression, value, format, htmlAttributes);
        }

        return default!;
    }

    /// <summary>What <c>ValidationMessage</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, object, string, IHtmlContent>? OnValidationMessage { get; set; }

    /// <summary>The arguments of every <c>ValidationMessage</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, string Message, object HtmlAttributes, string Tag)> ValidationMessageCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent ValidationMessage(string expression, string message, object htmlAttributes, string tag)
    {
        lock (ValidationMessageCalls) ValidationMessageCalls.Add((expression, message, htmlAttributes, tag));
        if (OnValidationMessage is { } on)
        {
            return on(expression, message, htmlAttributes, tag);
        }

        return default!;
    }

    /// <summary>What <c>ValidationSummary</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<bool, string, object, string, IHtmlContent>? OnValidationSummary { get; set; }

    /// <summary>The arguments of every <c>ValidationSummary</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(bool ExcludePropertyErrors, string Message, object HtmlAttributes, string Tag)> ValidationSummaryCalls { get; } = [];

    /// <inheritdoc/>
    public IHtmlContent ValidationSummary(bool excludePropertyErrors, string message, object htmlAttributes, string tag)
    {
        lock (ValidationSummaryCalls) ValidationSummaryCalls.Add((excludePropertyErrors, message, htmlAttributes, tag));
        if (OnValidationSummary is { } on)
        {
            return on(excludePropertyErrors, message, htmlAttributes, tag);
        }

        return default!;
    }

    /// <summary>What <c>Value</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string>? OnValue { get; set; }

    /// <summary>The arguments of every <c>Value</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Expression, string Format)> ValueCalls { get; } = [];

    /// <inheritdoc/>
    public string Value(string expression, string format)
    {
        lock (ValueCalls) ValueCalls.Add((expression, format));
        if (OnValue is { } on)
        {
            return on(expression, format);
        }

        return default!;
    }
}
