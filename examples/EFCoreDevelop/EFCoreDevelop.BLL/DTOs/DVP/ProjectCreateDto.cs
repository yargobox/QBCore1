using Develop.Entities.DVP;
using QBCore.DataSource;
using QBCore.DataSource.QueryBuilder.EfCore;

namespace Develop.DTOs.DVP;

public class ProjectCreateDto
{
	[DeName] public string Name { get; set; } = string.Empty;
	public string Desc { get; set; } = string.Empty;

	static void Builder(IEfCoreInsertQBBuilder<Project, ProjectCreateDto> builder)
	{
		builder.AutoBuild("projects");
	}
}