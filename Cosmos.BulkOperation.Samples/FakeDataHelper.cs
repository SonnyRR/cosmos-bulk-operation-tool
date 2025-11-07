using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cosmos.BulkOperation.Samples
{
    /// <summary>
    /// A utility for generating fake testing data.
    /// </summary>
    public static class FakeDataHelper
    {
        private static readonly Faker FAKER = new();
        private static readonly IEnumerable<string> EMAIL_LIST = Enumerable.Range(1, 1500)
            .Select(_ => FAKER.Internet.Email().ToLower())
            .ToArray();

        /// <summary>
        /// A user specific email address, used for identifying which records will be bulk operated on.
        /// </summary>
        /// <remarks>Used only for testing &amp; sample purposes only.</remarks>
        public const string SAMPLE_USER_ID = "carmella@sopranos.com";

        /// <summary>
        /// Generates dummy runs data for testing purposes.
        /// </summary>
        /// <param name="baseRecords">The number of base records to generate.</param>
        /// <param name="userSpecificRecords">The number of user specific records to generate.</param>
        /// <returns>An array of randomly generated runs.</returns>
        public static Run[] GenerateDummyRuns(int baseRecords = 500, int userSpecificRecords = 250)
        {
            var checkpointFaker = new Faker<Checkpoint>()
                .RuleFor(c => c.Latitude, f => f.Address.Latitude())
                .RuleFor(c => c.Longitude, f => f.Address.Longitude())
                .RuleFor(c => c.PinColor, f => f.Random.Enum<Color>());

            var randomRunFaker = new Faker<Run>()
                .StrictMode(false)
                .Rules((f, r) =>
                {
                    r.Id = f.Random.Uuid();
                    r.UserId = f.PickRandom(EMAIL_LIST);
                    r.StartedAt = f.Date.Between(new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateTime.Now);
                    r.Duration = f.Date.Timespan(TimeSpan.FromHours(6));
                    r.Checkpoints = checkpointFaker.GenerateBetween(1, Enum.GetValues<Color>().Length);
                });

            return [.. randomRunFaker.Generate(baseRecords), .. randomRunFaker.RuleFor(r => r.UserId, SAMPLE_USER_ID).Generate(userSpecificRecords)];
        }
    }
}