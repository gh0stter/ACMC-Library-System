using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACMC_Library_System.DbModels;

namespace ACMC_Library_System.Supports
{
    internal static class Cache
    {
        public static List<item_category> ItemCategories { get; set; }
        public static List<item_class> ItemClasses { get; set; }
        public static List<item_status> ItemStatuses { get; set; }
        public static List<patron> Members { get; set; }
        public static List<item> Items { get; set; }
        public static List<action_history> ActionHistories { get; set; }
        public static List<item> ItemsShouldReturn { get; set; }

        /// <summary>
        /// Refresh all cache data from database
        /// </summary>
        /// <returns></returns>
        public static async Task RefreshAllCache()
        {
            await Task.Run(() =>
            {
                using (var context = new LibraryDb())
                {
                    ItemCategories = context.item_category.ToList();
                    ItemClasses = context.item_class.ToList();
                    ItemStatuses = context.item_status.ToList();
                    Members = context.patron.ToList();
                    Items = context.item.ToList();
                    ActionHistories = context.action_history.OrderByDescending(i => i.id).Take(20).ToList();
                    foreach (var action in ActionHistories)
                    {
                        action.MemberName = Members.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                        action.ItemName = Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                        action.ActionType = action.action_type1.verb;
                    }
                    ItemsShouldReturn = Items.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).ToList();
                    foreach (var item in ItemsShouldReturn)
                    {
                        item.Borrower = Members.FirstOrDefault(i => i.id == item.patronid);
                    }
                }
            });
        }

        /// <summary>
        /// Refresh Main (member/item/history/itemsShouldReturn) cache from database
        /// </summary>
        /// <returns></returns>
        public static async Task RefreshMainCache()
        {
            await Task.Run(() =>
            {
                using (var context = new LibraryDb())
                {
                    Members = context.patron.ToList();
                    Items = context.item.ToList();
                    ActionHistories = context.action_history.OrderByDescending(i => i.id).Take(20).ToList();
                    foreach (var action in ActionHistories)
                    {
                        action.MemberName = Members.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                        action.ItemName = Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                        action.ActionType = action.action_type1.verb;
                    }
                    ItemsShouldReturn = Items.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).ToList();
                    foreach (var item in ItemsShouldReturn)
                    {
                        item.Borrower = Members.FirstOrDefault(i => i.id == item.patronid);
                    }
                }
            });
        }
    }
}
