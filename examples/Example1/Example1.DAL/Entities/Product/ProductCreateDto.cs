using QBCore.DataSource;
using QBCore.DataSource.QueryBuilder.Mongo;

namespace Example1.DAL.Entities.Products;

public class ProductCreateDto
{
	[DeName] public string? Name { get; set; }
	[DeForeignId] public int? ProductId { get; set; }

	static void Builder(IMongoInsertQBBuilder<Product, ProductCreateDto> builder)
	{
		builder.Insert("products");
	}
}