using Integration.Backend;
using Integration.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private readonly IMemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.
    public Result SaveItem(string itemContent)
    {
        var cacheKey = $"Integration:SaveItem:{itemContent}";

        bool found = MemoryCache.TryGetValue(cacheKey, out string _);

        if (found)
        {
            return new Result(false, $"The request already received with content {itemContent}.");
        }

        MemoryCache.Set(cacheKey, itemContent);

        if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
        {
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }

        var item = ItemIntegrationBackend.SaveItem(itemContent);

        MemoryCache.Remove(cacheKey);

        return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}