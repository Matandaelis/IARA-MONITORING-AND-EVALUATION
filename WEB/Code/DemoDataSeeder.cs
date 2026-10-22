using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// Result of seeding the demo project. Contains lookup dictionaries (code -> id)
    /// that tests and callers can use to verify values.
    /// </summary>
    public class DemoDataSeedResult
    {
        public Guid OrganisationId { get; internal set; }
        public string OrganisationName { get; internal set; } = "";
        public Guid ProjectId { get; internal set; }
        public Guid CategoryId { get; internal set; }
        public Guid SubcategoryId { get; internal set; }

        public Dictionary<string, Guid> EntityTypeIdsByName { get; } = new Dictionary<string, Guid>();
        public Dictionary<string, Guid> EntityIdsByCode { get; } = new Dictionary<string, Guid>();
        public Dictionary<string, Guid> DateIdsByCode { get; } = new Dictionary<string, Guid>();
        public Dictionary<string, Guid> IndicatorIdsByCode { get; } = new Dictionary<string, Guid>();

        public bool DataAlreadySeeded { get; internal set; }
        public int EnteredDatumCount { get; internal set; }

        /// <summary>Annual collected totals (facility sum) for calendar year 2025, by indicator code.</summary>
        public Dictionary<string, decimal> AnnualTotals2025 { get; } = new Dictionary<string, decimal>();

        /// <summary>Annual collected totals (facility sum) for calendar year 2024, by indicator code.</summary>
        public Dictionary<string, decimal> AnnualTotals2024 { get; } = new Dictionary<string, decimal>();
    }

    /// <summary>
    /// Seeds a complete, realistic demo project ("Rural Health Program 2024-25") with
    /// DETERMINISTIC data values, so that every aggregated & calculated value can be
    /// independently recomputed and verified (numeric accuracy testing).
    ///
    /// The demo exercises:
    ///   - entity roll-ups (Facility -> District -> Region -> Country, via direct ancestor links),
    ///   - date roll-ups (Month -> Quarter -> Year),
    ///   - SUM / AVERAGE / MOSTRECENT aggregation types,
    ///   - calculated indicators (precedence, parentheses, chained formulas, null handling).
    ///
    /// The seeder is idempotent: running it again returns the existing graph and does
    /// not duplicate anything.
    /// </summary>
    public static class DemoDataSeeder
    {
        // ------------------------------------------------------------------ names & codes
        public const string OrganisationName = "Demo Health Org";
        public const string OrganisationCode = "DHO";
        public const string ProjectName = "Rural Health Program 2024-25";
        public const string CategoryName = "Demo: Health Services";
        public const string CategoryCode = "DEMOHSD";
        public const string SubcategoryName = "Demo: Service Outputs";
        public const string SubcategoryCode = "DEMOOUT";

        public const string CountryEntityTypeName = "Demo Country";
        public const string RegionEntityTypeName = "Demo Region";
        public const string DistrictEntityTypeName = "Demo District";
        public const string FacilityEntityTypeName = "Demo Facility";

        public static readonly string[] IndicatorCodesCollected = { "PAT", "REC", "KIT", "STF", "BED" };
        public static readonly string[] IndicatorCodesCalculated = { "TOT", "CPS", "RATE" };

        public static System.Threading.Tasks.Task<DemoDataSeedResult> SeedAsync(
            ApplicationDbContext db,
            Guid userId)
        {
            return SeedInternalAsync(db, userId);
        }

        private static async System.Threading.Tasks.Task<DemoDataSeedResult> SeedInternalAsync(
            ApplicationDbContext db,
            Guid userId)
        {
            // 0) ensure a settings row exists (needed by Calculation)
            if (!await db.Settings.AnyAsync())
            {
                db.Entry(new Models.Settings { Id = Guid.Empty }).State = EntityState.Added;
                await db.SaveChangesAsync();
            }

            var result = new DemoDataSeedResult();

            await LoadOrCreateGraphAsync(db, userId, result);

            // 2) enter deterministic data through the REAL calculation pipeline
            //    (SaveData stored proc -> Aggregate stored proc -> RunCalculations stored proc)
            var facilityIds = result.EntityIdsByCode
                .Where(o => o.Key.StartsWith("F0", StringComparison.Ordinal))
                .Select(o => o.Value)
                .ToList();

            var collectedIds = result.IndicatorIdsByCode
                .Where(o => IndicatorCodesCollected.Contains(o.Key))
                .Select(o => o.Value)
                .ToList();

            var dataAlreadyExists = await db.Data
                .AnyAsync(o => collectedIds.Contains(o.IndicatorId) && facilityIds.Contains(o.EntityId));

            result.DataAlreadySeeded = dataAlreadyExists;

            if (!dataAlreadyExists)
            {
                var calculation = new Calculation(db, new AppSettings(), userId);

                for (int m = 1; m <= DemoExpected.MonthCount; m++)
                {
                    var datums = new List<Datum>();

                    for (int f = 1; f <= DemoExpected.FacilityCount; f++)
                    {
                        var facilityId = result.EntityIdsByCode[$"F00{f}"];
                        var monthId = result.DateIdsByCode[DemoExpected.MonthCode(m)];

                        foreach (var code in IndicatorCodesCollected)
                        {
                            datums.Add(new Datum
                            {
                                IndicatorId = result.IndicatorIdsByCode[code],
                                EntityId = facilityId,
                                DateId = monthId,
                                Value = CollectedValueForEntry(code, f, m),
                                Note = null,
                                LastSavedDateUtc = DateTime.UtcNow,
                                LastSavedById = userId
                            });
                        }
                    }

                    await calculation.SaveAsync(datums);

                    result.EnteredDatumCount += datums.Count;
                }
            }

            return result;
        }

        // ------------------------------------------------------------------ graph creation
        private static async System.Threading.Tasks.Task LoadOrCreateGraphAsync(
            ApplicationDbContext db,
            Guid userId,
            DemoDataSeedResult result)
        {
            // ---- organisation
            var organisation = await db.Organisations
                .FirstOrDefaultAsync(o => o.Code == OrganisationCode && o.Name == OrganisationName);

            if (organisation == null)
            {
                organisation = new Organisation
                {
                    Name = OrganisationName,
                    Code = OrganisationCode
                };
                db.Organisations.Add(organisation);
            }

            result.OrganisationId = organisation.OrganisationId;
            result.OrganisationName = organisation.Name;

            // ---- project + milestones
            var project = await db.Projects
                .Include(o => o.Milestones)
                .FirstOrDefaultAsync(o => o.Name == ProjectName);

            if (project == null)
            {
                project = new Project { Name = ProjectName };
                db.Projects.Add(project);
            }

            result.ProjectId = project.ProjectId;

            var milestoneNames = new[] { "Baseline & Setup", "Mid-term Review", "End of Program Evaluation" };
            for (var i = 0; i < milestoneNames.Length; i++)
            {
                var milestoneName = milestoneNames[i];
                if (project.Milestones.All(o => o.Name != milestoneName))
                {
                    db.Milestones.Add(new Milestone
                    {
                        Name = milestoneName,
                        ProjectId = project.ProjectId,
                        SortOrder = i,
                        Colour = new[] { "#0288d1", "#8e24aa", "#2e7d32" }[i]
                    });
                }
            }

            await db.SaveChangesAsync();

            // ---- entity types
            var entityTypeNames = new Dictionary<string, string>
            {
                { CountryEntityTypeName, "Countries" },
                { RegionEntityTypeName, "Regions" },
                { DistrictEntityTypeName, "Districts" },
                { FacilityEntityTypeName, "Facilities" }
            };

            var sort = 1;
            foreach (var pair in entityTypeNames)
            {
                var entityType = await db.EntityTypes
                    .FirstOrDefaultAsync(o => o.Name == pair.Key);

                if (entityType == null)
                {
                    entityType = new EntityType
                    {
                        Name = pair.Key,
                        Plural = pair.Value,
                        SortOrder = sort
                    };
                    db.EntityTypes.Add(entityType);
                }

                result.EntityTypeIdsByName[pair.Key] = entityType.EntityTypeId;
                sort++;
            }

            await db.SaveChangesAsync();

            // ---- entities
            var countryTypeId = result.EntityTypeIdsByName[CountryEntityTypeName];
            var regionTypeId = result.EntityTypeIdsByName[RegionEntityTypeName];
            var districtTypeId = result.EntityTypeIdsByName[DistrictEntityTypeName];
            var facilityTypeId = result.EntityTypeIdsByName[FacilityEntityTypeName];

            var entityPlan = new List<(string Code, string Name, string ShortName, Guid EntityTypeId)>
            {
                (DemoExpected.CountryCode, "Demo Country", "Country", countryTypeId)
            };

            for (var r = 0; r < DemoExpected.RegionCodes.Length; r++)
                entityPlan.Add((DemoExpected.RegionCodes[r], $"The {(r == 0 ? "North" : "South")} Region", DemoExpected.RegionCodes[r], regionTypeId));

            for (var d = 0; d < DemoExpected.DistrictCodes.Length; d++)
            {
                var districtNames = new[] { "Alpha", "Beta", "Gamma", "Delta" };
                entityPlan.Add((DemoExpected.DistrictCodes[d], $"The {districtNames[d]} District", DemoExpected.DistrictCodes[d], districtTypeId));
            }

            for (var f = 1; f <= DemoExpected.FacilityCount; f++)
                entityPlan.Add(($"F00{f}", $"Facility {f:00}", $"Fac {f:00}", facilityTypeId));

            foreach (var plan in entityPlan)
            {
                var entity = await db.Entities
                    .FirstOrDefaultAsync(o => o.Code == plan.Code && o.OrganisationId == result.OrganisationId);

                if (entity == null)
                {
                    entity = new Entity
                    {
                        Code = plan.Code,
                        Name = plan.Name,
                        ShortName = plan.ShortName,
                        EntityTypeId = plan.EntityTypeId,
                        OrganisationId = result.OrganisationId,
                        Disabled = false
                    };
                    db.Entities.Add(entity);

                    // entities are also Items (custom fields / documents)
                    db.Items.Add(new Item
                    {
                        ItemId = entity.EntityId,
                        ItemType = ItemType.Entity
                    });
                }

                result.EntityIdsByCode[plan.Code] = entity.EntityId;
            }

            await db.SaveChangesAsync();

            // ---- aggregate links: each entity links DIRECTLY to ALL of its ancestors
            //      (this is how multi-level entity aggregation works: the Aggregate proc
            //       sums the data of entities of the indicator's own entity type straight
            //       into every directly-linked parent)
            var links = new List<(string Child, string Parent)>();

            foreach (var regionCode in DemoExpected.RegionCodes)
                links.Add((regionCode, DemoExpected.CountryCode));

            for (var d = 0; d < DemoExpected.DistrictCodes.Length; d++)
            {
                var districtCode = DemoExpected.DistrictCodes[d];
                var regionCode = DemoExpected.RegionCodes[DemoDataHelper.DistrictRegionIndex(d)];
                links.Add((districtCode, regionCode));
                links.Add((districtCode, DemoExpected.CountryCode));
            }

            for (var f = 1; f <= DemoExpected.FacilityCount; f++)
            {
                var facilityCode = $"F00{f}";
                links.Add((facilityCode, DemoExpected.DistrictCodes[DemoDataHelper.FacilityDistrictIndex(f)]));
                links.Add((facilityCode, DemoExpected.RegionCodes[DemoDataHelper.FacilityRegionIndex(f)]));
                links.Add((facilityCode, DemoExpected.CountryCode));
            }

            foreach (var link in links)
            {
                var childId = result.EntityIdsByCode[link.Child];
                var parentId = result.EntityIdsByCode[link.Parent];

                var exists = await db.EntityLinks
                    .AnyAsync(o => o.ChildEntityId == childId && o.ParentEntityId == parentId);

                if (!exists)
                    db.EntityLinks.Add(new EntityLink { ChildEntityId = childId, ParentEntityId = parentId });
            }

            await db.SaveChangesAsync();

            // ---- dates (years -> quarters -> months)
            for (var y = 0; y < 2; y++)
            {
                var year = 2024 + y;
                var yearCode = year.ToString();

                var yearDate = await db.Dates.FirstOrDefaultAsync(o => o.Code == yearCode);
                if (yearDate == null)
                {
                    yearDate = new Date
                    {
                        Name = yearCode,
                        Code = yearCode,
                        DateType = DateType.Year,
                        OpenFrom = new DateTime(year, 1, 1),
                        OpenTo = new DateTime(year, 12, 31),
                        SortOrder = y + 1
                    };
                    db.Dates.Add(yearDate);
                    await db.SaveChangesAsync();
                }

                result.DateIdsByCode[yearCode] = yearDate.DateId;

                for (var q = 0; q < 4; q++)
                {
                    var quarterCode = $"{year}-Q{q + 1}";

                    var quarterDate = await db.Dates.FirstOrDefaultAsync(o => o.Code == quarterCode);
                    if (quarterDate == null)
                    {
                        var quarterStartMonth = q * 3 + 1;
                        quarterDate = new Date
                        {
                            Name = $"{year} Q{q + 1}",
                            Code = quarterCode,
                            DateType = DateType.Quarter,
                            YearId = yearDate.DateId,
                            OpenFrom = new DateTime(year, quarterStartMonth, 1),
                            OpenTo = new DateTime(year, quarterStartMonth + 2, 1).AddDays(-1),
                            SortOrder = y * 4 + q + 1
                        };
                        db.Dates.Add(quarterDate);
                        await db.SaveChangesAsync();
                    }

                    result.DateIdsByCode[quarterCode] = quarterDate.DateId;
                }
            }

            var monthNames = new[]
            {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            };

            for (var m = 1; m <= DemoExpected.MonthCount; m++)
            {
                var monthCode = DemoExpected.MonthCode(m);
                var year = DemoExpected.YearOf(m);
                var monthInYear = (m - 1) % 12;
                var quarterCode = DemoExpected.QuarterCode(m);

                var monthDate = await db.Dates.FirstOrDefaultAsync(o => o.Code == monthCode);
                if (monthDate == null)
                {
                    var daysInMonth = DateTime.DaysInMonth(year, monthInYear + 1);

                    monthDate = new Date
                    {
                        Name = $"{monthNames[monthInYear]} {year}",
                        Code = monthCode,
                        DateType = DateType.Month,
                        QuarterId = result.DateIdsByCode[quarterCode],
                        YearId = result.DateIdsByCode[year.ToString()],
                        OpenFrom = new DateTime(year, monthInYear + 1, 1),
                        OpenTo = new DateTime(year, monthInYear + 1, daysInMonth),
                        SortOrder = m
                    };
                    db.Dates.Add(monthDate);
                    await db.SaveChangesAsync();
                }

                result.DateIdsByCode[monthCode] = monthDate.DateId;
            }

            // ---- category & subcategory
            var category = await db.Categories.FirstOrDefaultAsync(o => o.Code == CategoryCode);
            if (category == null)
            {
                category = new Category
                {
                    Name = CategoryName,
                    Code = CategoryCode,
                    SortOrder = 999
                };
                db.Categories.Add(category);
                await db.SaveChangesAsync();
            }

            var subcategory = await db.Subcategories.FirstOrDefaultAsync(o => o.Code == SubcategoryCode);
            if (subcategory == null)
            {
                subcategory = new Subcategory
                {
                    CategoryId = category.CategoryId,
                    Name = SubcategoryName,
                    Code = SubcategoryCode,
                    DataEntrySubtotal = true,
                    SortOrder = 1
                };
                db.Subcategories.Add(subcategory);
                await db.SaveChangesAsync();
            }

            result.CategoryId = category.CategoryId;
            result.SubcategoryId = subcategory.SubcategoryId;

            // ---- indicators
            await LoadOrCreateIndicatorAsync(db, userId, result, "PAT", "Patients Treated", "patients", IndicatorType.Collected,
                AggregationType.Sum, EntityAggregationType.Sum, DataType.Number, 0, sortOrder: 1);

            await LoadOrCreateIndicatorAsync(db, userId, result, "REC", "Patients Recovered", "patients", IndicatorType.Collected,
                AggregationType.Sum, EntityAggregationType.Sum, DataType.Number, 0, sortOrder: 2);

            await LoadOrCreateIndicatorAsync(db, userId, result, "KIT", "Health Kits Distributed", "kits", IndicatorType.Collected,
                AggregationType.Sum, EntityAggregationType.Sum, DataType.Number, 0, sortOrder: 3);

            await LoadOrCreateIndicatorAsync(db, userId, result, "STF", "Staff Currently Deployed", "staff", IndicatorType.Collected,
                AggregationType.MostRecent, EntityAggregationType.Sum, DataType.Number, 0, sortOrder: 4);

            await LoadOrCreateIndicatorAsync(db, userId, result, "BED", "Inpatient Bed-Days", "bed-days", IndicatorType.Collected,
                AggregationType.Sum, EntityAggregationType.Average, DataType.Number, 0, sortOrder: 5);

            // calculated, formula: PAT * 2 + KIT  (tests operator precedence)
            await CreateCalculatedIndicatorAsync(db, userId, result, "TOT", "Total Health Contacts", "contacts", 6,
                new[]
                {
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = result.IndicatorIdsByCode["PAT"], ConvertNullToZero = false },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 2, TokenType = TokenType.Operator, OperatorType = OperatorType.Multiply },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 3, TokenType = TokenType.Number, Number = 2 },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 4, TokenType = TokenType.Operator, OperatorType = OperatorType.Add },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 5, TokenType = TokenType.Indicator, SourceIndicatorId = result.IndicatorIdsByCode["KIT"], ConvertNullToZero = false }
                });

            // calculated, formula: TOT / STF  (tests a chained calculated indicator)
            await CreateCalculatedIndicatorAsync(db, userId, result, "CPS", "Contacts per Staff Member", "contacts/staff", 7,
                new[]
                {
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = result.IndicatorIdsByCode["TOT"], ConvertNullToZero = false },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 2, TokenType = TokenType.Operator, OperatorType = OperatorType.Divide },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 3, TokenType = TokenType.Indicator, SourceIndicatorId = result.IndicatorIdsByCode["STF"], ConvertNullToZero = true }
                });

            // calculated, formula: (REC / PAT) * 100  (tests parentheses + null-to-zero)
            await CreateCalculatedIndicatorAsync(db, userId, result, "RATE", "Recovery Rate", "%", 8,
                new[]
                {
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 1, TokenType = TokenType.Parenthesis, ParenthesisType = ParenthesisType.Open },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 2, TokenType = TokenType.Indicator, SourceIndicatorId = result.IndicatorIdsByCode["REC"], ConvertNullToZero = true },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 3, TokenType = TokenType.Operator, OperatorType = OperatorType.Divide },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 4, TokenType = TokenType.Indicator, SourceIndicatorId = result.IndicatorIdsByCode["PAT"], ConvertNullToZero = true },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 5, TokenType = TokenType.Parenthesis, ParenthesisType = ParenthesisType.Close },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 6, TokenType = TokenType.Operator, OperatorType = OperatorType.Multiply },
                    new Token { IndicatorId = Guid.Empty, TokenNumber = 7, TokenType = TokenType.Number, Number = 100 }
                });

            // ---- an annual target for PAT (for dashboards / logframe variance): 110% of expected 2025 total
            var patId = result.IndicatorIdsByCode["PAT"];
            var yearId2025 = result.DateIdsByCode["2025"];
            var targetValue = Math.Round(result.AnnualTotals2025["PAT"] * 1.1m, 0);

            var targetExists = await db.IndicatorTargets
                .AnyAsync(o => o.IndicatorId == patId && o.DateId == yearId2025 && o.EntityId == null);

            if (!targetExists)
            {
                db.IndicatorTargets.Add(new IndicatorTarget
                {
                    IndicatorTargetId = Guid.NewGuid(),
                    IndicatorId = patId,
                    DateId = yearId2025,
                    EntityId = null,
                    TargetValue = targetValue,
                    BaselineValue = result.AnnualTotals2024["PAT"],
                    Notes = "Demo target: 110% of the exact expected 2025 annual total.",
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        private static decimal? CollectedValueForEntry(string code, int f, int m)
        {
            return DemoExpected.CollectedRaw(code, f, m);
        }

        private static async System.Threading.Tasks.Task LoadOrCreateIndicatorAsync(
            ApplicationDbContext db,
            Guid userId,
            DemoDataSeedResult result,
            string code,
            string name,
            string units,
            IndicatorType indicatorType,
            AggregationType dateAggregation,
            EntityAggregationType entityAggregation,
            DataType dataType,
            byte decimalPlaces,
            int sortOrder)
        {
            var indicator = await db.Indicators
                .Include(o => o.Tokens)
                .FirstOrDefaultAsync(o => o.SubcategoryId == result.SubcategoryId && o.Code == code);

            result.AnnualTotals2024[code] = DemoExpected.TotalAcrossFacilitiesAndMonths(code, DemoExpected.AllFacilityIndexes(), 1, 12);
            result.AnnualTotals2025[code] = DemoExpected.TotalAcrossFacilitiesAndMonths(code, DemoExpected.AllFacilityIndexes(), 13, 24);

            var facilityTypeId = result.EntityTypeIdsByName[FacilityEntityTypeName];

            if (indicator == null)
            {
                indicator = new Indicator
                {
                    SubcategoryId = result.SubcategoryId,
                    Name = name,
                    Code = code,
                    Units = units,
                    IndicatorType = indicatorType,
                    IndicatorStatus = IndicatorStatus.Enabled,
                    RequiresSubmit = false,
                    RequiresVerify = false,
                    RequiresApprove = false,
                    LowerIsBetter = false,
                    UseIndicatorDates = false,
                    DisableNote = false,
                    EntityTypeId = facilityTypeId,
                    Frequency = DateType.Month,
                    DateAggregationType = dateAggregation,
                    EntityAggregationType = entityAggregation,
                    DataType = dataType,
                    DecimalPlaces = decimalPlaces,
                    Prefix = "",
                    Suffix = "",
                    SortOrder = sortOrder,
                    ShortName = name,
                    CreatedDateUtc = DateTime.UtcNow,
                    CreatedById = userId,
                    LastSavedDateUtc = DateTime.UtcNow,
                    LastSavedById = userId
                };

                if (code == "PAT")
                {
                    indicator.Baseline = result.AnnualTotals2024["PAT"];
                }

                db.Indicators.Add(indicator);

                // annual collected totals as unaggregated context targets are NOT added here;
                // only the PAT target (added later in LoadOrCreateGraphAsync) uses them.
                await db.SaveChangesAsync();
            }

            result.IndicatorIdsByCode[code] = indicator.IndicatorId;
        }

        private static async System.Threading.Tasks.Task CreateCalculatedIndicatorAsync(
            ApplicationDbContext db,
            Guid userId,
            DemoDataSeedResult result,
            string code,
            string name,
            string units,
            int sortOrder,
            IEnumerable<Token> tokens)
        {
            var indicator = await db.Indicators
                .Include(o => o.Tokens)
                .FirstOrDefaultAsync(o => o.SubcategoryId == result.SubcategoryId && o.Code == code);

            var facilityTypeId = result.EntityTypeIdsByName[FacilityEntityTypeName];

            if (indicator == null)
            {
                indicator = new Indicator
                {
                    SubcategoryId = result.SubcategoryId,
                    Name = name,
                    Code = code,
                    Units = units,
                    IndicatorType = IndicatorType.Calculated,
                    IndicatorStatus = IndicatorStatus.Enabled,
                    RequiresSubmit = false,
                    RequiresVerify = false,
                    RequiresApprove = false,
                    LowerIsBetter = false,
                    UseIndicatorDates = false,
                    DisableNote = false,
                    EntityTypeId = facilityTypeId,
                    Frequency = DateType.Month,
                    DateAggregationType = AggregationType.Sum,
                    EntityAggregationType = EntityAggregationType.Sum,
                    DataType = DataType.Number,
                    DecimalPlaces = 2,
                    Prefix = "",
                    Suffix = "",
                    SortOrder = sortOrder,
                    ShortName = name,
                    CreatedDateUtc = DateTime.UtcNow,
                    CreatedById = userId,
                    LastSavedDateUtc = DateTime.UtcNow,
                    LastSavedById = userId
                };

                db.Indicators.Add(indicator);
                await db.SaveChangesAsync();

                foreach (var token in tokens)
                {
                    token.IndicatorId = indicator.IndicatorId;
                    db.Tokens.Add(token);
                }

                await db.SaveChangesAsync();
            }

            result.IndicatorIdsByCode[code] = indicator.IndicatorId;
        }
    }

    /// <summary>Internal helpers mapping facilities/districts/regions.</summary>
    internal static class DemoDataHelper
    {
        public static int FacilityDistrictIndex(int facilityIndex) => (facilityIndex - 1) / 2;
        public static int FacilityRegionIndex(int facilityIndex) => FacilityDistrictIndex(facilityIndex) / 2;
        public static int DistrictRegionIndex(int districtIndex) => districtIndex / 2;
    }

    /// <summary>
    /// Single source of truth for the deterministic demo values and hand-computed expectations.
    /// The seeder ENTERS these values; tests RE-COMPUTE them to prove numeric accuracy of the
    /// aggregate & calculation pipeline.
    /// </summary>
    public static class DemoExpected
    {
        public const int FacilityCount = 8;
        public const int MonthCount = 24;
        public const string CountryCode = "CTRY";
        public static readonly string[] RegionCodes = { "NRTH", "SUTH" };
        public static readonly string[] DistrictCodes = { "ALPH", "BETA", "GAMA", "DELT" };

        // ------------------------------------------------------------ calendar helpers

        public static int YearOf(int monthIndex) => monthIndex <= 12 ? 2024 : 2025;

        public static string YearCode(int monthIndex) => YearOf(monthIndex).ToString();

        public static string MonthCode(int monthIndex)
        {
            var year = YearOf(monthIndex);
            var monthInYear = (monthIndex - 1) % 12;
            return $"{year}-M{monthInYear + 1:00}";
        }

        public static string QuarterCode(int monthIndex)
        {
            var year = YearOf(monthIndex);
            var quarterInYear = ((monthIndex - 1) % 12) / 3;
            return $"{year}-Q{quarterInYear + 1}";
        }

        /// <summary>The 3 month indexes belonging to the quarter that contains monthIndex.</summary>
        public static IEnumerable<int> MonthsOfQuarter(int monthIndex)
        {
            var first = monthIndex - ((monthIndex - 1) % 3);
            return new[] { first, first + 1, first + 2 };
        }

        /// <summary>The 12 month indexes of the year that contains monthIndex.</summary>
        public static IEnumerable<int> MonthsOfYear(int monthIndex)
        {
            var first = monthIndex - ((monthIndex - 1) % 12);
            for (var i = 0; i < 12; i++) yield return first + i;
        }

        /// <summary>Most recent month index (by SortOrder) of the year containing monthIndex.</summary>
        public static int LastMonthOfYear(int monthIndex) =>
            MonthsOfYear(monthIndex).Last();

        // ------------------------------------------------------------ hierarchy helpers

        public static IEnumerable<int> AllFacilityIndexes()
        {
            for (var f = 1; f <= FacilityCount; f++) yield return f;
        }

        public static int FacilityDistrictIndex(int facilityIndex) => (facilityIndex - 1) / 2;

        public static int FacilityRegionIndex(int facilityIndex) => FacilityDistrictIndex(facilityIndex) / 2;

        public static IEnumerable<int> FacilitiesOfDistrict(int districtIndex)
        {
            yield return districtIndex * 2 + 1;
            yield return districtIndex * 2 + 2;
        }

        public static IEnumerable<int> FacilitiesOfRegion(int regionIndex)
        {
            return AllFacilityIndexes().Where(f => FacilityRegionIndex(f) == regionIndex);
        }

        public static string DistrictCode(int districtIndex) => DistrictCodes[districtIndex];
        public static string RegionCode(int regionIndex) => RegionCodes[regionIndex];

        // ------------------------------------------------------------ raw values

        public static decimal PatientTreated(int facilityIndex, int monthIndex)
            => 90 + 10 * facilityIndex + 5 * (monthIndex % 3);

        public static decimal PatientRecovered(int facilityIndex, int monthIndex)
            => PatientTreated(facilityIndex, monthIndex) - 20 - ((facilityIndex + monthIndex) % 25);

        /// <summary>Null at facility 8 / month 2025-M12 (index 24) to exercise null handling.</summary>
        public static decimal? HealthKits(int facilityIndex, int monthIndex)
            => (facilityIndex == 8 && monthIndex == 24) ? (decimal?)null : 5 * facilityIndex + 3 * (monthIndex % 6);

        public static decimal StaffDeployed(int facilityIndex, int monthIndex)
            => 5 + (facilityIndex % 4) + (monthIndex % 2);

        public static decimal BedDays(int facilityIndex, int monthIndex)
            => 40 + facilityIndex + 2 * (monthIndex % 10);

        public static decimal? CollectedRaw(string code, int facilityIndex, int monthIndex)
        {
            switch (code)
            {
                case "PAT": return PatientTreated(facilityIndex, monthIndex);
                case "REC": return PatientRecovered(facilityIndex, monthIndex);
                case "KIT": return HealthKits(facilityIndex, monthIndex);
                case "STF": return StaffDeployed(facilityIndex, monthIndex);
                case "BED": return BedDays(facilityIndex, monthIndex);
                default: throw new ArgumentException($"Unknown collected indicator code {code}");
            }
        }

        // ------------------------------------------------------------ aggregated collected values

        private static decimal? SumOrNull(List<decimal?> values)
            => values.All(v => v == null) ? (decimal?)null : values.Where(v => v != null).Sum();

        private static decimal? AvgOrNull(List<decimal?> values)
            => values.All(v => v == null) ? (decimal?)null
                : Math.Round(values.Where(v => v != null).Sum(v => v.Value) / values.Count(v => v != null), 8);

        /// <summary>
        /// Value of a COLLECTED indicator for one facility AT a given date level.
        /// dateLevel: 2 = month (raw), 1 = quarter, 0 = year.
        /// Month->Quarter/Year roll-up: SUM, or MOSTRECENT for STF.
        /// </summary>
        public static decimal? CollectedFacilityAtDate(string code, int facilityIndex, int dateLevel, int monthIndex)
        {
            var raw = CollectedRaw(code, facilityIndex, monthIndex);

            switch (dateLevel)
            {
                case 2: // month
                    return raw;

                case 1: // quarter
                {
                    var months = MonthsOfQuarter(monthIndex).ToList();
                    if (code == "STF")
                        return CollectedRaw(code, facilityIndex, months.Max());
                    return SumOrNull(months.Select(mm => CollectedRaw(code, facilityIndex, mm)).ToList());
                }

                case 0: // year
                {
                    var months = MonthsOfYear(monthIndex).ToList();
                    if (code == "STF")
                        return CollectedRaw(code, facilityIndex, months.Max());
                    return SumOrNull(months.Select(mm => CollectedRaw(code, facilityIndex, mm)).ToList());
                }

                default: throw new ArgumentException("dateLevel must be 0, 1 or 2");
            }
        }

        /// <summary>
        /// Value of a COLLECTED indicator AT an entity that is a direct ancestor of the
        /// facilities, at a date level. Members are the facilities directly linked to that entity.
        ///.entityAggregation: 0 = SUM, 1 = AVERAGE (SQL semantics: nulls ignored).
        /// </summary>
        public static decimal? CollectedEntityAtDate(
            string code,
            IEnumerable<int> memberFacilities,
            bool average,
            int dateLevel,
            int monthIndex)
        {
            var values = memberFacilities
                .Select(f => CollectedFacilityAtDate(code, f, dateLevel, monthIndex))
                .ToList();

            return average ? AvgOrNull(values) : SumOrNull(values);
        }

        /// <summary>Expected annual total (sum across all facilities &amp; 12 months), used for the seeded target/baseline.</summary>
        public static decimal TotalAcrossFacilitiesAndMonths(string code, IEnumerable<int> facilityIndexes, int fromMonth, int toMonth)
        {
            decimal total = 0;
            for (var f = 1; f <= FacilityCount; f++)
            {
                if (facilityIndexes != null && !facilityIndexes.Contains(f)) continue;
                for (var m = fromMonth; m <= toMonth; m++)
                    total += CollectedRaw(code, f, m) ?? 0;
            }
            return total;
        }

        // ------------------------------------------------------------ calculated values

        /// <summary>
        /// Expected value of a CALCULATED indicator at an entity key. Calculated indicators are
        /// re-computed from the source indicators' values at the SAME entity &amp; date, so their
        /// value must be derived from the expected values of the other indicators at that key.
        /// </summary>
        public static decimal? CalculatedAtKey(
            string code,
            IEnumerable<int> memberFacilities,
            bool average,
            int dateLevel,
            int monthIndex)
        {
            switch (code)
            {
                case "TOT":
                {
                    var pat = CollectedEntityAtDate("PAT", memberFacilities, average, dateLevel, monthIndex);
                    var kit = CollectedEntityAtDate("KIT", memberFacilities, average, dateLevel, monthIndex);
                    // formula: PAT * 2 + KIT   (SQL-level null handling: any null operand -> null)
                    if (pat == null || kit == null) return null;
                    return pat * 2 + kit;
                }

                case "CPS":
                {
                    var tot = CalculatedAtKey("TOT", memberFacilities, average, dateLevel, monthIndex);
                    var stf = CollectedEntityAtDate("STF", memberFacilities, average, dateLevel, monthIndex);
                    if (tot == null || stf == null || stf == 0) return null;
                    return tot / stf;
                }

                case "RATE":
                {
                    var rec = CollectedEntityAtDate("REC", memberFacilities, average, dateLevel, monthIndex) ?? 0;
                    var pat = CollectedEntityAtDate("PAT", memberFacilities, average, dateLevel, monthIndex) ?? 0;
                    // formula: (REC / PAT) * 100  with ConvertNullToZero = true on both tokens
                    if (pat == 0) return null; // divide by zero -> null
                    return rec / pat * 100;
                }

                default:
                    throw new ArgumentException($"Unknown calculated indicator code {code}");
            }
        }
    }
}
