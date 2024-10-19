using System;
using System.Collections.Generic;

namespace Cosmos.BulkOperation.Samples
{
    public class Run
    {
        public Guid Id { get; set; }

        public string UserId { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTime StartedAt { get; set; }

        public IEnumerable<Checkpoint> Checkpoints { get; set; }
    }
}