const _localizerMvcGrid1 = {
    Equals: $('#_localizerMvcGridEquals').val(),
    NotEquals: $('#_localizerMvcGridNotEquals').val(),
    Contains: $('#_localizerMvcGridContains').val(),
    ConsistsOf: $('#_localizerMvcGridConsistsOf').val(),
    StartsWith: $('#_localizerMvcGridStartsWith').val(),
    EndsWith: $('#_localizerMvcGridEndsWith').val(),
    LessThan: $('#_localizerMvcGridLessThan').val(),
    GreaterThan: $('#_localizerMvcGridGreaterThan').val(),
    LessThanOrEqual: $('#_localizerMvcGridLessThanOrEqual').val(),
    GreaterThanOrEqual: $('#_localizerMvcGridGreaterThanOrEqual').val(),
    EarlierThan: $('#_localizerMvcGridEarlierThan').val(),
    LaterThan: $('#_localizerMvcGridLaterThan').val(),
    EarlierThanOrEqual: $('#_localizerMvcGridEarlierThanOrEqual').val(),
    LaterThanOrEqual: $('#_localizerMvcGridLaterThanOrEqual').val(),
    And: $('#_localizerMvcGridAnd').val(),
    Or: $('#_localizerMvcGridOr').val()
};

MvcGrid.instances = [];
MvcGrid.lang = {
    default: {
        "equals": _localizerMvcGrid1.Equals,
        "not-equals": _localizerMvcGrid1.NotEquals
    },
    text: {
        "contains": _localizerMvcGrid1.Contains,
        "consists-of": _localizerMvcGrid1.ConsistsOf,
        "equals": _localizerMvcGrid1.Equals,
        "not-equals": _localizerMvcGrid1.NotEquals,
        "starts-with": _localizerMvcGrid1.StartsWith,
        "ends-with": _localizerMvcGrid1.EndsWith
    },
    number: {
        "equals": _localizerMvcGrid1.Equals,
        "not-equals": _localizerMvcGrid1.NotEquals,
        "less-than": _localizerMvcGrid1.LessThan,
        "greater-than": _localizerMvcGrid1.GreaterThan,
        "less-than-or-equal": _localizerMvcGrid1.LessThanOrEqual,
        "greater-than-or-equal": _localizerMvcGrid1.GreaterThanOrEqual
    },
    date: {
        "equals": _localizerMvcGrid1.Equals,
        "not-equals": _localizerMvcGrid1.NotEquals,
        "earlier-than": _localizerMvcGrid1.EarlierThan,
        "later-than": _localizerMvcGrid1.LaterThan,
        "earlier-than-or-equal": _localizerMvcGrid1.EarlierThanOrEqual,
        "later-than-or-equal": _localizerMvcGrid1.LaterThanOrEqual
    },
    guid: {
        "equals": _localizerMvcGrid1.Equals,
        "not-equals": _localizerMvcGrid1.NotEquals
    },
    filter: {
        "apply": "&#10003;",
        "remove": "&#10008;"
    },
    operator: {
        "select": "",
        "and": _localizerMvcGrid1.And,
        "or": _localizerMvcGrid1.Or
    }
};
