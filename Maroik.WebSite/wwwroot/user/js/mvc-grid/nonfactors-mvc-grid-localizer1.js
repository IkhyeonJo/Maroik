const zlocalizerNonFactors1 = {
    Equals: $('#zlocalizerNonFactorsEquals').val(),
    NotEquals: $('#zlocalizerNonFactorsNotEquals').val(),
    Contains: $('#zlocalizerNonFactorsContains').val(),
    StartsWith: $('#zlocalizerNonFactorsStartsWith').val(),
    EndsWith: $('#zlocalizerNonFactorsEndsWith').val(),
    LessThan: $('#zlocalizerNonFactorsLessThan').val(),
    GreaterThan: $('#zlocalizerNonFactorsGreaterThan').val(),
    LessThanOrEqual: $('#zlocalizerNonFactorsLessThanOrEqual').val(),
    GreaterThanOrEqual: $('#zlocalizerNonFactorsGreaterThanOrEqual').val(),
    EarlierThan: $('#zlocalizerNonFactorsEarlierThan').val(),
    LaterThan: $('#zlocalizerNonFactorsLaterThan').val(),
    EarlierThanOrEqual: $('#zlocalizerNonFactorsEarlierThanOrEqual').val(),
    LaterThanOrEqual: $('#zlocalizerNonFactorsLaterThanOrEqual').val(),
    And: $('#zlocalizerNonFactorsAnd').val(),
    Or: $('#zlocalizerNonFactorsOr').val()
};

MvcGrid.instances = [];
MvcGrid.lang = {
    default: {
        "equals": zlocalizerNonFactors1.Equals,
        "not-equals": zlocalizerNonFactors1.NotEquals
    },
    text: {
        "contains": zlocalizerNonFactors1.Contains,
        "equals": zlocalizerNonFactors1.Equals,
        "not-equals": zlocalizerNonFactors1.NotEquals,
        "starts-with": zlocalizerNonFactors1.StartsWith,
        "ends-with": zlocalizerNonFactors1.EndsWith
    },
    number: {
        "equals": zlocalizerNonFactors1.Equals,
        "not-equals": zlocalizerNonFactors1.NotEquals,
        "less-than": zlocalizerNonFactors1.LessThan,
        "greater-than": zlocalizerNonFactors1.GreaterThan,
        "less-than-or-equal": zlocalizerNonFactors1.LessThanOrEqual,
        "greater-than-or-equal": zlocalizerNonFactors1.GreaterThanOrEqual
    },
    date: {
        "equals": zlocalizerNonFactors1.Equals,
        "not-equals": zlocalizerNonFactors1.NotEquals,
        "earlier-than": zlocalizerNonFactors1.EarlierThan,
        "later-than": zlocalizerNonFactors1.LaterThan,
        "earlier-than-or-equal": zlocalizerNonFactors1.EarlierThanOrEqual,
        "later-than-or-equal": zlocalizerNonFactors1.LaterThanOrEqual
    },
    guid: {
        "equals": zlocalizerNonFactors1.Equals,
        "not-equals": zlocalizerNonFactors1.NotEquals
    },
    filter: {
        "apply": "&#10003;",
        "remove": "&#10008;"
    },
    operator: {
        "select": "",
        "and": zlocalizerNonFactors1.And,
        "or": zlocalizerNonFactors1.Or
    }
};