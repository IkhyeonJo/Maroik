const nonFactorsLocalizer1 = {
    Equals: $('#_localizerNonFactorsEquals').val(),
    NotEquals: $('#_localizerNonFactorsNotEquals').val(),
    Contains: $('#_localizerNonFactorsContains').val(),
    StartsWith: $('#_localizerNonFactorsStartsWith').val(),
    EndsWith: $('#_localizerNonFactorsEndsWith').val(),
    LessThan: $('#_localizerNonFactorsLessThan').val(),
    GreaterThan: $('#_localizerNonFactorsGreaterThan').val(),
    LessThanOrEqual: $('#_localizerNonFactorsLessThanOrEqual').val(),
    GreaterThanOrEqual: $('#_localizerNonFactorsGreaterThanOrEqual').val(),
    EarlierThan: $('#_localizerNonFactorsEarlierThan').val(),
    LaterThan: $('#_localizerNonFactorsLaterThan').val(),
    EarlierThanOrEqual: $('#_localizerNonFactorsEarlierThanOrEqual').val(),
    LaterThanOrEqual: $('#_localizerNonFactorsLaterThanOrEqual').val(),
    And: $('#_localizerNonFactorsAnd').val(),
    Or: $('#_localizerNonFactorsOr').val()
};

MvcGrid.instances = [];
MvcGrid.lang = {
    default: {
        "equals": nonFactorsLocalizer1.Equals,
        "not-equals": nonFactorsLocalizer1.NotEquals
    },
    text: {
        "contains": nonFactorsLocalizer1.Contains,
        "equals": nonFactorsLocalizer1.Equals,
        "not-equals": nonFactorsLocalizer1.NotEquals,
        "starts-with": nonFactorsLocalizer1.StartsWith,
        "ends-with": nonFactorsLocalizer1.EndsWith
    },
    number: {
        "equals": nonFactorsLocalizer1.Equals,
        "not-equals": nonFactorsLocalizer1.NotEquals,
        "less-than": nonFactorsLocalizer1.LessThan,
        "greater-than": nonFactorsLocalizer1.GreaterThan,
        "less-than-or-equal": nonFactorsLocalizer1.LessThanOrEqual,
        "greater-than-or-equal": nonFactorsLocalizer1.GreaterThanOrEqual
    },
    date: {
        "equals": nonFactorsLocalizer1.Equals,
        "not-equals": nonFactorsLocalizer1.NotEquals,
        "earlier-than": nonFactorsLocalizer1.EarlierThan,
        "later-than": nonFactorsLocalizer1.LaterThan,
        "earlier-than-or-equal": nonFactorsLocalizer1.EarlierThanOrEqual,
        "later-than-or-equal": nonFactorsLocalizer1.LaterThanOrEqual
    },
    guid: {
        "equals": nonFactorsLocalizer1.Equals,
        "not-equals": nonFactorsLocalizer1.NotEquals
    },
    filter: {
        "apply": "&#10003;",
        "remove": "&#10008;"
    },
    operator: {
        "select": "",
        "and": nonFactorsLocalizer1.And,
        "or": nonFactorsLocalizer1.Or
    }
};