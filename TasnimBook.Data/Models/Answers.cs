namespace TasnimBook.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("answers")]
    public partial class Answers
    {
        public Answers()
        {
        }

        [Column("id")]

        public int Id { get; set; }
        [Column("user_id")]
        public string UserId { get; set; }

        [Column("title")]

        public string Title { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("score")]
        public int Score { get; set; }
        [DefaultValue("not_yet")]
        [Column("accepted")]
        public string Accepted { get; set; }
        [Column("date")]
        public DateTime? Date { get; set; }

    }
}
