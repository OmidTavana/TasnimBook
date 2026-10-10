namespace TasnimBook.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("questions")]
    public partial class questions
    {
        public questions()
        {
        }

        [Column("id")]

        public int Id { get; set; }

        [Column("client_id")]

        public string ClientId { get; set; }

        [Column("question")]

        public string Question { get; set; }
        [DefaultValue("not_yet")]
        [Column("accepted")]
        public string Accepted { get; set; }


        [Column("date")]


        public DateTime? Date { get; set; }

     
        public ICollection<ManyToManyBookAndBookCatcs> BookCategories { get; set; }

    }
}
