using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Develop.DAL.PostgreSQL;
using QBCore.DataSource;
using System.ComponentModel.DataAnnotations.Schema;

namespace Develop.Entities.DVP;

public record struct DataEntryTranslationID
{
	public int DataEntryId { get; set; }
	public int LanguageId { get; set; }

	public DataEntryTranslationID() { }
	public DataEntryTranslationID(int DataEntryId, int LanguageId)
	{
		this.DataEntryId = DataEntryId;
		this.LanguageId = LanguageId;
	}
}

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

	private int DataEntryId { get; set; }
	private string RefKey { get => nameof(DataEntry); set { } }
	public string Name { get; set; } = string.Empty;
	public string? Desc { get; set; }
	public DateTime Inserted { get; set; }
	public DateTime? Updated { get; set; }
	public DateTime? Deleted { get; set; }

	private int LanguageId { get; set; }
	public virtual Language Language { get; set; } = null!;

	public virtual DataEntry? DataEntry { get; set; } = null!;


	internal class EntityTypeConfiguration : PluralNamingConfiguration<DataEntryTranslation>
	{
		public override string ObjectName => $"{ToPlural(nameof(DataEntry))}By{ToPlural(nameof(Translation))}";

		public override void Configure(EntityTypeBuilder<DataEntryTranslation> builder)
		{
			builder
				.ToTable(ObjectName, SchemaName) // Replace it with a view in migration!!! SELECT * FROM dvp.""Translations"" WHERE ""RefKey"" = 'DataEntry'
				.HasKey(x => new { x.DataEntryId, x.LanguageId });

			builder
				.HasOne(x => x.Language)
				.WithMany(x => x.DataEntryTranslations);

			builder
				.HasOne(x => x.DataEntry)
				.WithMany(x => x.DataEntryTranslations);

			builder.HasIndex(x => x.Deleted).HasFilterNotNull(x => x.Deleted);

			builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
			builder.Property(x => x.Desc).HasMaxLength(400);
			builder.Property(x => x.Inserted).HasDefaultDateTimeNowConstraint();

			builder.Property(x => x.DataEntryId).ValueGeneratedNever().IsRequired().HasColumnName(nameof(Translation.RefId));
			builder.Property(x => x.LanguageId).ValueGeneratedNever().IsRequired();
			builder.Property(x => x.RefKey).ValueGeneratedNever().IsRequired();
		}
	}
}