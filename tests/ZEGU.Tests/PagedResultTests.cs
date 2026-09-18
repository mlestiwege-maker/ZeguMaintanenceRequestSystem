using ZEGU.WebApp.ViewModels;

namespace ZEGU.Tests;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_RoundsUpForPartialLastPage()
    {
        var result = new PagedResult<int> { TotalCount = 25, PageSize = 10 };

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ExactMultiple_DoesNotAddExtraPage()
    {
        var result = new PagedResult<int> { TotalCount = 20, PageSize = 10 };

        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public void HasPreviousPage_FalseOnFirstPage()
    {
        var result = new PagedResult<int> { PageNumber = 1, TotalCount = 30, PageSize = 10 };

        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_TrueAfterFirstPage()
    {
        var result = new PagedResult<int> { PageNumber = 2, TotalCount = 30, PageSize = 10 };

        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_FalseOnLastPage()
    {
        var result = new PagedResult<int> { PageNumber = 3, TotalCount = 25, PageSize = 10 };

        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void HasNextPage_TrueBeforeLastPage()
    {
        var result = new PagedResult<int> { PageNumber = 2, TotalCount = 25, PageSize = 10 };

        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void EmptyResult_HasOneOrZeroPagesAndNoNavigation()
    {
        var result = new PagedResult<int> { PageNumber = 1, TotalCount = 0, PageSize = 10 };

        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_IsAssignableToIPagingInfo_ForAnyItemType()
    {
        IPagingInfo paging = new PagedResult<string> { PageNumber = 1, TotalCount = 5, PageSize = 10 };

        Assert.Equal(1, paging.TotalPages);
    }
}
