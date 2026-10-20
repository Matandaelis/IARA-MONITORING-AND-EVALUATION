using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    public partial class AddBaselineEntityAggregationAndWeights : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "LogFrameRowIndicators",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Baseline",
                table: "Indicators",
                type: "decimal(20,8)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntityAggregationType",
                table: "Indicators",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
IF EXISTS(SELECT * FROM SYSOBJECTS WHERE XTYPE = 'P' AND NAME = 'Aggregate')
	DROP PROC Aggregate;
");

            migrationBuilder.Sql(@"CREATE PROC [dbo].[Aggregate]
	@IndicatorIds dbo.GuidIds READONLY,
	@EntityIds dbo.GuidIds READONLY,
	@DateIds dbo.GuidIds READONLY,
	@UserId uniqueidentifier
AS
SET NOCOUNT ON;
SET XACT_ABORT ON; -- protect against client timeouts

/* --- EXPLAINER FOR AGGREGATION LOGIC: -----------------------------

There are 2 basic steps that aggregation will occur in: 
   (A) source entities up the date hierarchy, and then 
   (B) source entities to higher entities

Suppose you are capturing data at Scheme & Month, and there is a higher entity Cluster, and a higher date Year; you have this situation (matrix):

			|	DATE:		DATE:
ENTITY:		|  MONTH		YEAR	
---------------------------------
SCHEME		|     X   --->   A
			|     |          |
			|     v          v
CLUSTER		|     B1         B2

Steps:
	[X] Data entry (non-aggregated data)
	[A] Aggregate by Date: from the source entity type & date, aggregate to higher level dates. This can be a SUM or MOST RECENT aggregation.
	[B 1&2] Aggregate by Entity: This is always a SUM aggregation, from the source entity type to higher entity types.

*/

BEGIN TRY
	BEGIN TRANSACTION
	
	DECLARE @AggregationType_SUM tinyint = 0,
			@AggregationType_MOSTRECENT tinyint = 1,
			@EntityAggregationType_SUM tinyint = 0,
			@EntityAggregationType_AVERAGE tinyint = 1,
			@EntityAggregationType_NONE tinyint = 2,
			@DateType_YEAR tinyint = 0,
			@DateType_QUARTER tinyint = 1,
			@DateType_MONTH tinyint = 2,
			@step nvarchar(100)

	-- this table param stores all the values that will be used in various filters during aggregation
	DECLARE @filters TABLE (
		SourceIndicatorId uniqueidentifier NOT NULL,
		EntityId uniqueidentifier NOT NULL,
		DateId uniqueidentifier NOT NULL,
		Frequency int NOT NULL,
		DateAggregationType int NOT NULL,
		EntityAggregationType int NOT NULL,
		IndicatorEntityTypeId uniqueidentifier NOT NULL,
		EntityEntityTypeId uniqueidentifier NOT NULL,
		DateType int NOT NULL,
		QuarterId uniqueidentifier NULL,
		YearId uniqueidentifier NULL,
		SortOrder int NOT NULL
		PRIMARY KEY (SourceIndicatorId, EntityId, DateId)
	);

	-- populate the filters with all the required data
	INSERT 
	INTO		@filters
	SELECT		d.Id, 
				en.Id, 
				da.Id, 
				Frequency, 
				DateAggregationType, 
				COALESCE(si.EntityAggregationType, 0),
				si.EntityTypeId, 
				entities.EntityTypeId, 
				dates.DateType, 
				dates.QuarterId, 
				dates.YearId, 
				dates.SortOrder
	FROM		@IndicatorIds d
	CROSS JOIN	@EntityIds en
	CROSS JOIN	@DateIds da
	INNER JOIN	Indicators si ON d.Id = si.IndicatorId
	INNER JOIN	Entities entities ON en.Id = entities.EntityId
	INNER JOIN	Dates dates ON da.Id = dates.DateId

	-----------------------------------------------------------------------------------------------------------------------------------------------------

	SET @step = 'AGGREGATE.DATE(MOSTRECENT)->Q'

	-- a) populate temp table
	;WITH cte AS 
	(
		SELECT		d.IndicatorId,
					d.EntityId, 			-- keep entity fixed
					f.DateId,				-- aggregate to quarter
					d.Value, 
					ROW_NUMBER() OVER (
						PARTITION BY 
							f.SourceIndicatorId, 
							f.EntityId, 
							f.DateId		-- partition/GROUP BY quarter
						ORDER BY 
							da.SortOrder DESC -- most recent will have row = 1
					) as RowNumber
		FROM		Data d
		INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId
		INNER JOIN	Entities en on d.EntityId = en.EntityId
		INNER JOIN	Dates da on d.DateId = da.DateId
		WHERE		f.DateAggregationType = @AggregationType_MOSTRECENT
		AND			f.Frequency = @DateType_MONTH
		AND			f.DateType = @DateType_QUARTER
		AND			da.QuarterId = f.DateId
		AND			f.EntityEntityTypeId = f.IndicatorEntityTypeId
		AND			d.Aggregated = 0
		AND			d.Value IS NOT NULL -- todo: maybe a setting on the indicator to determine if nulls should be overlooked or not?
	)
	SELECT		IndicatorId,
				EntityId, 
				DateId, 
				Value
	INTO		#mostrecent_q
	FROM		cte
	-- filter to the most recent record only (date.sortOrder Descending) - see inner row_number()
	WHERE		RowNumber = 1;

	-- b) apply from temp table to data table
	;WITH	d 
	AS (
			SELECT		d.*
			FROM		Data d
			INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId AND d.DateId = f.DateId
			INNER JOIN	Entities en on d.EntityId = en.EntityId
			WHERE		Aggregated = 1
			AND			f.DateAggregationType = @AggregationType_MOSTRECENT
			AND			f.Frequency = @DateType_MONTH
			AND			f.DateType = @DateType_QUARTER
			AND			f.EntityEntityTypeId = f.IndicatorEntityTypeId
		)
	MERGE	d
	USING	(
		SELECT			*
		FROM			#mostrecent_q
	) source
	ON		d.IndicatorId = source.IndicatorId
	AND		d.EntityId = source.EntityId
	AND		d.DateId = source.DateId
	WHEN MATCHED THEN
		UPDATE SET 
			d.Value = source.Value,
			d.Note = @step,
			d.LastSavedDateUtc = GETUTCDATE(),
			d.LastSavedById = @UserId
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (IndicatorId, EntityId, DateId, Value, Note, Aggregated, LastSavedDateUtc, LastSavedById)
		VALUES (source.IndicatorId, source.EntityId, source.DateId, source.Value, @step, 1, GETUTCDATE(), @UserId)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE;

	-----------------------------------------------------------------------------------------------------------------------------------------------------

	SET @step = 'AGGREGATE.DATE(MOSTRECENT)->Y'

	-- a) populate temp table
	;WITH cte AS 
	(
		SELECT		d.IndicatorId,
					d.EntityId, 			-- keep entity fixed
					f.DateId,				-- aggregate to year
					d.Value, 
					ROW_NUMBER() OVER (
						PARTITION BY 
							f.SourceIndicatorId, 
							f.EntityId, 
							f.DateId		-- partition/GROUP BY year
						ORDER BY 
							da.SortOrder DESC -- most recent will have row = 1
					) as RowNumber
		FROM		Data d
		INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId
		INNER JOIN	Entities en on d.EntityId = en.EntityId
		INNER JOIN	Dates da on d.DateId = da.DateId
		WHERE		f.DateAggregationType = @AggregationType_MOSTRECENT
		AND			f.Frequency IN (@DateType_MONTH, @DateType_QUARTER)
		AND			f.DateType = @DateType_YEAR
		AND			da.YearId = f.DateId
		AND			f.EntityEntityTypeId = f.IndicatorEntityTypeId
		AND			d.Aggregated = 0
		AND			d.Value IS NOT NULL -- todo: maybe a setting on the indicator to determine if nulls should be overlooked or not?
	)
	SELECT		IndicatorId,
				EntityId, 
				DateId, 
				Value
	INTO		#mostrecent_y
	FROM		cte
	-- filter to the most recent record only (date.sortOrder Descending) - see inner row_number()
	WHERE		RowNumber = 1;

	-- b) apply from temp table to data table
	;WITH	d 
	AS (
			SELECT		d.*
			FROM		Data d
			INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId AND d.DateId = f.DateId
			INNER JOIN	Entities en on d.EntityId = en.EntityId
			WHERE		Aggregated = 1
			AND			f.DateAggregationType = @AggregationType_MOSTRECENT
			AND			f.Frequency IN (@DateType_MONTH, @DateType_QUARTER)
			AND			f.DateType = @DateType_YEAR
			AND			f.EntityEntityTypeId = f.IndicatorEntityTypeId
		)
	MERGE	d
	USING	(
		SELECT			*
		FROM			#mostrecent_y
	) source
	ON		d.IndicatorId = source.IndicatorId
	AND		d.EntityId = source.EntityId
	AND		d.DateId = source.DateId
	WHEN MATCHED THEN
		UPDATE SET 
			d.Value = source.Value,
			d.Note = @step,
			d.LastSavedDateUtc = GETUTCDATE(),
			d.LastSavedById = @UserId
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (IndicatorId, EntityId, DateId, Value, Note, Aggregated, LastSavedDateUtc, LastSavedById)
		VALUES (source.IndicatorId, source.EntityId, source.DateId, source.Value, @step, 1, GETUTCDATE(), @UserId)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE;
		
	-----------------------------------------------------------------------------------------------------------------------------------------------------

	SET @step = 'AGGREGATE.DATE(SUM)->Q'

	;WITH	d 
	AS (
			SELECT		d.*
			FROM		Data d
			INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId AND d.DateId = f.DateId
			INNER JOIN	Entities en on d.EntityId = en.EntityId
			WHERE		Aggregated = 1
			AND			f.DateAggregationType = @AggregationType_SUM
			AND			f.Frequency = @DateType_MONTH
			AND			f.DateType = @DateType_QUARTER
			AND			f.EntityEntityTypeId = f.IndicatorEntityTypeId
		)
	MERGE	d
	USING	(
			SELECT		d.IndicatorId, 
						d.EntityId, 
						f.DateId, 
						SUM(d.value) Value
			FROM		Data d
			INNER JOIN	Dates da ON d.DateId = da.DateId
			INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId
			WHERE		da.DateType = f.Frequency -- dates that are the collection frequency
			AND			d.Aggregated = 0
			AND			f.EntityEntityTypeId = f.IndicatorEntityTypeId
			AND			da.QuarterId = f.DateId
			AND			f.DateAggregationType = @AggregationType_SUM -- 0: sum, 1: most recent
			AND			f.Frequency = @DateType_MONTH
			AND			d.Value IS NOT NULL
			GROUP BY	d.IndicatorId, 
						d.EntityId, 
						f.DateId
	) source
	ON		d.IndicatorId = source.IndicatorId
	AND		d.EntityId = source.EntityId
	AND		d.DateId = source.DateId
	WHEN MATCHED THEN
		UPDATE SET 
			d.Value = source.Value,
			d.Note = @step,
			d.LastSavedDateUtc = GETUTCDATE(),
			d.LastSavedById = @UserId
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (IndicatorId, EntityId, DateId, Value, Note, Aggregated, LastSavedDateUtc, LastSavedById)
		VALUES (source.IndicatorId, source.EntityId, source.DateId, source.Value, @step, 1, GETUTCDATE(), @UserId)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE;

	-----------------------------------------------------------------------------------------------------------------------------------------------------

	SET @step = 'AGGREGATE.DATE(SUM)->Y'

	;WITH	d 
	AS (
			SELECT		d.*
			FROM		Data d
			INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId AND d.DateId = f.DateId
			INNER JOIN	Entities en on d.EntityId = en.EntityId
			-- targeting: Aggregated Year data for indicators that are MostRecentSUMs collected at Month/Quarter at the collection entity type
			WHERE		Aggregated = 1
			AND			f.DateAggregationType = @AggregationType_SUM
			AND			f.Frequency IN (@DateType_MONTH, @DateType_QUARTER)
			AND			f.DateType = @DateType_YEAR
			AND			f.EntityEntityTypeId = f.IndicatorEntityTypeId
		)
	MERGE	d
	USING	(
			SELECT		d.IndicatorId, 
						d.EntityId, 
						f.DateId, 
						SUM(d.value) Value
			FROM		Data d
			INNER JOIN	Dates da ON d.DateId = da.DateId
			INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId
			WHERE		da.DateType = f.Frequency -- dates that are the collection frequency
			AND			d.Aggregated = 0
			AND			f.EntityEntityTypeId = f.IndicatorEntityTypeId
			AND			da.YearId = f.DateId
			AND			f.DateAggregationType = @AggregationType_SUM -- 0: sum, 1: most recent
			AND			f.Frequency IN (@DateType_MONTH, @DateType_QUARTER)
			AND			d.Value IS NOT NULL
			GROUP BY	d.IndicatorId, 
						d.EntityId, 
						f.DateId
	) source
	ON		d.IndicatorId = source.IndicatorId
	AND		d.EntityId = source.EntityId
	AND		d.DateId = source.DateId
	WHEN MATCHED THEN
		UPDATE SET 
			d.Value = source.Value,
			d.Note = @step,
			d.LastSavedDateUtc = GETUTCDATE(),
			d.LastSavedById = @UserId
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (IndicatorId, EntityId, DateId, Value, Note, Aggregated, LastSavedDateUtc, LastSavedById)
		VALUES (source.IndicatorId, source.EntityId, source.DateId, source.Value, @step, 1, GETUTCDATE(), @UserId)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE;

	-----------------------------------------------------------------------------------------------------------------------------------------------------

	SET @step = 'AGGREGATE.ENTITY(SUM)'

	;WITH	d AS ( 
		SELECT		d.*
		FROM		Data d
		INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId AND d.DateId = f.DateId
		-- restrict to: aggregated data for the same date frequency but different entitytypes, and EntityAggregationType = SUM
		WHERE		Aggregated = 1
		AND			f.EntityEntityTypeId <> f.IndicatorEntityTypeId
		AND			f.EntityAggregationType = @EntityAggregationType_SUM
	)
	MERGE	d
	USING	(
		SELECT		f.SourceIndicatorId,
					f.EntityId, 
					f.DateId, 
					SUM(d.Value) Value
		FROM		Data d
		INNER JOIN	Entities en ON d.EntityId = en.EntityId
		INNER JOIN	EntityLinks enL ON d.EntityId = enL.ChildEntityId
		INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND enL.ParentEntityId = f.EntityId AND d.DateId = f.DateId
		-- filter: exclude nulls, non-aggregated (source) data at the level of collection frequency
		WHERE		d.Value IS NOT NULL
		AND			en.EntityTypeId = f.IndicatorEntityTypeId
		AND			f.EntityEntityTypeId <> f.IndicatorEntityTypeId
		AND			f.EntityAggregationType = @EntityAggregationType_SUM
		GROUP BY	f.SourceIndicatorId,
					f.EntityId, 
					f.DateId
	) source
	ON		d.IndicatorId = source.SourceIndicatorId
	AND		d.EntityId = source.EntityId
	AND		d.DateId = source.DateId
	WHEN MATCHED THEN
		UPDATE SET 
			d.Value = source.Value,
			d.Note = @step,
			d.LastSavedDateUtc = GETUTCDATE(),
			d.LastSavedById = @UserId
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (IndicatorId, EntityId, DateId, Value, Note, Aggregated, LastSavedDateUtc, LastSavedById)
		VALUES (source.SourceIndicatorId, source.EntityId, source.DateId, source.Value, @step, 1, GETUTCDATE(), @UserId)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE;

	-----------------------------------------------------------------------------------------------------------------------------------------------------

	SET @step = 'AGGREGATE.ENTITY(AVG)'

	;WITH	d AS ( 
		SELECT		d.*
		FROM		Data d
		INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND d.EntityId = f.EntityId AND d.DateId = f.DateId
		-- restrict to: aggregated data for the same date frequency but different entitytypes, and EntityAggregationType = AVERAGE
		WHERE		Aggregated = 1
		AND			f.EntityEntityTypeId <> f.IndicatorEntityTypeId
		AND			f.EntityAggregationType = @EntityAggregationType_AVERAGE
	)
	MERGE	d
	USING	(
		SELECT		f.SourceIndicatorId,
					f.EntityId, 
					f.DateId, 
					AVG(d.Value) Value
		FROM		Data d
		INNER JOIN	Entities en ON d.EntityId = en.EntityId
		INNER JOIN	EntityLinks enL ON d.EntityId = enL.ChildEntityId
		INNER JOIN	@filters f ON d.IndicatorId = f.SourceIndicatorId AND enL.ParentEntityId = f.EntityId AND d.DateId = f.DateId
		-- filter: exclude nulls, non-aggregated (source) data at the level of collection frequency
		WHERE		d.Value IS NOT NULL
		AND			en.EntityTypeId = f.IndicatorEntityTypeId
		AND			f.EntityEntityTypeId <> f.IndicatorEntityTypeId
		AND			f.EntityAggregationType = @EntityAggregationType_AVERAGE
		GROUP BY	f.SourceIndicatorId,
					f.EntityId, 
					f.DateId
	) source
	ON		d.IndicatorId = source.SourceIndicatorId
	AND		d.EntityId = source.EntityId
	AND		d.DateId = source.DateId
	WHEN MATCHED THEN
		UPDATE SET 
			d.Value = source.Value,
			d.Note = @step,
			d.LastSavedDateUtc = GETUTCDATE(),
			d.LastSavedById = @UserId
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (IndicatorId, EntityId, DateId, Value, Note, Aggregated, LastSavedDateUtc, LastSavedById)
		VALUES (source.SourceIndicatorId, source.EntityId, source.DateId, source.Value, @step, 1, GETUTCDATE(), @UserId)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE;

	COMMIT

END TRY
BEGIN CATCH

	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	THROW;

END CATCH
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "LogFrameRowIndicators");

            migrationBuilder.DropColumn(
                name: "Baseline",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "EntityAggregationType",
                table: "Indicators");
        }
    }
}
