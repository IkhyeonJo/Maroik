const nonFactorsLocalizer1 = {
    Equals: $('#NonFactorsLocalizerEquals').val(),
    NotEquals: $('#NonFactorsLocalizerNotEquals').val(),
    Contains: $('#NonFactorsLocalizerContains').val(),
    StartsWith: $('#NonFactorsLocalizerStartsWith').val(),
    EndsWith: $('#NonFactorsLocalizerEndsWith').val(),
    LessThan: $('#NonFactorsLocalizerLessThan').val(),
    GreaterThan: $('#NonFactorsLocalizerGreaterThan').val(),
    LessThanOrEqual: $('#NonFactorsLocalizerLessThanOrEqual').val(),
    GreaterThanOrEqual: $('#NonFactorsLocalizerGreaterThanOrEqual').val(),
    EarlierThan: $('#NonFactorsLocalizerEarlierThan').val(),
    LaterThan: $('#NonFactorsLocalizerLaterThan').val(),
    EarlierThanOrEqual: $('#NonFactorsLocalizerEarlierThanOrEqual').val(),
    LaterThanOrEqual: $('#NonFactorsLocalizerLaterThanOrEqual').val(),
    And: $('#NonFactorsLocalizerAnd').val(),
    Or: $('#NonFactorsLocalizerOr').val()
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