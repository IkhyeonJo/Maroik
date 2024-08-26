const localizer1 = {
    Equals: $('#localizerEquals').val(),
    NotEquals: $('#localizerNotEquals').val(),
    Contains: $('#localizerContains').val(),
    StartsWith: $('#localizerStartsWith').val(),
    EndsWith: $('#localizerEndsWith').val(),
    LessThan: $('#localizerLessThan').val(),
    GreaterThan: $('#localizerGreaterThan').val(),
    LessThanOrEqual: $('#localizerLessThanOrEqual').val(),
    GreaterThanOrEqual: $('#localizerGreaterThanOrEqual').val(),
    EarlierThan: $('#localizerEarlierThan').val(),
    LaterThan: $('#localizerLaterThan').val(),
    EarlierThanOrEqual: $('#localizerEarlierThanOrEqual').val(),
    LaterThanOrEqual: $('#localizerLaterThanOrEqual').val(),
    And: $('#localizerAnd').val(),
    Or: $('#localizerOr').val()
};

MvcGrid.instances = [];
MvcGrid.lang = {
    default: {
        "equals": localizer1.Equals,
        "not-equals": localizer1.NotEquals
    },
    text: {
        "contains": localizer1.Contains,
        "equals": localizer1.Equals,
        "not-equals": localizer1.NotEquals,
        "starts-with": localizer1.StartsWith,
        "ends-with": localizer1.EndsWith
    },
    number: {
        "equals": localizer1.Equals,
        "not-equals": localizer1.NotEquals,
        "less-than": localizer1.LessThan,
        "greater-than": localizer1.GreaterThan,
        "less-than-or-equal": localizer1.LessThanOrEqual,
        "greater-than-or-equal": localizer1.GreaterThanOrEqual
    },
    date: {
        "equals": localizer1.Equals,
        "not-equals": localizer1.NotEquals,
        "earlier-than": localizer1.EarlierThan,
        "later-than": localizer1.LaterThan,
        "earlier-than-or-equal": localizer1.EarlierThanOrEqual,
        "later-than-or-equal": localizer1.LaterThanOrEqual
    },
    guid: {
        "equals": localizer1.Equals,
        "not-equals": localizer1.NotEquals
    },
    filter: {
        "apply": "&#10003;",
        "remove": "&#10008;"
    },
    operator: {
        "select": "",
        "and": localizer1.And,
        "or": localizer1.Or
    }
};