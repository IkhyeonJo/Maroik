using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.Tests.Fixtures;

namespace Maroik.Core.Domain.Tests.Primitives;

/// <summary>Unit tests for <see cref="TitledContentPolicy"/>: the bound on the inline images of a body.</summary>
public class TitledContentPolicyTests
{
    /// <summary>
    /// A body holds at most <see cref="TitledContentPolicy.MaxInlineImages"/> images: every view fetches each of them whole
    /// and embeds it as base64, so the count bounds what one view costs.
    /// </summary>
    [Fact]
    public void CheckInlineImageCount_AllowsUpToTheLimit_AndRefusesOneMore()
    {
        Assert.Equal(20, TitledContentPolicy.MaxInlineImages);

        Assert.False(TitledContentPolicy.CheckInlineImageCount(0).IsError);
        Assert.False(TitledContentPolicy.CheckInlineImageCount(TitledContentPolicy.MaxInlineImages).IsError);
        ErrorAssert.Validation(TitledContentPolicy.CheckInlineImageCount(TitledContentPolicy.MaxInlineImages + 1),
            "Content.TooManyImages", "You can insert at most {0} images.", TitledContentPolicy.MaxInlineImages);
    }
}
