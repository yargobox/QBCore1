using QBCore.DataSource.QueryBuilder.Mongo;

namespace Example1.DAL.Entities.Stores;

public class StoreUpdateDto
{
	public string? Name { get; set; }

	static void Builder(IQBMongoUpdateBuilder<Store, StoreUpdateDto> builder)
	{
		builder.Update("stores");
	}
}