namespace ACMC_Library_System.DbModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class item_status
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }

        [Required]
        [StringLength(100)]
        public string status_text { get; set; }

        #region Extend property

        public enum StatusEnum
        {
            In = 1,
            Out = 2,
            Lost = 3,
            OnOrder = 4,
            ForSale = 5
        }

        #endregion
    }
}
