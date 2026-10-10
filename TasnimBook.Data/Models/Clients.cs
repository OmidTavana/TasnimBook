namespace TasnimBook.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("clients")]
    public partial class Clients
    {
        public Clients()
        {
        }

        [Column("id")]

        public int Id { get; set; }
        [DefaultValue(false)]
        [Column("banStatus")]
        public bool BanStatus { get; set; }
        [Column("date")]
        public DateTime? Date { get; set; }

    }
}
