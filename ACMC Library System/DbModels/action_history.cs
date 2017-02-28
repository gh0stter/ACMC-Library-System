using System.ComponentModel.DataAnnotations.Schema;

namespace ACMC_Library_System.DbModels
{
    using System;

    public partial class action_history
    {
        public int id { get; set; }

        public int? patronid { get; set; }

        public int itemid { get; set; }

        public DateTime action_datetime { get; set; }

        public int action_type { get; set; }

        public virtual action_type action_type1 { get; set; }

        #region Extended Property

        [NotMapped]
        public string UserName { get; set; }

        [NotMapped]
        public string ItemName { get; set; }

        [NotMapped]
        public string ActionType { get; set; }

        #endregion
    }
}
