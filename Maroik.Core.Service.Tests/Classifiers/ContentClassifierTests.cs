using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Classifiers;

namespace Maroik.Core.Service.Tests.Classifiers;

/// <summary>
/// Unit tests for <see cref="ContentClassifier"/>, the text model behind the expenditure class suggestion: character n-gram
/// TF-IDF features and a logistic regression over all classes (ML.NET), trained on an account's learnable records.
/// </summary>
public class ContentClassifierTests
{
    /// <summary>A learnable sample of <paramref name="text"/> in <paramref name="mainClass"/> / <paramref name="subClass"/>.</summary>
    private static ClassSample Sample(string text, string subClass, string mainClass = "ConsumerSpending") =>
        new(ClassSuggestionPolicy.Normalize(text), new ClassSuggestion(mainClass, subClass));

    /// <summary>A small history with three classes whose content has distinct character patterns.</summary>
    private static List<ClassSample> History() =>
    [
        Sample("스타벅스 역삼점", "MealOrEatOutExpenses"), Sample("스타벅스 강남", "MealOrEatOutExpenses"), Sample("맥도날드 점심", "MealOrEatOutExpenses"),
        Sample("지하철", "TransportationCost"), Sample("지하철 정기권", "TransportationCost"), Sample("버스 교통카드", "TransportationCost"),
        Sample("적금 자동이체", "Deposit", "RegularSavings"), Sample("청년 적금", "Deposit", "RegularSavings"), Sample("적금 납입", "Deposit", "RegularSavings"),
    ];

    /// <summary>
    /// Content never recorded before is classified by the character patterns it shares with the history — written with spaces or
    /// without, and in any letter case — and the class comes as its main class + sub-class.
    /// </summary>
    [Theory]
    [InlineData("스타벅스 판교", "ConsumerSpending", "MealOrEatOutExpenses")]
    [InlineData("스타벅스판교점", "ConsumerSpending", "MealOrEatOutExpenses")]
    [InlineData("지하철 2호선", "ConsumerSpending", "TransportationCost")]
    [InlineData("주택청약 적금", "RegularSavings", "Deposit")]
    public void Predict_ClassifiesNewContent_ByTheCharacterPatternsItShares(string content, string mainClass, string subClass)
    {
        ContentClassifier classifier = ContentClassifier.Train(History());

        Assert.Equal(new ClassSuggestion(mainClass, subClass), classifier.Predict(content));
    }

    /// <summary>A history of one class has nothing to tell apart: every content gets that class.</summary>
    [Fact]
    public void Predict_WithASingleClass_AlwaysAnswersIt()
    {
        ContentClassifier classifier = ContentClassifier.Train([Sample("지하철", "TransportationCost"), Sample("버스", "TransportationCost")]);

        Assert.Equal(new ClassSuggestion("ConsumerSpending", "TransportationCost"), classifier.Predict("월세"));
    }
}
