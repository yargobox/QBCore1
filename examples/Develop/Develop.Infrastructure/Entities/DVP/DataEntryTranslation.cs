using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Develop.DTOs.DVP;
using QBCore.DataSource;

namespace Develop.Entities.DVP;

public class DataEntryTranslation
{
	[DeId, NotMapped, DeDependsOn(nameof(DataEntryId), nameof(LanguageId))]
	public DataEntryTranslationID DataEntryTranslationId
	{
		get => new DataEntryTranslationID(DataEntryId, LanguageId);
		set
		{
			DataEntryId = value.DataEntryId;
			LanguageId = value.LanguageId;
		}
	}

	[Required, Column("RefId")]
	public int DataEntryId { get; set; }

	[DeHidden]
	public string RefKey => "DataEntry";

	[DeName, MaxLength(80), Required]
	public string Name { get; set; } = null!;

	[MaxLength(400)]
	public string? Desc { get; set; }

	[DeCreated, DeReadOnly]
	public DateTime Inserted { get; set; }

	[DeUpdated]
	public DateTime? Updated { get; set; }

	[DeDeleted]
	public DateTime? Deleted { get; set; }

	[DeForeignId, Required]
	public int LanguageId { get; set; }
	//public virtual Language Language { get; set; } = null!;

	//public virtual DataEntry? DataEntry { get; set; } = null!;
}