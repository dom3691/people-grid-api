namespace PeopleGrid.Shared.Pagination;

public sealed record PaginationRequest(int PageNumber = 1, int PageSize = 10)
{
    public int Skip => (Math.Max(PageNumber, 1) - 1) * Math.Max(PageSize, 1);
    public int Take => Math.Clamp(PageSize, 1, 200);
}
