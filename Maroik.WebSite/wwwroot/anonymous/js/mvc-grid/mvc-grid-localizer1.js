const localizerNonFactors1 = {
    Equals: $('#localizerNonFactorsEquals').val(),
    NotEquals: $('#localizerNonFactorsNotEquals').val(),
    Contains: $('#localizerNonFactorsContains').val(),
    StartsWith: $('#localizerNonFactorsStartsWith').val(),
    EndsWith: $('#localizerNonFactorsEndsWith').val(),
    LessThan: $('#localizerNonFactorsLessThan').val(),
    GreaterThan: $('#localizerNonFactorsGreaterThan').val(),
    LessThanOrEqual: $('#localizerNonFactorsLessThanOrEqual').val(),
    GreaterThanOrEqual: $('#localizerNonFactorsGreaterThanOrEqual').val(),
    EarlierThan: $('#localizerNonFactorsEarlierThan').val(),
    LaterThan: $('#localizerNonFactorsLaterThan').val(),
    EarlierThanOrEqual: $('#localizerNonFactorsEarlierThanOrEqual').val(),
    LaterThanOrEqual: $('#localizerNonFactorsLaterThanOrEqual').val(),
    And: $('#localizerNonFactorsAnd').val(),
    Or: $('#localizerNonFactorsOr').val()
};

MvcGrid.instances = [];
MvcGrid.lang = {
    default: {
        "equals": localizerNonFactors1.Equals,
        "not-equals": localizerNonFactors1.NotEquals
    },
    text: {
        "contains": localizerNonFactors1.Contains,
        "equals": localizerNonFactors1.Equals,
        "not-equals": localizerNonFactors1.NotEquals,
        "starts-with": localizerNonFactors1.StartsWith,
        "ends-with": localizerNonFactors1.EndsWith
    },
    number: {
        "equals": localizerNonFactors1.Equals,
        "not-equals": localizerNonFactors1.NotEquals,
        "less-than": localizerNonFactors1.LessThan,
        "greater-than": localizerNonFactors1.GreaterThan,
        "less-than-or-equal": localizerNonFactors1.LessThanOrEqual,
        "greater-than-or-equal": localizerNonFactors1.GreaterThanOrEqual
    },
    date: {
        "equals": localizerNonFactors1.Equals,
        "not-equals": localizerNonFactors1.NotEquals,
        "earlier-than": localizerNonFactors1.EarlierThan,
        "later-than": localizerNonFactors1.LaterThan,
        "earlier-than-or-equal": localizerNonFactors1.EarlierThanOrEqual,
        "later-than-or-equal": localizerNonFactors1.LaterThanOrEqual
    },
    guid: {
        "equals": localizerNonFactors1.Equals,
        "not-equals": localizerNonFactors1.NotEquals
    },
    filter: {
        "apply": "&#10003;",
        "remove": "&#10008;"
    },
    operator: {
        "select": "",
        "and": localizerNonFactors1.And,
        "or": localizerNonFactors1.Or
    }
};