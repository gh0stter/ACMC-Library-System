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
            In,
            Out,
            Lost,
            OnOrder,
            ForSale
        }

        #endregion
    }
}
