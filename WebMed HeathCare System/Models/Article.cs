using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Article
{
    public int ArticleId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Category { get; set; } = null!;

    public int AuthorId { get; set; }

    public bool IsPublished { get; set; }

    public bool IsActive { get; set; }

    public DateTime PublishedAt { get; set; }

    public virtual User Author { get; set; } = null!;
}
