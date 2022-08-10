using Example1.DAL.Entities.OrderPositions;
using QBCore.DataSource.QueryBuilder.Mongo;

namespace Example1.DAL.Entities.Orders;

public class OrderCreateDto
{
	public string? Name { get; set; }

	public List<OrderPosition>? OrderPositions { get; set; }

	private static void Builder(IQBMongoInsertBuilder<Order, OrderCreateDto> builder)
	{
		builder.InsertTo("orders").AutoBindParameters();

		builder.CustomIdGenerator = () => new PesemisticSequentialIdGenerator<Order>(1, 1, 8);
	}
}