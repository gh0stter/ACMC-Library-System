namespace ACMC_Library_System.DbModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("issue")]
    public partial class issue
    {
        public int id { get; set; }

        public int? patronid { get; set; }

        public int itemid { get; set; }

        public virtual item item { get; set; }

        public virtual patron patron { get; set; }
    }
}
