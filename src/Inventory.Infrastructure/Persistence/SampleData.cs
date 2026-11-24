using Inventory.Domain;

namespace Inventory.Infrastructure.Persistence;

/// <summary>Sample catalog so the UI has something to show on first run. Built through the domain, so it obeys the same rules.</summary>
internal static class SampleData
{
    private static readonly (string Sku, string Name, int Minimum, int[] Changes)[] Catalog =
    [
        ("KB-100", "Mechanical keyboard", 10, [40, -12, -9, -5]),
        ("MS-200", "Wireless mouse", 15, [60, -18, -20, 10, -7]),
        ("MN-270", "27\" 4K monitor", 4, [12, -3, -2, -4]),
        ("HS-310", "Noise-cancelling headset", 8, [25, -6, -4]),
        ("CB-USB-C", "USB-C cable 2 m", 50, [200, -80, -45, -38]),
        ("DK-400", "USB-C docking station", 5, [15, -4, -3]),
        ("WC-1080", "1080p webcam", 6, [20, -9, -8]),
        ("LP-STAND", "Aluminium laptop stand", 5, [18, -2, -1]),
        ("SSD-1TB", "NVMe SSD 1 TB", 12, [30, -10, -14]),
        ("RT-AX", "Wi-Fi 6 router", 3, [8, -3, -3]),
        ("CH-ERG", "Ergonomic office chair", 2, [6, -1]),
        ("PD-XL", "XL desk pad", 10, [10, -10, 25, -4]),
    ];

    public static IReadOnlyList<Product> Create(DateTimeOffset now, out IReadOnlyList<StockMovement> movements)
    {
        var products = new List<Product>();
        var history = new List<StockMovement>();
        var start = now.AddDays(-Catalog.Length - 14);

        for (var i = 0; i < Catalog.Length; i++)
        {
            var (sku, name, minimum, changes) = Catalog[i];
            var time = start.AddDays(i);
            var product = Product.Register(Sku.Create(sku), name, minimum, time);

            for (var c = 0; c < changes.Length; c++)
            {
                time = time.AddHours(7 + (c * 13));
                if (changes[c] > 0)
                {
                    product.Receive(changes[c], c == 0 ? "Initial stock" : "Supplier delivery", time);
                }
                else
                {
                    product.Issue(-changes[c], $"Sales order {1000 + (i * 10) + c}", time);
                }
            }

            products.Add(product);
            history.AddRange(product.NewMovements);
        }

        movements = history;
        return products;
    }
}
