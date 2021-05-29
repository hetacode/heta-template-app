using System;
using System.Collections.Generic;

#nullable disable

namespace RepositoryProcessorFunc.Models
{
    public partial class Commit
    {
        public string RepoHash { get; set; }
        public string CommitHash { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
